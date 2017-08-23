using dnlib.DotNet;

namespace il2cpp
{
	// 对于可实例化的基类/接口, 解析其对应的 TypeX, 并获得展开后的方法签名列表
	// 对于包含类型泛型参数的不可实例化的基类/接口, 则单独构建其 MethodTable 对象, 并获得未展开的方法签名列表
	// 不可实例化的 MethodTable 放入独立的映射中管理, 以便复用
	internal class MethodTable
	{
		internal MethodTable(TypeDef tyDef)
		{
			// 收集所有非静态方法的 MethodDef, 并加入签名映射
		}
	}
}
