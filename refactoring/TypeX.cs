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

		// 类型全名
		public readonly string DefFullName;
		// 类型属性
		public readonly TypeAttributes DefAttr;
		// 是否为值类型
		public readonly bool IsValueType;

		// 唯一名称
		private string NameKey;

		// 继承的类型
		private readonly HashSet<TypeX> DerivedTypes = new HashSet<TypeX>();
		public bool IsDerivedTypesChanged { get; private set; }

		// 方法映射
		private readonly Dictionary<string, MethodX> MethodMap = new Dictionary<string, MethodX>();
		// 字段映射
		private readonly Dictionary<string, FieldX> FieldMap = new Dictionary<string, FieldX>();

		// 是否实例化过
		public OnceBool IsInstantiated;

		internal TypeX(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
			DefFullName = tyDef.FullName;
			DefAttr = tyDef.Attributes;
			IsValueType = tyDef.IsValueType;
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
	}
}
