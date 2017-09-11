using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 方法表与方法编组
	using TableMethodPair = Tuple<MethodTable, MethodDef>;
	// 类型与方法编组
	using TypeMethodPair = Tuple<TypeX, MethodDef>;

	// 虚槽数据
	internal class VirtualSlot
	{
		public readonly HashSet<TableMethodPair> Entries = new HashSet<TableMethodPair>();
		public readonly TableMethodPair NewSlotEntry;
		public TableMethodPair Implemented;

		public VirtualSlot(VirtualSlot other, TableMethodPair newEntry)
		{
			Entries.UnionWith(other.Entries);
			NewSlotEntry = other.NewSlotEntry;
			Implemented = other.Implemented;

			if (NewSlotEntry == null)
				NewSlotEntry = newEntry;
		}

		public VirtualSlot(TableMethodPair newEntry)
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

		public bool ContainsConflicts()
		{
			return ReuseSlots.Count + NewSlots.Count > 1;
		}
	}

	// 展开的虚表
	internal class VirtualTable
	{
		private readonly string Name;
		// 入口实现映射
		private readonly Dictionary<TypeMethodPair, TypeMethodPair> EntryMap =
			new Dictionary<TypeMethodPair, TypeMethodPair>();
		// 同类内的方法替换映射
		private readonly Dictionary<MethodDef, TypeMethodPair> SameTypeReplaceMap =
			new Dictionary<MethodDef, TypeMethodPair>();
		// 方法新建槽映射
		private readonly Dictionary<MethodDef, TypeMethodPair> NewSlotEntryMap =
			new Dictionary<MethodDef, TypeMethodPair>();

		// 虚方法关联缓存
		private readonly Dictionary<TypeMethodPair, TypeMethodPair> CachedMap =
			new Dictionary<TypeMethodPair, TypeMethodPair>();

		public VirtualTable(MethodTable mtable)
		{
			Name = mtable.GetNameKey();

			foreach (var kv in mtable.EntryMap)
			{
				EntryMap.Add(
					new TypeMethodPair(kv.Key.Item1.GetTypeX(), kv.Key.Item2),
					new TypeMethodPair(kv.Value.Item1.GetTypeX(), kv.Value.Item2));
			}

			foreach (var kv in mtable.SameTypeReplaceMap)
			{
				SameTypeReplaceMap.Add(
					kv.Key,
					new TypeMethodPair(kv.Value.Item1.GetTypeX(), kv.Value.Item2));
			}

			foreach (var kv in mtable.NewSlotEntryMap)
			{
				NewSlotEntryMap.Add(
					kv.Key,
					new TypeMethodPair(kv.Value.Item1.GetTypeX(), kv.Value.Item2));
			}
		}

		public bool QueryCallReplace(
			MethodDef entryDef,
			out TypeX implTyX,
			out MethodDef implDef)
		{
			if (SameTypeReplaceMap.TryGetValue(entryDef, out var implPair))
			{
				implTyX = implPair.Item1;
				implDef = implPair.Item2;
				return true;
			}
			implTyX = null;
			implDef = null;
			return false;
		}

		public void QueryCallVirt(
			TypeX entryTyX,
			MethodDef entryDef,
			out TypeX implTyX,
			out MethodDef implDef)
		{
			var entryPair = new TypeMethodPair(entryTyX, entryDef);
			if (!CachedMap.TryGetValue(entryPair, out var implPair))
			{
				QueryCallVirtImpl(entryPair, out implPair);
				CachedMap[entryPair] = implPair;
			}
			implTyX = implPair.Item1;
			implDef = implPair.Item2;
		}

		private void QueryCallVirtImpl(
			TypeMethodPair entryPair,
			out TypeMethodPair implPair)
		{
			for (; ; )
			{
				// 尝试重定向入口到新建槽方法
				if (NewSlotEntryMap.TryGetValue(entryPair.Item2, out var newSlotPair))
					entryPair = newSlotPair;

				if (!EntryMap.TryGetValue(
					entryPair,
					out implPair))
				{
					throw new TypeLoadException(
						string.Format("Virtual method can't resolve in type {0}: {1} -> {2}",
							Name,
							entryPair.Item1,
							entryPair.Item2.FullName));
				}

				if (NewSlotEntryMap.TryGetValue(implPair.Item2, out newSlotPair) &&
					!entryPair.Equals(newSlotPair))
				{
					entryPair = newSlotPair;
					continue;
				}

				if (SameTypeReplaceMap.TryGetValue(implPair.Item2, out var repPair))
				{
					entryPair = repPair;
				}
				else
					break;
			}
		}
	}

	// 方法表
	internal class MethodTable
	{
		public readonly Il2cppContext Context;
		public readonly TypeDef Def;

		private string NameKey;
		private IList<TypeSig> InstGenArgs;

		// 槽位映射
		private readonly Dictionary<string, VirtualSlot> SlotMap = new Dictionary<string, VirtualSlot>();
		// 入口实现映射
		public Dictionary<TableMethodPair, TableMethodPair> EntryMap = new Dictionary<TableMethodPair, TableMethodPair>();
		// 同类内的方法替换映射
		public readonly Dictionary<MethodDef, TableMethodPair> SameTypeReplaceMap = new Dictionary<MethodDef, TableMethodPair>();
		// 方法新建槽映射
		public readonly Dictionary<MethodDef, TableMethodPair> NewSlotEntryMap = new Dictionary<MethodDef, TableMethodPair>();

		// 未实现的接口映射
		private Dictionary<string, HashSet<TableMethodPair>> NotImplInterfaces;

		public MethodTable(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
			Def = tyDef;
		}

		public override string ToString()
		{
			return NameKey;
		}

		public TypeX GetTypeX()
		{
			TypeX tyX = Context.TypeMgr.GetTypeByName(GetNameKey());
			Debug.Assert(tyX != null);
			return tyX;
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

		private string GetExpandedMethodNameKey(StringBuilder sb, MethodDef metDef)
		{
			IGenericReplacer replacer = new TypeDefGenReplacer(Def, InstGenArgs);
			Helper.MethodDefNameKey(sb, metDef, replacer);
			string metNameKey = sb.ToString();
			sb.Clear();
			return metNameKey;
		}

		public MethodTable ExpandTable(IList<TypeSig> genArgs)
		{
			Debug.Assert(genArgs.IsCollectionValid());
			Debug.Assert(!InstGenArgs.IsCollectionValid());
			Debug.Assert(Def.GenericParameters.Count == genArgs.Count);

			bool isInterface = Def.IsInterface;

			MethodTable expMetTable = new MethodTable(Context, Def);
			expMetTable.InstGenArgs = genArgs;
			expMetTable.GetNameKey();

			IGenericReplacer replacer = new TypeDefGenReplacer(Def, genArgs);
			StringBuilder sb = new StringBuilder();

			foreach (var kv in SlotMap)
			{
				Debug.Assert(kv.Value.Entries.Count != 0);

				VirtualSlot vslot = ExpandVirtualSlot(kv.Value, expMetTable, replacer);

				TableMethodPair metPair = vslot.Entries.First();
				string metNameKey = metPair.Item1.GetExpandedMethodNameKey(sb, metPair.Item2);

				// 接口合并相同签名的方法
				if (isInterface)
					expMetTable.MergeInterfaces(metNameKey, vslot.Entries);
				else
					expMetTable.SlotMap[metNameKey] = vslot;
			}

			var expEntryMap = expMetTable.EntryMap;
			foreach (var kv in EntryMap)
			{
				var key = ExpandMethodPair(kv.Key, expMetTable, replacer);
				var value = ExpandMethodPair(kv.Value, expMetTable, replacer);

				MergeExpandedEntry(expEntryMap, key, value);
			}

			foreach (var kv in SameTypeReplaceMap)
				expMetTable.SameTypeReplaceMap[kv.Key] = ExpandMethodPair(kv.Value, expMetTable, replacer);

			foreach (var kv in NewSlotEntryMap)
				expMetTable.NewSlotEntryMap[kv.Key] = ExpandMethodPair(kv.Value, expMetTable, replacer);

			return expMetTable;
		}

		private void MergeExpandedEntry(
			Dictionary<TableMethodPair, TableMethodPair> expEntryMap,
			TableMethodPair key,
			TableMethodPair value)
		{
			if (expEntryMap.TryGetValue(key, out var oval))
			{
				// 不覆盖同一个类内的相同签名
				if (oval.Item1.Equals(value.Item1))
					return;

				Debug.Assert(oval.Item2 != value.Item2);
				// 对于不同的类则子类方法优先
				if (IsBaseType(oval.Item2.DeclaringType, value.Item2.DeclaringType))
					return;
			}
			expEntryMap[key] = value;
		}

		private static bool IsBaseType(TypeDef currType, TypeDef baseType)
		{
			TypeDef currBase = currType.BaseType?.ResolveTypeDef();
			if (currBase != null)
			{
				if (currBase == baseType)
					return true;
				return IsBaseType(currBase, baseType);
			}
			return false;
		}

		private TableMethodPair ExpandMethodPair(TableMethodPair metPair, MethodTable expMetTable, IGenericReplacer replacer)
		{
			if (metPair == null)
				return null;

			MethodTable mtable = metPair.Item1;
			if (mtable == this)
				return new TableMethodPair(expMetTable, metPair.Item2);

			if (mtable.InstGenArgs.IsCollectionValid())
			{
				var genArgs = Helper.ReplaceGenericSigList(mtable.InstGenArgs, replacer);
				mtable = Context.TypeMgr.ResolveMethodTableSpec(mtable.Def, genArgs);

				Debug.Assert(mtable != this);
				return new TableMethodPair(mtable, metPair.Item2);
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
			Debug.Assert(InstGenArgs == null);

			var metDefList = new List<Tuple<string, MethodDef>>();
			var conflictMap = new Dictionary<string, ConflictPair>();
			var nameSet = new HashSet<string>();

			bool thisIsInterface = Def.IsInterface;
			bool thisIsAbstract = Def.IsAbstract;

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
							string.Format("Explicit overridden method must be virtual in type {0}: {1}",
								GetNameKey(),
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
				nameSet.Add(metNameKey);

				if (thisIsInterface)
				{
					Debug.Assert(metDef.IsAbstract && metDef.IsNewSlot);
					metDefList.Add(new Tuple<string, MethodDef>(metNameKey, metDef));
				}
				else
				{
					// 特殊处理签名冲突的方法
					if (!conflictMap.TryGetValue(metNameKey, out var confPair))
					{
						confPair = new ConflictPair();
						conflictMap.Add(metNameKey, confPair);
						metDefList.Add(new Tuple<string, MethodDef>(metNameKey, metDef));
					}

					if (metDef.IsNewSlot)
						confPair.NewSlots.Add(metDef);
					else
						confPair.ReuseSlots.Add(metDef);
				}
			}

			if (!thisIsInterface)
			{
				foreach (var item in conflictMap.Where(kvp => !kvp.Value.ContainsConflicts()).ToList())
				{
					conflictMap.Remove(item.Key);
				}
			}

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

				if (thisIsInterface)
				{
					// 接口需要合并相同签名的方法
					MethodDef metDef = metItem.Item2;
					var entry = new TableMethodPair(this, metDef);
					MergeInterface(metNameKey, entry);
				}
				else if (conflictMap.TryGetValue(metNameKey, out var confPair))
				{
					Debug.Assert(confPair.ContainsConflicts());

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

			// 关联抽象基类未实现的接口
			if (NotImplInterfaces.IsCollectionValid())
			{
				List<string> removedKeys = new List<string>();
				foreach (var kv in NotImplInterfaces)
				{
					string metNameKey = kv.Key;
					var notImplEntries = kv.Value;
					if (SlotMap.TryGetValue(metNameKey, out var vslot))
					{
						vslot.Entries.UnionWith(notImplEntries);
						removedKeys.Add(metNameKey);
						ApplyVirtualSlot(vslot);
					}
				}
				foreach (var key in removedKeys)
					NotImplInterfaces.Remove(key);
			}

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

						if (thisIsInterface)
						{
							MergeInterfaces(metNameKey, infEntries);
						}
						else if (nameSet.Contains(metNameKey) &&
								 SlotMap.TryGetValue(metNameKey, out var vslot))
						{
							// 关联当前类型内签名相同的方法
							vslot.Entries.UnionWith(infEntries);
							ApplyVirtualSlot(vslot);
						}
						else
						{
							foreach (var entry in infEntries)
							{
								if (!EntryMap.ContainsKey(entry))
								{
									if (SlotMap.TryGetValue(metNameKey, out vslot))
									{
										// 关联继承链上签名相同的方法
										vslot.Entries.Add(entry);
										ApplyVirtualSlot(vslot);
									}
									else if (thisIsAbstract)
									{
										AddNotImplInterface(metNameKey, entry);
									}
									else
									{
										// 暂时未实现的接口入口
										EntryMap[entry] = null;
									}
								}
							}
						}
					}
				}
			}

			// 记录显式重写目标以便查重
			var expOverTargets = new HashSet<TableMethodPair>();

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
							string.Format("Illegal explicit overriding target in type {0}: {1}",
								GetNameKey(),
								overTarget.FullName));
					}
					if (!targetDef.IsVirtual)
					{
						throw new TypeLoadException(
							string.Format("Explicit overriding target must be virtual in type {0}: {1}",
								GetNameKey(),
								overTarget.FullName));
					}

					var targetEntry = new TableMethodPair(targetTable, targetDef);

					MethodDef implDef = overImpl.ResolveMethodDef();
					Debug.Assert(metDef == implDef);

					// 同一个类内重复的显式重写, 以及重写存在重写的方法, 视为错误
					if ((targetTable == this && targetDef.HasOverrides) ||
						expOverTargets.Contains(targetEntry))
					{
						throw new TypeLoadException(
							string.Format("Explicit overriding target has been overridden in type {0}: {1}",
								GetNameKey(),
								overTarget.FullName));
					}
					expOverTargets.Add(targetEntry);

					if (targetTable.Def.IsInterface)
					{
						// 接口方法显式重写
						ExplicitOverride(targetEntry, metNameKey);
					}
					else
					{
						// 相同类型的需要添加到替换映射, 以便非虚调用时处理替换
						if (targetTable == this)
							SameTypeReplaceMap[targetDef] = new TableMethodPair(this, implDef);
						else
						{
							var vslot = SlotMap[metNameKey];
							Debug.Assert(vslot != null);
							EntryMap[targetEntry] = vslot.Implemented;
						}
					}
				}
			}
			expOverTargets = null;

			// 接口不需要展开入口
			if (thisIsInterface)
				return;

			// 展开新建槽位映射
			foreach (var kv in SlotMap)
			{
				var entries = kv.Value.Entries;
				var newSlotEntry = kv.Value.NewSlotEntry;
				Debug.Assert(newSlotEntry.Item2.IsNewSlot);

				foreach (TableMethodPair entry in entries)
				{
					var entryDef = entry.Item2;
					if (entryDef.IsReuseSlot && entryDef != newSlotEntry.Item2)
						NewSlotEntryMap[entryDef] = newSlotEntry;
				}
			}

			// 对于非抽象类需要检查是否存在实现
			if (!thisIsAbstract)
			{
				foreach (var kv in EntryMap)
				{
					if (kv.Value == null)
					{
						throw new TypeLoadException(
							string.Format("Interface/abstract method not implemented in type {0}: {1} -> {2}",
								GetNameKey(),
								kv.Key.Item1.GetNameKey(),
								kv.Key.Item2.FullName));
					}
				}
			}
		}

		private void DerivedTable(MethodTable baseTable)
		{
			foreach (var kv in baseTable.SlotMap)
				SlotMap.Add(kv.Key, new VirtualSlot(kv.Value, null));

			foreach (var kv in baseTable.EntryMap)
				EntryMap.Add(kv.Key, kv.Value);

			foreach (var kv in baseTable.SameTypeReplaceMap)
				SameTypeReplaceMap.Add(kv.Key, kv.Value);

			foreach (var kv in baseTable.NewSlotEntryMap)
				NewSlotEntryMap.Add(kv.Key, kv.Value);

			if (baseTable.NotImplInterfaces.IsCollectionValid())
			{
				Debug.Assert(baseTable.Def.IsAbstract);
				NotImplInterfaces = new Dictionary<string, HashSet<TableMethodPair>>();
				foreach (var kv in baseTable.NotImplInterfaces)
					NotImplInterfaces.Add(kv.Key, new HashSet<TableMethodPair>(kv.Value));
			}
		}

		private void MergeInterface(string metNameKey, TableMethodPair entry)
		{
			Debug.Assert(entry.Item1.Def.IsInterface);
			if (!SlotMap.TryGetValue(metNameKey, out var vslot))
			{
				vslot = new VirtualSlot(null);
				SlotMap.Add(metNameKey, vslot);
			}
			vslot.Entries.Add(entry);
		}

		private void MergeInterfaces(string metNameKey, HashSet<TableMethodPair> entrySet)
		{
			if (!SlotMap.TryGetValue(metNameKey, out var vslot))
			{
				vslot = new VirtualSlot(null);
				SlotMap.Add(metNameKey, vslot);
			}
			vslot.Entries.UnionWith(entrySet);
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

			var entry = new TableMethodPair(this, metDef);
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
					ApplyVirtualSlot(vslot);
					return vslot;
				}
			}

			Debug.Assert(metDef.IsNewSlot);
			vslot = new VirtualSlot(impl);
			vslot.Entries.Add(entry);
			vslot.Implemented = impl;
			SlotMap[metNameKey] = vslot;
			ApplyVirtualSlot(vslot);
			return vslot;
		}

		private void ExplicitOverride(TableMethodPair targetEntry, string overriddenMet)
		{
			RemoveSlotEntry(targetEntry);
			var vslot = SlotMap[overriddenMet];
			Debug.Assert(vslot != null);
			vslot.Entries.Add(targetEntry);
			ApplyVirtualSlot(vslot);
		}

		private void ApplyVirtualSlot(VirtualSlot vslot)
		{
			Debug.Assert(vslot != null);

			var impl = vslot.Implemented;
			foreach (TableMethodPair entry in vslot.Entries)
			{
				EntryMap[entry] = impl;
			}
		}

		private void RemoveSlotEntry(TableMethodPair entry)
		{
			List<string> removedKeys = new List<string>();
			foreach (var kv in SlotMap)
			{
				string metNameKey = kv.Key;
				var vslot = kv.Value;

				vslot.Entries.Remove(entry);

				if (vslot.Entries.Count == 0)
				{
					removedKeys.Add(metNameKey);
				}
			}
			foreach (var key in removedKeys)
				SlotMap.Remove(key);

			if (NotImplInterfaces.IsCollectionValid())
			{
				removedKeys.Clear();
				foreach (var kv in NotImplInterfaces)
				{
					string metNameKey = kv.Key;
					var notImplEntries = kv.Value;

					notImplEntries.Remove(entry);

					if (notImplEntries.Count == 0)
						removedKeys.Add(metNameKey);
				}
				foreach (var key in removedKeys)
					NotImplInterfaces.Remove(key);
			}
		}

		private void AddNotImplInterface(string metNameKey, TableMethodPair notImplPair)
		{
			if (NotImplInterfaces == null)
				NotImplInterfaces = new Dictionary<string, HashSet<TableMethodPair>>();

			if (!NotImplInterfaces.TryGetValue(metNameKey, out var entries))
			{
				entries = new HashSet<TableMethodPair>();
				NotImplInterfaces.Add(metNameKey, entries);
			}
			entries.Add(notImplPair);
		}
	}
}
