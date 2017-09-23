using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class GenericArgs
	{
		public IList<TypeSig> GenArgs;
		public bool HasGenArgs => GenArgs.IsCollectionValid();
	}

	internal enum VarianceType
	{
		NonVariant,
		// 协变
		Covariant,
		// 逆变
		Contravariant
	}

	internal class ArrayProperty
	{
		public uint Rank;
		public IList<uint> Sizes;
		public IList<int> LowerBounds;
	}

	internal class TypeX : GenericArgs
	{
		// 类型定义
		public readonly TypeDef Def;
		// 是否为值类型
		public bool IsValueType => Def.IsValueType;

		// 唯一名称
		private string NameKey;

		// 基类型
		public TypeX BaseType;
		// 接口列表
		public readonly List<TypeX> Interfaces = new List<TypeX>();

		// 继承类型集合
		public readonly HashSet<TypeX> DerivedTypes = new HashSet<TypeX>();

		// 协逆变关联基类型集合
		public HashSet<TypeX> VarianceBaseTypes;
		public bool HasVarianceBaseTypes => VarianceBaseTypes.IsCollectionValid();

		// 泛型参数协逆变
		public List<VarianceType> Variances;
		public bool HasVariances => Variances.IsCollectionValid();

		// 方法映射
		private readonly Dictionary<string, MethodX> MethodMap = new Dictionary<string, MethodX>();
		public Dictionary<string, MethodX>.ValueCollection Methods => MethodMap.Values;
		// 字段映射
		private readonly Dictionary<string, FieldX> FieldMap = new Dictionary<string, FieldX>();
		public Dictionary<string, FieldX>.ValueCollection Fields => FieldMap.Values;

		// 方法表
		private VirtualTable VTable;

		// 静态构造方法
		public MethodX CctorMethod;
		// 终结器方法
		public MethodX FinalizerMethod;

		public ArrayProperty ArrayInfo;
		// 是否为数组类型
		public bool IsArrayType => ArrayInfo != null;

		// 是否实例化过
		public bool IsInstantiated;

		// 是否已生成静态构造
		public bool IsCctorGenerated;
		// 是否已生成终结器
		public bool IsFinalizerGenerated;

		// 生成的类型名称
		public string GeneratedTypeName;
		// 生成的类型索引
		public uint GeneratedTypeID;
		// 无引用标记. 1=true, 2=false
		public byte NoRefFlag;

		public TypeX(TypeDef tyDef)
		{
			Def = tyDef;
		}

		public override string ToString()
		{
			return NameKey;
		}

		// 获得类型唯一名称
		public string GetNameKey()
		{
			if (NameKey == null)
			{
				// Name<GenArgs>
				StringBuilder sb = new StringBuilder();

				if (IsArrayType)
					Helper.TypeSigName(sb, GetTypeSig(), true);
				else
					Helper.TypeNameKey(sb, Def, GenArgs);

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public TypeSig GetTypeSig()
		{
			if (IsArrayType)
			{
				if (ArrayInfo.Rank == 1)
					return new SZArraySig(GenArgs[0]);

				return new ArraySig(GenArgs[0], ArrayInfo.Rank, ArrayInfo.Sizes, ArrayInfo.LowerBounds);
			}

			ClassOrValueTypeSig tySig;

			if (IsValueType)
				tySig = new ValueTypeSig(Def);
			else
				tySig = new ClassSig(Def);

			TypeSig thisSig = tySig;
			if (HasGenArgs)
				thisSig = new GenericInstSig(tySig, GenArgs);

			return thisSig;
		}

		public TypeSig GetThisTypeSig()
		{
			TypeSig thisSig = GetTypeSig();

			if (IsValueType)
				thisSig = new ByRefSig(thisSig);
			return thisSig;
		}

		public bool IsDerivedType(TypeX tyX)
		{
			return DerivedTypes.Contains(tyX);
		}

		public void UpdateDerivedTypes()
		{
			BaseType?.AddDerivedTypeRecursive(this);
			foreach (var inf in Interfaces)
				inf.AddDerivedTypeRecursive(this);
		}

		private void AddDerivedTypeRecursive(TypeX tyX)
		{
			DerivedTypes.Add(tyX);

			// 递归添加
			BaseType?.AddDerivedTypeRecursive(tyX);
			foreach (var inf in Interfaces)
				inf.AddDerivedTypeRecursive(tyX);

			if (HasVarianceBaseTypes)
			{
				foreach (var va in VarianceBaseTypes)
					va.AddDerivedTypeRecursive(tyX);
			}
		}

		private void AddDerivedTypeRecursive(HashSet<TypeX> tySet)
		{
			DerivedTypes.UnionWith(tySet);

			// 递归添加
			BaseType?.AddDerivedTypeRecursive(tySet);
			foreach (var inf in Interfaces)
				inf.AddDerivedTypeRecursive(tySet);

			if (HasVarianceBaseTypes)
			{
				foreach (var va in VarianceBaseTypes)
					va.AddDerivedTypeRecursive(tySet);
			}
		}

		public void AddVarianceBaseType(TypeX vaBaseType)
		{
			if (VarianceBaseTypes == null)
				VarianceBaseTypes = new HashSet<TypeX>();

			if (VarianceBaseTypes.Add(vaBaseType))
			{
				var tySet = new HashSet<TypeX>(DerivedTypes);
				tySet.Add(this);
				vaBaseType.AddDerivedTypeRecursive(tySet);
			}
		}

		public bool GetMethod(string key, out MethodX metX)
		{
			return MethodMap.TryGetValue(key, out metX);
		}

		public void AddMethod(string key, MethodX metX)
		{
			MethodMap.Add(key, metX);
		}

		public bool GetField(string key, out FieldX fldX)
		{
			return FieldMap.TryGetValue(key, out fldX);
		}

		public void AddField(string key, FieldX fldX)
		{
			FieldMap.Add(key, fldX);
		}

		private void ResolveVTable(TypeManager typeMgr)
		{
			if (VTable != null)
				return;

			MethodTable mtable;
			if (HasGenArgs)
				mtable = typeMgr.ResolveMethodTableSpec(Def, GenArgs);
			else
				mtable = typeMgr.ResolveMethodTableDefRef(Def);

			VTable = new VirtualTable(mtable);
		}

		public bool QueryCallReplace(
			TypeManager typeMgr,
			MethodDef entryDef,
			out TypeX implTyX,
			out MethodDef implDef)
		{
			ResolveVTable(typeMgr);
			return VTable.QueryCallReplace(entryDef, out implTyX, out implDef);
		}

		public void QueryCallVirt(
			TypeManager typeMgr,
			TypeX entryTyX,
			MethodDef entryDef,
			out TypeX implTyX,
			out MethodDef implDef)
		{
			ResolveVTable(typeMgr);
			VTable.QueryCallVirt(entryTyX, entryDef, out implTyX, out implDef);
		}

		public TypeX FindBaseType(TypeDef tyDef)
		{
			if (BaseType == null)
				return null;
			if (BaseType.Def == tyDef)
				return BaseType;
			return BaseType.FindBaseType(tyDef);
		}
	}
}
