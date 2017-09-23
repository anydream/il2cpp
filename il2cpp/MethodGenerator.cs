using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

		private void GenFuncDef(CodePrinter prt, string prefix)
		{
			// 函数签名
			prt.AppendFormat("{0} {1}(",
				GenContext.GetTypeName(CurrMethod.ReturnType),
				GenContext.GetMethodName(CurrMethod, prefix));
			RefValueTypeDecl(CurrMethod.ReturnType);

			for (int i = 0, sz = CurrMethod.ParamTypes.Count; i < sz; ++i)
			{
				if (i != 0)
					prt.Append(", ");

				var argType = CurrMethod.ParamTypes[i];
				RefValueTypeDecl(argType);
				prt.AppendFormat("{0} {1}",
					GenContext.GetTypeName(argType),
					ArgName(i));
			}

			prt.Append(")");
		}

		private void GenFuncType(CodePrinter prt)
		{
			prt.AppendFormat("{0}(*)(",
				GenContext.GetTypeName(CurrMethod.ReturnType));

			for (int i = 0, sz = CurrMethod.ParamTypes.Count; i < sz; ++i)
			{
				if (i != 0)
					prt.Append(",");

				var argType = CurrMethod.ParamTypes[i];
				prt.Append(GenContext.GetTypeName(argType));
			}

			prt.Append(")");
		}

		public void Generate()
		{
			if (!CurrMethod.IsSkipProcessing)
			{
				GenerateMet();
			}

			if (CurrMethod.IsVirtual)
			{
				GenerateVFtn();
				GenerateVMet();
			}
		}

		private void GenerateMet()
		{
			CodePrinter prt = new CodePrinter();

			GenFuncDef(prt, PrefixMet);
			DeclCode += prt + ";\n";

			// 生成指令代码
			var instList = CurrMethod.InstList;
			if (instList == null)
				return;

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

			// 生成函数体
			prt.AppendLine("\n{");
			++prt.Indents;

			// 局部变量
			if (CurrMethod.LocalTypes.IsCollectionValid())
			{
				prt.AppendLine("// locals");
				for (int i = 0, sz = CurrMethod.LocalTypes.Count; i < sz; ++i)
				{
					var locType = CurrMethod.LocalTypes[i];
					prt.AppendFormatLine("{0} {1};",
						GenContext.GetTypeName(locType),
						LocalName(i));
				}
				prt.AppendLine();
			}

			// 临时变量
			if (SlotMap.Count > 0)
			{
				prt.AppendLine("// temps");
				foreach (var kv in SlotMap)
				{
					foreach (var stype in kv.Value)
					{
						prt.AppendFormatLine(
							"{0} {1};",
							stype.GetTypeName(),
							TempName(kv.Key, stype));
					}
				}
				prt.AppendLine();
			}

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

			--prt.Indents;
			prt.AppendLine("}");

			ImplCode += prt;
		}

		private void GenerateVFtn()
		{
			CodePrinter prt = new CodePrinter();

			// 函数签名
			prt.AppendFormat("void* {0}(uint32_t typeID)",
				GenContext.GetMethodName(CurrMethod, PrefixVFtn));

			DeclCode += prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			HashSet<MethodX> implSet = CurrMethod.OverrideImpls;
			if (!implSet.IsCollectionValid())
				implSet = new HashSet<MethodX>() { CurrMethod };

			prt.AppendLine("switch (typeID)\n{");
			++prt.Indents;

			foreach (MethodX implMetX in implSet)
			{
				RefTypeImpl(implMetX.DeclType);

				prt.AppendFormatLine("// {0}",
					implMetX.GetReplacedNameKey());
				prt.AppendFormatLine("case {0}: return (void*)&{1};",
					GenContext.GetTypeID(implMetX.DeclType),
					GenContext.GetMethodName(implMetX, PrefixMet));
			}

			--prt.Indents;
			prt.AppendLine("}");

			prt.AppendLine("abort();\nreturn 0;");

			--prt.Indents;
			prt.AppendLine("}");

			ImplCode += prt;
		}

		private void GenerateVMet()
		{
			CodePrinter prt = new CodePrinter();

			GenFuncDef(prt, PrefixVMet);
			DeclCode += prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			prt.AppendFormatLine("void* pftn = {0}(((cls_Object*){1})->TypeID);\nIL2CPP_ASSERT(pftn);",
				GenContext.GetMethodName(CurrMethod, PrefixVFtn),
				ArgName(0));

			if (CurrMethod.ReturnType.ElementType != ElementType.Void)
				prt.Append("return ");

			prt.Append("((");
			GenFuncType(prt);
			prt.Append(")pftn)(");

			for (int i = 0, sz = CurrMethod.ParamTypes.Count; i < sz; ++i)
			{
				if (i != 0)
					prt.Append(", ");
				prt.Append(ArgName(i));
			}

			prt.AppendLine(");");

			--prt.Indents;
			prt.AppendLine("}");

			ImplCode += prt;
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

				case Code.Call:
					inst.InstCode = GenCall((MethodX)operand);
					return;
				case Code.Callvirt:
					inst.InstCode = GenCall((MethodX)operand, PrefixVMet);
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
					GenLdc(inst, StackType.R4, AddFloatPostfix(operand.ToString()));
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
					GenBrCond(inst, (int)operand, GenBoolCond(false));
					return;

				case Code.Brtrue:
				case Code.Brtrue_S:
					GenBrCond(inst, (int)operand, GenBoolCond(true));
					return;

				case Code.Beq:
				case Code.Beq_S:
				case Code.Bge:
				case Code.Bge_S:
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Ble:
				case Code.Ble_S:
				case Code.Blt:
				case Code.Blt_S:
				case Code.Bne_Un:
				case Code.Bne_Un_S:
				case Code.Bge_Un:
				case Code.Bge_Un_S:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Ble_Un:
				case Code.Ble_Un_S:
				case Code.Blt_Un:
				case Code.Blt_Un_S:
					GenBrCond(inst, (int)operand, GenCompareCond(inst.OpCode));
					return;

				case Code.Ceq:
				case Code.Cgt:
				case Code.Cgt_Un:
				case Code.Clt:
				case Code.Clt_Un:
					GenLdc(inst, StackType.I4, '(' + GenCompareCond(inst.OpCode) + ')');
					return;

				case Code.Conv_I1:
					GenConv(inst, StackType.I4, "int8_t");
					return;
				case Code.Conv_I2:
					GenConv(inst, StackType.I4, "int16_t");
					return;
				case Code.Conv_I4:
					GenConv(inst, StackType.I4, "int32_t");
					return;
				case Code.Conv_I8:
					GenConv(inst, StackType.I8, "int64_t");
					return;
				case Code.Conv_U1:
					GenConv(inst, StackType.I4, "uint8_t");
					return;
				case Code.Conv_U2:
					GenConv(inst, StackType.I4, "uint16_t");
					return;
				case Code.Conv_U4:
					GenConv(inst, StackType.I4, "uint32_t");
					return;
				case Code.Conv_U8:
					GenConv(inst, StackType.I8, "uint64_t");
					return;
				case Code.Conv_R4:
					GenConv(inst, StackType.R4, "float");
					return;
				case Code.Conv_R8:
					GenConv(inst, StackType.R8, "double");
					return;
				case Code.Conv_R_Un:
					GenConv(inst, StackType.R8, "uintptr_t");
					return;
				case Code.Conv_I:
					GenConv(inst, StackType.Ptr, "intptr_t");
					return;
				case Code.Conv_U:
					GenConv(inst, StackType.Ptr, "uintptr_t");
					return;

				case Code.Add:
					GenBinOp(inst, " + ");
					return;
				case Code.Sub:
					GenBinOp(inst, " - ");
					return;
				case Code.Mul:
					GenBinOp(inst, " * ");
					return;
				case Code.Div:
					GenBinOp(inst, " / ");
					return;
				case Code.Rem:
					GenBinOp(inst, " % ");
					return;

				case Code.Newobj:
					GenNewObj(inst, (MethodX)operand);
					return;

				case Code.Newarr:
				case Code.Ldlen:
				case Code.Ldelema:
				case Code.Ldelem_I1:
				case Code.Ldelem_U1:
				case Code.Ldelem_I2:
				case Code.Ldelem_U2:
				case Code.Ldelem_I4:
				case Code.Ldelem_U4:
				case Code.Ldelem_I8:
				case Code.Ldelem_I:
				case Code.Ldelem_R4:
				case Code.Ldelem_R8:
				case Code.Ldelem_Ref:
				case Code.Ldelem:
				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_I:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Ref:
				case Code.Stelem:
					throw new ArgumentOutOfRangeException();
			}

			throw new NotImplementedException(inst.ToString());
		}

		private void GenDup(InstInfo inst)
		{
			var slotTop = Peek();
			var slotPush = Push(slotTop.SlotType);
			inst.InstCode = GenAssign(TempName(slotPush), TempName(slotTop), (TypeSig)null);
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
			RefValueTypeImpl(argType);
			var slotPush = isAddr ? Push(StackType.Ptr) : Push(ToStackType(argType));
			inst.InstCode = GenAssign(TempName(slotPush), (isAddr ? "&" : null) + ArgName(argID), slotPush.SlotType);
		}

		private void GenStarg(InstInfo inst, int argID)
		{
			Debug.Assert(argID < CurrMethod.ParamTypes.Count);
			var argType = CurrMethod.ParamTypes[argID];
			RefValueTypeImpl(argType);
			var slotPop = Pop();
			inst.InstCode = GenAssign(ArgName(argID), TempName(slotPop), argType);
		}

		private void GenLdloc(InstInfo inst, int locID, bool isAddr = false)
		{
			Debug.Assert(locID < CurrMethod.LocalTypes.Count);
			var locType = CurrMethod.LocalTypes[locID];
			RefValueTypeImpl(locType);
			var slotPush = isAddr ? Push(StackType.Ptr) : Push(ToStackType(locType));
			inst.InstCode = GenAssign(TempName(slotPush), (isAddr ? "&" : null) + LocalName(locID), slotPush.SlotType);
		}

		private void GenStloc(InstInfo inst, int locID)
		{
			Debug.Assert(locID < CurrMethod.LocalTypes.Count);
			var locType = CurrMethod.LocalTypes[locID];
			RefValueTypeImpl(locType);
			var slotPop = Pop();
			inst.InstCode = GenAssign(LocalName(locID), TempName(slotPop), locType);
		}

		private void GenBrCond(InstInfo inst, int labelID, string cond)
		{
			inst.InstCode = "if (" + cond + ") " + GenGoto(labelID);
		}

		private void GenConv(InstInfo inst, StackType stype, string cast)
		{
			var slotPop = Pop();
			var slotPush = Push(stype);

			if (cast == stype.GetTypeName())
				cast = null;

			inst.InstCode = GenAssign(
				TempName(slotPush),
				(cast != null ? '(' + cast + ')' : null) + TempName(slotPop),
				stype);
		}

		enum CompareKind
		{
			Eq,
			Ne,
			Ge,
			Gt,
			Le,
			Lt
		}

		private string GenCompareCond(OpCode opCode)
		{
			var slotPops = Pop(2);

			if (!IsBinaryCompareValid(slotPops[0].SlotType.Kind, slotPops[1].SlotType.Kind, opCode.Code))
				throw new InvalidOperationException();

			string lhs = TempName(slotPops[0]);
			string rhs = TempName(slotPops[1]);

			CompareKind cmp;
			bool isUn = false;

			switch (opCode.Code)
			{
				case Code.Beq:
				case Code.Beq_S:
				case Code.Ceq:
					cmp = CompareKind.Eq;
					break;
				case Code.Bge:
				case Code.Bge_S:
					cmp = CompareKind.Ge;
					break;
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Cgt:
					cmp = CompareKind.Gt;
					break;
				case Code.Ble:
				case Code.Ble_S:
					cmp = CompareKind.Le;
					break;
				case Code.Blt:
				case Code.Blt_S:
				case Code.Clt:
					cmp = CompareKind.Lt;
					break;
				case Code.Bne_Un:
				case Code.Bne_Un_S:
					cmp = CompareKind.Ne;
					break;
				case Code.Bge_Un:
				case Code.Bge_Un_S:
					cmp = CompareKind.Ge;
					isUn = true;
					break;
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Cgt_Un:
					cmp = CompareKind.Gt;
					isUn = true;
					break;
				case Code.Ble_Un:
				case Code.Ble_Un_S:
					cmp = CompareKind.Le;
					isUn = true;
					break;
				case Code.Blt_Un:
				case Code.Blt_Un_S:
				case Code.Clt_Un:
					cmp = CompareKind.Lt;
					isUn = true;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (cmp)
			{
				case CompareKind.Eq:
					return lhs + " == " + rhs;
				case CompareKind.Ne:
					return lhs + " != " + rhs;
				case CompareKind.Ge:
					{
						if (isUn)
							return '!' + (lhs + " < " + rhs);
						else
							return lhs + " >= " + rhs;
					}
				case CompareKind.Gt:
					{
						if (isUn)
							return '!' + (lhs + " <= " + rhs);
						else
							return lhs + " > " + rhs;
					}
				case CompareKind.Le:
					{
						if (isUn)
							return '!' + (lhs + " > " + rhs);
						else
							return lhs + " <= " + rhs;
					}
				case CompareKind.Lt:
					{
						if (isUn)
							return '!' + (lhs + " >= " + rhs);
						else
							return lhs + " < " + rhs;
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(cmp), cmp, null);
			}
		}

		private string GenBoolCond(bool b)
		{
			if (b)
				return TempName(Pop()) + " != 0";
			else
				return TempName(Pop()) + " == 0";
		}

		private void GenBinOp(InstInfo inst, string op)
		{
			var slotPops = Pop(2);

			if (!IsBinaryOpValid(slotPops[0].SlotType.Kind, slotPops[1].SlotType.Kind, out var retType, inst.OpCode.Code))
				throw new InvalidOperationException();

			var slotPush = Push(new StackType(retType));
			inst.InstCode = GenAssign(
				TempName(slotPush),
				'(' + TempName(slotPops[0]) + op + TempName(slotPops[1]) + ')',
				slotPush.SlotType);
		}

		private void GenReturn(InstInfo inst)
		{
			if (TypeStack.Count > 0)
			{
				Debug.Assert(TypeStack.Count == 1);
				var slotPop = Pop();
				inst.InstCode = "return " + CastType(CurrMethod.ReturnType) + TempName(slotPop) + ';';
			}
			else
				inst.InstCode = "return;";
		}

		private string GenCall(MethodX metX, string prefix = PrefixMet, List<SlotInfo> slotArgs = null)
		{
			RefTypeImpl(metX.DeclType);

			StringBuilder sb = new StringBuilder();
			sb.Append(GenContext.GetMethodName(metX, prefix));
			sb.Append('(');

			if (slotArgs == null)
			{
				int numArgs = metX.ParamTypes.Count;
				slotArgs = Pop(numArgs).ToList();
			}

			for (int i = 0, sz = slotArgs.Count; i < sz; ++i)
			{
				if (i != 0)
					sb.Append(", ");

				var argType = metX.ParamTypes[i];
				sb.AppendFormat("{0}{1}",
					CastType(argType),
					TempName(slotArgs[i]));
			}
			sb.Append(')');

			if (metX.ReturnType.ElementType != ElementType.Void)
			{
				var slotPush = Push(ToStackType(metX.ReturnType));
				return GenAssign(TempName(slotPush), sb.ToString(), slotPush.SlotType);
			}
			else
				return sb.ToString() + ';';
		}

		private void GenNewObj(InstInfo inst, MethodX metX)
		{
			TypeX tyX = metX.DeclType;
			if (tyX.IsValueType)
				throw new InvalidOperationException("newobj can't create value type");

			var ctorArgs = Pop(metX.ParamTypes.Count - 1);
			var newSlot = Push(StackType.Obj);

			string strAddSize = null;
			if (tyX.IsArrayType)
			{
				strAddSize = string.Format(" + sizeof({0})", GenContext.GetTypeName(tyX.GenArgs[0]));

				uint rank = tyX.ArrayInfo.Rank;
				if (rank == 1)
				{
					strAddSize += " * " + TempName(ctorArgs[0]) +
						// Length
						" + sizeof(int)";
				}
				else if (ctorArgs.Length == rank)
				{
					for (int i = 0; i < rank; ++i)
						strAddSize += " * " + TempName(ctorArgs[i]);
					// LowerBound+Size
					strAddSize += " + sizeof(int) * 2 * " + rank;
				}
				else if (ctorArgs.Length == rank * 2)
				{
					for (int i = 0; i < rank; ++i)
						strAddSize += " * " + TempName(ctorArgs[i * 2 + 1]);
					// LowerBound+Size
					strAddSize += " + sizeof(int) * 2 * " + rank;
				}
				else
					throw new ArgumentOutOfRangeException();
			}

			string strCode = GenAssign(
				TempName(newSlot),
				string.Format("IL2CPP_NEW(sizeof({0}){1}, {2}, {3})",
					GenContext.GetTypeName(tyX),
					strAddSize,
					GenContext.GetTypeID(tyX),
					GenContext.IsNoRefType(tyX) ? "1" : "0"),
				newSlot.SlotType);

			var ctorList = new List<SlotInfo>();
			ctorList.Add(newSlot);
			ctorList.AddRange(ctorArgs);
			strCode += '\n' + GenCall(metX, PrefixMet, ctorList);

			inst.InstCode = strCode;
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

		private string CastType(TypeSig tySig)
		{
			if (Helper.IsValueType(tySig))
				return "*(" + GenContext.GetTypeName(tySig) + "*)&";
			else
				return '(' + GenContext.GetTypeName(tySig) + ')';
		}

		private void RefTypeDecl(TypeSig tySig)
		{
			DeclDepends.Add(GenContext.GetTypeName(tySig));
		}

		private void RefValueTypeDecl(TypeSig tySig)
		{
			if (Helper.IsValueType(tySig))
				RefTypeDecl(tySig);
		}

		private void RefTypeImpl(TypeSig tySig)
		{
			ImplDepends.Add(GenContext.GetTypeName(tySig));
		}

		private void RefTypeImpl(TypeX tyX)
		{
			ImplDepends.Add(GenContext.GetTypeName(tyX));
		}

		private void RefValueTypeImpl(TypeSig tySig)
		{
			if (Helper.IsValueType(tySig))
				RefTypeImpl(tySig);
		}

		private static string GenAssign(string lhs, string rhs, StackType? stype)
		{
			return lhs + " = " + (stype != null ? CastType(stype.Value) : null) + rhs + ';';
		}

		private string GenAssign(string lhs, string rhs, TypeSig tySig)
		{
			return lhs + " = " + (tySig != null ? CastType(tySig) : null) + rhs + ';';
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

		private static string AddFloatPostfix(string str)
		{
			if (str.IndexOf(".", StringComparison.Ordinal) != -1)
				return str + 'f';
			else
				return str + ".0f";
		}

		private const string PrefixMet = "met_";
		private const string PrefixVMet = "vmet_";
		private const string PrefixVFtn = "vftn_";

		private static bool IsBinaryOpValid(StackTypeKind op1, StackTypeKind op2, out StackTypeKind retType, Code code)
		{
			retType = StackTypeKind.I4;
			switch (op1)
			{
				case StackTypeKind.I4:
					switch (op2)
					{
						case StackTypeKind.I4:
							retType = StackTypeKind.I4;
							return true;

						case StackTypeKind.Ptr:
							retType = StackTypeKind.Ptr;
							return true;

						case StackTypeKind.Ref:
							if (code == Code.Add)
							{
								retType = StackTypeKind.Ref;
								return true;
							}
							break;
					}
					return false;

				case StackTypeKind.I8:
					if (op2 == StackTypeKind.I8)
					{
						retType = StackTypeKind.I8;
						return true;
					}
					return false;

				case StackTypeKind.R4:
					if (op2 == StackTypeKind.R4)
					{
						retType = StackTypeKind.R4;
						return true;
					}
					if (op2 == StackTypeKind.R8)
					{
						retType = StackTypeKind.R8;
						return true;
					}
					return false;

				case StackTypeKind.R8:
					if (op2 == StackTypeKind.R4 || op2 == StackTypeKind.R8)
					{
						retType = StackTypeKind.R8;
						return true;
					}
					return false;

				case StackTypeKind.Ptr:
					switch (op2)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							retType = StackTypeKind.Ptr;
							return true;

						case StackTypeKind.Ref:
							if (code == Code.Add)
							{
								retType = StackTypeKind.Ref;
								return true;
							}
							break;
					}
					return false;

				case StackTypeKind.Ref:
					switch (op2)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							if (code == Code.Add || code == Code.Sub)
							{
								retType = StackTypeKind.Ref;
								return true;
							}
							break;

						case StackTypeKind.Ref:
							if (code == Code.Sub)
							{
								retType = StackTypeKind.Ptr;
								return true;
							}
							break;
					}
					return false;

				case StackTypeKind.Obj:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}

		private static bool IsBinaryCompareValid(StackTypeKind op1, StackTypeKind op2, Code code)
		{
			switch (op1)
			{
				case StackTypeKind.I4:
					switch (op2)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							return true;
					}
					return false;

				case StackTypeKind.I8:
					if (op2 == StackTypeKind.I8)
						return true;
					return false;

				case StackTypeKind.R4:
				case StackTypeKind.R8:
					return op2 == StackTypeKind.R4 || op2 == StackTypeKind.R8;

				case StackTypeKind.Ptr:
					switch (op2)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							return true;
						case StackTypeKind.Ref:
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

				case StackTypeKind.Ref:
					switch (op2)
					{
						case StackTypeKind.Ref:
							return true;
						case StackTypeKind.Ptr:
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

				case StackTypeKind.Obj:
					if (op2 == StackTypeKind.Obj)
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

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}
	}
}
