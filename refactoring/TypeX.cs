using System.Collections.Generic;
using dnlib.DotNet;

namespace il2cpp
{
	internal class TypeX
	{
		// 当前环境
		private readonly Il2cppContext Context;

		// 全局唯一的名称键
		public readonly string NameKey;

		// 继承的类型
		private readonly HashSet<TypeX> DerivedTypes = new HashSet<TypeX>();
		public bool IsDerivedTypesChanged { get; private set; }

		// 是否实例化过
		public OnceBool IsInstantiated;

		internal TypeX(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
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
	}
}
