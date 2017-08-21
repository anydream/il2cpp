using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace il2cpp
{
	internal class MethodX : GenericArgs
	{
		public readonly TypeX DeclType;

		// 类型定义的全名
		public readonly string DefFullName;

		internal MethodX(TypeX declType, MethodDef metDef)
		{
			DeclType = declType;
			DefFullName = metDef.FullName;
		}
	}
}
