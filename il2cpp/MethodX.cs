using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 指令
	internal class InstInfo
	{
		public OpCode OpCode;
		public object Operand;
		public int Offset;

		public bool IsBrTarget;
		public bool IsGenerated;
		public string InstCode;

		public override string ToString()
		{
			if (Operand is int[] iary)
			{
				string p = null;
				for (int i = 0; i < iary.Length; i++)
				{
					if (i != 0)
						p += ',';
					p += iary[i];
				}
				return OpCode + " " + p;
			}

			if (Operand is string str)
			{
				str = Helper.EscapeString(str);
				return OpCode + " " + str + ' ';
			}

			string strOperand = Operand?.ToString();
			if (string.IsNullOrEmpty(strOperand))
				return OpCode.ToString();

			return OpCode + " " + strOperand + ' ';
		}
	}

	// 异常处理信息
	internal class ExHandlerInfo
	{
		public int TryStart;
		public int TryEnd;
		public int FilterStart;
		public int HandlerStart;
		public int HandlerEnd;
		public TypeX CatchType;
		public ExceptionHandlerType HandlerType;

		public readonly List<ExHandlerInfo> CombinedHandlers = new List<ExHandlerInfo>();
		public HashSet<int> LeaveTargets;

		public int HandlerOrFilterStart => FilterStart != -1 ? FilterStart : HandlerStart;

		public bool NeedCombine(ExHandlerInfo other)
		{
			return TryStart == other.TryStart &&
				   TryEnd == other.TryEnd &&
				   (HandlerType == ExceptionHandlerType.Catch || HandlerType == ExceptionHandlerType.Filter) &&
				   (other.HandlerType == ExceptionHandlerType.Catch || other.HandlerType == ExceptionHandlerType.Filter);
		}

		public void AddLeaveTarget(int target)
		{
			if (LeaveTargets == null)
				LeaveTargets = new HashSet<int>();
			LeaveTargets.Add(target);
		}
	}

	internal class MethodX : GenericArgs
	{
		// 所属类型
		public readonly TypeX DeclType;

		// 方法定义
		public readonly MethodDef Def;
		// 方法签名
		public MethodSig DefSig => Def.MethodSig;
		// 是否为虚方法
		public bool IsVirtual => Def.IsVirtual;

		// 唯一名称
		private string NameKey;

		// 返回值类型
		public TypeSig ReturnType;
		// 参数类型列表, 包含 this 类型
		public IList<TypeSig> ParamTypes;
		public IList<TypeSig> ParamAfterSentinel;

		// 局部变量类型列表
		public IList<TypeSig> LocalTypes;

		// 异常处理器列表
		public List<ExHandlerInfo> ExHandlerList;

		// 指令列表
		public InstInfo[] InstList;

		// 虚方法绑定的实现方法
		public Dictionary<MethodX, HashSet<TypeX>> OverrideImpls;
		public bool HasOverrideImpls => OverrideImpls.IsCollectionValid();

		// 是否已处理过
		public bool IsProcessed;
		// 是否跳过处理 (1: true, 2: false)
		private byte SkipProcessingFlag;
		public bool IsSkipProcessing
		{
			get => SkipProcessingFlag == 1;
			set
			{
				if (!value)
					SkipProcessingFlag = 2;
				else if (SkipProcessingFlag == 0)
					SkipProcessingFlag = 1;
			}
		}

		// 生成的方法名称
		public string GeneratedMethodName;
		// 是否需要生成元数据
		public bool NeedGenMetadata;

		public bool HasThis
		{
			get
			{
				if (DefSig.HasThis)
				{
					Debug.Assert(!Def.IsStatic);
					return true;
				}
				return false;
			}
		}

		public bool IsStatic
		{
			get
			{
				if (Def.IsStatic)
				{
					Debug.Assert(!DefSig.HasThis);
					return true;
				}
				return false;
			}
		}

		public MethodX(TypeX declType, MethodDef metDef)
		{
			Debug.Assert(declType != null);
			Debug.Assert(metDef.DeclaringType == declType.Def);
			DeclType = declType;
			Def = metDef;

			Debug.Assert(HasThis && !metDef.IsStatic || !HasThis && metDef.IsStatic);
		}

		public override string ToString()
		{
			return DeclType + " -> " + NameKey;
		}

		public string GetNameKey()
		{
			if (NameKey == null)
			{
				// Name|RetType<GenArgs>(DefArgList)|CC
				StringBuilder sb = new StringBuilder();
				Helper.MethodNameKeyWithGen(sb, Def.Name, GenArgs, DefSig.RetType, DefSig.Params, DefSig.CallingConvention);

				NameKey = sb.ToString();
			}
			return NameKey;
		}

		public string GetReplacedNameKey()
		{
			Debug.Assert(ReturnType != null);
			Debug.Assert(ParamTypes != null);

			StringBuilder sb = new StringBuilder();
			Helper.MethodNameKeyWithGen(sb, Def.Name, GenArgs, ReturnType, ParamTypes, DefSig.CallingConvention);

			return sb.ToString();
		}

		public void AddOverrideImpl(MethodX implMetX, TypeX implTyX)
		{
			if (OverrideImpls == null)
				OverrideImpls = new Dictionary<MethodX, HashSet<TypeX>>();

			if (!OverrideImpls.TryGetValue(implMetX, out var implTypeSet))
			{
				implTypeSet = new HashSet<TypeX>();
				OverrideImpls.Add(implMetX, implTypeSet);
			}
			implTypeSet.Add(implTyX);
		}
	}
}
