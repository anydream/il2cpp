using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 栈槽类型
	internal enum StackType
	{
		I4,
		I8,
		R4,
		R8,
		Ptr,
		Obj,
		Ref
	}

	internal struct SlotInfo
	{
		public StackType SlotType;
		public int StackIndex;
	}

	// 包装的指令
	internal class InstructionInfo
	{
		public Instruction Inst;
		public Code Code;
		public object Operand;
		public int Index;

		public bool IsProcessed;
		public string CppCode;

		public InstructionInfo(int index, Instruction inst)
		{
			Inst = inst;
			Code = inst.OpCode.Code;
			Operand = inst.Operand;
			Index = index;
		}

		public override string ToString()
		{
			return CppCode ?? string.Format("{0}: {1} {2}{3}", Index, Code, Operand, IsProcessed ? " √" : "");
		}
	}

	// 执行栈
	internal class EvalStack
	{
		private readonly ICorLibTypes CorTypes;

		private MethodX TargetMethod;
		private Func<Instruction, object> OperandResolver;

		// 指令列表
		private InstructionInfo[] InstList;

		// 当前类型栈
		private Stack<StackType> TypeStack = new Stack<StackType>();
		// 栈槽类型映射
		private Dictionary<int, HashSet<StackType>> StackTypeMap = new Dictionary<int, HashSet<StackType>>();
		// 待处理的分支
		private readonly Queue<Tuple<int, Stack<StackType>>> Branches = new Queue<Tuple<int, Stack<StackType>>>();

		public EvalStack(ICorLibTypes corTypes)
		{
			CorTypes = corTypes;
		}

		private void AddStackTypeMap(ref SlotInfo sinfo)
		{
			if (!StackTypeMap.TryGetValue(sinfo.StackIndex, out var typeSet))
			{
				typeSet = new HashSet<StackType>();
				StackTypeMap.Add(sinfo.StackIndex, typeSet);
			}
			typeSet.Add(sinfo.SlotType);
		}

		private SlotInfo Push(StackType stype)
		{
			SlotInfo sinfo = new SlotInfo();
			sinfo.StackIndex = TypeStack.Count;
			sinfo.SlotType = stype;
			TypeStack.Push(stype);
			AddStackTypeMap(ref sinfo);
			return sinfo;
		}

		private SlotInfo Pop()
		{
			StackType stype = TypeStack.Pop();
			SlotInfo sinfo = new SlotInfo();
			sinfo.StackIndex = TypeStack.Count;
			sinfo.SlotType = stype;
			return sinfo;
		}

		private SlotInfo[] Pop(int num)
		{
			SlotInfo[] sinfos = new SlotInfo[num];
			for (int i = 0; i < num; ++i)
				sinfos[i] = Pop();

			return sinfos;
		}

		private void Dup()
		{
			TypeStack.Push(TypeStack.Peek());
		}

		private void AddBranch(int targetIP)
		{
			Branches.Enqueue(new Tuple<int, Stack<StackType>>(targetIP, new Stack<StackType>(TypeStack)));
		}

		private void Reset()
		{
			InstList = null;
			TypeStack.Clear();
			StackTypeMap.Clear();
			Branches.Clear();
		}

		public void Process(MethodX metX, Func<Instruction, object> operandRes)
		{
			Debug.Assert(metX.Def.HasBody);

			Reset();

			TargetMethod = metX;
			OperandResolver = operandRes;

			// 转换为自定义类型的指令列表
			var origInstList = metX.Def.Body.Instructions;
			InstList = new InstructionInfo[origInstList.Count];

			Dictionary<uint, int> offsetMap = new Dictionary<uint, int>();
			List<int> targetFixup = new List<int>();

			for (int i = 0; i < InstList.Length; ++i)
			{
				var origInst = origInstList[i];
				InstList[i] = new InstructionInfo(i, origInst);

				offsetMap.Add(origInst.Offset, i);

				if (origInst.OpCode.OperandType == OperandType.InlineBrTarget ||
					origInst.OpCode.OperandType == OperandType.ShortInlineBrTarget ||
					origInst.OpCode.OperandType == OperandType.InlineSwitch)
				{
					targetFixup.Add(i);
				}
			}
			foreach (int fixIndex in targetFixup)
			{
				var operand = InstList[fixIndex].Operand;
				if (operand is Instruction targetInst)
					InstList[fixIndex].Operand = offsetMap[targetInst.Offset];
				else
				{
					var targetInstList = (Instruction[])operand;
					int[] targetIndices = new int[targetInstList.Length];
					for (int i = 0; i < targetInstList.Length; ++i)
						targetIndices[i] = offsetMap[targetInstList[i].Offset];

					InstList[fixIndex].Operand = targetIndices;
				}
			}

			offsetMap = null;
			targetFixup = null;

			// 开始模拟执行
			int currIP = 0;
			int nextIP = 0;
			for (;;)
			{
				if (ProcessStep(currIP, ref nextIP))
					currIP = nextIP;
				else if (Branches.Count > 0)
				{
					var branch = Branches.Dequeue();
					currIP = branch.Item1;
					TypeStack = branch.Item2;
				}
				else
					break;
			}
		}

		private bool ProcessStep(int currIP, ref int nextIP)
		{
			var iinfo = InstList[currIP];
			if (iinfo.IsProcessed)
				return false;
			iinfo.IsProcessed = true;

			switch (iinfo.Code)
			{
				case Code.Nop:
					break;

				case Code.Pop:
					Pop();
					break;

				case Code.Dup:
					Dup();
					break;

				default:
					ProcessInstruction(iinfo);
					break;
			}

			// 计算下一指令位置
			switch (iinfo.Inst.OpCode.FlowControl)
			{
				case FlowControl.Branch:
					nextIP = (int)iinfo.Operand;
					return true;

				case FlowControl.Cond_Branch:
					if (iinfo.Code == Code.Switch)
					{
						int[] targetList = (int[])iinfo.Operand;
						foreach (int targetIP in targetList)
							AddBranch(targetIP);
					}
					else
					{
						int targetIP = (int)iinfo.Operand;
						AddBranch(targetIP);
					}
					nextIP = currIP + 1;
					return true;

				case FlowControl.Break:
				case FlowControl.Call:
				case FlowControl.Meta:
				case FlowControl.Next:
					nextIP = currIP + 1;
					return true;

				case FlowControl.Return:
				case FlowControl.Throw:
					return false;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}

		private void ProcessInstruction(InstructionInfo iinfo)
		{
			// 处理无需解析操作数的指令
			switch (iinfo.Code)
			{
				case Code.Ldc_I4_M1:
					Load(iinfo, StackType.I4, "-1");
					return;
				case Code.Ldc_I4_0:
					Load(iinfo, StackType.I4, "0");
					return;
				case Code.Ldc_I4_1:
					Load(iinfo, StackType.I4, "1");
					return;
				case Code.Ldc_I4_2:
					Load(iinfo, StackType.I4, "2");
					return;
				case Code.Ldc_I4_3:
					Load(iinfo, StackType.I4, "3");
					return;
				case Code.Ldc_I4_4:
					Load(iinfo, StackType.I4, "4");
					return;
				case Code.Ldc_I4_5:
					Load(iinfo, StackType.I4, "5");
					return;
				case Code.Ldc_I4_6:
					Load(iinfo, StackType.I4, "6");
					return;
				case Code.Ldc_I4_7:
					Load(iinfo, StackType.I4, "7");
					return;
				case Code.Ldc_I4_8:
					Load(iinfo, StackType.I4, "8");
					return;
				case Code.Ldc_I4_S:
					Load(iinfo, StackType.I4, ((sbyte)iinfo.Operand).ToString());
					return;
				case Code.Ldc_I4:
					Load(iinfo, StackType.I4, ((int)iinfo.Operand).ToString());
					return;
				case Code.Ldc_I8:
					Load(iinfo, StackType.I8, ((long)iinfo.Operand).ToString());
					return;
				case Code.Ldc_R4:
					Load(iinfo, StackType.R4, ((float)iinfo.Operand).ToString(CultureInfo.InvariantCulture));
					return;
				case Code.Ldc_R8:
					Load(iinfo, StackType.R8, ((double)iinfo.Operand).ToString(CultureInfo.InvariantCulture));
					return;

				case Code.Ret:
					if (TypeStack.Count > 0)
					{
						Debug.Assert(TypeStack.Count == 1);
						SlotInfo poped = Pop();
						iinfo.CppCode = string.Format("return {0}", SlotInfoName(ref poped));
					}
					else
					{
						iinfo.CppCode = "return";
					}
					return;
			}

			// 解析操作数
			object operand = OperandResolver(iinfo.Inst);
			switch (iinfo.Code)
			{
				case Code.Call:
				case Code.Callvirt:
					{
						MethodX metX = (MethodX)operand;

						int popCount = metX.ParamTypes.Count;
						if (!metX.IsStatic)
							++popCount;

						Call(iinfo,
							metX.GetCppName(iinfo.Code == Code.Callvirt),
							popCount,
							metX.ReturnType);
					}
					return;
			}
		}

		private string ArgName(int argID)
		{
			return "arg_" + argID;
		}

		private string LocalName(int localID)
		{
			return "loc_" + localID;
		}

		private string SlotInfoName(ref SlotInfo sinfo)
		{
			return string.Format("tmp_{0}_{1}", sinfo.StackIndex, sinfo.SlotType);
		}

		private void Load(InstructionInfo iinfo, StackType stype, string rval)
		{
			SlotInfo pushed = Push(stype);
			iinfo.CppCode = string.Format("{0} = {1}", SlotInfoName(ref pushed), rval);
		}

		private void Store(InstructionInfo iinfo, string lval, string cast = null)
		{
			SlotInfo poped = Pop();
			iinfo.CppCode = string.Format("{0} = {1}{2}", lval, cast != null ? "(" + cast + ")" : "", SlotInfoName(ref poped));
		}

		private void Call(InstructionInfo iinfo, string metName, int popCount, TypeSig retType)
		{
			SlotInfo[] popList = Pop(popCount);

			StringBuilder sb = new StringBuilder();
			if (!SigHelper.IsVoidSig(retType))
			{
				SlotInfo pushed = Push(ToStackType(retType));
				sb.AppendFormat("{0} = ", SlotInfoName(ref pushed));
			}

			sb.AppendFormat("{0}(", metName);

			bool last = false;
			for (int i = 0; i < popList.Length; ++i)
			{
				if (last)
					sb.Append(", ");
				last = true;

				var arg = popList[i];
				sb.Append(SlotInfoName(ref arg));
			}

			sb.Append(')');

			iinfo.CppCode = sb.ToString();
		}

		private StackType ToStackType(TypeSig sig)
		{
			if (sig.Equals(CorTypes.SByte) ||
				sig.Equals(CorTypes.Byte) ||
				sig.Equals(CorTypes.Int16) ||
				sig.Equals(CorTypes.UInt16) ||
				sig.Equals(CorTypes.Int32) ||
				sig.Equals(CorTypes.UInt32) ||
				sig.Equals(CorTypes.Boolean))
			{
				return StackType.I4;
			}
			if (sig.Equals(CorTypes.Int64) ||
				sig.Equals(CorTypes.UInt64))
			{
				return StackType.I8;
			}
			if (sig.Equals(CorTypes.Single))
			{
				return StackType.R4;
			}
			if (sig.Equals(CorTypes.Double))
			{
				return StackType.R8;
			}
			if (sig.IsPointer ||
				sig.Equals(CorTypes.IntPtr) ||
				sig.Equals(CorTypes.UIntPtr))
			{
				return StackType.Ptr;
			}
			if (sig.IsByRef)
			{
				return StackType.Ref;
			}
			return StackType.Obj;
		}
	}
}
