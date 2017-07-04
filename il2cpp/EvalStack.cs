using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 指令数据
	internal class InstInfo
	{
		public int Index;
		public bool IsProcessed;

		public InstInfo(int n)
		{
			Index = n;
		}
	}

	// 执行栈
	internal class EvalStack
	{
		// 类型名称映射
		private readonly Dictionary<string, TypeSig> TypeMap = new Dictionary<string, TypeSig>();
		// 当前类型栈
		private Stack<string> TypeStack = new Stack<string>();
		// 需要继续执行的分支指令位置和类型栈的队列
		private readonly Queue<Tuple<int, Stack<string>>> PendingBranch = new Queue<Tuple<int, Stack<string>>>();
		// 指令列表
		private readonly Dictionary<Instruction, InstInfo> InstMap = new Dictionary<Instruction, InstInfo>();

		public void Reset()
		{
			TypeMap.Clear();
			TypeStack.Clear();
			PendingBranch.Clear();
			InstMap.Clear();
		}

		private string RegType(TypeSig type)
		{
			string typeName = type.FullName;
			TypeMap[typeName] = type;
			return typeName;
		}

		private TypeSig GetType(string typeName)
		{
			if (TypeMap.TryGetValue(typeName, out var type))
				return type;
			return null;
		}

		private void Push(string typeName)
		{
			TypeStack.Push(typeName);
		}

		private void Push(TypeSig type)
		{
			TypeStack.Push(RegType(type));
		}

		private string Pop()
		{
			return TypeStack.Pop();
		}

		private void Dup()
		{
			TypeStack.Push(TypeStack.Peek());
		}

		private void AddPendingBranch(int targetIP)
		{
			PendingBranch.Enqueue(new Tuple<int, Stack<string>>(targetIP, new Stack<string>(TypeStack)));
		}

		public void Process(MethodX metX)
		{
			Debug.Assert(metX.Def.HasBody);

			Reset();

			// 构建指令列表
			var instList = metX.Def.Body.Instructions;

			// 构建指令信息映射
			for (int i = 0; i < instList.Count; ++i)
				InstMap[instList[i]] = new InstInfo(i);

			int currIP = 0;
			for (;;)
			{
				int result = ProcessInstruction(currIP, instList);
				if (result < 0 || result >= instList.Count)
				{
					if (PendingBranch.Count > 0)
					{
						// 一条路径执行完毕, 执行下一条
						var branch = PendingBranch.Dequeue();
						TypeStack = branch.Item2;
						currIP = branch.Item1;
					}
					else
						break;
				}
				else
					currIP = result;
			}
		}

		private int ProcessInstruction(int currIP, IList<Instruction> instList)
		{
			// 跳过已经处理过的指令
			var inst = instList[currIP];
			var instInfo = InstMap[inst];
			if (instInfo.IsProcessed)
			{
				// 比较栈信息
				return -1;
			}

			switch (inst.OpCode.StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					break;

				case StackBehaviour.Pop1:
					break;
				case StackBehaviour.Popi:
					break;
				case StackBehaviour.Popref:
					break;

				case StackBehaviour.Pop1_pop1:
					break;
				case StackBehaviour.Popi_pop1:
					break;
				case StackBehaviour.Popi_popi:
					break;
				case StackBehaviour.Popi_popi8:
					break;
				case StackBehaviour.Popi_popr4:
					break;
				case StackBehaviour.Popi_popr8:
					break;
				case StackBehaviour.Popref_pop1:
					break;
				case StackBehaviour.Popref_popi:
					break;

				case StackBehaviour.Popi_popi_popi:
					break;
				case StackBehaviour.Popref_popi_popi:
					break;
				case StackBehaviour.Popref_popi_popi8:
					break;
				case StackBehaviour.Popref_popi_popr4:
					break;
				case StackBehaviour.Popref_popi_popr8:
					break;
				case StackBehaviour.Popref_popi_popref:
					break;
				case StackBehaviour.Popref_popi_pop1:
					break;

				case StackBehaviour.Varpop:
					break;

				case StackBehaviour.PopAll:
					break;

				default:
					Debug.Fail("StackBehaviourPop " + inst.OpCode.StackBehaviourPop);
					break;
			}

			switch (inst.OpCode.StackBehaviourPush)
			{
				case StackBehaviour.Push0:
					break;

				case StackBehaviour.Push1:
					break;

				case StackBehaviour.Pushi:
					break;
				case StackBehaviour.Pushi8:
					break;
				case StackBehaviour.Pushr4:
					break;
				case StackBehaviour.Pushr8:
					break;
				case StackBehaviour.Pushref:
					break;

				case StackBehaviour.Push1_push1:
					break;

				case StackBehaviour.Varpush:
					break;

				default:
					Debug.Fail("StackBehaviourPush " + inst.OpCode.StackBehaviourPush);
					break;
			}

			int nextIP = currIP + 1;

			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineBrTarget:
					{
						if (inst.OpCode.FlowControl == FlowControl.Branch)
						{
							nextIP = InstMap[(Instruction)inst.Operand].Index;
						}
						else
						{
							Debug.Assert(inst.OpCode.FlowControl == FlowControl.Cond_Branch);
							int targetIP = InstMap[(Instruction)inst.Operand].Index;
							AddPendingBranch(targetIP);
						}
						break;
					}

				case OperandType.InlineSwitch:
					{
						throw new NotImplementedException();
						break;
					}
			}

			return nextIP;
		}
	}
}
