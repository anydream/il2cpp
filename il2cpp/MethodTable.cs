using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class VirtualTable
	{
		public readonly Dictionary<string, Dictionary<MethodDef, Tuple<string, MethodDef>>> Table =
			new Dictionary<string, Dictionary<MethodDef, Tuple<string, MethodDef>>>();

		public readonly Dictionary<string, Tuple<string, MethodDef>> NewSlotMap;

		public readonly Dictionary<MethodDef, Tuple<string, MethodDef>> MethodReplaceMap;

		public readonly Dictionary<string, MethodDef> FallbackTable;

		public VirtualTable(
			Dictionary<string, Tuple<string, MethodDef>> newSlotMap,
			Dictionary<MethodDef, Tuple<string, MethodDef>> metReplaceMap,
			Dictionary<string, MethodDef> fallbackTable)
		{
			NewSlotMap = newSlotMap;
			MethodReplaceMap = metReplaceMap;
			FallbackTable = fallbackTable;
		}

		public void Set(string entryTypeName, MethodDef entryDef, string implTypeName, MethodDef implDef)
		{
			if (!Table.TryGetValue(entryTypeName, out var defMap))
			{
				defMap = new Dictionary<MethodDef, Tuple<string, MethodDef>>();
				Table.Add(entryTypeName, defMap);
			}

			if (defMap.TryGetValue(entryDef, out var oitem))
			{
				if (oitem.Item2 == implDef)
				{
					Debug.Assert(oitem.Item1 == implTypeName);
					return;
				}
				Debug.Assert(oitem.Item2.Rid != implDef.Rid);

				// 如果已存在则保留现有入口
				return;
			}
			defMap[entryDef] = new Tuple<string, MethodDef>(implTypeName, implDef);
		}

		public bool Query(
			string entryTypeName, MethodDef entryDef,
			out string implTypeName, out MethodDef implDef)
		{
			if (Table.TryGetValue(entryTypeName, out var defMap))
			{
				if (defMap.TryGetValue(entryDef, out var impl))
				{
					implTypeName = impl.Item1;
					implDef = impl.Item2;
					return true;
				}
			}
			implTypeName = null;
			implDef = null;
			return false;
		}
	}

	internal struct VirtualImpl
	{
		public MethodTable ImplTable;
		public MethodDef ImplMethod;

		public override string ToString()
		{
			if (IsValid())
				return ImplTable + " -> " + ImplMethod;
			return "null";
		}

		public bool IsValid()
		{
			return ImplTable != null && ImplMethod != null;
		}
	}

	internal class VirtualSlot
	{
		// 入口集合, 为具体的类型和方法的映射
		public readonly Dictionary<MethodTable, HashSet<MethodDef>> Entries =
			new Dictionary<MethodTable, HashSet<MethodDef>>();
		// 实现方法
		public VirtualImpl Impl;

		public VirtualSlot()
		{
		}

		public VirtualSlot(VirtualSlot other)
		{
			foreach (var kv in other.Entries)
			{
				Entries.Add(kv.Key, new HashSet<MethodDef>(kv.Value));
			}

			if (other.Entries.Count > 0)
				Impl = other.Impl;
		}

		public void AddEntry(MethodTable entryTable, MethodDef entryDef)
		{
			if (!Entries.TryGetValue(entryTable, out var defSet))
			{
				defSet = new HashSet<MethodDef>();
				Entries.Add(entryTable, defSet);
			}
			defSet.Add(entryDef);
		}

		public void SetImpl(MethodTable mtable, MethodDef metDef)
		{
			// 抽象方法不作为实现
			if (metDef.IsAbstract)
				return;

			Impl.ImplTable = mtable;
			Impl.ImplMethod = metDef;
		}
	}

	internal class MethodTable : GenericArgs
	{
		private readonly Il2cppContext Context;
		private readonly TypeDef Def;
		private string NameKey;

		// 实例方法列表
		private readonly List<MethodDef> MethodDefList = new List<MethodDef>();
		// 展开类型泛型的方法签名列表
		private readonly List<string> ExpandedSigList = new List<string>();

		// 方法槽映射
		public readonly Dictionary<string, VirtualSlot> VSlotMap = new Dictionary<string, VirtualSlot>();
		// 展平的方法槽映射
		private readonly Dictionary<MethodTable, Dictionary<MethodDef, VirtualImpl>> ExpandedVSlotMap =
			new Dictionary<MethodTable, Dictionary<MethodDef, VirtualImpl>>();

		// 新建槽位方法签名映射
		public readonly Dictionary<string, Tuple<MethodTable, MethodDef>> NewSlotMap =
			new Dictionary<string, Tuple<MethodTable, MethodDef>>();

		// 方法替换映射
		public readonly Dictionary<MethodDef, Tuple<MethodTable, MethodDef>> MethodReplaceMap =
			new Dictionary<MethodDef, Tuple<MethodTable, MethodDef>>();

		// 抽象类未实现的接口方法槽映射
		public Dictionary<string, Dictionary<MethodTable, HashSet<MethodDef>>> AbsNoImplSlotMap;

		// 泛型可能产生的相同签名的解决冲突映射
		public Dictionary<string, Tuple<MethodTable, MethodDef>> SameSigResolvedMap;

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
				Helper.TypeNameKey(sb, Def, GenArgs);
				NameKey = sb.ToString();
			}
			return NameKey;
		}

		private string GetReplacedNameKey(IGenericReplacer replacer)
		{
			if (HasGenArgs)
			{
				var repGenArgs = Helper.ReplaceGenericSigList(GenArgs, replacer);
				StringBuilder sb = new StringBuilder();
				Helper.TypeNameKey(sb, Def, repGenArgs);
				return sb.ToString();
			}
			return GetNameKey();
		}

		public VirtualTable ExpandVTable(IList<TypeSig> tyGenArgs)
		{
			Debug.Assert(!HasGenArgs);
			Debug.Assert(tyGenArgs == null || Def.GenericParameters.Count == tyGenArgs.Count);

			// 展开当前类型名
			StringBuilder sb = new StringBuilder();
			Helper.TypeNameKey(sb, Def, tyGenArgs);
			string thisNameKey = sb.ToString();
			sb = null;

			// 构建泛型替换器
			IGenericReplacer replacer = null;
			if (Helper.IsCollectionValid(tyGenArgs))
				replacer = new TypeDefGenReplacer(Def, tyGenArgs);

			// 替换类型名称
			var newSlotMap = new Dictionary<string, Tuple<string, MethodDef>>();
			foreach (var kv in NewSlotMap)
			{
				MethodTable slotTable = kv.Value.Item1;
				string slotType = slotTable == this ? thisNameKey : slotTable.GetReplacedNameKey(replacer);

				newSlotMap.Add(kv.Key, new Tuple<string, MethodDef>(slotType, kv.Value.Item2));
			}

			var metReplaceMap = new Dictionary<MethodDef, Tuple<string, MethodDef>>();
			foreach (var kv in MethodReplaceMap)
			{
				MethodTable repTable = kv.Value.Item1;
				string repType = repTable == this ? thisNameKey : repTable.GetReplacedNameKey(replacer);

				metReplaceMap.Add(kv.Key, new Tuple<string, MethodDef>(repType, kv.Value.Item2));
			}

			var fallbackTable = new Dictionary<string, MethodDef>();
			if (Helper.IsCollectionValid(SameSigResolvedMap))
			{
				foreach (var kv in SameSigResolvedMap)
				{
					MethodTable resTable = kv.Value.Item1;
					string resType = resTable == this ? thisNameKey : resTable.GetReplacedNameKey(replacer);

					fallbackTable.Add(resType, kv.Value.Item2);
				}
			}

			VirtualTable vtable = new VirtualTable(newSlotMap, metReplaceMap, fallbackTable);

			// 不可实例化的类型不展开虚表
			if (Def.IsAbstract || Def.IsInterface)
				return vtable;

			foreach (var kv in ExpandedVSlotMap)
			{
				MethodTable entryTable = kv.Key;
				Debug.Assert(entryTable != null);

				foreach (var item in kv.Value)
				{
					MethodDef entryDef = item.Key;
					Debug.Assert(entryDef != null);

					MethodTable implTable = item.Value.ImplTable;
					MethodDef implDef = item.Value.ImplMethod;
					Debug.Assert(implTable != null);

					string entryTypeName = entryTable == this ? thisNameKey : entryTable.GetReplacedNameKey(replacer);
					string implTypeName = implTable == this ? thisNameKey : implTable.GetReplacedNameKey(replacer);

					vtable.Set(entryTypeName, entryDef, implTypeName, implDef);
				}
			}

			return vtable;
		}

		public void ResolveTable()
		{
			StringBuilder sb = new StringBuilder();

			IGenericReplacer replacer = null;
			if (HasGenArgs)
				replacer = new TypeDefGenReplacer(Def, GenArgs);

			Dictionary<string, MethodDef> sameSigDefs = new Dictionary<string, MethodDef>();

			// 解析不展开类型泛型的方法签名, 和展开的方法签名
			uint lastRid = 0;
			foreach (var metDef in Def.Methods)
			{
				if (metDef.IsStatic || metDef.IsConstructor)
					continue;

				Debug.Assert(lastRid < metDef.Rid);
				lastRid = metDef.Rid;

				MethodDefList.Add(metDef);

				Helper.MethodDefNameKey(sb, metDef, replacer);

				string expSigName = sb.ToString();
				sb.Clear();

				ExpandedSigList.Add(expSigName);

				if (sameSigDefs.TryGetValue(expSigName, out var osameDef))
				{
					// 遇到相同签名的方法
					if (!Def.HasGenericParameters)
						throw new TypeLoadException("Conflicted method signature");

					if (SameSigResolvedMap == null)
						SameSigResolvedMap = new Dictionary<string, Tuple<MethodTable, MethodDef>>();
					SameSigResolvedMap.Add(expSigName, new Tuple<MethodTable, MethodDef>(this, osameDef));
				}
				else
					sameSigDefs.Add(expSigName, metDef);
			}

			// 解析并继承基类方法表
			if (Def.BaseType != null)
			{
				MethodTable baseTable = Context.TypeMgr.ResolveMethodTable(Def.BaseType, replacer);
				DerivedFrom(baseTable);
			}

			List<int> explicitOverrides = new List<int>();

			// 处理当前类型的重写
			for (int i = 0; i < MethodDefList.Count; ++i)
			{
				MethodDef metDef = MethodDefList[i];
				string expSigName = ExpandedSigList[i];

				if (metDef.HasOverrides)
				{
					// 记录存在显式重写的方法
					explicitOverrides.Add(i);
				}

				if (Def.IsInterface)
				{
					// 特殊处理接口方法, 用于解决泛型接口内签名相同的情况
					Debug.Assert(metDef.IsVirtual && metDef.IsNewSlot && metDef.IsAbstract);
					MergeSlot(expSigName, this, metDef);
				}
				else if (metDef.IsVirtual)
				{
					if (metDef.IsNewSlot)
					{
						NewSlot(expSigName, metDef);
					}
					else
					{
						Debug.Assert(metDef.IsReuseSlot);
						ReuseSlot(expSigName, metDef);
					}
				}
				else
				{
					NewSlot(expSigName, metDef);
				}
			}

			HashSet<string> absNoImplSlots = null;
			if (Def.IsAbstract)
				absNoImplSlots = new HashSet<string>();

			// 合并接口的方法槽到当前方法槽
			if (Def.HasInterfaces)
			{
				foreach (var inf in Def.Interfaces)
				{
					MethodTable infTable = Context.TypeMgr.ResolveMethodTable(inf.Interface, replacer);

					foreach (var kv in infTable.VSlotMap)
					{
						string expSigName = kv.Key;
						VirtualSlot vslot = kv.Value;
						foreach (var kv2 in vslot.Entries)
						{
							MethodTable entryTable = kv2.Key;
							foreach (MethodDef entryDef in kv2.Value)
							{
								if (NeedMergeInterface(expSigName, entryTable, entryDef))
								{
									// 合并
									var mergedVSlot = MergeSlot(expSigName, entryTable, entryDef);

									// 记录虚类型未实现的接口签名
									if (absNoImplSlots != null && !mergedVSlot.Impl.IsValid())
										absNoImplSlots.Add(expSigName);
								}
							}
						}
					}
				}
			}

			// 合并抽象基类未实现的接口
			if (Helper.IsCollectionValid(AbsNoImplSlotMap))
			{
				foreach (var kv in AbsNoImplSlotMap)
				{
					string expSigName = kv.Key;
					foreach (var kv2 in kv.Value)
					{
						MethodTable entryTable = kv2.Key;
						foreach (MethodDef entryDef in kv2.Value)
						{
							if (NeedMergeInterface(expSigName, entryTable, entryDef))
							{
								// 合并
								var mergedVSlot = MergeSlot(expSigName, entryTable, entryDef);
							}
						}
					}
				}
			}

			// 处理显式重写
			foreach (int idx in explicitOverrides)
			{
				MethodDef overDef = MethodDefList[idx];
				ExplicitOverride(overDef, idx);
			}

			if (!Def.IsInterface)
			{
				foreach (var kv in VSlotMap)
				{
					string expSigName = kv.Key;
					VirtualSlot vslot = kv.Value;
					if (vslot.Entries.Count > 0)
					{
						if (!Def.IsAbstract && !vslot.Impl.IsValid())
						{
							// 遇到非抽象类且存在空的绑定实现则报错
							StringBuilder sb2 = new StringBuilder();

							sb2.AppendFormat("Slot has no implementation. Class: {0}, Slot: {1}, ",
								this, expSigName);
							foreach (var item in vslot.Entries)
							{
								sb2.AppendFormat("{0} -> [", item.Key);
								foreach (var entryDef in item.Value)
								{
									sb2.AppendFormat("{0} ", entryDef);
								}
								sb2.Append(']');
							}

							// 检查是否存在未实现的接口
							throw new TypeLoadException(sb2.ToString());
						}
						else
						{
							// 删除存在实现的接口入口
							if (absNoImplSlots != null && vslot.Impl.IsValid())
							{
								absNoImplSlots.Remove(expSigName);
								if (AbsNoImplSlotMap != null)
									AbsNoImplSlotMap.Remove(expSigName);
							}

							// 展平方法槽
							foreach (var kv2 in vslot.Entries)
							{
								MethodTable entryTable = kv2.Key;
								foreach (MethodDef entryDef in kv2.Value)
									SetExpandedVSlotMap(entryTable, entryDef, ref vslot.Impl);
							}
						}
					}
				}
			}

			if (Helper.IsCollectionValid(absNoImplSlots))
			{
				if (AbsNoImplSlotMap == null)
					AbsNoImplSlotMap = new Dictionary<string, Dictionary<MethodTable, HashSet<MethodDef>>>();

				foreach (var expSigName in absNoImplSlots)
				{
					var vslot = VSlotMap[expSigName];
					Debug.Assert(!vslot.Impl.IsValid());

					if (!AbsNoImplSlotMap.TryGetValue(expSigName, out var defMap))
					{
						defMap = new Dictionary<MethodTable, HashSet<MethodDef>>();
						AbsNoImplSlotMap.Add(expSigName, defMap);
					}

					foreach (var entry in vslot.Entries)
					{
						if (!defMap.TryGetValue(entry.Key, out var defSet))
						{
							defSet = new HashSet<MethodDef>();
							defMap.Add(entry.Key, defSet);
						}
						defSet.UnionWith(entry.Value);
					}
				}
			}
		}

		private bool NeedMergeInterface(string expSigName, MethodTable entryTable, MethodDef entryDef)
		{
			// 当前方法表内存在该接口签名, 则需要合并接口
			foreach (string sigName in ExpandedSigList)
			{
				if (sigName == expSigName)
					return true;
			}

			// 当前方法不存在该接口签名, 但是展平的方法槽内已经有该签名了, 则无需合并接口
			if (ExpandedVSlotMap.TryGetValue(entryTable, out var defMap))
			{
				if (defMap.ContainsKey(entryDef))
				{
					return false;
				}
			}

			// 其他情况需要合并接口
			return true;
		}

		private void SetExpandedVSlotMap(MethodTable entryTable, MethodDef entryDef, ref VirtualImpl impl)
		{
			if (!ExpandedVSlotMap.TryGetValue(entryTable, out var defMap))
			{
				defMap = new Dictionary<MethodDef, VirtualImpl>();
				ExpandedVSlotMap.Add(entryTable, defMap);
			}
			defMap[entryDef] = impl;
		}

		private void DerivedFrom(MethodTable other)
		{
			foreach (var kv in other.VSlotMap)
			{
				if (kv.Value.Entries.Count > 0)
					VSlotMap.Add(kv.Key, new VirtualSlot(kv.Value));
			}

			foreach (var kv in other.ExpandedVSlotMap)
				ExpandedVSlotMap.Add(kv.Key, new Dictionary<MethodDef, VirtualImpl>(kv.Value));

			foreach (var kv in other.NewSlotMap)
				NewSlotMap.Add(kv.Key, kv.Value);

			foreach (var kv in other.MethodReplaceMap)
				MethodReplaceMap.Add(kv.Key, kv.Value);

			if (Helper.IsCollectionValid(other.AbsNoImplSlotMap))
			{
				AbsNoImplSlotMap = new Dictionary<string, Dictionary<MethodTable, HashSet<MethodDef>>>();
				foreach (var kv in other.AbsNoImplSlotMap)
				{
					var defMap = new Dictionary<MethodTable, HashSet<MethodDef>>();
					AbsNoImplSlotMap.Add(kv.Key, defMap);

					foreach (var kv2 in kv.Value)
					{
						defMap.Add(kv2.Key, new HashSet<MethodDef>(kv2.Value));
					}
				}
			}

			if (Helper.IsCollectionValid(other.SameSigResolvedMap))
			{
				SameSigResolvedMap = new Dictionary<string, Tuple<MethodTable, MethodDef>>();
				foreach (var kv in other.SameSigResolvedMap)
					SameSigResolvedMap.Add(kv.Key, kv.Value);
			}
		}

		private void NewSlot(string expSigName, MethodDef metDef)
		{
			VirtualSlot vslot = new VirtualSlot();
			VSlotMap[expSigName] = vslot;
			vslot.AddEntry(this, metDef);
			vslot.SetImpl(this, metDef);

			NewSlotMap[expSigName] = new Tuple<MethodTable, MethodDef>(this, metDef);
		}

		private void ReuseSlot(string expSigName, MethodDef metDef)
		{
			if (!VSlotMap.TryGetValue(expSigName, out var vslot))
			{
				NewSlot(expSigName, metDef);
				return;
			}
			vslot.AddEntry(this, metDef);
			vslot.SetImpl(this, metDef);
		}

		private VirtualSlot MergeSlot(string expSigName, MethodTable entryTable, MethodDef entryDef)
		{
			// 合并之前需要删除现有的入口
			RemoveEntry(entryTable, entryDef);

			if (!VSlotMap.TryGetValue(expSigName, out var vslot))
			{
				vslot = new VirtualSlot();
				VSlotMap.Add(expSigName, vslot);
			}
			vslot.AddEntry(entryTable, entryDef);
			return vslot;
		}

		private void ExplicitOverride(MethodDef ownerMetDef, int ownerMetIdx)
		{
			IGenericReplacer replacer = null;
			if (HasGenArgs)
				replacer = new TypeDefGenReplacer(Def, GenArgs);

			foreach (var overItem in ownerMetDef.Overrides)
			{
				var target = overItem.MethodDeclaration;
				var impl = overItem.MethodBody;

				MethodTable targetTable = Context.TypeMgr.ResolveMethodTable(target.DeclaringType, replacer);
				MethodDef targetDef = target.ResolveMethodDef();

				if (targetDef == null)
					throw new TypeLoadException("Cannot find overriding signature: " + target.FullName);

				if (targetDef.HasOverrides)
					throw new TypeLoadException("Method already has overrides: " + targetDef.FullName);

				MethodDef implDef = impl.ResolveMethodDef();

				if (targetTable == this)
				{
					// 处理显式重写当前类型方法的情况
					MethodReplaceMap.Add(targetDef, new Tuple<MethodTable, MethodDef>(this, implDef));
				}
				else
				{
					string expSigName;
					if (implDef == ownerMetDef)
						expSigName = ExpandedSigList[ownerMetIdx];
					else
						throw new NotSupportedException();

					// 合并目标入口到实现方法的方法槽内
					MergeSlot(expSigName, targetTable, targetDef);
				}
			}
		}

		private void RemoveEntry(MethodTable entryTable, MethodDef entryDef)
		{
			// 在所有方法槽内查找, 找到匹配的入口则删除
			foreach (VirtualSlot vslot in VSlotMap.Values)
			{
				if (vslot.Entries.TryGetValue(entryTable, out var defSet))
				{
					if (defSet.Contains(entryDef))
					{
						defSet.Remove(entryDef);
						if (defSet.Count == 0)
						{
							vslot.Entries.Remove(entryTable);
						}
					}
				}
			}
		}
	}
}
