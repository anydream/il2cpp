using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	internal class GenericArgs
	{
		public IList<TypeSig> GenArgs;
		public bool HasGenArgs => GenArgs != null && GenArgs.Count > 0;
	}

	internal class TypeX : GenericArgs
	{
		// 当前环境
		public readonly Il2cppContext Context;

		// 类型定义
		public TypeDef Def;
		// 类型全名
		public readonly string DefFullName;
		// 类型签名
		public readonly ClassOrValueTypeSig DefSig;
		// 类型属性
		public readonly TypeAttributes DefAttr;
		// 定义的基类型
		public ITypeDefOrRef DefBaseType;
		// 定义的接口列表
		public IList<InterfaceImpl> DefInterfaces;
		// 是否为值类型
		public readonly bool IsValueType;

		// 唯一名称
		private string NameKey;

		// 基类型
		public TypeX BaseType;
		// 接口列表
		public readonly List<TypeX> Interfaces = new List<TypeX>();

		// 子类型集合
		private readonly HashSet<TypeX> DerivedTypes = new HashSet<TypeX>();
		public bool IsDerivedTypesChanged { get; private set; }

		// 方法映射
		private readonly Dictionary<string, MethodX> MethodMap = new Dictionary<string, MethodX>();
		// 字段映射
		private readonly Dictionary<string, FieldX> FieldMap = new Dictionary<string, FieldX>();

		// 实例方法定义列表
		private List<MethodDef> InstanceMetDefs;
		// 展开泛型类型的方法签名
		private List<string> ExpandedMetSigs;
		// 不展开泛型类型的方法签名
		private List<string> NotExpandedMetSigs;

		// 展开的方法表
		private MethodTable ExpandedMethodTable;
		// 不展开的方法表
		private MethodTable NotExpandedMethodTable;

		// 是否可实例化
		public bool IsInstantiatable;
		// 是否实例化过
		public OnceBool IsInstantiated;

		internal TypeX(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
			Def = tyDef;
			DefFullName = tyDef.FullName;
			DefAttr = tyDef.Attributes;
			DefBaseType = tyDef.BaseType;
			DefInterfaces = tyDef.Interfaces;
			IsValueType = tyDef.IsValueType;

			if (IsValueType)
				DefSig = new ValueTypeSig(tyDef);
			else
				DefSig = new ClassSig(tyDef);
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
				sb.Append(NameManager.EscapeName(DefFullName));
				if (HasGenArgs)
				{
					sb.Append('<');
					NameManager.TypeSigListName(sb, GenArgs);
					sb.Append('>');
				}

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public TypeSig GetThisTypeSig()
		{
			TypeSig thisSig = DefSig;
			if (HasGenArgs)
				thisSig = new GenericInstSig(DefSig, GenArgs);
			if (IsValueType)
				thisSig = new ByRefSig(thisSig);
			return thisSig;
		}

		public HashSet<TypeX> GetDerivedTypes()
		{
			return DerivedTypes;
		}

		public void UpdateDerivedTypes()
		{
			BaseType?.AddDerivedTypeRecursive(this);
			foreach (var inf in Interfaces)
				inf.AddDerivedTypeRecursive(this);
		}

		private void AddDerivedTypeRecursive(TypeX tyX)
		{
			IsDerivedTypesChanged = true;
			DerivedTypes.Add(tyX);

			// 递归添加
			BaseType?.AddDerivedTypeRecursive(tyX);
			foreach (var inf in Interfaces)
				inf.AddDerivedTypeRecursive(tyX);
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

		public MethodTable GetNotExpandedMethodTable()
		{
			if (NotExpandedMethodTable == null)
			{
				InitMethods();
				NotExpandedMethodTable = new MethodTable();
				NotExpandedMethodTable.ResolveBindings(this, NotExpandedMetSigs);
			}
			return NotExpandedMethodTable;
		}

		public MethodTable GetExpandedMethodTable()
		{
			if (ExpandedMethodTable == null)
			{
				InitMethods();
				ExpandedMethodTable = new MethodTable();
				ExpandedMethodTable.ResolveBindings(this, ExpandedMetSigs);
			}
			return ExpandedMethodTable;
		}

		public List<string> GetExpandedSigNames()
		{
			return ExpandedMetSigs;
		}

		public string GetNotExpandedSigName(int idx)
		{
			return NotExpandedMetSigs[idx];
		}

		public MethodDef GetInstanceMethodDef(int idx)
		{
			return InstanceMetDefs[idx];
		}

		private void InitMethods()
		{
			if (InstanceMetDefs != null)
				return;

			InstanceMetDefs = new List<MethodDef>();
			ExpandedMetSigs = new List<string>();
			NotExpandedMetSigs = new List<string>();

			GenericReplacer replacer = null;
			if (HasGenArgs)
			{
				replacer = new GenericReplacer();
				replacer.OwnerType = this;
			}

			// 收集所有非静态方法的定义, 并加入映射
			StringBuilder sb = new StringBuilder();
			foreach (var metDef in Def.Methods)
			{
				if (metDef.IsStatic || metDef.IsConstructor)
					continue;

				InstanceMetDefs.Add(metDef);

				// 展开返回值与参数类型
				TypeSig retType = TypeManager.ReplaceGenericSig(metDef.MethodSig.RetType, replacer);
				IList<TypeSig> paramTypes = TypeManager.ReplaceGenericSigList(metDef.MethodSig.Params, replacer);

				NameManager.MethodDefName(
					sb,
					metDef.Name,
					metDef.GenericParameters,
					retType,
					paramTypes,
					metDef.MethodSig.CallingConvention);
				string expMetSigName = sb.ToString();
				sb.Clear();

				NameManager.MethodDefName(
					sb,
					metDef.Name,
					metDef.GenericParameters,
					metDef.MethodSig.RetType,
					metDef.MethodSig.Params,
					metDef.MethodSig.CallingConvention);
				string notExpMetSigName = sb.ToString();
				sb.Clear();

				ExpandedMetSigs.Add(expMetSigName);
				NotExpandedMetSigs.Add(notExpMetSigName);
			}
		}
	}
}
