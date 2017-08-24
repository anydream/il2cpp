namespace il2cpp
{
	internal class NameManager
	{
		// 当前环境
		private readonly Il2cppContext Context;

		internal uint NameCounter;
		internal uint TypeIdCounter;

		internal const string ClassPrefix = "cls_";
		internal const string StructPrefix = "stru_";
		internal const string FieldPrefix = "fld_";
		internal const string MethodPrefix = "met_";
		internal const string VMethodPrefix = "vmet_";
		internal const string VFuncPrefix = "vftn_";
		internal const string ICallPrefix = "icall_";
		internal const string TempPrefix = "tmp_";
		internal const string LocalPrefix = "loc_";
		internal const string ArgPrefix = "arg_";

		public NameManager(Il2cppContext context)
		{
			Context = context;
		}
	}
}
