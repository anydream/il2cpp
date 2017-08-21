using System.Collections.Generic;
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

		// 类型定义的全名
		public readonly string DefFullName;
		// 是否为值类型
		public readonly bool IsValueType;

		// 唯一名称
		private string NameKey;

		// 继承的类型
		private readonly HashSet<TypeX> DerivedTypes = new HashSet<TypeX>();
		public bool IsDerivedTypesChanged { get; private set; }

		// 是否实例化过
		public OnceBool IsInstantiated;

		internal TypeX(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
			DefFullName = tyDef.FullName;
			IsValueType = tyDef.IsValueType;
		}

		// 获得类型唯一名称
		public string GetNameKey()
		{
			if (NameKey == null)
			{
				//!
			}
			return NameKey;
		}

		public HashSet<TypeX> GetDerivedTypes()
		{
			return DerivedTypes;
		}

		public void AddDerivedType(TypeX tyX)
		{
			DerivedTypes.Add(tyX);
			IsDerivedTypesChanged = true;
		}

		public bool GetMethod(string key, out MethodX metX)
		{

		}

		public void AddMethod(string key, MethodX metX)
		{

		}
	}
}
