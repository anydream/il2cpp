using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		// 待处理的分支
		private readonly Queue<Tuple<int, Stack<StackType>>> Branches = new Queue<Tuple<int, Stack<StackType>>>();

		public EvalStack(ICorLibTypes corTypes)
		{
			CorTypes = corTypes;
		}

		private SlotInfo Push(StackType stype)
		{
			SlotInfo sinfo = new SlotInfo();
			sinfo.StackIndex = TypeStack.Count;
			sinfo.SlotType = stype;
			TypeStack.Push(stype);
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
	}
}
