using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public class HierarchyDump
	{
		private readonly Il2cppContext Context;

		public HierarchyDump(Il2cppContext context)
		{
			Context = context;
		}

		// 导出方法表结构
		public void DumpMethodTables(StringBuilder sb)
		{
			foreach (var kv in Context.TypeMgr.MethodTableMap)
			{
				sb.AppendFormat("[{0}]\n", kv.Key);
				foreach (var kv2 in kv.Value.VSlotMap)
				{
					string expSigName = kv2.Key;
					var entries = kv2.Value.Entries;
					VirtualImpl impl = kv2.Value.Impl;

					// 跳过无覆盖的方法
					if (entries.Count == 1 &&
						impl.IsValid() &&
						entries.TryGetValue(impl.ImplTable, out var implDef) &&
						implDef == impl.ImplMethod)
					{
						continue;
					}

					sb.AppendFormat("- {0}: {1}\n", expSigName, impl);
					foreach (var kv3 in entries)
						sb.AppendFormat("  - {0} -> {1}\n", kv3.Key, kv3.Value);
					sb.Append('\n');
				}
				sb.Append('\n');
			}
		}

		public void DumpTypes(StringBuilder sb)
		{
			foreach (var kv in Context.TypeMgr.TypeMap)
			{
				TypeX tyX = kv.Value;
				sb.AppendFormat("[{0} {1}] {2}\n",
					tyX.IsValueType ? "struct" : "class",
					kv.Key,
					TypeAttrToString(tyX.DefAttr));

				if (tyX.IsInstantiated)
					sb.Append("- Instantiated\n");

				if (tyX.BaseType != null)
					sb.AppendFormat("- Base: {0}\n", tyX.BaseType);
				if (tyX.Interfaces.Count > 0)
				{
					sb.Append("- Interfaces:\n");
					foreach (TypeX infTyX in tyX.Interfaces)
						sb.AppendFormat("  - {0}\n", infTyX);
				}
				if (tyX.DerivedTypes.Count > 0)
				{
					sb.Append("- DerivedTypes:\n");
					foreach (TypeX derivedTyX in tyX.DerivedTypes)
						sb.AppendFormat("  - {0}\n", derivedTyX);
				}

				if (tyX.FieldMap.Count > 0)
				{
					sb.Append("- Fields:\n");
					foreach (FieldX fldX in tyX.FieldMap.Values)
					{
						sb.AppendFormat("  - {0}, {1}, {2}\n",
							fldX.GetNameKey(),
							fldX.GetReplacedNameKey(),
							fldX.DefAttr);
					}
				}

				if (tyX.MethodMap.Count > 0)
				{
					sb.Append("- Methods:\n");
					foreach (MethodX metX in tyX.MethodMap.Values)
					{
						sb.AppendFormat("  - {0}, {1}, {2}\n",
							metX.GetNameKey(),
							metX.GetReplacedNameKey(),
							metX.DefAttr);

						if (metX.HasOverrideImpls)
						{
							foreach (MethodX overMetX in metX.OverrideImpls)
							{
								sb.AppendFormat("    - {0}, {1}\n",
									overMetX,
									overMetX.GetReplacedNameKey());
							}
						}
					}
				}


				sb.Append('\n');
			}
		}

		private static string TypeAttrToString(TypeAttributes tyAttr)
		{
			StringBuilder sb = new StringBuilder();
			switch (tyAttr & TypeAttributes.VisibilityMask)
			{
				case TypeAttributes.NotPublic:
					sb.Append("NotPublic ");
					break;

				case TypeAttributes.Public:
					sb.Append("Public ");
					break;

				case TypeAttributes.NestedPublic:
					sb.Append("NestedPublic ");
					break;

				case TypeAttributes.NestedPrivate:
					sb.Append("NestedPrivate ");
					break;

				case TypeAttributes.NestedFamily:
					sb.Append("NestedFamily ");
					break;

				case TypeAttributes.NestedAssembly:
					sb.Append("NestedAssembly ");
					break;

				case TypeAttributes.NestedFamANDAssem:
					sb.Append("NestedFamANDAssem ");
					break;

				case TypeAttributes.NestedFamORAssem:
					sb.Append("NestedFamORAssem ");
					break;
			}

			switch (tyAttr & TypeAttributes.LayoutMask)
			{
				case TypeAttributes.AutoLayout:
					sb.Append("AutoLayout ");
					break;

				case TypeAttributes.SequentialLayout:
					sb.Append("SequentialLayout ");
					break;

				case TypeAttributes.ExplicitLayout:
					sb.Append("ExplicitLayout ");
					break;
			}

			sb.Append((tyAttr & TypeAttributes.Interface) != 0 ? "Interface " : "");
			sb.Append((tyAttr & TypeAttributes.Abstract) != 0 ? "Abstract " : "");
			sb.Append((tyAttr & TypeAttributes.Sealed) != 0 ? "Sealed " : "");
			sb.Append((tyAttr & TypeAttributes.SpecialName) != 0 ? "SpecialName " : "");
			sb.Append((tyAttr & TypeAttributes.Import) != 0 ? "Import " : "");
			sb.Append((tyAttr & TypeAttributes.Serializable) != 0 ? "Serializable " : "");
			sb.Append((tyAttr & TypeAttributes.WindowsRuntime) != 0 ? "WindowsRuntime " : "");

			switch (tyAttr & TypeAttributes.StringFormatMask)
			{
				case TypeAttributes.AnsiClass:
					sb.Append("AnsiClass ");
					break;

				case TypeAttributes.UnicodeClass:
					sb.Append("UnicodeClass ");
					break;

				case TypeAttributes.AutoClass:
					sb.Append("AutoClass ");
					break;

				case TypeAttributes.CustomFormatClass:
					sb.Append("CustomFormatClass ");
					break;
			}

			sb.Append((tyAttr & TypeAttributes.BeforeFieldInit) != 0 ? "BeforeFieldInit " : "");
			sb.Append((tyAttr & TypeAttributes.Forwarder) != 0 ? "Forwarder " : "");
			sb.Append((tyAttr & TypeAttributes.RTSpecialName) != 0 ? "RTSpecialName " : "");
			sb.Append((tyAttr & TypeAttributes.HasSecurity) != 0 ? "HasSecurity " : "");

			return sb.ToString();
		}

		private static string MethodAttrToString(MethodAttributes metAttr)
		{
			StringBuilder sb = new StringBuilder();

			switch (metAttr & MethodAttributes.MemberAccessMask)
			{
				case MethodAttributes.PrivateScope:
					sb.Append("PrivateScope ");
					break;

				case MethodAttributes.Private:
					sb.Append("Private ");
					break;

				case MethodAttributes.FamANDAssem:
					sb.Append("FamANDAssem ");
					break;

				case MethodAttributes.Assembly:
					sb.Append("Assembly ");
					break;

				case MethodAttributes.Family:
					sb.Append("Family ");
					break;

				case MethodAttributes.FamORAssem:
					sb.Append("FamORAssem ");
					break;

				case MethodAttributes.Public:
					sb.Append("Public ");
					break;
			}

			sb.Append((metAttr & MethodAttributes.Static) != 0 ? "Static " : "");
			sb.Append((metAttr & MethodAttributes.Final) != 0 ? "Final " : "");
			sb.Append((metAttr & MethodAttributes.Virtual) != 0 ? "Virtual " : "");
			sb.Append((metAttr & MethodAttributes.HideBySig) != 0 ? "HideBySig " : "");
			sb.Append((metAttr & MethodAttributes.NewSlot) != 0 ? "NewSlot " : "");
			sb.Append((metAttr & MethodAttributes.CheckAccessOnOverride) != 0 ? "CheckAccessOnOverride " : "");
			sb.Append((metAttr & MethodAttributes.Abstract) != 0 ? "Abstract " : "");
			sb.Append((metAttr & MethodAttributes.SpecialName) != 0 ? "SpecialName " : "");
			sb.Append((metAttr & MethodAttributes.PinvokeImpl) != 0 ? "PinvokeImpl " : "");
			sb.Append((metAttr & MethodAttributes.UnmanagedExport) != 0 ? "UnmanagedExport " : "");
			sb.Append((metAttr & MethodAttributes.RTSpecialName) != 0 ? "RTSpecialName " : "");
			sb.Append((metAttr & MethodAttributes.HasSecurity) != 0 ? "HasSecurity " : "");
			sb.Append((metAttr & MethodAttributes.RequireSecObject) != 0 ? "RequireSecObject " : "");

			return sb.ToString();
		}
	}
}
