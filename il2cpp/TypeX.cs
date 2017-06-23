using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 方法签名. 如果存在类型泛型则替换成具体类型
	internal class MethodSignature : IEquatable<MethodSignature>
	{
		private readonly string SignatureStr;

		public MethodSignature(MethodDef metDef, List<TypeX> tyGenArgs)
		{
			string sig = metDef.Signature.ToString();
			// 替换类型泛型为具体类型
			if (tyGenArgs != null)
			{
				for (int i = 0; i < tyGenArgs.Count; ++i)
				{
					string from = "!" + i;
					string to = tyGenArgs[i].ToString();
					sig = TypeGenericReplace(sig, from, to);
				}
			}
			SignatureStr = metDef.Name + ": " + sig;
		}

		public override int GetHashCode()
		{
			return SignatureStr.GetHashCode();
		}

		public bool Equals(MethodSignature other)
		{
			Debug.Assert(other != null);

			if (ReferenceEquals(this, other))
				return true;

			return SignatureStr == other.SignatureStr;
		}

		public override bool Equals(object obj)
		{
			return obj is MethodSignature other && Equals(other);
		}

		public override string ToString()
		{
			return SignatureStr;
		}

		private static bool IsDigit(char ch)
		{
			return ch >= '0' && ch <= '9';
		}

		private static string TypeGenericReplace(string input, string from, string to)
		{
			int pos = 0;
			for (;;)
			{
				pos = input.IndexOf(from, pos, StringComparison.Ordinal);
				if (pos == -1)
					break;

				if (pos > 0 && input[pos - 1] == '!')
				{
					++pos;
					continue;
				}

				if (pos < input.Length - from.Length)
				{
					if (IsDigit(input[pos + from.Length]))
					{
						++pos;
						continue;
					}
				}

				input = input.Substring(0, pos) + to + input.Substring(pos + from.Length);
				pos += to.Length;
			}
			return input;
		}
	}

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
		private readonly Dictionary<MethodSignature, List<VirtualSlot>> VMap_ =
			new Dictionary<MethodSignature, List<VirtualSlot>>();

		// 展平的虚表
		private readonly Dictionary<MethodDef, MethodDef> VTable_ =
			new Dictionary<MethodDef, MethodDef>(MethodEqualityComparer.CompareDeclaringTypes);

		// 显式覆盖的虚表
		private readonly Dictionary<string, MethodDef> ExplicitVTable_ = new Dictionary<string, MethodDef>();

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

			return vtbl;
		}

		public void NewSlot(MethodSignature sig, VirtualSlot vslot)
		{
			if (!VMap_.TryGetValue(sig, out var slotList))
			{
				slotList = new List<VirtualSlot>();
				VMap_.Add(sig, slotList);
			}
			slotList.Add(vslot);
		}

		public void ReuseSlot(MethodSignature sig, VirtualSlot vslot)
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

		public void AddExplicit(string entry, MethodDef impl)
		{
			ExplicitVTable_[entry] = impl;
		}

		public MethodDef FindImplMethod(string sig, MethodDef entry)
		{
			if (ExplicitVTable_.TryGetValue(sig, out MethodDef metDef))
				return metDef;

			if (VTable_.TryGetValue(entry, out metDef))
				return metDef;

			Debug.Fail("FindImplMethod " + sig + ", " + entry);
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
			foreach (var sig in ModifierList)
				sb.Append(sig.ModifierToString(isPretty));
			return sb.ToString();
		}

		public override int GetHashCode()
		{
			return Def.TypeHashCode() ^
				   GenericHashCode() ^
				   ModifierHashCode();
		}

		public bool Equals(TypeX other)
		{
			Debug.Assert(other != null);

			if (ReferenceEquals(this, other))
				return true;

			return Def.TypeEquals(other.Def) &&
				   GenericEquals(other) &&
				   ModifierEquals(other);
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

		private bool IsExpiredType(TypeDef tyDef, TypeX vmetDecl)
		{
			return Def.TypeEquals(tyDef) && IsDerivedOrEqual(vmetDecl);
		}

		public TypeX GetImplType(TypeDef tyDef, TypeX vmetDecl)
		{
			if (IsExpiredType(tyDef, vmetDecl))
				return this;

			foreach (var baseCls in BaseClasses)
			{
				if (baseCls.IsExpiredType(tyDef, vmetDecl))
					return baseCls;
			}

			Debug.Fail("GetImplType " + tyDef + ", " + vmetDecl);
			return null;
		}

		public MethodDef GetImplMethod(string sig, MethodDef entry)
		{
			return VTable.FindImplMethod(sig, entry);
		}

		private void CollectInterfaceMethods(Dictionary<MethodSignature, HashSet<MethodDef>> infMetMap)
		{
			Debug.Assert(Def.IsInterface);

			if (HasInterfaces)
			{
				foreach (var inf in Interfaces_)
					inf.CollectInterfaceMethods(infMetMap);
			}

			foreach (var metDef in Def.Methods)
			{
				var sig = new MethodSignature(metDef, GenArgs);
				if (!infMetMap.TryGetValue(sig, out var metSet))
				{
					metSet = new HashSet<MethodDef>(MethodEqualityComparer.CompareDeclaringTypes);
					infMetMap.Add(sig, metSet);
				}
				metSet.Add(metDef);
			}
		}

		private void MergeVTable(
			Dictionary<MethodSignature, HashSet<MethodDef>> infMetMap,
			MethodSignature sig, MethodDef metDef)
		{
			// 合并接口相同构型的方法
			if (!infMetMap.TryGetValue(sig, out var metSet))
			{
				metSet = new HashSet<MethodDef>();
				infMetMap.Add(sig, metSet);
			}
			metSet.Add(metDef);

			var vslot = new VirtualSlot(metSet.ToList(), metDef);

			if (metDef.IsNewSlot)
			{
				VTable.NewSlot(sig, vslot);
			}
			else if (metDef.IsReuseSlot)
			{
				VTable.ReuseSlot(sig, vslot);
			}
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
			var infMetMap = new Dictionary<MethodSignature, HashSet<MethodDef>>();
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
					var sig = new MethodSignature(metDef, GenArgs);
					MergeVTable(infMetMap, sig, metDef);
				}
			}

			// 展平虚表
			VTable.ExpandTable();

			// 强制替换显式覆盖的方法
			foreach (var metDef in overMets)
			{
				foreach (var overInfo in metDef.Overrides)
				{
					string entry = overInfo.MethodDeclaration.ToString();
					VTable.AddExplicit(entry, metDef);
				}
			}
		}
	}
}
