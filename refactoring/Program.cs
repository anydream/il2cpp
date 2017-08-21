namespace il2cpp
{
	class Program
	{
		static void Main(string[] args)
		{
			Il2cppContext context = new Il2cppContext(@"testCS.exe");
			context.AddEntry(context.Module.EntryPoint);
			context.Process();
		}
	}
}
