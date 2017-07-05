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
		public Instruction Inst;
		public int Index;
		public bool IsProcessed;
		public string Code;

		public InstInfo(Instruction inst, int n)
		{
			Inst = inst;
			Index = n;
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}{2}", Index, Inst, IsProcessed ? " √" : "");
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

		// 指令偏移对应的下标映射
		private readonly Dictionary<int, int> OffsetIndexMap = new Dictionary<int, int>();
		// 指令对应信息列表
		private readonly List<InstInfo> InstInfoList = new List<InstInfo>();

		public void Reset()
		{
			TypeMap.Clear();
			TypeStack.Clear();
			PendingBranch.Clear();
			OffsetIndexMap.Clear();
			InstInfoList.Clear();
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

		private bool Push(string typeName, out int stackIndex)
		{
			if (typeName != "System.Void")
			{
				stackIndex = TypeStack.Count;
				TypeStack.Push(typeName);
				return true;
			}
			stackIndex = -1;
			return false;
		}

		private bool Push(TypeSig type, out int stackIndex)
		{
			return Push(RegType(type), out stackIndex);
		}

		private string Pop(out int stackIndex)
		{
			stackIndex = TypeStack.Count - 1;
			return TypeStack.Pop();
		}

		private void Pop(int num, List<Tuple<int, string>> popList)
		{
			for (int i = 0; i < num; ++i)
			{
				int stackIndex;
				string type = Pop(out stackIndex);
				popList.Add(new Tuple<int, string>(stackIndex, type));
			}
		}

		private void AddPendingBranch(int targetIP)
		{
			PendingBranch.Enqueue(new Tuple<int, Stack<string>>(targetIP, new Stack<string>(TypeStack)));
		}

		public void Process(MethodX metX, Func<Instruction, object> resolver)
		{
			Debug.Assert(metX.Def.HasBody);
			Reset();

			// 构建指令信息映射
			var instList = metX.Def.Body.Instructions;
			int instRange = instList.Count;
			for (int i = 0; i < instRange; ++i)
			{
				var inst = instList[i];
				OffsetIndexMap[(int)inst.Offset] = i;
				InstInfoList.Add(new InstInfo(inst, i));
			}

			int currIP = 0;
			for (;;)
			{
				int result = ProcessStep(currIP, resolver);
				if (result < 0 || result >= instRange)
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

		private int ProcessStep(int currIP, Func<Instruction, object> resolver)
		{
			// 跳过已经处理过的指令
			var instInfo = InstInfoList[currIP];
			if (instInfo.IsProcessed)
			{
				// 比较栈信息
				return -1;
			}
			instInfo.IsProcessed = true;
			var inst = instInfo.Inst;

			List<Tuple<int, string>> popList = new List<Tuple<int, string>>();

			switch (inst.OpCode.StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					break;

				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					Pop(1, popList);
					break;

				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					Pop(2, popList);
					break;

				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
				case StackBehaviour.Popref_popi_pop1:
					Pop(3, popList);
					break;

				case StackBehaviour.Varpop:
					// 不处理
					break;

				case StackBehaviour.PopAll:
					TypeStack.Clear();
					break;

				default:
					Debug.Fail("StackBehaviourPop " + inst.OpCode.StackBehaviourPop);
					break;
			}

			// 针对具体指令单独处理
			ProcessInstruction(inst, popList, resolver);

			int nextIP = -1;
			switch (inst.OpCode.FlowControl)
			{
				case FlowControl.Branch:
					nextIP = OffsetIndexMap[(int)((Instruction)inst.Operand).Offset];
					break;

				case FlowControl.Cond_Branch:
					{
						if (inst.OpCode.OperandType == OperandType.InlineSwitch)
						{
							throw new NotImplementedException();
						}
						else
						{
							int targetIP = OffsetIndexMap[(int)((Instruction)inst.Operand).Offset];
							AddPendingBranch(targetIP);
						}
						break;
					}

				case FlowControl.Break:
				case FlowControl.Call:
				case FlowControl.Meta:
				case FlowControl.Next:
					nextIP = currIP + 1;
					break;

				case FlowControl.Return:
				case FlowControl.Throw:
					nextIP = -1;
					break;

				default:
					Debug.Fail("FlowControl " + inst.OpCode.FlowControl);
					break;
			}

			return nextIP;
		}

		private void ProcessInstruction(
			Instruction inst,
			List<Tuple<int, string>> popList,
			Func<Instruction, object> resolver)
		{
			if (inst.OpCode.Code == Code.Dup)
			{
				var type = popList[0].Item2;
				Push(type, out _);
				Push(type, out _);
				return;
			}
			if (inst.OpCode.Code == Code.Pop ||
				inst.OpCode.Code == Code.Nop)
				return;

			object operand = resolver(inst);
			switch (inst.OpCode.Code)
			{
				case Code.Call:
				case Code.Callvirt:
				case Code.Calli:
					{
						MethodX metX = (MethodX)operand;
						Push(metX.ReturnType, out var stackIndex);
						return;
					}

				case Code.Newobj:
					{
						if (operand != null)
						{
							MethodX metX = (MethodX)operand;
							Push(metX.DeclType.FullName, out var stackIndex);
							return;
						}
						else
						{
							throw new NotImplementedException();
						}
					}
			}
		}
	}
}
