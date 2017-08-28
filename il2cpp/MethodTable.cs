using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class VirtualTable
	{
		public readonly Dictionary<string, Dictionary<MethodDef, Tuple<string, MethodDef>>> Table =
			new Dictionary<string, Dictionary<MethodDef, Tuple<string, MethodDef>>>();

		public void Set(string entryType, MethodDef entryDef, string implType, MethodDef implDef)
		{
			if (!Table.TryGetValue(entryType, out var defMap))
			{
				defMap = new Dictionary<MethodDef, Tuple<string, MethodDef>>();
				Table.Add(entryType, defMap);
			}
			defMap.Add(entryDef, new Tuple<string, MethodDef>(implType, implDef));
		}

		public bool Query(
			string entryType, MethodDef entryDef,
			out string implType, out MethodDef implDef)
		{
			if (Table.TryGetValue(entryType, out var defMap))
			{
				if (defMap.TryGetValue(entryDef, out var impl))
				{
					implType = impl.Item1;
					implDef = impl.Item2;
					return true;
				}
			}
			implType = null;
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
		private Dictionary<MethodTable, Dictionary<MethodDef, VirtualImpl>> ExpandedVSlotMap;

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

			VirtualTable vtable = new VirtualTable();

			IGenericReplacer replacer = null;
			if (tyGenArgs != null && tyGenArgs.Count > 0)
				replacer = new TypeDefGenReplacer(Def, tyGenArgs);

			foreach (var kv in ExpandedVSlotMap)
			{
				MethodTable entryTable = kv.Key;
				foreach (var item in kv.Value)
				{
					MethodDef entryDef = item.Key;

					MethodTable implTable = item.Value.ImplTable;
					MethodDef implDef = item.Value.ImplMethod;

					string entryType = entryTable == this ? thisNameKey : entryTable.GetReplacedNameKey(replacer);
					string implType = implTable == this ? thisNameKey : implTable.GetReplacedNameKey(replacer);

					vtable.Set(entryType, entryDef, implType, implDef);
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

			// 解析不展开类型泛型的方法签名, 和展开的方法签名
			foreach (var metDef in Def.Methods)
			{
				if (metDef.IsStatic || metDef.IsConstructor)
					continue;

				MethodDefList.Add(metDef);

				if (replacer == null)
				{
					Helper.MethodNameKey(
						sb,
						metDef.Name,
						metDef.GenericParameters.Count,
						metDef.MethodSig.RetType,
						metDef.MethodSig.Params,
						metDef.MethodSig.CallingConvention);
				}
				else
				{
					TypeSig retType = Helper.ReplaceGenericSig(metDef.MethodSig.RetType, replacer);
					IList<TypeSig> paramTypes = Helper.ReplaceGenericSigList(metDef.MethodSig.Params, replacer);

					Helper.MethodNameKey(
						sb,
						metDef.Name,
						metDef.GenericParameters.Count,
						retType,
						paramTypes,
						metDef.MethodSig.CallingConvention);
				}

				ExpandedSigList.Add(sb.ToString());
				sb.Clear();
			}

			// 解析并继承基类方法表
			if (Def.BaseType != null)
			{
				MethodTable baseTable = Context.TypeMgr.ResolveMethodTable(Def.BaseType, replacer);
				DerivedFrom(baseTable);
			}

			List<int> explicitOverrides = new List<int>();

			// 处理当前类型的覆盖
			for (int i = 0; i < MethodDefList.Count; ++i)
			{
				MethodDef metDef = MethodDefList[i];
				string expSigName = ExpandedSigList[i];

				if (metDef.HasOverrides)
				{
					// 记录存在显式覆盖的方法
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
									MergeSlot(expSigName, entryTable, entryDef);
								}
							}
						}
					}
				}
			}

			// 处理显式覆盖
			foreach (int idx in explicitOverrides)
			{
				MethodDef overDef = MethodDefList[idx];
				ExplicitOverride(overDef.Overrides, overDef, idx);
			}

			if (!Def.IsInterface && !Def.IsAbstract)
			{
				foreach (var kv in VSlotMap)
				{
					VirtualSlot vslot = kv.Value;
					if (vslot.Entries.Count > 0)
					{
						if (!vslot.Impl.IsValid())
						{
							// 检查是否存在未实现的接口
							throw new TypeLoadException(string.Format(
								"Slot has no implementation. Class: {0}, Slot: {1}, {2}",
								this, kv.Key,
								vslot.Entries.First()));
						}
						else
						{
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
					return false;
			}

			// 其他情况需要合并接口
			return true;
		}

		private void SetExpandedVSlotMap(MethodTable entryTable, MethodDef entryDef, ref VirtualImpl impl)
		{
			if (ExpandedVSlotMap == null)
				ExpandedVSlotMap = new Dictionary<MethodTable, Dictionary<MethodDef, VirtualImpl>>();

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

			if (other.ExpandedVSlotMap != null)
			{
				ExpandedVSlotMap = new Dictionary<MethodTable, Dictionary<MethodDef, VirtualImpl>>();
				foreach (var kv in other.ExpandedVSlotMap)
					ExpandedVSlotMap.Add(kv.Key, new Dictionary<MethodDef, VirtualImpl>(kv.Value));
			}
		}

		private void NewSlot(string expSigName, MethodDef metDef)
		{
			VirtualSlot vslot = new VirtualSlot();
			VSlotMap[expSigName] = vslot;
			vslot.AddEntry(this, metDef);
			vslot.SetImpl(this, metDef);
		}

		private void ReuseSlot(string expSigName, MethodDef metDef)
		{
			bool status = VSlotMap.TryGetValue(expSigName, out var vslot);
			Debug.Assert(status);
			vslot.AddEntry(this, metDef);
			vslot.SetImpl(this, metDef);
		}

		private void MergeSlot(string expSigName, MethodTable entryTable, MethodDef entryDef)
		{
			// 合并之前需要删除现有的入口
			RemoveEntry(entryTable, entryDef);

			if (!VSlotMap.TryGetValue(expSigName, out var vslot))
			{
				vslot = new VirtualSlot();
				VSlotMap.Add(expSigName, vslot);
			}
			vslot.AddEntry(entryTable, entryDef);
		}

		private void ExplicitOverride(IList<MethodOverride> overList, MethodDef ownerMetDef, int ownerMetIdx)
		{
			IGenericReplacer replacer = null;
			if (HasGenArgs)
				replacer = new TypeDefGenReplacer(Def, GenArgs);

			foreach (var overItem in overList)
			{
				var target = overItem.MethodDeclaration;
				var impl = overItem.MethodBody;

				MethodTable targetTable = Context.TypeMgr.ResolveMethodTable(target.DeclaringType, replacer);
				MethodDef targetDef = target.ResolveMethodDef();

				MethodDef implDef = impl.ResolveMethodDef();
				string expSigName;

				if (implDef == ownerMetDef)
				{
					expSigName = ExpandedSigList[ownerMetIdx];
				}
				else
					throw new NotSupportedException();

				// 合并目标入口到实现方法的方法槽内
				MergeSlot(expSigName, targetTable, targetDef);
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
