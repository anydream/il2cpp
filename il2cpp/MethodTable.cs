using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 类型方法编组
	using TypeMethodPair = Tuple<MethodTable, MethodDef>;

	// 虚槽数据
	internal class VirtualSlot
	{
		public readonly HashSet<TypeMethodPair> Entries = new HashSet<TypeMethodPair>();
		public readonly TypeMethodPair NewSlotEntry;
		public TypeMethodPair Implemented;

		public VirtualSlot(VirtualSlot other, TypeMethodPair newEntry)
		{
			Entries.UnionWith(other.Entries);
			NewSlotEntry = other.NewSlotEntry;
			Implemented = other.Implemented;

			if (NewSlotEntry == null)
				NewSlotEntry = newEntry;
		}

		public VirtualSlot(TypeMethodPair newEntry)
		{
			NewSlotEntry = newEntry;
		}
	}

	// 冲突解决编组
	internal class ConflictPair
	{
		// 重用槽位的方法
		public readonly List<MethodDef> ReuseSlots = new List<MethodDef>();
		// 新建槽位的方法
		public readonly List<MethodDef> NewSlots = new List<MethodDef>();
	}

	// 方法表
	internal class MethodTable
	{
		public readonly Il2cppContext Context;
		public readonly TypeDef Def;

		private string NameKey;
		private IList<TypeSig> InstGenArgs;

		// 槽位映射
		public readonly Dictionary<string, VirtualSlot> SlotMap = new Dictionary<string, VirtualSlot>();
		// 入口实现映射
		public readonly Dictionary<TypeMethodPair, TypeMethodPair> EntryMap = new Dictionary<TypeMethodPair, TypeMethodPair>();
		// 方法替换映射
		public readonly Dictionary<MethodDef, TypeMethodPair> ReplaceMap = new Dictionary<MethodDef, TypeMethodPair>();
		// 同类内的方法替换映射
		public readonly Dictionary<MethodDef, TypeMethodPair> SameTypeReplaceMap = new Dictionary<MethodDef, TypeMethodPair>();

		public MethodTable(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
			Def = tyDef;
		}

		public override string ToString()
		{
			return NameKey;
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				StringBuilder sb = new StringBuilder();
				Helper.TypeNameKey(sb, Def, InstGenArgs);
				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public string GetExpandedNameKey(IList<TypeSig> genArgs)
		{
			Debug.Assert(genArgs.IsCollectionValid());
			Debug.Assert(!InstGenArgs.IsCollectionValid());
			Debug.Assert(Def.GenericParameters.Count == genArgs.Count);

			StringBuilder sb = new StringBuilder();
			Helper.TypeNameKey(sb, Def, genArgs);
			return sb.ToString();
		}

		public MethodTable ExpandTable(IList<TypeSig> genArgs)
		{
			Debug.Assert(genArgs.IsCollectionValid());
			Debug.Assert(!InstGenArgs.IsCollectionValid());
			Debug.Assert(Def.GenericParameters.Count == genArgs.Count);

			MethodTable expMetTable = new MethodTable(Context, Def);
			expMetTable.InstGenArgs = genArgs;
			expMetTable.GetNameKey();

			IGenericReplacer replacer = new TypeDefGenReplacer(Def, genArgs);
			StringBuilder sb = new StringBuilder();

			foreach (var kv in SlotMap)
			{
				if (kv.Value.Entries.Count == 0)
					continue;

				VirtualSlot vslot = ExpandVirtualSlot(kv.Value, expMetTable, replacer);

				Helper.MethodDefNameKey(sb, vslot.Entries.First().Item2, replacer);
				string metNameKey = sb.ToString();
				sb.Clear();

				expMetTable.SlotMap[metNameKey] = vslot;
			}

			foreach (var kv in EntryMap)
			{
				expMetTable.EntryMap[ExpandMethodPair(kv.Key, expMetTable, replacer)] =
					ExpandMethodPair(kv.Value, expMetTable, replacer);
			}

			foreach (var kv in ReplaceMap)
			{
				expMetTable.ReplaceMap[kv.Key] = ExpandMethodPair(kv.Value, expMetTable, replacer);
			}

			foreach (var kv in SameTypeReplaceMap)
			{
				expMetTable.SameTypeReplaceMap[kv.Key] = ExpandMethodPair(kv.Value, expMetTable, replacer);
			}

			return expMetTable;
		}

		private TypeMethodPair ExpandMethodPair(TypeMethodPair metPair, MethodTable expMetTable, IGenericReplacer replacer)
		{
			if (metPair == null)
				return null;

			MethodTable mtable = metPair.Item1;
			if (mtable == this)
				return new TypeMethodPair(expMetTable, metPair.Item2);

			if (mtable.InstGenArgs.IsCollectionValid())
			{
				var genArgs = Helper.ReplaceGenericSigList(mtable.InstGenArgs, replacer);
				mtable = Context.TypeMgr.ResolveMethodTableSpec(mtable.Def, genArgs);

				Debug.Assert(mtable != this);
				return new TypeMethodPair(mtable, metPair.Item2);
			}

			return metPair;
		}

		private VirtualSlot ExpandVirtualSlot(VirtualSlot vslot, MethodTable expMetTable, IGenericReplacer replacer)
		{
			VirtualSlot expVSlot = new VirtualSlot(ExpandMethodPair(vslot.NewSlotEntry, expMetTable, replacer));

			foreach (var entry in vslot.Entries)
				expVSlot.Entries.Add(ExpandMethodPair(entry, expMetTable, replacer));

			expVSlot.Implemented = ExpandMethodPair(vslot.Implemented, expMetTable, replacer);

			return expVSlot;
		}

		public void ResolveTable()
		{
			var metDefList = new List<Tuple<string, MethodDef>>();
			var conflictMap = new Dictionary<string, ConflictPair>();
			var nameSet = new HashSet<string>();

			StringBuilder sb = new StringBuilder();

			uint lastRid = 0;
			foreach (MethodDef metDef in Def.Methods)
			{
				// 跳过非虚方法
				if (!metDef.IsVirtual)
				{
					// 非虚方法如果存在显式重写则视为错误
					if (metDef.HasOverrides)
					{
						throw new TypeLoadException(
							string.Format("Explicit overridden method must be virtual: {0}",
								metDef.FullName));
					}

					continue;
				}

				Debug.Assert(lastRid == 0 || lastRid < metDef.Rid);
				lastRid = metDef.Rid;

				// 获得方法签名
				Helper.MethodDefNameKey(sb, metDef, null);
				string metNameKey = sb.ToString();
				sb.Clear();

				// 特殊处理签名冲突的方法
				if (nameSet.Contains(metNameKey))
				{
					var pair = conflictMap.GetOrCreate(metNameKey, () => new ConflictPair());
					if (metDef.IsNewSlot)
						pair.NewSlots.Add(metDef);
					else
						pair.ReuseSlots.Add(metDef);
				}
				else
					metDefList.Add(new Tuple<string, MethodDef>(metNameKey, metDef));

				nameSet.Add(metNameKey);
			}
			nameSet = null;

			// 解析基类方法表
			MethodTable baseTable = null;
			if (Def.BaseType != null)
			{
				baseTable = Context.TypeMgr.ResolveMethodTable(Def.BaseType);

				// 继承基类数据
				DerivedTable(baseTable);
			}

			var expOverrides = new List<Tuple<string, MethodDef>>();
			// 解析隐式重写
			foreach (var metItem in metDefList)
			{
				string metNameKey = metItem.Item1;

				if (conflictMap.TryGetValue(metNameKey, out var confPair))
				{
					// 冲突签名的方法需要先处理重写槽方法, 再处理新建槽方法
					VirtualSlot lastVSlot = null;
					foreach (var metDef in confPair.ReuseSlots)
						lastVSlot = ProcessMethod(metNameKey, metDef, baseTable, expOverrides);

					// 应用重写信息到入口映射
					ApplyVirtualSlot(lastVSlot);

					foreach (var metDef in confPair.NewSlots)
						ProcessMethod(metNameKey, metDef, baseTable, expOverrides);
				}
				else
				{
					MethodDef metDef = metItem.Item2;
					ProcessMethod(metNameKey, metDef, baseTable, expOverrides);
				}
			}
			metDefList = null;
			conflictMap = null;
			baseTable = null;

			// 关联接口
			if (Def.HasInterfaces)
			{
				foreach (var inf in Def.Interfaces)
				{
					MethodTable infTable = Context.TypeMgr.ResolveMethodTable(inf.Interface);
					foreach (var kv in infTable.SlotMap)
					{
						string metNameKey = kv.Key;
						var infEntries = kv.Value.Entries;
						if (SlotMap.TryGetValue(metNameKey, out var vslot))
						{
							vslot.Entries.UnionWith(infEntries);
						}
						else
						{
							// 当前类没有对应接口的签名, 可能存在显式重写, 或者为抽象类
							vslot = new VirtualSlot(null);
							SlotMap[metNameKey] = vslot;
							vslot.Entries.UnionWith(infEntries);
						}
					}
				}
			}

			// 记录显式重写目标以便查重
			var expOverTargets = new HashSet<TypeMethodPair>();

			// 解析显式重写
			foreach (var expItem in expOverrides)
			{
				string metNameKey = expItem.Item1;
				MethodDef metDef = expItem.Item2;

				foreach (MethodOverride metOver in metDef.Overrides)
				{
					var overTarget = metOver.MethodDeclaration;
					var overImpl = metOver.MethodBody;

					MethodTable targetTable = Context.TypeMgr.ResolveMethodTable(overTarget.DeclaringType);
					MethodDef targetDef = overTarget.ResolveMethodDef();

					// 验证显式重写目标的正确性
					if (targetDef == null || targetDef.DeclaringType != targetTable.Def)
					{
						throw new TypeLoadException(
							string.Format("Illegal explicit overriding target: {0}",
								overTarget.FullName));
					}

					var targetEntry = new TypeMethodPair(targetTable, targetDef);

					MethodDef implDef = overImpl.ResolveMethodDef();
					Debug.Assert(metDef == implDef);

					// 同一个类内重复的显式重写, 以及重写存在重写的方法, 视为错误
					if ((targetTable == this && targetDef.HasOverrides) ||
						expOverTargets.Contains(targetEntry))
					{
						throw new TypeLoadException(
							string.Format("Explicit overriding target has been overridden: {0}",
								overTarget.FullName));
					}
					expOverTargets.Add(targetEntry);

					if (targetTable.Def.IsInterface)
					{
						// 接口方法显式重写
						RemoveSlotEntry(targetEntry);
						SlotMap[metNameKey].Entries.Add(targetEntry);
					}
					else
					{
						// 类方法显式重写
						var impl = new TypeMethodPair(this, implDef);

						// 相同类型的需要单独添加, 以便非虚调用时处理替换
						if (targetTable == this)
							SameTypeReplaceMap[targetDef] = impl;

						// 如果当前方法存在新建槽位的那个方法, 则显式重写记录使用新建槽方法
						var newSlotEntry = SlotMap[metNameKey].NewSlotEntry;
						if (newSlotEntry != null)
							ReplaceMap[targetDef] = newSlotEntry;
						else
							ReplaceMap[targetDef] = impl;
					}
				}
			}
			expOverTargets = null;

			// 展开入口映射
			foreach (var kv in SlotMap)
			{
				TypeMethodPair impl = kv.Value.Implemented;
				var entries = kv.Value.Entries;

				if (impl == null || impl.Item2.IsAbstract)
				{
					// 对于非抽象类需要检查是否存在实现
					if (!Def.IsInterface && !Def.IsAbstract)
					{
						throw new TypeLoadException(
							string.Format("Interface/abstract methods not implemented in type {0}: {1}",
								Def.FullName,
								entries.First().Item2.FullName));
					}
				}

				foreach (TypeMethodPair entry in entries)
				{
					EntryMap[entry] = impl;
				}
			}
		}

		private void DerivedTable(MethodTable baseTable)
		{
			foreach (var kv in baseTable.SlotMap)
				SlotMap.Add(kv.Key, new VirtualSlot(kv.Value, null));

			foreach (var kv in baseTable.EntryMap)
				EntryMap.Add(kv.Key, kv.Value);

			foreach (var kv in baseTable.ReplaceMap)
				ReplaceMap.Add(kv.Key, kv.Value);

			foreach (var kv in baseTable.SameTypeReplaceMap)
				SameTypeReplaceMap.Add(kv.Key, kv.Value);
		}

		private VirtualSlot ProcessMethod(
			string metNameKey,
			MethodDef metDef,
			MethodTable baseTable,
			List<Tuple<string, MethodDef>> expOverrides)
		{
			Debug.Assert(metDef.IsVirtual);

			// 记录显式重写方法
			if (metDef.HasOverrides)
				expOverrides.Add(new Tuple<string, MethodDef>(metNameKey, metDef));

			var entry = new TypeMethodPair(this, metDef);
			var impl = Def.IsInterface ? null : entry;

			VirtualSlot vslot;
			if (metDef.IsReuseSlot)
			{
				// 对于重写槽方法, 如果不存在可重写的槽则转换为新建槽方法
				if (baseTable == null)
					metDef.IsNewSlot = true;
				else if (!baseTable.SlotMap.TryGetValue(metNameKey, out vslot))
					metDef.IsNewSlot = true;
				else
				{
					vslot = new VirtualSlot(vslot, impl);
					vslot.Entries.Add(entry);
					vslot.Implemented = impl;
					SlotMap[metNameKey] = vslot;
					return vslot;
				}
			}

			Debug.Assert(metDef.IsNewSlot);
			vslot = new VirtualSlot(impl);
			vslot.Entries.Add(entry);
			vslot.Implemented = impl;
			SlotMap[metNameKey] = vslot;
			return vslot;
		}

		private void ApplyVirtualSlot(VirtualSlot vslot)
		{
			foreach (TypeMethodPair entry in vslot.Entries)
			{
				EntryMap[entry] = vslot.Implemented;
			}
		}

		private void RemoveSlotEntry(TypeMethodPair entry)
		{
			foreach (var kv in SlotMap)
			{
				kv.Value.Entries.Remove(entry);
			}
		}
	}
}
