using System.Text;

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
					tyX.DefAttr);

				if (tyX.BaseType != null)
					sb.AppendFormat(" - Base: {0}\n", tyX.BaseType);
				foreach (TypeX infTyX in tyX.Interfaces)
					sb.AppendFormat(" - Interface: {0}\n", infTyX);
				foreach (TypeX derivedTyX in tyX.DerivedTypes)
					sb.AppendFormat(" - DerivedType: {0}\n", derivedTyX);

				sb.Append(" - Methods:\n");
				foreach (MethodX metX in tyX.MethodMap.Values)
				{
					sb.AppendFormat("   - {0}, {1}, {2}\n",
						metX.GetNameKey(),
						metX.GetReplacedNameKey(),
						metX.DefAttr);

					if (metX.HasOverrideImpls)
					{
						foreach (MethodX overMetX in metX.OverrideImpls)
						{
							sb.AppendFormat("     - Impl: {0}, {1}\n",
								overMetX,
								overMetX.GetReplacedNameKey());
						}
					}
				}

				sb.Append(" - Fields:\n");
				foreach (FieldX fldX in tyX.FieldMap.Values)
				{
					sb.AppendFormat("   - {0}, {1}, {2}\n",
						fldX.GetNameKey(),
						fldX.GetReplacedNameKey(),
						fldX.DefAttr);
				}

				sb.Append('\n');
			}
		}
	}
}
