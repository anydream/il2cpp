using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
			return (IsBrTarget ? Offset + ": " : null) +
				OpCode + ' ' + Operand;
		}
	}

	internal enum StackTypeKind
	{
		I4,
		I8,
		R4,
		R8,
		Ptr,
		Ref,
		Obj,
		ValueType
	}

	// 栈类型
	internal struct StackType
	{
		public readonly string TypeName;
		public readonly StackTypeKind Kind;

		public static StackType I4 = new StackType(StackTypeKind.I4);
		public static StackType I8 = new StackType(StackTypeKind.I8);
		public static StackType R4 = new StackType(StackTypeKind.R4);
		public static StackType R8 = new StackType(StackTypeKind.R8);
		public static StackType Ptr = new StackType(StackTypeKind.Ptr);
		public static StackType Ref = new StackType(StackTypeKind.Ref);
		public static StackType Obj = new StackType(StackTypeKind.Obj);

		public StackType(StackTypeKind kind)
		{
			Debug.Assert(kind != StackTypeKind.ValueType);
			TypeName = null;
			Kind = kind;
		}

		public StackType(string typeName)
		{
			TypeName = typeName;
			Kind = StackTypeKind.ValueType;
		}

		public string GetPostfix()
		{
			switch (Kind)
			{
				case StackTypeKind.I4:
					return "i4";
				case StackTypeKind.I8:
					return "i8";
				case StackTypeKind.R4:
					return "r4";
				case StackTypeKind.R8:
					return "r8";
				case StackTypeKind.Ptr:
					return "ptr";
				case StackTypeKind.Ref:
					return "ref";
				case StackTypeKind.Obj:
					return "obj";
				case StackTypeKind.ValueType:
					return TypeName;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public string GetTypeName()
		{
			switch (Kind)
			{
				case StackTypeKind.I4:
					return "int32_t";
				case StackTypeKind.I8:
					return "int64_t";
				case StackTypeKind.R4:
					return "float";
				case StackTypeKind.R8:
					return "double";
				case StackTypeKind.Ptr:
					return "void*";
				case StackTypeKind.Ref:
					return "void*";
				case StackTypeKind.Obj:
					return "cls_Object*";
				case StackTypeKind.ValueType:
					return TypeName;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	// 栈槽
	internal class SlotInfo
	{
		public readonly StackType SlotType;
		public readonly int SlotIndex;

		public SlotInfo(StackType stype, int idx)
		{
			SlotType = stype;
			SlotIndex = idx;
		}
	}

	// 方法生成器
	internal class MethodGenerator
	{
		private readonly GeneratorContext GenContext;
		private readonly MethodX CurrMethod;

		// 类型栈
		private Stack<StackType> TypeStack = new Stack<StackType>();
		// 分支队列
		private readonly Queue<Tuple<Stack<StackType>, int>> Branches = new Queue<Tuple<Stack<StackType>, int>>();
		private readonly Dictionary<int, HashSet<StackType>> SlotMap = new Dictionary<int, HashSet<StackType>>();

		public readonly HashSet<string> DeclDepends = new HashSet<string>();
		public readonly HashSet<string> ImplDepends = new HashSet<string>();
		public string DeclCode;
		public string ImplCode;

		private int PushCount = 0;
		private int PopCount = 0;

		public MethodGenerator(GeneratorContext genContext, MethodX metX)
		{
			GenContext = genContext;
			CurrMethod = metX;
		}

		private SlotInfo Push(StackType stype)
		{
			SlotInfo slot = new SlotInfo(stype, TypeStack.Count);
			TypeStack.Push(stype);
			++PushCount;
			AddSlotMap(slot);
			return slot;
		}

		private SlotInfo Pop()
		{
			Debug.Assert(TypeStack.Count > 0);
			StackType stype = TypeStack.Pop();
			++PopCount;
			return new SlotInfo(stype, TypeStack.Count);
		}

		private SlotInfo[] Pop(int num)
		{
			SlotInfo[] slots = new SlotInfo[num];
			for (int i = num - 1; i >= 0; --i)
				slots[i] = Pop();
			return slots;
		}

		private SlotInfo Peek()
		{
			Debug.Assert(TypeStack.Count > 0);
			return new SlotInfo(TypeStack.Peek(), TypeStack.Count - 1);
		}

		private void AddBranch(int target)
		{
			Branches.Enqueue(new Tuple<Stack<StackType>, int>(
				new Stack<StackType>(TypeStack),
				target));
		}

		private void AddSlotMap(SlotInfo slot)
		{
			if (!SlotMap.TryGetValue(slot.SlotIndex, out var tset))
			{
				tset = new HashSet<StackType>();
				SlotMap.Add(slot.SlotIndex, tset);
			}
			tset.Add(slot.SlotType);
		}

		public void Generate()
		{
			// 生成指令代码
			var instList = CurrMethod.InstList;
			if (instList != null)
			{
				int currIP = 0;
				for (; ; )
				{
					if (!GenerateInst(instList[currIP], ref currIP))
					{
						if (Branches.Count > 0)
						{
							var branch = Branches.Dequeue();
							TypeStack = branch.Item1;
							currIP = branch.Item2;
						}
						else
							break;
					}
				}

				// 代码合并
				CodePrinter prt = new CodePrinter();

				// 局部变量
				prt.AppendLine("// locals");
				for (int i = 0, sz = CurrMethod.LocalTypes.Count; i < sz; ++i)
				{
					var locType = CurrMethod.LocalTypes[i];
					prt.AppendFormatLine("{0} {1};",
						ToStackType(locType).GetTypeName(),
						LocalName(i));
				}

				// 临时变量
				prt.AppendLine("// temps");
				foreach (var kv in SlotMap)
				{
					foreach (var stype in kv.Value)
					{
						prt.AppendFormatLine(
							"{0} {1}",
							stype.GetTypeName(),
							TempName(kv.Key, stype));
					}
				}
				prt.AppendLine();

				// 代码体
				foreach (var inst in instList)
				{
					if (inst.IsBrTarget)
					{
						--prt.Indents;
						prt.AppendLine(LabelName(inst.Offset) + ':');
						++prt.Indents;
					}

					if (inst.InstCode != null)
						prt.AppendLine(inst.InstCode);
				}

				ImplCode = prt.ToString();
			}
		}

		private bool GenerateInst(InstInfo inst, ref int currIP)
		{
			if (inst.IsGenerated)
				return false;
			inst.IsGenerated = true;

			GenerateInstCode(inst);

			var opCode = inst.OpCode;
			var operand = inst.Operand;

			switch (opCode.StackBehaviourPop)
			{ }

			switch (opCode.StackBehaviourPush)
			{ }

			PushCount = PopCount = 0;

			switch (opCode.FlowControl)
			{
				case FlowControl.Branch:
					currIP = (int)inst.Operand;
					return true;

				case FlowControl.Cond_Branch:
					if (opCode.Code == Code.Switch)
					{
						Debug.Assert(operand is int[]);
						int[] targets = (int[])operand;
						foreach (int target in targets)
							AddBranch(target);
					}
					else
					{
						Debug.Assert(operand is int);
						AddBranch((int)operand);
					}
					++currIP;
					return true;

				case FlowControl.Break:
				case FlowControl.Call:
				case FlowControl.Meta:
				case FlowControl.Next:
					++currIP;
					return true;

				case FlowControl.Return:
				case FlowControl.Throw:
					return false;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void GenerateInstCode(InstInfo inst)
		{
			var opCode = inst.OpCode;
			var operand = inst.Operand;

			switch (opCode.Code)
			{
				case Code.Nop:
					return;

				case Code.Pop:
					Pop();
					return;

				case Code.Dup:
					GenDup(inst);
					return;

				case Code.Ret:
					GenReturn(inst);
					return;

				case Code.Ldc_I4_M1:
					GenLdc(inst, StackType.I4, "-1");
					return;
				case Code.Ldc_I4_0:
					GenLdc(inst, StackType.I4, "0");
					return;
				case Code.Ldc_I4_1:
					GenLdc(inst, StackType.I4, "1");
					return;
				case Code.Ldc_I4_2:
					GenLdc(inst, StackType.I4, "2");
					return;
				case Code.Ldc_I4_3:
					GenLdc(inst, StackType.I4, "3");
					return;
				case Code.Ldc_I4_4:
					GenLdc(inst, StackType.I4, "4");
					return;
				case Code.Ldc_I4_5:
					GenLdc(inst, StackType.I4, "5");
					return;
				case Code.Ldc_I4_6:
					GenLdc(inst, StackType.I4, "6");
					return;
				case Code.Ldc_I4_7:
					GenLdc(inst, StackType.I4, "7");
					return;
				case Code.Ldc_I4_8:
					GenLdc(inst, StackType.I4, "8");
					return;

				case Code.Ldc_I4_S:
				case Code.Ldc_I4:
					GenLdc(inst, StackType.I4, operand.ToString());
					return;
				case Code.Ldc_I8:
					GenLdc(inst, StackType.I8, operand.ToString());
					return;
				case Code.Ldc_R4:
					GenLdc(inst, StackType.R4, operand.ToString());
					return;
				case Code.Ldc_R8:
					GenLdc(inst, StackType.R8, operand.ToString());
					return;

				case Code.Ldarg_0:
					GenLdarg(inst, 0);
					return;
				case Code.Ldarg_1:
					GenLdarg(inst, 1);
					return;
				case Code.Ldarg_2:
					GenLdarg(inst, 2);
					return;
				case Code.Ldarg_3:
					GenLdarg(inst, 3);
					return;

				case Code.Ldarg:
				case Code.Ldarg_S:
					GenLdarg(inst, ((Parameter)operand).Index);
					return;

				case Code.Ldarga:
				case Code.Ldarga_S:
					GenLdarg(inst, ((Parameter)operand).Index, true);
					return;

				case Code.Starg:
				case Code.Starg_S:
					GenStarg(inst, ((Parameter)operand).Index);
					return;

				case Code.Ldloc_0:
					GenLdloc(inst, 0);
					return;
				case Code.Ldloc_1:
					GenLdloc(inst, 1);
					return;
				case Code.Ldloc_2:
					GenLdloc(inst, 2);
					return;
				case Code.Ldloc_3:
					GenLdloc(inst, 3);
					return;

				case Code.Ldloc:
				case Code.Ldloc_S:
					GenLdloc(inst, ((Local)operand).Index);
					return;

				case Code.Ldloca:
				case Code.Ldloca_S:
					GenLdloc(inst, ((Local)operand).Index, true);
					return;

				case Code.Stloc_0:
					GenStloc(inst, 0);
					return;
				case Code.Stloc_1:
					GenStloc(inst, 1);
					return;
				case Code.Stloc_2:
					GenStloc(inst, 2);
					return;
				case Code.Stloc_3:
					GenStloc(inst, 3);
					return;

				case Code.Stloc:
				case Code.Stloc_S:
					GenStloc(inst, ((Local)operand).Index);
					return;

				case Code.Br:
				case Code.Br_S:
					inst.InstCode = GenGoto((int)operand);
					return;

				case Code.Brfalse:
				case Code.Brfalse_S:
					GenBrCond(inst, (int)operand, TempName(Pop()) + " == 0");
					return;

				case Code.Brtrue:
				case Code.Brtrue_S:
					GenBrCond(inst, (int)operand, TempName(Pop()) + " != 0");
					return;
			}

			throw new NotImplementedException();
		}

		private void GenDup(InstInfo inst)
		{
			var slotTop = Peek();
			var slotPush = Push(slotTop.SlotType);
			inst.InstCode = GenAssign(TempName(slotPush), TempName(slotTop), null);
		}

		private void GenLdc(InstInfo inst, StackType stype, string val)
		{
			var slotPush = Push(stype);
			inst.InstCode = GenAssign(TempName(slotPush), val, stype);
		}

		private void GenLdarg(InstInfo inst, int argID, bool isAddr = false)
		{
			Debug.Assert(argID < CurrMethod.ParamTypes.Count);
			var argType = CurrMethod.ParamTypes[argID];
			var slotPush = isAddr ? Push(StackType.Ptr) : Push(ToStackType(argType));
			inst.InstCode = GenAssign(TempName(slotPush), (isAddr ? "&" : null) + ArgName(argID), slotPush.SlotType);
		}

		private void GenStarg(InstInfo inst, int argID)
		{
			Debug.Assert(argID < CurrMethod.ParamTypes.Count);
			var argType = CurrMethod.ParamTypes[argID];
			var slotPop = Pop();
			inst.InstCode = GenAssign(ArgName(argID), TempName(slotPop), ToStackType(argType));
		}

		private void GenLdloc(InstInfo inst, int locID, bool isAddr = false)
		{
			Debug.Assert(locID < CurrMethod.LocalTypes.Count);
			var locType = CurrMethod.LocalTypes[locID];
			var slotPush = isAddr ? Push(StackType.Ptr) : Push(ToStackType(locType));
			inst.InstCode = GenAssign(TempName(slotPush), (isAddr ? "&" : null) + LocalName(locID), slotPush.SlotType);
		}

		private void GenStloc(InstInfo inst, int locID)
		{
			Debug.Assert(locID < CurrMethod.LocalTypes.Count);
			var locType = CurrMethod.LocalTypes[locID];
			var slotPop = Pop();
			inst.InstCode = GenAssign(LocalName(locID), TempName(slotPop), ToStackType(locType));
		}

		private void GenBrCond(InstInfo inst, int labelID, string cond)
		{
			inst.InstCode = "if (" + cond + ") " + GenGoto(labelID);
		}

		private void GenReturn(InstInfo inst)
		{
			if (TypeStack.Count > 0)
			{
				Debug.Assert(TypeStack.Count == 1);
				var slotPop = Pop();
				inst.InstCode = "return " + CastType(ToStackType(CurrMethod.ReturnType)) + TempName(slotPop);
			}
			else
				inst.InstCode = "return;";
		}

		private StackType ToStackType(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
				case ElementType.I1:
				case ElementType.I2:
				case ElementType.I4:
				case ElementType.U1:
				case ElementType.U2:
				case ElementType.U4:
				case ElementType.Boolean:
				case ElementType.Char:
					return StackType.I4;

				case ElementType.I8:
				case ElementType.U8:
					return StackType.I8;

				case ElementType.R4:
					return StackType.R4;

				case ElementType.R8:
					return StackType.R8;

				case ElementType.I:
				case ElementType.U:
				case ElementType.Ptr:
					return StackType.Ptr;

				case ElementType.ByRef:
					return StackType.Ref;
			}

			if (tySig.IsValueType)
				return new StackType(GenContext.GetTypeName(tySig));

			return StackType.Obj;
		}

		private static string CastType(StackType stype)
		{
			if (stype.Kind == StackTypeKind.ValueType)
				return "*(" + stype.GetTypeName() + "*)&";
			else
				return '(' + stype.GetTypeName() + ')';
		}

		private static string GenAssign(string lhs, string rhs, StackType? stype)
		{
			return lhs + " = " + (stype != null ? CastType(stype.Value) : null) + rhs + ';';
		}

		private static string GenGoto(int labelID)
		{
			return "goto " + LabelName(labelID) + ';';
		}

		private static string ArgName(int argID)
		{
			return "arg_" + argID;
		}

		private static string LocalName(int locID)
		{
			return "loc_" + locID;
		}

		private static string TempName(int idx, StackType stype)
		{
			return "tmp_" + idx + '_' + stype.GetPostfix();
		}

		private static string TempName(SlotInfo slot)
		{
			return TempName(slot.SlotIndex, slot.SlotType);
		}

		private static string LabelName(int labelID)
		{
			return "LB_" + labelID;
		}
	}
}
