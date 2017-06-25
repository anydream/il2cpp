using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 虚表槽. 包含一系列入口方法和它们所对应的实现方法
	internal class VirtualSlot
	{
		public readonly List<MethodDef> Entries;
		public MethodDef ImplMethod;

		public VirtualSlot(List<MethodDef> entries, MethodDef impl)
		{
			Entries = entries;
			ImplMethod = impl;
		}

		public VirtualSlot Clone()
		{
			return new VirtualSlot(new List<MethodDef>(Entries), ImplMethod);
		}
	}

	// 虚表
	internal class VirtualTable
	{
		// 方法构型对应的虚表槽
		private readonly Dictionary<string, List<VirtualSlot>> VMap_ =
			new Dictionary<string, List<VirtualSlot>>();
		// 展平的虚表
		private readonly Dictionary<MethodDef, MethodDef> VTable_ =
			new Dictionary<MethodDef, MethodDef>(MethodEqualityComparer.CompareDeclaringTypes);
		// 显式覆盖的虚表
		private readonly Dictionary<string, MethodDef> ExplicitVTable_ = new Dictionary<string, MethodDef>();
		// 显式覆盖的构型
		public readonly HashSet<string> ExplicitSignature = new HashSet<string>();

		public VirtualTable Clone()
		{
			VirtualTable vtbl = new VirtualTable();

			foreach (var kv in VMap_)
			{
				var slotList = kv.Value.Select(vslot => vslot.Clone()).ToList();
				vtbl.VMap_.Add(kv.Key, slotList);
			}

			foreach (var kv in ExplicitVTable_)
				vtbl.ExplicitVTable_.Add(kv.Key, kv.Value);

			vtbl.ExplicitSignature.UnionWith(ExplicitSignature);

			return vtbl;
		}

		public void NewSlot(string sig, VirtualSlot vslot)
		{
			if (!VMap_.TryGetValue(sig, out var slotList))
			{
				slotList = new List<VirtualSlot>();
				VMap_.Add(sig, slotList);
			}
			slotList.Add(vslot);
		}

		public void ReuseSlot(string sig, VirtualSlot vslot)
		{
			bool status = VMap_.TryGetValue(sig, out var slotList);
			Debug.Assert(status);
			Debug.Assert(slotList.Count > 0);

			VirtualSlot last = slotList.Last();
			last.Entries.AddRange(vslot.Entries);
			last.ImplMethod = vslot.ImplMethod;
		}

		public void ExpandTable()
		{
			foreach (var slotList in VMap_.Values)
			{
				foreach (var vslot in slotList)
				{
					foreach (var entry in vslot.Entries)
					{
						VTable_[entry] = vslot.ImplMethod;
					}
				}
			}
		}

		public void AddExplicit(string entry, string sig, MethodDef impl)
		{
			ExplicitVTable_[entry] = impl;
			ExplicitSignature.Add(sig);
		}

		public MethodDef FindImplMethod(string sig, MethodDef entry)
		{
			// 优先搜索显式覆盖映射
			if (ExplicitVTable_.TryGetValue(sig, out MethodDef metDef))
				return metDef;

			if (VTable_.TryGetValue(entry, out metDef))
				return metDef;

			return null;
		}
	}

	// 类型包装
	public class TypeX : GenericArgs, IEquatable<TypeX>
	{
		// 定义
		public readonly TypeDef Def;
		// 类型修饰列表
		private List<NonLeafSig> ModifierList_;
		public List<NonLeafSig> ModifierList
		{
			get => ModifierList_;
			set
			{
				Debug.Assert(ModifierList_ == null);
				Debug.Assert(value == null || value.Count > 0);
				ModifierList_ = value;
			}
		}
		public bool HasModifierList => ModifierList_ != null && ModifierList_.Count > 0;

		// 基类
		public TypeX BaseType { get; set; }
		// 接口列表
		private List<TypeX> Interfaces_;
		public List<TypeX> Interfaces => Interfaces_ ?? (Interfaces_ = new List<TypeX>());
		public bool HasInterfaces => Interfaces_ != null && Interfaces_.Count > 0;
		// 方法列表
		private List<MethodX> Methods_;
		public List<MethodX> Methods => Methods_ ?? (Methods_ = new List<MethodX>());
		public bool HasMethods => Methods_ != null && Methods_.Count > 0;
		// 字段列表
		private List<FieldX> Fields_;
		public List<FieldX> Fields => Fields_ ?? (Fields_ = new List<FieldX>());
		public bool HasFields => Fields_ != null && Fields_.Count > 0;

		// 是否为纯引用类型
		public bool IsRefOnly => !HasMethods && !HasFields;

		// 是否生成了静态构造
		public bool IsCctorGenerated { get; private set; }
		// 是否生成了终结方法
		public bool IsFinalizerGenerated { get; private set; }

		// 展平的继承类集合
		private HashSet<TypeX> BaseClasses;
		// 虚表
		private VirtualTable VTable;
		// 基类覆盖子类接口的交叉覆盖
		private HashSet<MethodDef> CrossOverrides_;
		private HashSet<MethodDef> CrossOverrides => CrossOverrides_ ?? (CrossOverrides_ = new HashSet<MethodDef>());
		private bool HasCrossOverrides => CrossOverrides_ != null && CrossOverrides_.Count > 0;

		public TypeX(TypeDef tyDef)
		{
			Def = tyDef;
		}

		public TypeX Clone(List<NonLeafSig> modList)
		{
			TypeX tyX = new TypeX(Def);
			if (HasGenArgs)
				tyX.GenArgs = new List<TypeX>(GenArgs);

			if (HasModifierList)
			{
				if (modList != null)
					tyX.ModifierList = ModifierList.Concat(modList).ToList();
				else
					tyX.ModifierList = new List<NonLeafSig>(ModifierList);
			}
			else
				tyX.ModifierList = modList;

			return tyX;
		}

		public void SetCctorGenerated()
		{
			IsCctorGenerated = true;
		}

		public void SetFinalizerGenerated()
		{
			IsFinalizerGenerated = true;
		}

		public int ModifierHashCode()
		{
			return ModifierList?.Count ?? 0;
		}

		public bool ModifierEquals(TypeX other)
		{
			if (ModifierList == null && other.ModifierList == null)
				return true;
			if (ModifierList == null || other.ModifierList == null)
				return false;

			if (ModifierList.Count != other.ModifierList.Count)
				return false;

			for (int i = 0; i < ModifierList.Count; ++i)
			{
				if (!ModifierList[i].ModifierEquals(other.ModifierList[i]))
					return false;
			}
			return true;
		}

		public string ModifierToString(bool isPretty = false)
		{
			if (ModifierList == null)
				return "";

			StringBuilder sb = new StringBuilder();
			foreach (var mod in ModifierList)
				sb.Append(mod.ModifierToString(isPretty));
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			return Def.TypeHashCode() ^
				   GenericHashCode() ^
				   ModifierHashCode() ^
				   Def.Module.RuntimeVersion.GetHashCode();
		}

		public bool Equals(TypeX other)
		{
			Debug.Assert(other != null);

			if (ReferenceEquals(this, other))
				return true;

			return Def.TypeEquals(other.Def) &&
				   GenericEquals(other) &&
				   ModifierEquals(other) &&
				   Def.Module.RuntimeVersion == other.Def.Module.RuntimeVersion;
		}

		public override bool Equals(object obj)
		{
			return obj is TypeX other && Equals(other);
		}

		public override string ToString()
		{
			return Def.FullName + GenericToString() + ModifierToString();
		}

		public string PrettyName()
		{
			return Def.PrettyName() + GenericToString(true) + ModifierToString(true);
		}

		private HashSet<TypeX> CollectBaseClasses()
		{
			if (BaseClasses == null)
			{
				BaseClasses = new HashSet<TypeX>();

				if (HasInterfaces)
				{
					foreach (var inf in Interfaces_)
					{
						BaseClasses.UnionWith(inf.CollectBaseClasses());
						BaseClasses.Add(inf);
					}
				}

				if (BaseType != null)
				{
					BaseClasses.UnionWith(BaseType.CollectBaseClasses());
					BaseClasses.Add(BaseType);
				}
			}

			return BaseClasses;
		}

		public void InitVTable()
		{
			CollectBaseClasses();
			ResolveVTable();
		}

		public bool IsDerivedOrEqual(TypeX tyX)
		{
			if (Equals(tyX))
				return true;
			return BaseClasses.Contains(tyX);
		}

		private bool IsExpiredType(TypeDef tyDef, TypeX vmetDecl, MethodDef vmetDef)
		{
			if (Def.TypeEquals(tyDef))
			{
				if (IsDerivedOrEqual(vmetDecl))
					return true;
				return HasCrossOverrides && CrossOverrides_.Contains(vmetDef);
			}
			return false;
		}

		public TypeX GetImplType(TypeDef tyDef, TypeX vmetDecl, MethodDef vmetDef)
		{
			if (IsExpiredType(tyDef, vmetDecl, vmetDef))
				return this;

			foreach (var baseCls in BaseClasses)
			{
				if (baseCls.IsExpiredType(tyDef, vmetDecl, vmetDef))
					return baseCls;
			}

			Debug.Fail("GetImplType " + tyDef + ", " + vmetDecl);
			return null;
		}

		public MethodDef GetImplMethod(string sig, MethodDef entry)
		{
			return VTable.FindImplMethod(sig, entry);
		}

		private void CollectInterfaceMethods(Dictionary<string, HashSet<MethodDef>> infMetMap)
		{
			Debug.Assert(Def.IsInterface);

			if (HasInterfaces)
			{
				foreach (var inf in Interfaces_)
					inf.CollectInterfaceMethods(infMetMap);
			}

			foreach (var metDef in Def.Methods)
			{
				string sig = Helpers.MakeSignature(metDef, GenArgs);
				if (!infMetMap.TryGetValue(sig, out var metSet))
				{
					metSet = new HashSet<MethodDef>(MethodEqualityComparer.CompareDeclaringTypes);
					infMetMap.Add(sig, metSet);
				}
				metSet.Add(metDef);
			}
		}

		private void MergeVTable(
			Dictionary<string, HashSet<MethodDef>> infMetMap,
			string sig, MethodDef metDef)
		{
			// 合并接口相同构型的方法
			if (!infMetMap.TryGetValue(sig, out var metSet))
			{
				metSet = new HashSet<MethodDef>();
			}
			metSet.Add(metDef);

			var vslot = new VirtualSlot(metSet.ToList(), metDef);

			if (metDef.IsNewSlot ||
				Def.FullName == "System.Object")
			{
				VTable.NewSlot(sig, vslot);
			}
			else if (metDef.IsReuseSlot)
			{
				VTable.ReuseSlot(sig, vslot);
			}
		}

		private bool MergeAbstract(string targetSig, MethodDef targetDef, VirtualTable targetVTable)
		{
			foreach (var metDef in Def.Methods)
			{
				string sig = Helpers.MakeSignature(metDef, GenArgs);
				if (sig == targetSig)
				{
					targetVTable.NewSlot(targetSig, new VirtualSlot(new List<MethodDef> { targetDef }, metDef));
					CrossOverrides.Add(targetDef);
					return true;
				}
			}
			return false;
		}

		private static bool IsAbstract(HashSet<MethodDef> metSet, out MethodDef metDef)
		{
			if (metSet.Count == 1)
			{
				metDef = metSet.First();
				Debug.Assert(metDef.IsAbstract && metDef.DeclaringType.IsInterface);
				return true;
			}
			metDef = null;
			return false;
		}

		private void ResolveVTable()
		{
			if (VTable != null)
				return;

			if (BaseType != null)
			{
				BaseType.ResolveVTable();
				VTable = BaseType.VTable.Clone();
			}
			else
				VTable = new VirtualTable();

			// 收集接口方法
			var infMetMap = new Dictionary<string, HashSet<MethodDef>>();
			if (HasInterfaces)
			{
				foreach (var inf in Interfaces_)
					inf.CollectInterfaceMethods(infMetMap);
			}

			List<MethodDef> overMets = new List<MethodDef>();

			foreach (var metDef in Def.Methods)
			{
				if (!metDef.IsVirtual)
					continue;

				if (metDef.HasOverrides)
					overMets.Add(metDef);
				else
				{
					string sig = Helpers.MakeSignature(metDef, GenArgs);
					MergeVTable(infMetMap, sig, metDef);
				}
			}

			// 强制替换显式覆盖的方法
			foreach (var metDef in overMets)
			{
				foreach (var overInfo in metDef.Overrides)
				{
					string metSig = "";
					if (overInfo.MethodDeclaration is MethodDef ometDef)
					{
						metSig = ometDef.Signature.ToString();
					}
					else if (overInfo.MethodDeclaration is MemberRef ometRef)
					{
						metSig = Helpers.GetMethodRefSig(ometRef);
					}
					else
						Debug.Fail("MethodDeclaration");

					string sig = Helpers.MakeSignature(
						overInfo.MethodDeclaration.Name,
						metSig,
						GenArgs);

					string entry = overInfo.MethodDeclaration.ToString();
					entry = Helpers.TypeGenericReplace(entry, GenArgs);

					VTable.AddExplicit(entry, sig, metDef);
				}
			}

			// 处理基类覆盖子类接口方法的情况
			foreach (var kv in infMetMap)
			{
				// 过滤显式覆盖过的构型
				if (VTable.ExplicitSignature.Contains(kv.Key))
					continue;

				if (IsAbstract(kv.Value, out var targetMet))
				{
					var curr = BaseType;
					Debug.Assert(curr != null);
					while (!curr.MergeAbstract(kv.Key, targetMet, VTable))
					{
						curr = curr.BaseType;
						Debug.Assert(curr != null);
					}
				}
			}

			// 展平虚表
			VTable.ExpandTable();
		}
	}
}
