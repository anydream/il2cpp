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

		public bool IsProcessed;
		public string CppCode;

		public InstructionInfo(Instruction inst)
		{
			Inst = inst;
			Code = inst.OpCode.Code;
		}

		public override string ToString()
		{
			return CppCode ?? string.Format("{0}{1}", Inst, IsProcessed ? " √" : "");
		}
	}

	// 执行栈
	public class EvalStack
	{
		private readonly TypeManager TypeMgr;
		private readonly ICorLibTypes CorTypes;

		private MethodX TargetMethod;
		// 指令列表
		private InstructionInfo[] InstList;

		// 当前类型栈
		private Stack<StackType> TypeStack = new Stack<StackType>();
		// 栈槽类型映射
		private readonly Dictionary<int, HashSet<StackType>> StackTypeMap = new Dictionary<int, HashSet<StackType>>();
		// 待处理的分支
		private readonly Queue<Tuple<int, Stack<StackType>>> Branches = new Queue<Tuple<int, Stack<StackType>>>();

		public EvalStack(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
			CorTypes = typeMgr.CorTypes;
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
			for (int i = num - 1; i >= 0; --i)
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

		public void Process(MethodX metX)
		{
			Debug.Assert(metX.Def.HasBody);

			Reset();

			TargetMethod = metX;

			// 转换为自定义类型的指令列表
			var origInstList = metX.Def.Body.Instructions;
			InstList = new InstructionInfo[origInstList.Count];
			for (int i = 0; i < InstList.Length; ++i)
				InstList[i] = new InstructionInfo(origInstList[i]);

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
					nextIP = (int)((Instruction)iinfo.Inst.Operand).Offset;
					return true;

				case FlowControl.Cond_Branch:
					if (iinfo.Code == Code.Switch)
					{
						Instruction[] targetList = (Instruction[])iinfo.Inst.Operand;
						foreach (Instruction targetInst in targetList)
							AddBranch((int)targetInst.Offset);
					}
					else
					{
						int targetIP = (int)((Instruction)iinfo.Inst.Operand).Offset;
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
			object operand = iinfo.Inst.Operand;

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
					Load(iinfo, StackType.I4, ((sbyte)iinfo.Inst.Operand).ToString());
					return;
				case Code.Ldc_I4:
					Load(iinfo, StackType.I4, ((int)iinfo.Inst.Operand).ToString());
					return;
				case Code.Ldc_I8:
					Load(iinfo, StackType.I8, ((long)iinfo.Inst.Operand).ToString());
					return;
				case Code.Ldc_R4:
					Load(iinfo, StackType.R4, ((float)iinfo.Inst.Operand).ToString(CultureInfo.InvariantCulture));
					return;
				case Code.Ldc_R8:
					Load(iinfo, StackType.R8, ((double)iinfo.Inst.Operand).ToString(CultureInfo.InvariantCulture));
					return;

				case Code.Ldloc_0:
					Load(iinfo, ToStackType(TargetMethod.LocalTypes[0]), LocalName(0));
					return;
				case Code.Ldloc_1:
					Load(iinfo, ToStackType(TargetMethod.LocalTypes[1]), LocalName(1));
					return;
				case Code.Ldloc_2:
					Load(iinfo, ToStackType(TargetMethod.LocalTypes[2]), LocalName(2));
					return;
				case Code.Ldloc_3:
					Load(iinfo, ToStackType(TargetMethod.LocalTypes[3]), LocalName(3));
					return;
				case Code.Ldloc:
				case Code.Ldloc_S:
					{
						Local loc = (Local)operand;
						Debug.Assert(loc.Type.Equals(TargetMethod.LocalTypes[loc.Index]));
						Load(iinfo, ToStackType(loc.Type), LocalName(loc.Index));
					}
					return;
				case Code.Ldloca:
				case Code.Ldloca_S:
					{
						Local loc = (Local)operand;
						Debug.Assert(loc.Type.Equals(TargetMethod.LocalTypes[loc.Index]));
						Load(iinfo, StackType.Ptr, '&' + LocalName(loc.Index));
					}
					return;

				case Code.Stloc_0:
					Store(iinfo, LocalName(0), TargetMethod.LocalTypes[0].GetCppName(TypeMgr));
					return;
				case Code.Stloc_1:
					Store(iinfo, LocalName(1), TargetMethod.LocalTypes[1].GetCppName(TypeMgr));
					return;
				case Code.Stloc_2:
					Store(iinfo, LocalName(2), TargetMethod.LocalTypes[2].GetCppName(TypeMgr));
					return;
				case Code.Stloc_3:
					Store(iinfo, LocalName(3), TargetMethod.LocalTypes[3].GetCppName(TypeMgr));
					return;
				case Code.Stloc:
				case Code.Stloc_S:
					{
						Local loc = (Local)operand;
						Debug.Assert(loc.Type.Equals(TargetMethod.LocalTypes[loc.Index]));
						Store(iinfo, LocalName(loc.Index), TargetMethod.LocalTypes[loc.Index].GetCppName(TypeMgr));
					}
					return;

				case Code.Ceq:
					Cmp(iinfo, "==");
					return;
				case Code.Cgt:
					Cmp(iinfo, ">");
					return;
				case Code.Cgt_Un:
					return;
				case Code.Clt:
					Cmp(iinfo, "<");
					return;
				case Code.Clt_Un:
					return;

				case Code.Br:
				case Code.Br_S:
					iinfo.CppCode = "goto " + LabelName(((Instruction)operand).Offset);
					return;
				case Code.Brfalse:
				case Code.Brfalse_S:
					BrCond(iinfo, false, ((Instruction)operand).Offset);
					return;
				case Code.Brtrue:
				case Code.Brtrue_S:
					BrCond(iinfo, true, ((Instruction)operand).Offset);
					return;
				case Code.Beq:
				case Code.Beq_S:
					BrCmp(iinfo, "==", ((Instruction)operand).Offset);
					return;
				case Code.Bge:
				case Code.Bge_S:
					BrCmp(iinfo, ">=", ((Instruction)operand).Offset);
					return;
				case Code.Bgt:
				case Code.Bgt_S:
					BrCmp(iinfo, ">", ((Instruction)operand).Offset);
					return;
				case Code.Ble:
				case Code.Ble_S:
					BrCmp(iinfo, "<=", ((Instruction)operand).Offset);
					return;
				case Code.Blt:
				case Code.Blt_S:
					BrCmp(iinfo, "<", ((Instruction)operand).Offset);
					return;
				case Code.Bne_Un:
					return;
				case Code.Bne_Un_S:
					return;
				case Code.Bge_Un:
					return;
				case Code.Bge_Un_S:
					return;
				case Code.Bgt_Un:
					return;
				case Code.Bgt_Un_S:
					return;
				case Code.Ble_Un:
					return;
				case Code.Ble_Un_S:
					return;
				case Code.Blt_Un:
					return;
				case Code.Blt_Un_S:
					return;
				case Code.Switch:
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

				case Code.Call:
				case Code.Callvirt:
					{
						MethodX metX = (MethodX)operand;

						int popCount = metX.ParamTypes.Count;
						if (!metX.IsStatic)
							++popCount;

						Call(iinfo,
							metX.GetCppName(iinfo.Code == Code.Callvirt && metX.Def.IsVirtual),
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

		private string LabelName(uint offset)
		{
			return "label_" + offset;
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

		private void BrCond(InstructionInfo iinfo, bool cond, uint target)
		{
			SlotInfo poped = Pop();
			iinfo.CppCode = string.Format("if ({0}{1}) goto {2}", cond ? "" : "!", SlotInfoName(ref poped), LabelName(target));
		}

		private void BrCmp(InstructionInfo iinfo, string oper, uint target)
		{
			iinfo.CppCode = string.Format("if ({0}) goto {1}", CmpCode(iinfo.Code, oper), LabelName(target));
		}

		private void Cmp(InstructionInfo iinfo, string oper)
		{
			string str = CmpCode(iinfo.Code, oper);
			SlotInfo pushed = Push(StackType.I4);
			iinfo.CppCode = string.Format("{0} = {1}", SlotInfoName(ref pushed), str);
		}

		private string CmpCode(Code code, string oper)
		{
			SlotInfo[] popList = Pop(2);

			if (!IsBinCompareValid(popList[0].SlotType, popList[1].SlotType, code))
			{
				Debug.Fail("Compare Invalid");
			}

			return string.Format("{0} {1} {2}",
				SlotInfoName(ref popList[0]),
				oper,
				SlotInfoName(ref popList[1]));
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
			if (sig.IsByRef)
			{
				return StackType.Ref;
			}
			if (sig.IsPointer ||
				sig.Equals(CorTypes.IntPtr) ||
				sig.Equals(CorTypes.UIntPtr))
			{
				return StackType.Ptr;
			}
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

			Debug.Assert(!sig.IsValueType);
			return StackType.Obj;
		}

		private static bool IsBinoperValid(StackType op1, StackType op2, out StackType retType, Code code)
		{
			switch (op1)
			{
				case StackType.I4:
					switch (op2)
					{
						case StackType.I4:
							retType = StackType.I4;
							return true;

						case StackType.Ptr:
							retType = StackType.Ptr;
							return true;

						case StackType.Ref:
							if (code == Code.Add)
							{
								retType = StackType.Ref;
								return true;
							}
							break;
					}
					retType = StackType.I4;
					return false;

				case StackType.I8:
					if (op2 == StackType.I8)
					{
						retType = StackType.I8;
						return true;
					}
					retType = StackType.I4;
					return false;

				case StackType.R4:
					if (op2 == StackType.R4)
					{
						retType = StackType.R4;
						return true;
					}
					if (op2 == StackType.R8)
					{
						retType = StackType.R8;
						return true;
					}
					retType = StackType.I4;
					return false;

				case StackType.R8:
					if (op2 == StackType.R4 || op2 == StackType.R8)
					{
						retType = StackType.R8;
						return true;
					}
					retType = StackType.I4;
					return false;

				case StackType.Ptr:
					switch (op2)
					{
						case StackType.I4:
						case StackType.Ptr:
							retType = StackType.Ptr;
							return true;

						case StackType.Ref:
							if (code == Code.Add)
							{
								retType = StackType.Ref;
								return true;
							}
							break;
					}
					retType = StackType.I4;
					return false;

				case StackType.Obj:
					retType = StackType.I4;
					return false;

				case StackType.Ref:
					switch (op2)
					{
						case StackType.I4:
						case StackType.Ptr:
							if (code == Code.Add || code == Code.Sub)
							{
								retType = StackType.Ref;
								return true;
							}
							break;

						case StackType.Ref:
							if (code == Code.Sub)
							{
								retType = StackType.Ptr;
								return true;
							}
							break;
					}
					retType = StackType.I4;
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}

		private static bool IsBinCompareValid(StackType op1, StackType op2, Code code)
		{
			switch (op1)
			{
				case StackType.I4:
					switch (op2)
					{
						case StackType.I4:
						case StackType.Ptr:
							return true;
					}
					return false;

				case StackType.I8:
					if (op2 == StackType.I8)
						return true;
					return false;

				case StackType.R4:
				case StackType.R8:
					return op2 == StackType.R4 || op2 == StackType.R8;

				case StackType.Ptr:
					switch (op2)
					{
						case StackType.I4:
						case StackType.Ptr:
							return true;
						case StackType.Ref:
							{
								switch (code)
								{
									case Code.Beq:
									case Code.Beq_S:
									case Code.Bne_Un:
									case Code.Bne_Un_S:
									case Code.Ceq:
										return true;
								}
							}
							break;
					}
					return false;

				case StackType.Obj:
					if (op2 == StackType.Obj)
					{
						switch (code)
						{
							case Code.Beq:
							case Code.Beq_S:
							case Code.Bne_Un:
							case Code.Bne_Un_S:
							case Code.Ceq:
								return true;
						}
					}
					return false;

				case StackType.Ref:
					switch (op2)
					{
						case StackType.Ref:
							return true;
						case StackType.Ptr:
							{
								switch (code)
								{
									case Code.Beq:
									case Code.Beq_S:
									case Code.Bne_Un:
									case Code.Bne_Un_S:
									case Code.Ceq:
										return true;
								}
							}
							break;
					}
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}
	}
}
