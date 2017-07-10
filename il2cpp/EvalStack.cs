using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 指令数据
	internal struct InstructionInfo
	{
		public Instruction Inst;
		public bool IsProcessed;
		public string CppCode;

		public InstructionInfo(Instruction inst)
		{
			Inst = inst;
			IsProcessed = false;
			CppCode = null;
		}

		public override string ToString()
		{
			return CppCode ?? string.Format("{0}{1}", Inst, IsProcessed ? " √" : "");
		}
	}

	internal struct SlotInfo
	{
		public object TypeObj;
		public int StackID;
	}

	// 执行栈
	internal class EvalStack
	{
		private MethodX TargetMethod;
		private Func<Instruction, object> ResolverFunc;

		// 当前类型栈
		private Stack<object> CurrStack = new Stack<object>();
		// 栈槽类型映射. 用于构造临时变量声明
		private readonly Dictionary<int, Dictionary<object, int>> StackSlotMap = new Dictionary<int, Dictionary<object, int>>();
		// 指令偏移索引映射
		private readonly Dictionary<uint, int> OffsetIndexMap = new Dictionary<uint, int>();
		// 指令信息映射
		private readonly List<InstructionInfo> InstInfoList = new List<InstructionInfo>();
		// 暂存的其他分支
		private readonly Queue<Tuple<int, Stack<object>>> PendingBranches = new Queue<Tuple<int, Stack<object>>>();

		private void AddStackSlotMap(object type, int stackID)
		{
			if (!StackSlotMap.TryGetValue(stackID, out var typeIndexMap))
			{
				typeIndexMap = new Dictionary<object, int>();
				StackSlotMap.Add(stackID, typeIndexMap);
			}
			if (!typeIndexMap.ContainsKey(type))
				typeIndexMap.Add(type, typeIndexMap.Count);
		}

		private int GetTypeID(object type, int stackID)
		{
			if (StackSlotMap.TryGetValue(stackID, out var typeIndexMap))
			{
				if (typeIndexMap.TryGetValue(type, out int typeID))
					return typeID;
			}
			return -1;
		}

		private int Push(object type)
		{
			int stackID = CurrStack.Count;
			CurrStack.Push(type);
			AddStackSlotMap(type, stackID);
			return stackID;
		}

		private void Push(object type, out SlotInfo sinfo)
		{
			sinfo.StackID = Push(type);
			sinfo.TypeObj = type;
		}

		private bool Pop(out SlotInfo sinfo)
		{
			if (CurrStack.Count > 0)
			{
				sinfo.TypeObj = CurrStack.Pop();
				sinfo.StackID = CurrStack.Count;
				return true;
			}
			sinfo = new SlotInfo();
			return false;
		}

		private SlotInfo[] Pop(int num)
		{
			SlotInfo[] result = new SlotInfo[num];
			for (int i = 0; i < num; ++i)
			{
				bool status = Pop(out result[i]);
				Debug.Assert(status);
			}
			return result;
		}

		private void Pop()
		{
			CurrStack.Pop();
		}

		private void Dup()
		{
			CurrStack.Push(CurrStack.Peek());
		}

		private void AddPendingBranch(int targetIP)
		{
			PendingBranches.Enqueue(new Tuple<int, Stack<object>>(targetIP, new Stack<object>(CurrStack)));
		}

		public void Reset()
		{
			CurrStack.Clear();
			StackSlotMap.Clear();
			OffsetIndexMap.Clear();
			InstInfoList.Clear();
			PendingBranches.Clear();
		}

		public void Process(MethodX metX, Func<Instruction, object> resolver)
		{
			Debug.Assert(metX.Def.HasBody);
			Reset();

			TargetMethod = metX;
			ResolverFunc = resolver;

			// 包装指令列表
			var instList = metX.Def.Body.Instructions;
			for (int i = 0; i < instList.Count; ++i)
			{
				var inst = instList[i];
				OffsetIndexMap[inst.Offset] = i;
				InstInfoList.Add(new InstructionInfo(inst));
			}

			// 处理循环
			int currIP = 0;
			int nextIP = -1;
			for (;;)
			{
				if (ProcessStep(currIP, ref nextIP))
					currIP = nextIP;
				else if (PendingBranches.Count > 0)
				{
					// 处理其他分支
					var branch = PendingBranches.Dequeue();
					currIP = branch.Item1;
					CurrStack = branch.Item2;
				}
				else
					break;
			}

			//! 构造方法体代码, 构造虚调用代码
		}

		private bool ProcessStep(int currIP, ref int nextIP)
		{
			var instInfo = InstInfoList[currIP];
			// 跳过已处理项
			if (instInfo.IsProcessed)
				return false;

			instInfo.IsProcessed = true;
			var inst = instInfo.Inst;

			switch (inst.OpCode.Code)
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
					ProcessInstruction(instInfo);
					break;
			}

			switch (inst.OpCode.FlowControl)
			{
				case FlowControl.Branch:
					nextIP = OffsetIndexMap[((Instruction)inst.Operand).Offset];
					return true;

				case FlowControl.Cond_Branch:
					{
						if (inst.OpCode.Code == Code.Switch)
						{
							throw new NotImplementedException();
						}
						else
						{
							int targetIP = OffsetIndexMap[((Instruction)inst.Operand).Offset];
							AddPendingBranch(targetIP);
						}
						nextIP = currIP + 1;
						return true;
					}

				case FlowControl.Return:
				case FlowControl.Throw:
					return false;

				case FlowControl.Break:
				case FlowControl.Call:
				case FlowControl.Meta:
				case FlowControl.Next:
					nextIP = currIP + 1;
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}

		private void ProcessInstruction(InstructionInfo instInfo)
		{
			var inst = instInfo.Inst;

			// 处理通用的出栈逻辑
			SlotInfo[] popList = null;
			switch (inst.OpCode.StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					break;

				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					popList = Pop(1);
					break;

				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					popList = Pop(2);
					break;

				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
				case StackBehaviour.Popref_popi_pop1:
					popList = Pop(3);
					break;

				case StackBehaviour.Varpop:
					// 动态出栈由具体指令来处理
					break;

				case StackBehaviour.PopAll:
					CurrStack.Clear();
					break;

				default:
					Debug.Fail("StackBehaviourPop " + inst.OpCode.StackBehaviourPop);
					break;
			}

			// 处理无操作数的指令
			switch (inst.OpCode.Code)
			{
				case Code.Ret:
					{
						if (CurrStack.Count > 0)
						{
							Debug.Assert(CurrStack.Count == 1);

							SlotInfo poped;
							Pop(out poped);

							instInfo.CppCode = "return " + SlotInfoToCode(ref poped);
						}
						else
						{
							instInfo.CppCode = "return";
						}
						return;
					}
			}

			// 解析操作数
			object operand = ResolverFunc(inst);
			switch (inst.OpCode.Code)
			{
				case Code.Call:
				case Code.Callvirt:
					{
						MethodX metX = (MethodX)operand;

						int popCount = metX.ParamTypes.Count;
						if (!metX.IsStatic)
							++popCount;

						instInfo.CppCode = CallToCode(
							popCount,
							metX.GetCppName(inst.OpCode.Code == Code.Callvirt),
							metX.ReturnType);

						return;
					}
			}
		}

		private string CallToCode(int popCount, string metName, TypeSig retType)
		{
			SlotInfo[] popList = Pop(popCount);

			StringBuilder sb = new StringBuilder();
			if (!SigHelper.IsVoidSig(retType))
			{
				Push(retType, out var pushed);
				sb.AppendFormat("{0} = ", SlotInfoToCode(ref pushed));
			}

			sb.AppendFormat("{0}(", metName);

			bool last = false;
			for (int i = 0; i < popList.Length; ++i)
			{
				if (last)
					sb.Append(", ");
				last = true;
				sb.Append(SlotInfoToCode(ref popList[i]));
			}

			sb.Append(')');

			return sb.ToString();
		}
		
		private string SlotInfoToCode(ref SlotInfo sinfo)
		{
			int typeID = GetTypeID(sinfo.TypeObj, sinfo.StackID);
			Debug.Assert(typeID >= 0);
			return string.Format("tmp_{0}_{1}", sinfo.StackID, typeID);
		}
	}
}
