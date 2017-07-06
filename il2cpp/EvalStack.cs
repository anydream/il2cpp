using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 指令数据
	internal class InstructionInfo
	{
		public Instruction Inst;
		public int Index;
		public bool IsProcessed;
		public string CppCode;

		public InstructionInfo(Instruction inst, int n)
		{
			Inst = inst;
			Index = n;
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}{2}", Index, Inst, IsProcessed ? " √" : "");
		}
	}

	struct SlotInfo
	{
		public string TypeName;
		public int Slot;
	}

	// 执行栈
	internal class EvalStack
	{
		private int TypeCounter;
		// 类型名称映射
		private readonly Dictionary<string, Tuple<int, TypeSig>> TypeMap = new Dictionary<string, Tuple<int, TypeSig>>();
		// 当前类型栈
		private Stack<string> TypeStack = new Stack<string>();

		// 需要继续执行的分支指令位置和类型栈的队列
		private readonly Queue<Tuple<int, Stack<string>>> PendingBranch = new Queue<Tuple<int, Stack<string>>>();

		// 指令偏移对应的下标映射
		private readonly Dictionary<int, int> OffsetIndexMap = new Dictionary<int, int>();
		// 指令对应信息列表
		private readonly List<InstructionInfo> InstInfoList = new List<InstructionInfo>();

		private MethodX TargetMethod;
		private Func<Instruction, object> ResolverFunc;

		public void Reset()
		{
			TypeCounter = 0;
			TypeMap.Clear();
			TypeStack.Clear();
			PendingBranch.Clear();
			OffsetIndexMap.Clear();
			InstInfoList.Clear();
		}

		private string RegType(TypeSig type)
		{
			string typeName = type.FullName;

			if (!TypeMap.TryGetValue(typeName, out var typeTup))
			{
				typeTup = new Tuple<int, TypeSig>(TypeCounter++, type);
				TypeMap.Add(typeName, typeTup);
			}

			return typeName;
		}

		private TypeSig GetType(string typeName, out int typeID)
		{
			if (TypeMap.TryGetValue(typeName, out var typeTup))
			{
				typeID = typeTup.Item1;
				return typeTup.Item2;
			}
			typeID = -1;
			return null;
		}

		private bool Push(string typeName, out int slot)
		{
			if (typeName != "System.Void")
			{
				slot = TypeStack.Count;
				TypeStack.Push(typeName);
				return true;
			}
			slot = -1;
			return false;
		}

		private bool Push(string typeName, out SlotInfo sinfo)
		{
			if (Push(typeName, out int slot))
			{
				sinfo.TypeName = typeName;
				sinfo.Slot = slot;
				return true;
			}
			sinfo = new SlotInfo();
			return false;
		}

		private bool Push(TypeSig type, out SlotInfo sinfo)
		{
			return Push(RegType(type), out sinfo);
		}

		private void Pop()
		{
			TypeStack.Pop();
		}

		private void Pop(out SlotInfo sinfo)
		{
			sinfo = new SlotInfo();
			sinfo.TypeName = TypeStack.Pop();
			sinfo.Slot = TypeStack.Count;
		}

		private void Pop(int num, out SlotInfo[] sinfoAry)
		{
			sinfoAry = new SlotInfo[num];
			for (int i = 0; i < num; ++i)
			{
				Pop(out sinfoAry[i]);
			}
		}

		private void Dup()
		{
			TypeStack.Push(TypeStack.Peek());
		}

		private void AddPendingBranch(int targetIP)
		{
			PendingBranch.Enqueue(new Tuple<int, Stack<string>>(targetIP, new Stack<string>(TypeStack)));
		}

		public void Process(MethodX metX, Func<Instruction, object> resolver)
		{
			Debug.Assert(metX.Def.HasBody);
			Reset();

			TargetMethod = metX;
			ResolverFunc = resolver;

			// 构建指令信息映射
			var instList = metX.Def.Body.Instructions;
			int instRange = instList.Count;
			for (int i = 0; i < instRange; ++i)
			{
				var inst = instList[i];
				OffsetIndexMap[(int)inst.Offset] = i;
				InstInfoList.Add(new InstructionInfo(inst, i));
			}

			int currIP = 0;
			for (;;)
			{
				int result = ProcessStep(currIP);
				if (result >= 0 || result < instRange)
				{
					currIP = result;
				}
				else if (PendingBranch.Count > 0)
				{
					// 一条路径执行完毕, 执行下一条
					var branch = PendingBranch.Dequeue();
					currIP = branch.Item1;
					TypeStack = branch.Item2;
				}
				else
					break;
			}
		}

		private int ProcessStep(int currIP)
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

			if (inst.OpCode.Code == Code.Nop)
			{
				// 无操作
			}
			else if (inst.OpCode.Code == Code.Dup)
			{
				// 复制栈顶
				Dup();
			}
			else if (inst.OpCode.Code == Code.Pop)
			{
				// 出栈
				Pop();
			}
			else
			{
				SlotInfo[] popList = null;

				switch (inst.OpCode.StackBehaviourPop)
				{
					case StackBehaviour.Pop0:
						break;

					case StackBehaviour.Pop1:
					case StackBehaviour.Popi:
					case StackBehaviour.Popref:
						Pop(1, out popList);
						break;

					case StackBehaviour.Pop1_pop1:
					case StackBehaviour.Popi_pop1:
					case StackBehaviour.Popi_popi:
					case StackBehaviour.Popi_popi8:
					case StackBehaviour.Popi_popr4:
					case StackBehaviour.Popi_popr8:
					case StackBehaviour.Popref_pop1:
					case StackBehaviour.Popref_popi:
						Pop(2, out popList);
						break;

					case StackBehaviour.Popi_popi_popi:
					case StackBehaviour.Popref_popi_popi:
					case StackBehaviour.Popref_popi_popi8:
					case StackBehaviour.Popref_popi_popr4:
					case StackBehaviour.Popref_popi_popr8:
					case StackBehaviour.Popref_popi_popref:
					case StackBehaviour.Popref_popi_pop1:
						Pop(3, out popList);
						break;

					case StackBehaviour.Varpop:
						// 动态出栈由具体指令来处理
						break;

					case StackBehaviour.PopAll:
						TypeStack.Clear();
						break;

					default:
						Debug.Fail("StackBehaviourPop " + inst.OpCode.StackBehaviourPop);
						break;
				}

				// 针对具体指令单独处理
				ProcessInstruction(instInfo, popList);
			}

			// 计算下一指令位置
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
			InstructionInfo instInfo,
			SlotInfo[] popList)
		{
			var inst = instInfo.Inst;

			// 处理无操作数的指令
			switch (inst.OpCode.Code)
			{
				case Code.Ret:
					{
						if (TypeStack.Count > 0)
						{
							Debug.Assert(TypeStack.Count == 1);

							Pop(1, out popList);
							instInfo.CppCode = "return " + GetSlotCode(popList[0]);
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

						// 计算出栈个数
						int popCount = metX.ParamTypes.Count;
						if (!metX.IsStatic)
							++popCount;

						instInfo.CppCode = GenerateCallCode(
							popCount,
							RegType(metX.ReturnType),
							GetMethodCppName(metX, inst.OpCode.Code == Code.Callvirt));

						return;
					}

				case Code.Newobj:
					{
						if (operand != null)
						{
							MethodX metX = (MethodX)operand;
							//! pop
							//Push(metX.DeclType.FullName, out var pushSlot);
							return;
						}
						else
						{
							throw new NotImplementedException();
						}
					}

				default:
					Debug.Fail(inst.ToString());
					break;
			}
		}

		private string GetSlotCode(SlotInfo sinfo)
		{
			if (GetType(sinfo.TypeName, out int typeID) != null)
				return "tmp_" + sinfo.Slot + "_" + typeID;
			return "";
		}

		private string GenerateCallCode(int popCount, string returnType, string methodName)
		{
			StringBuilder sb = new StringBuilder();

			// 出栈
			Pop(popCount, out var popList);

			// 返回值入栈
			if (Push(returnType, out SlotInfo pushSlot))
				sb.Append(GetSlotCode(pushSlot) + " = ");

			// 构建调用代码
			sb.Append(methodName);
			sb.Append('(');

			// 构建参数列表
			bool last = false;
			foreach (var argSlot in popList)
			{
				if (last)
					sb.Append(", ");
				last = true;
				sb.Append(GetSlotCode(argSlot));
			}
			sb.Append(')');

			return sb.ToString();
		}

		private static string GetMethodCppName(MethodX metX, bool isVirt)
		{
			if (metX.CppName == null)
			{
				metX.CppName = ToCppName(metX.FullFuncName);
			}
			return (isVirt ? "vmet_" : "met_") + metX.CppName;
		}

		private static string ToCppName(string str)
		{
			StringBuilder sb = new StringBuilder();

			string hash = ToRadix((uint)str.GetHashCode(), (uint)DigMap.Length);
			sb.Append(hash + "_");

			for (int i = 0; i < str.Length; ++i)
			{
				if (IsLegalIdentChar(str[i]))
				{
					sb.Append(str[i]);
				}
				else
				{
					sb.Append('_');
				}
			}
			return sb.ToString();
		}

		private static bool IsLegalIdentChar(char ch)
		{
			return ch >= 'a' && ch <= 'z' ||
				   ch >= 'A' && ch <= 'Z' ||
				   ch >= '0' && ch <= '9' ||
				   ch == '_';
		}

		private static string DigMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static string ToRadix(uint value, uint radix)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				uint dig = value % radix;
				value /= radix;
				sb.Append(DigMap[(int)dig]);
			} while (value != 0);

			return sb.ToString();
		}
	}
}
