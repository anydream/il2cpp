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

		// 方法映射
		public readonly Dictionary<string, MethodX> MethodMap = new Dictionary<string, MethodX>();
		// 字段映射
		public readonly Dictionary<string, FieldX> FieldMap = new Dictionary<string, FieldX>();

		// 虚表
		public VirtualTable VTable;

		// 静态构造方法
		public MethodX CctorMethod;
		// 终结器方法
		public MethodX FinalizerMethod;

		// 是否实例化过
		public bool IsInstantiated;

		// 是否已生成静态构造
		public bool IsCctorGenerated;
		// 是否已生成终结器
		public bool IsFinalizerGenerated;

		public TypeX(Il2cppContext context, TypeDef tyDef)
		{
			Context = context;
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
				Helper.TypeNameKey(sb, Def, GenArgs);
				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public TypeSig GetThisTypeSig()
		{
			ClassOrValueTypeSig tySig;
			if (IsValueType)
				tySig = new ValueTypeSig(Def);
			else
				tySig = new ClassSig(Def);

			TypeSig thisSig = tySig;
			if (HasGenArgs)
				thisSig = new GenericInstSig(tySig, GenArgs);
			if (IsValueType)
				thisSig = new ByRefSig(thisSig);
			return thisSig;
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

		public void ResolveVTable()
		{
			if (VTable == null)
			{
				MethodTable mtable = Context.TypeMgr.ResolveMethodTable(Def, null);
				VTable = mtable.ExpandVTable(GenArgs);
			}
		}

		public bool QueryVTable(
			string entryType, MethodDef entryDef,
			out string implType, out MethodDef implDef)
		{
			return VTable.Query(entryType, entryDef, out implType, out implDef);
		}

		public MethodDef IsMethodReplaced(MethodDef metDef)
		{
			ResolveVTable();
			return VTable.IsMethodReplaced(metDef);
		}
	}
}
