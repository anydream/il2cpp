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
				sb.Append(kv.Key);
				sb.Append('\n');
				foreach (var kv2 in kv.Value.VSlotMap)
				{
					sb.AppendFormat("- {0}: {1}\n", kv2.Key, kv2.Value.Impl);
					foreach (var kv3 in kv2.Value.Entries)
						sb.AppendFormat("  | {0} -> {1}\n", kv3.Key, kv3.Value);
				}
			}
		}
	}
}
