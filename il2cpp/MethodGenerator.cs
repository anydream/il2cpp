using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
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
				case StackTypeKind.Ref:
					return "uintptr_t";
				case StackTypeKind.Obj:
					return "cls_Object*";
				case StackTypeKind.ValueType:
					return TypeName;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public string GetSignedTypeName()
		{
			switch (Kind)
			{
				case StackTypeKind.I4:
					return "int32_t";
				case StackTypeKind.I8:
					return "int64_t";
				case StackTypeKind.Ptr:
				case StackTypeKind.Ref:
				case StackTypeKind.Obj:
					return "intptr_t";
			}
			return null;
		}

		public string GetUnsignedTypeName()
		{
			switch (Kind)
			{
				case StackTypeKind.I4:
					return "uint32_t";
				case StackTypeKind.I8:
					return "uint64_t";
				case StackTypeKind.Ptr:
				case StackTypeKind.Ref:
				case StackTypeKind.Obj:
					return "uintptr_t";
			}
			return null;
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

		public override string ToString()
		{
			return "tmp_" + SlotIndex + '_' + SlotType.GetPostfix();
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

		// 离开异常映射
		private readonly Dictionary<int, int> LeaveMap = new Dictionary<int, int>();
		private int LeaveCount;

		public readonly HashSet<string> DeclDepends = new HashSet<string>();
		public readonly HashSet<string> ImplDepends = new HashSet<string>();
		public readonly HashSet<string> StringDepends = new HashSet<string>();
		public string DeclCode;
		public string ImplCode;

		private int PushCount;
		private int PopCount;

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
			if (!SlotMap.TryGetValue(slot.SlotIndex, out var stSet))
			{
				stSet = new HashSet<StackType>();
				SlotMap.Add(slot.SlotIndex, stSet);
			}
			stSet.Add(slot.SlotType);
		}

		private int RegLeaveMap(int target)
		{
			if (!LeaveMap.TryGetValue(target, out int idx))
			{
				idx = ++LeaveCount;
				LeaveMap.Add(target, idx);
			}
			return idx;
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
			GenerateMet();
			GenerateVFtn();
			GenerateVMet();
		}

		private void GenerateMet()
		{
			if (CurrMethod.IsSkipProcessing)
				return;

			CodePrinter prt = new CodePrinter();

			if (CurrMethod.IsStatic && CurrMethod.Def.IsConstructor)
			{
				// 静态构造实现
				string onceFuncName = string.Format("once_{0}",
					GenContext.GetMethodName(CurrMethod, PrefixMet));
				prt.AppendFormatLine("static void {0}()",
					onceFuncName);
				prt.AppendLine("{");
				++prt.Indents;

				GenMethodImpl(prt);

				--prt.Indents;
				prt.AppendLine("}");

				// 静态构造调用包装
				CodePrinter prt2 = new CodePrinter();
				GenFuncDef(prt2, PrefixMet);
				DeclCode += prt2 + ";\n";

				prt.Append(prt2.ToString());
				prt.AppendLine("\n{");
				++prt.Indents;

				prt.AppendLine("static uintptr_t s_LockTid = 0;");
				prt.AppendLine("static int8_t s_OnceFlag = 0;");
				prt.AppendFormatLine("IL2CPP_CALL_ONCE(s_OnceFlag, s_LockTid, &{0});",
					onceFuncName);

				--prt.Indents;
				prt.AppendLine("}");

				ImplCode += prt;
			}
			else
			{
				GenFuncDef(prt, PrefixMet);
				DeclCode += prt + ";\n";

				CodePrinter prt2 = new CodePrinter();
				prt2.AppendLine("\n{");
				++prt2.Indents;

				bool isGen = GenMethodImpl(prt2);

				--prt2.Indents;
				prt2.AppendLine("}");

				if (isGen)
				{
					prt.Append(prt2.ToString());
					ImplCode += prt;
				}
			}
		}

		private bool GenMethodImpl(CodePrinter prt)
		{
			var instList = CurrMethod.InstList;
			if (instList == null)
			{
				// 生成内部实现
				return GenerateRuntimeImpl(prt);
			}

			// 添加异常处理块分支
			if (CurrMethod.ExHandlerList.IsCollectionValid())
			{
				Push(StackType.Obj);
				foreach (var handler in CurrMethod.ExHandlerList)
				{
					foreach (var chandler in handler.CombinedHandlers)
					{
						AddBranch(chandler.HandlerStart);
						if (chandler.FilterStart != -1)
							AddBranch(chandler.FilterStart);
					}
				}
				TypeStack.Clear();
			}

			// 构造指令代码
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

			if (!CurrMethod.IsStatic && CurrMethod.Def.IsConstructor)
			{
				// 构造函数里调用静态构造函数
				prt.Append(GenInvokeStaticCctor(CurrMethod.DeclType));
			}
			else if (CurrMethod.IsStatic && !CurrMethod.Def.IsConstructor)
			{
				// 对于引用了本类型静态字段的静态方法, 把调用静态构造放在最前面
				bool isGen = false;
				foreach (var inst in instList)
				{
					var code = inst.OpCode.Code;
					if (code == Code.Ldsfld ||
						code == Code.Ldsflda ||
						code == Code.Stsfld)
					{
						FieldX fldX = (FieldX)inst.Operand;
						if (fldX.DeclType == CurrMethod.DeclType)
						{
							isGen = true;
							break;
						}
					}
				}

				if (isGen)
					prt.Append(GenInvokeStaticCctor(CurrMethod.DeclType));
			}

			// 局部变量
			if (CurrMethod.LocalTypes.IsCollectionValid())
			{
				prt.AppendLine("// locals");
				bool isInitLocals = CurrMethod.Def.Body.InitLocals;
				for (int i = 0, sz = CurrMethod.LocalTypes.Count; i < sz; ++i)
				{
					var locType = CurrMethod.LocalTypes[i];
					prt.AppendFormatLine("{0} {1}{2};",
						GenContext.GetTypeName(locType),
						LocalName(i),
						isInitLocals ? " = " + GenContext.GetTypeDefaultValue(locType) : null);
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

			// 构造异常辅助变量
			if (CurrMethod.ExHandlerList.IsCollectionValid())
			{
				prt.AppendLine("// exceptions");
				prt.AppendLine("cls_Object* lastException = 0;");
				prt.AppendLine("int leaveTarget = 0;");
				prt.AppendLine();
			}

			// 生成代码体
			foreach (var inst in instList)
			{
				GenExHandlerEnd(inst.Offset, prt);

				if (inst.IsBrTarget)
				{
					--prt.Indents;
					prt.AppendLine(LabelName(inst.Offset) + ':');
					++prt.Indents;
				}

				GenExHandlerStart(inst, prt);

#if true
				prt.AppendLine("// " + inst);
#endif

				if (inst.InstCode != null)
					prt.AppendLine(inst.InstCode);
			}

			// 异常块扫尾
			GenExHandlerEnd(instList.Length, prt);

			return true;
		}

		private void GenerateVFtn()
		{
			if (!CurrMethod.IsVirtual)
				return;

			CodePrinter prt = new CodePrinter();

			// 函数签名
			prt.AppendFormat("void* {0}(uint32_t typeID)",
				GenContext.GetMethodName(CurrMethod, PrefixVFtn));

			DeclCode += prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			HashSet<MethodX> implSet = CurrMethod.OverrideImpls;
			if (!implSet.IsCollectionValid() && !CurrMethod.IsSkipProcessing)
				implSet = new HashSet<MethodX>() { CurrMethod };

			if (implSet.IsCollectionValid())
			{
				prt.AppendLine("switch (typeID)\n{");
				++prt.Indents;

				List<MethodX> implMets = new List<MethodX>(implSet);
				implMets.Sort((lhs, rhs) =>
					GenContext.GetTypeID(lhs.DeclType).CompareTo(GenContext.GetTypeID(rhs.DeclType)));

				foreach (MethodX implMetX in implMets)
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
			}

			prt.AppendLine("abort();");
			prt.AppendLine("return nullptr;");

			--prt.Indents;
			prt.AppendLine("}");

			ImplCode += prt;
		}

		private void GenerateVMet()
		{
			if (!CurrMethod.IsVirtual)
				return;

			CodePrinter prt = new CodePrinter();

			GenFuncDef(prt, PrefixVMet);
			DeclCode += prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			prt.AppendFormatLine("void* pftn = {0}(((cls_Object*){1})->TypeID);",
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

		private bool GenerateRuntimeImpl(CodePrinter prt)
		{
			if (CurrMethod.DeclType.IsArrayType)
			{
				TypeSig elemType = CurrMethod.DeclType.GenArgs[0];
				string metName = CurrMethod.Def.Name;
				int pCount = CurrMethod.ParamTypes.Count - 1;
				var arrayInfo = CurrMethod.DeclType.ArrayInfo;
				bool isSZArray = arrayInfo.IsSZArray;
				uint rank = arrayInfo.Rank;

				RefTypeImpl(elemType);

				if (metName == ".ctor")
				{
					if (isSZArray)
					{
						prt.AppendFormatLine("{0}->Rank = 0;\n{0}->Length = {1};",
							ArgName(0),
							ArgName(1));
					}
					else if (rank == pCount)
					{
						prt.AppendFormatLine("{0}->Rank = {1};",
							ArgName(0),
							rank);
						for (int i = 0; i < rank; ++i)
						{
							prt.AppendFormatLine("{0}->LowerBound{1} = 0;\n{0}->Size{1} = {2};",
								ArgName(0),
								i,
								ArgName(i + 1));
						}
					}
					else if (rank * 2 == pCount)
					{
						prt.AppendFormatLine("{0}->Rank = {1};",
							ArgName(0),
							rank);
						for (int i = 0; i < rank; ++i)
						{
							prt.AppendFormatLine("{0}->LowerBound{1} = {2};\n{0}->Size{1} = {3};",
								ArgName(0),
								i,
								ArgName(i * 2 + 1),
								ArgName(i * 2 + 2));
						}
					}
					else
						throw new ArgumentOutOfRangeException();

					return true;
				}
				else if (metName == "Get")
				{
					if (isSZArray)
					{
						prt.AppendFormatLine("IL2CPP_CHECK_RANGE(0, {0}->Length, {1});",
							ArgName(0),
							ArgName(1));
						prt.AppendFormatLine("return (({0}*)(&{1}[1]))[{2}];",
							GenContext.GetTypeName(elemType),
							ArgName(0),
							ArgName(1));
					}
					else if (rank == pCount)
					{
						GenerateMDArrayIndex(prt, rank);
						prt.AppendFormatLine("return (({0}*)(&{1}[1]))[index];",
							GenContext.GetTypeName(elemType),
							ArgName(0));
					}
					else
						throw new ArgumentOutOfRangeException();

					return true;
				}
				else if (metName == "Set")
				{
					if (isSZArray)
					{
						prt.AppendFormatLine("IL2CPP_CHECK_RANGE(0, {0}->Length, {1});",
							ArgName(0),
							ArgName(1));
						prt.AppendFormatLine("(({0}*)(&{1}[1]))[{2}] = {3};",
							GenContext.GetTypeName(elemType),
							ArgName(0),
							ArgName(1),
							ArgName(2));
					}
					else if (rank + 1 == pCount)
					{
						GenerateMDArrayIndex(prt, rank);
						prt.AppendFormatLine("(({0}*)(&{1}[1]))[index] = {2};",
							GenContext.GetTypeName(elemType),
							ArgName(0),
							ArgName(pCount));
					}
					else
						throw new ArgumentOutOfRangeException();

					return true;
				}
				else if (metName == "Address")
				{
					if (isSZArray)
					{
						prt.AppendFormatLine("IL2CPP_CHECK_RANGE(0, {0}->Length, {1});",
							ArgName(0),
							ArgName(1));
						prt.AppendFormatLine("return &(({0}*)(&{1}[1]))[{2}];",
							GenContext.GetTypeName(elemType),
							ArgName(0),
							ArgName(1));
					}
					else if (rank == pCount)
					{
						GenerateMDArrayIndex(prt, rank);
						prt.AppendFormatLine("return &(({0}*)(&{1}[1]))[index];",
							GenContext.GetTypeName(elemType),
							ArgName(0));
					}
					else
						throw new ArgumentOutOfRangeException();

					return true;
				}
			}
			else
				return RuntimeInternals.GenInternalMethod(CurrMethod, prt, GenContext);

			return false;
		}

		private void GenerateMDArrayIndex(CodePrinter prt, uint rank)
		{
			for (int i = 0; i < rank; ++i)
			{
				prt.AppendFormatLine("IL2CPP_CHECK_RANGE({0}->LowerBound{1}, {0}->Size{1}, {2});",
					ArgName(0),
					i,
					ArgName(i + 1));
			}

			prt.AppendLine("uintptr_t index =");
			++prt.Indents;
			for (int i = 0; i < rank; ++i)
			{
				prt.AppendFormat("({0} - {1}->LowerBound{2})",
					ArgName(i + 1),
					ArgName(0),
					i);

				for (int j = i + 1; j < rank; ++j)
				{
					prt.AppendFormat(" * {0}->Size{1}",
						ArgName(0),
						j);
				}

				if (i == rank - 1)
					prt.AppendLine(";");
				else
					prt.AppendLine(" +");
			}
			--prt.Indents;
		}

		private void GenExHandlerStart(InstInfo inst, CodePrinter prt)
		{
			var handlerList = CurrMethod.ExHandlerList;
			if (!handlerList.IsCollectionValid())
				return;

			int offset = inst.Offset;

			foreach (var handler in handlerList)
			{
				if (offset == handler.TryStart)
				{
					prt.AppendLine("try\n{");
					++prt.Indents;
				}
				foreach (var chandler in handler.CombinedHandlers)
				{
					if (offset == chandler.HandlerStart)
					{
						if (chandler.HandlerType == ExceptionHandlerType.Catch)
						{
							RefTypeImpl(chandler.CatchType);

							prt.AppendFormatLine("if (istype_{0}({1}->TypeID))",
								GenContext.GetTypeName(chandler.CatchType),
								TempName(0, StackType.Obj));
							prt.AppendLine("{");
							++prt.Indents;
						}
						else if (chandler.HandlerType == ExceptionHandlerType.Filter)
						{
							prt.AppendLine("{");
							++prt.Indents;
						}
					}
				}

				if (offset >= handler.HandlerStart && offset < handler.HandlerEnd &&
					inst.OpCode.Code == Code.Endfinally)
				{
					if (handler.HandlerType == ExceptionHandlerType.Finally)
					{
						prt.AppendLine("if (lastException) IL2CPP_THROW(lastException);");

						if (handler.LeaveTargets.IsCollectionValid())
						{
							prt.AppendLine("switch (leaveTarget)\n{");
							++prt.Indents;

							List<int> leaveTargets = new List<int>(handler.LeaveTargets);
							leaveTargets.Sort((lhs, rhs) => LeaveMap[lhs].CompareTo(LeaveMap[rhs]));
							foreach (int target in leaveTargets)
							{
								prt.AppendFormatLine("case {0}: {1}",
									LeaveMap[target],
									GenGoto(target));
							}

							--prt.Indents;
							prt.AppendLine("}");
						}
						prt.AppendLine("abort();");
					}
					else
					{
						Debug.Assert(handler.HandlerType == ExceptionHandlerType.Fault);
						prt.AppendLine("IL2CPP_THROW(lastException);");
					}
				}
			}
		}

		private void GenExHandlerEnd(int offset, CodePrinter prt)
		{
			var hlist = CurrMethod.ExHandlerList;
			if (!hlist.IsCollectionValid())
				return;

			var rhlist = new List<ExHandlerInfo>(hlist);
			rhlist.Reverse();

			foreach (var info in rhlist)
			{
				for (int i = 0, sz = info.CombinedHandlers.Count; i < sz; ++i)
				{
					var chandler = info.CombinedHandlers[i];

					if (offset == chandler.HandlerEnd)
					{
						if (chandler.HandlerType == ExceptionHandlerType.Catch ||
							chandler.HandlerType == ExceptionHandlerType.Filter)
						{
							--prt.Indents;
							prt.AppendLine("}");

							if (i == sz - 1)
							{
								// 最后一项往上抛异常
								prt.AppendLine("IL2CPP_THROW(lastException);");
							}
							else
							{
								// 跳到下一个异常处理块
								var cnext = info.CombinedHandlers[i + 1];
								prt.AppendLine(GenGoto(cnext.HandlerOrFilterStart));
							}
						}
					}
				}

				if (offset == info.TryEnd)
				{
					--prt.Indents;
					prt.AppendLine("}\ncatch (il2cppException& ex)\n{");
					++prt.Indents;

					prt.AppendLine("lastException = ex.ExceptionPtr;");
					prt.AppendLine(GenAssign(TempName(0, StackType.Obj), "lastException", StackType.Obj));

					--prt.Indents;
					prt.AppendLine("}");

					prt.AppendLine(GenGoto(info.HandlerOrFilterStart));
				}
			}
		}

		private string GenInvokeStaticCctor(TypeX tyX)
		{
			MethodX cctor = tyX.CctorMethod;
			if (cctor != null && cctor != CurrMethod)
				return string.Format("{0}();\n", GenContext.GetMethodName(cctor, PrefixMet));
			return null;
		}

		private bool GenerateInst(InstInfo inst, ref int currIP)
		{
			if (inst.IsGenerated)
				return false;
			inst.IsGenerated = true;

			PushCount = PopCount = 0;

			GenerateInstCode(inst);

			var opCode = inst.OpCode;
			var operand = inst.Operand;

			switch (opCode.StackBehaviourPop)
			{
				case StackBehaviour.Pop0:
					Debug.Assert(PopCount == 0);
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popi:
				case StackBehaviour.Popref:
					Debug.Assert(PopCount == 1);
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					Debug.Assert(PopCount == 2);
					break;
				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
				case StackBehaviour.Popref_popi_pop1:
					Debug.Assert(PopCount == 3);
					break;
				case StackBehaviour.PopAll:
					Debug.Assert(TypeStack.Count == 0);
					break;
				case StackBehaviour.Varpop:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			switch (opCode.StackBehaviourPush)
			{
				case StackBehaviour.Push0:
					Debug.Assert(PushCount == 0);
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					Debug.Assert(PushCount == 1);
					break;
				case StackBehaviour.Push1_push1:
					Debug.Assert(PushCount == 2);
					break;
				case StackBehaviour.Varpush:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

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

				case Code.Ldnull:
					GenLdc(inst, StackType.Obj, "nullptr");
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
					GenLdc(inst, StackType.R4, AddFloatPostfix(((float)operand).ToString("R")));
					return;
				case Code.Ldc_R8:
					GenLdc(inst, StackType.R8, ((double)operand).ToString("R"));
					return;

				case Code.Ldstr:
					GenLdstr(inst, (string)operand);
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

				case Code.Switch:
					GenSwitch(inst, (int[])operand);
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
					GenConv(inst, StackType.R8, "uint32_t");
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
					GenRem(inst);
					return;
				case Code.Neg:
					GenUnaryOp(inst, "-");
					return;

				case Code.And:
					GenIntBinOp(inst, " & ");
					return;
				case Code.Or:
					GenIntBinOp(inst, " | ");
					return;
				case Code.Xor:
					GenIntBinOp(inst, " ^ ");
					return;
				case Code.Not:
					GenIntUnaryOp(inst, "~");
					return;
				case Code.Shl:
					GenShiftOp(inst, " << ");
					return;
				case Code.Shr:
					GenShiftOp(inst, " >> ");
					return;

				case Code.Newobj:
					GenNewobj(inst, (MethodX)operand);
					return;

				case Code.Box:
					GenBox(inst, (TypeX)operand);
					return;
				case Code.Unbox:
					GenUnbox(inst, (TypeX)operand, true);
					return;
				case Code.Unbox_Any:
					GenUnbox(inst, (TypeX)operand);
					return;

				case Code.Isinst:
					GenIsinst(inst, (TypeX)operand);
					return;

				case Code.Ldfld:
					GenLdfld(inst, (FieldX)operand);
					return;
				case Code.Ldflda:
					GenLdfld(inst, (FieldX)operand, true);
					return;
				case Code.Ldsfld:
					GenLdsfld(inst, (FieldX)operand);
					return;
				case Code.Ldsflda:
					GenLdsfld(inst, (FieldX)operand, true);
					return;

				case Code.Stfld:
					GenStfld(inst, (FieldX)operand);
					return;
				case Code.Stsfld:
					GenStsfld(inst, (FieldX)operand);
					return;

				case Code.Initobj:
					GenInitobj(inst, (TypeX)operand);
					return;
				case Code.Ldobj:
					GenLdobj(inst, ((TypeX)operand).GetTypeSig());
					return;
				case Code.Stobj:
					GenStobj(inst, ((TypeX)operand).GetTypeSig());
					return;

				case Code.Ldind_I1:
					GenLdobj(inst, GetCorLibTypes().SByte);
					return;
				case Code.Ldind_I2:
					GenLdobj(inst, GetCorLibTypes().Int16);
					return;
				case Code.Ldind_I4:
					GenLdobj(inst, GetCorLibTypes().Int32);
					return;
				case Code.Ldind_I8:
					GenLdobj(inst, GetCorLibTypes().Int64);
					return;
				case Code.Ldind_U1:
					GenLdobj(inst, GetCorLibTypes().Byte);
					return;
				case Code.Ldind_U2:
					GenLdobj(inst, GetCorLibTypes().UInt16);
					return;
				case Code.Ldind_U4:
					GenLdobj(inst, GetCorLibTypes().UInt32);
					return;
				case Code.Ldind_R4:
					GenLdobj(inst, GetCorLibTypes().Single);
					return;
				case Code.Ldind_R8:
					GenLdobj(inst, GetCorLibTypes().Double);
					return;
				case Code.Ldind_I:
					GenLdobj(inst, GetCorLibTypes().IntPtr);
					return;
				case Code.Ldind_Ref:
					GenLdobj(inst, GetCorLibTypes().Object);
					return;

				case Code.Stind_I1:
					GenStobj(inst, GetCorLibTypes().SByte);
					return;
				case Code.Stind_I2:
					GenStobj(inst, GetCorLibTypes().Int16);
					return;
				case Code.Stind_I4:
					GenStobj(inst, GetCorLibTypes().Int32);
					return;
				case Code.Stind_I8:
					GenStobj(inst, GetCorLibTypes().Int64);
					return;
				case Code.Stind_R4:
					GenStobj(inst, GetCorLibTypes().Single);
					return;
				case Code.Stind_R8:
					GenStobj(inst, GetCorLibTypes().Double);
					return;
				case Code.Stind_I:
					GenStobj(inst, GetCorLibTypes().IntPtr);
					return;
				case Code.Stind_Ref:
					GenStobj(inst, GetCorLibTypes().Object);
					return;

				case Code.Sizeof:
					GenSizeof(inst, (TypeX)operand);
					return;

				case Code.Throw:
					GenThrow(inst);
					return;
				case Code.Rethrow:
					inst.InstCode = "IL2CPP_THROW(lastException);";
					return;

				case Code.Endfilter:
					GenEndfilter(inst);
					return;

				case Code.Endfinally:
					TypeStack.Clear();
					return;

				case Code.Leave:
				case Code.Leave_S:
					GenLeave(inst, (int)operand);
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
			++PopCount;
			++PushCount;
		}

		private void GenLdc(InstInfo inst, StackType stype, string val)
		{
			var slotPush = Push(stype);
			inst.InstCode = GenAssign(TempName(slotPush), val, stype);
		}

		private void GenLdstr(InstInfo inst, string str)
		{
			StringDepends.Add(str);
			string constName = GenContext.StrGen.AddString(str);
			GenLdc(inst, StackType.Obj, "&" + constName);
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

		private void GenSwitch(InstInfo inst, int[] targets)
		{
			var slotPop = Pop();

			CodePrinter prt = new CodePrinter();
			prt.AppendFormatLine("switch ((uint32_t){0})",
				TempName(slotPop));

			prt.AppendLine("{");
			++prt.Indents;

			for (int i = 0; i < targets.Length; ++i)
			{
				prt.AppendFormatLine("case {0}: {1}",
					i,
					GenGoto(targets[i]));
			}

			--prt.Indents;
			prt.AppendLine("}");

			inst.InstCode = prt.ToString();
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
			var slotLhs = slotPops[0];
			var slotRhs = slotPops[1];

			if (!IsBinaryCompareValid(slotLhs.SlotType.Kind, slotRhs.SlotType.Kind, opCode.Code))
				throw new InvalidOperationException();

			string lhs = TempName(slotLhs);
			string rhs = TempName(slotRhs);

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

			string castType;
			if (isUn)
			{
				castType = slotLhs.SlotType.GetUnsignedTypeName();
				Debug.Assert(castType == slotRhs.SlotType.GetUnsignedTypeName());
			}
			else
			{
				castType = slotLhs.SlotType.GetSignedTypeName();
				Debug.Assert(castType == slotRhs.SlotType.GetSignedTypeName());
			}

			switch (cmp)
			{
				case CompareKind.Eq:
					return GenCompare(false, lhs, "==", rhs, castType);
				case CompareKind.Ne:
					return GenCompare(false, lhs, "!=", rhs, castType);
				case CompareKind.Ge:
					{
						if (isUn)
							return GenCompare(true, lhs, "<", rhs, castType);
						else
							return GenCompare(false, lhs, ">=", rhs, castType);
					}
				case CompareKind.Gt:
					{
						if (isUn)
							return GenCompare(true, lhs, "<=", rhs, castType);
						else
							return GenCompare(false, lhs, ">", rhs, castType);
					}
				case CompareKind.Le:
					{
						if (isUn)
							return GenCompare(true, lhs, ">", rhs, castType);
						else
							return GenCompare(false, lhs, "<=", rhs, castType);
					}
				case CompareKind.Lt:
					{
						if (isUn)
							return GenCompare(true, lhs, ">=", rhs, castType);
						else
							return GenCompare(false, lhs, "<", rhs, castType);
					}
				default:
					throw new ArgumentOutOfRangeException(nameof(cmp), cmp, null);
			}
		}

		private static string GenCompare(bool neg, string lhs, string op, string rhs, string cast)
		{
			string boolToInt = " ? 1 : 0";
			if (cast != null)
				cast = '(' + cast + ')';
			if (neg)
				return "!(" + cast + lhs + ' ' + op + ' ' + cast + rhs + ')' + boolToInt;
			return cast + lhs + ' ' + op + ' ' + cast + rhs + boolToInt;
		}

		private string GenBoolCond(bool b)
		{
			if (b)
				return TempName(Pop()) + " != 0";
			else
				return TempName(Pop()) + " == 0";
		}

		private void GenRem(InstInfo inst)
		{
			var slotPops = Pop(2);
			var op1 = slotPops[0];
			var op2 = slotPops[1];

			if (!IsBinaryOpValid(op1.SlotType.Kind, op2.SlotType.Kind, out var retType, inst.OpCode.Code))
				throw new InvalidOperationException();

			var slotPush = Push(new StackType(retType));
			if (retType == StackTypeKind.R4 || retType == StackTypeKind.R8)
			{
				inst.InstCode = GenAssign(
					TempName(slotPush),
					"IL2CPP_REMAINDER(" + TempName(op1) + ", " + TempName(op2) + ')',
					slotPush.SlotType);
			}
			else
			{
				inst.InstCode = GenAssign(
					TempName(slotPush),
					'(' + TempName(op1) + " % " + TempName(op2) + ')',
					slotPush.SlotType);
			}
		}

		private void GenBinOp(InstInfo inst, string op)
		{
			var slotPops = Pop(2);
			var op1 = slotPops[0];
			var op2 = slotPops[1];

			if (!IsBinaryOpValid(op1.SlotType.Kind, op2.SlotType.Kind, out var retType, inst.OpCode.Code))
				throw new InvalidOperationException();

			var slotPush = Push(new StackType(retType));
			inst.InstCode = GenAssign(
				TempName(slotPush),
				'(' + TempName(op1) + op + TempName(op2) + ')',
				slotPush.SlotType);
		}

		private void GenUnaryOp(InstInfo inst, string op)
		{
			var slotPop = Pop();

			var kind = slotPop.SlotType.Kind;
			if (kind != StackTypeKind.I4 && kind != StackTypeKind.I8 && kind != StackTypeKind.Ptr &&
				kind != StackTypeKind.R4 && kind != StackTypeKind.R8)
				throw new InvalidOperationException();

			var slotPush = Push(slotPop.SlotType);
			inst.InstCode = GenAssign(
				TempName(slotPush),
				op + TempName(slotPop),
				slotPush.SlotType);
		}

		private void GenIntBinOp(InstInfo inst, string op)
		{
			var slotPops = Pop(2);
			var op1 = slotPops[0];
			var op2 = slotPops[1];

			if (!IsIntegerOpValid(op1.SlotType.Kind, op2.SlotType.Kind, out var retType))
				throw new InvalidOperationException();

			var slotPush = Push(new StackType(retType));
			inst.InstCode = GenAssign(
				TempName(slotPush),
				'(' + TempName(op1) + op + TempName(op2) + ')',
				slotPush.SlotType);
		}

		private void GenIntUnaryOp(InstInfo inst, string op)
		{
			var slotPop = Pop();

			var kind = slotPop.SlotType.Kind;
			if (kind != StackTypeKind.I4 && kind != StackTypeKind.I8 && kind != StackTypeKind.Ptr)
				throw new InvalidOperationException();

			var slotPush = Push(slotPop.SlotType);
			inst.InstCode = GenAssign(
				TempName(slotPush),
				op + TempName(slotPop),
				slotPush.SlotType);
		}

		private void GenShiftOp(InstInfo inst, string op)
		{
			var slotPops = Pop(2);
			var op1 = slotPops[0];
			var op2 = slotPops[1];

			if (!IsShiftOpValid(op1.SlotType.Kind, op2.SlotType.Kind, out var retType))
				throw new InvalidOperationException();

			var slotPush = Push(new StackType(retType));
			inst.InstCode = GenAssign(
				TempName(slotPush),
				'(' + TempName(op1) + op + TempName(op2) + ')',
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

		private string GenCall(MethodX metX, string prefix = PrefixMet, List<SlotInfo> slotArgs = null, bool isArg0ValueType = false)
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

				sb.AppendFormat("{0}{1}{2}",
					CastType(argType),
					isArg0ValueType && i == 0 ? "&" : null,
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

		private void GenNewobj(InstInfo inst, MethodX metX)
		{
			TypeX tyX = metX.DeclType;

			var ctorArgs = Pop(metX.ParamTypes.Count - 1);

			if (tyX.IsValueType)
			{
				var slotPush = Push(ToStackType(tyX.GetTypeSig()));
				var ctorList = new List<SlotInfo>();
				ctorList.Add(slotPush);
				ctorList.AddRange(ctorArgs);
				inst.InstCode = GenCall(metX, PrefixMet, ctorList, true);
			}
			else
			{
				var slotPush = Push(StackType.Obj);

				string strAddSize = null;
				if (tyX.IsArrayType)
				{
					var elemType = tyX.GenArgs[0];
					RefTypeImpl(elemType);

					strAddSize = string.Format(" + sizeof({0})",
						GenContext.GetTypeName(elemType));

					uint rank = tyX.ArrayInfo.Rank;
					if (rank == ctorArgs.Length)
					{
						for (int i = 0; i < rank; ++i)
							strAddSize += " * " + TempName(ctorArgs[i]);
					}
					else if (rank * 2 == ctorArgs.Length)
					{
						for (int i = 0; i < rank; ++i)
							strAddSize += " * " + TempName(ctorArgs[i * 2 + 1]);
					}
					else
						throw new ArgumentOutOfRangeException();
				}

				string strCode = GenAssign(
					TempName(slotPush),
					string.Format("IL2CPP_NEW(sizeof({0}){1}, {2}, {3})",
						GenContext.GetTypeName(tyX),
						strAddSize,
						GenContext.GetTypeID(tyX),
						GenContext.IsNoRefType(tyX) ? "1" : "0"),
					slotPush.SlotType);

				var ctorList = new List<SlotInfo>();
				ctorList.Add(slotPush);
				ctorList.AddRange(ctorArgs);
				strCode += '\n' + GenCall(metX, PrefixMet, ctorList);

				inst.InstCode = strCode;
			}
		}

		private void GenBox(InstInfo inst, TypeX tyX)
		{
			var slotPop = Pop();
			var slotPush = Push(StackType.Obj);

			CodePrinter prt = new CodePrinter();

			FieldX fldNullableValue = null;
			if (tyX.IsNullableType)
			{
				FieldX fldHasValue = tyX.Fields.FirstOrDefault(fldX => fldX.Def.FieldType.ElementType == ElementType.Boolean);
				fldNullableValue = tyX.Fields.FirstOrDefault(fldX => fldX.Def.FieldType.ElementType == ElementType.Var);

				prt.AppendFormatLine("if ({0}.{1})",
					TempName(slotPop),
					GenContext.GetFieldName(fldHasValue));
				prt.AppendLine("{");
				++prt.Indents;

				tyX = tyX.NullableType;
				Debug.Assert(tyX.IsValueType);
			}

			if (tyX.IsValueType)
			{
				TypeX boxedTyX = tyX.BoxedType;
				Debug.Assert(boxedTyX != null);
				RefTypeImpl(boxedTyX);

				prt.AppendLine(GenAssign(
					TempName(slotPush),
					string.Format("IL2CPP_NEW(sizeof({0}), {1}, {2})",
						GenContext.GetTypeName(boxedTyX),
						GenContext.GetTypeID(boxedTyX),
						GenContext.IsNoRefType(boxedTyX) ? "1" : "0"),
					slotPush.SlotType));

				string rhs;
				if (fldNullableValue != null)
				{
					rhs = string.Format("{0}.{1}",
						TempName(slotPop),
						GenContext.GetFieldName(fldNullableValue));
				}
				else
					rhs = TempName(slotPop);

				FieldX valueFldX = boxedTyX.Fields.First();
				prt.AppendLine(GenAssign(
					string.Format("(({0}*){1})->{2}",
						GenContext.GetTypeName(boxedTyX),
						TempName(slotPush),
						GenContext.GetFieldName(valueFldX)),
					rhs,
					valueFldX.FieldType));
			}
			else
			{
				// 引用类型不做处理
			}

			if (fldNullableValue != null)
			{
				--prt.Indents;
				prt.AppendLine("}\nelse");
				++prt.Indents;
				prt.AppendLine(GenAssign(
					TempName(slotPush),
					"nullptr",
					slotPush.SlotType));
				--prt.Indents;
			}

			inst.InstCode = prt.ToString();
		}

		private void GenUnbox(InstInfo inst, TypeX tyX, bool isAddr = false)
		{
			var slotPop = Pop();

			if (tyX.IsNullableType)
			{
				tyX = tyX.NullableType;
				Debug.Assert(tyX != null);
			}

			Debug.Assert(tyX.IsValueType);

			SlotInfo slotPush;
			if (isAddr)
				slotPush = Push(StackType.Ptr);
			else
				slotPush = Push(ToStackType(tyX.GetTypeSig()));

			tyX = tyX.BoxedType;
			Debug.Assert(tyX != null);
			RefTypeImpl(tyX);

			inst.InstCode = GenAssign(
				TempName(slotPush),
				string.Format("{0}(({1}*){2})->{3}",
					isAddr ? "&" : null,
					GenContext.GetTypeName(tyX),
					TempName(slotPop),
					GenContext.GetFieldName(tyX.Fields.First())),
				slotPush.SlotType);
		}

		private void GenIsinst(InstInfo inst, TypeX tyX)
		{
			var slotPop = Pop();
			var slotPush = Push(StackType.Obj);

			if (tyX.IsNullableType)
			{
				tyX = tyX.NullableType;
				Debug.Assert(tyX != null);
				Debug.Assert(tyX.IsValueType);
			}

			if (tyX.IsValueType)
			{
				tyX = tyX.BoxedType;
				Debug.Assert(tyX != null);
			}

			RefTypeImpl(tyX);
			inst.InstCode = GenAssign(
				TempName(slotPush),
				string.Format("(({0} && istype_{1}({0}->TypeID)) ? {0} : nullptr)",
					TempName(slotPop),
					GenContext.GetTypeName(tyX)),
				slotPush.SlotType);
		}

		private void GenLdfld(InstInfo inst, FieldX fldX, bool isAddr = false)
		{
			RefTypeImpl(fldX.DeclType);

			var slotPop = Pop();

			SlotInfo slotPush;
			if (isAddr)
				slotPush = Push(StackType.Ref);
			else
				slotPush = Push(ToStackType(fldX.FieldType));

			if (slotPop.SlotType.Kind == StackTypeKind.ValueType)
			{
				inst.InstCode = GenAssign(
					TempName(slotPush),
					string.Format("{0}{1}.{2}",
						isAddr ? "&" : null,
						TempName(slotPop),
						GenContext.GetFieldName(fldX)),
					slotPush.SlotType);
			}
			else
			{
				inst.InstCode = GenAssign(
					TempName(slotPush),
					string.Format("{0}(({1}*){2})->{3}",
						isAddr ? "&" : null,
						GenContext.GetTypeName(fldX.DeclType),
						TempName(slotPop),
						GenContext.GetFieldName(fldX)),
					slotPush.SlotType);
			}
		}

		private void GenStfld(InstInfo inst, FieldX fldX)
		{
			RefTypeImpl(fldX.DeclType);

			var slotPops = Pop(2);
			var slotObj = slotPops[0];
			var slotVal = slotPops[1];

			inst.InstCode = GenAssign(
				string.Format("(({0}*){1})->{2}",
					GenContext.GetTypeName(fldX.DeclType),
					TempName(slotObj),
					GenContext.GetFieldName(fldX)),
				TempName(slotVal),
				fldX.FieldType);
		}

		private void GenLdsfld(InstInfo inst, FieldX fldX, bool isAddr = false)
		{
			Debug.Assert(fldX.IsStatic);
			RefTypeImpl(fldX.DeclType);

			SlotInfo slotPush;
			if (isAddr)
				slotPush = Push(StackType.Ref);
			else
				slotPush = Push(ToStackType(fldX.FieldType));

			inst.InstCode =
				(fldX.DeclType != CurrMethod.DeclType ? GenInvokeStaticCctor(fldX.DeclType) : null) +
				GenAssign(
					TempName(slotPush),
					string.Format("{0}{1}",
						isAddr ? "&" : null,
						GenContext.GetFieldName(fldX)),
					slotPush.SlotType);
		}

		private void GenStsfld(InstInfo inst, FieldX fldX)
		{
			Debug.Assert(fldX.IsStatic);
			RefTypeImpl(fldX.DeclType);

			var slotPop = Pop();

			inst.InstCode =
				(fldX.DeclType != CurrMethod.DeclType ? GenInvokeStaticCctor(fldX.DeclType) : null) +
				GenAssign(
					GenContext.GetFieldName(fldX),
					TempName(slotPop),
					fldX.FieldType);
		}

		private void GenInitobj(InstInfo inst, TypeX tyX)
		{
			var slotPop = Pop();

			if (tyX.IsValueType)
			{
				RefTypeImpl(tyX);
				string typeName = GenContext.GetTypeName(tyX);
				inst.InstCode = GenAssign(
					"*(" + typeName + "*)" + TempName(slotPop),
					typeName + "()",
					(TypeSig)null);
			}
			else
			{
				inst.InstCode = GenAssign(
					TempName(slotPop),
					"nullptr",
					slotPop.SlotType);
			}
		}

		private void GenLdobj(InstInfo inst, TypeSig tySig)
		{
			RefTypeImpl(tySig);

			var slotPop = Pop();
			var slotPush = Push(ToStackType(tySig));

			inst.InstCode = GenAssign(
				TempName(slotPush),
				string.Format("*({0}*){1}",
					GenContext.GetTypeName(tySig),
					TempName(slotPop)),
				slotPush.SlotType);
		}

		private void GenStobj(InstInfo inst, TypeSig tySig)
		{
			RefTypeImpl(tySig);

			var slotPops = Pop(2);
			var slotDest = slotPops[0];
			var slotSrc = slotPops[1];

			inst.InstCode = GenAssign(
				string.Format("*({0}*){1}",
					GenContext.GetTypeName(tySig),
					TempName(slotDest)),
				TempName(slotSrc),
				tySig);
		}

		private void GenSizeof(InstInfo inst, TypeX tyX)
		{
			var slotPush = Push(StackType.I4);

			if (tyX.IsValueType)
			{
				RefTypeImpl(tyX);

				inst.InstCode = GenAssign(
					TempName(slotPush),
					string.Format("sizeof({0})",
						GenContext.GetTypeName(tyX)),
					slotPush.SlotType);
			}
			else
			{
				inst.InstCode = GenAssign(
					TempName(slotPush),
					"sizeof(uintptr_t)",
					slotPush.SlotType);
			}
		}

		private void GenThrow(InstInfo inst)
		{
			var slotPop = Pop();
			inst.InstCode = string.Format("IL2CPP_THROW({0});",
				TempName(slotPop));
		}

		private void GenEndfilter(InstInfo inst)
		{
			var slotPop = Pop();
			inst.InstCode = string.Format("if ({0} == 1)",
				TempName(slotPop));
		}

		private void GenLeave(InstInfo inst, int target)
		{
			inst.InstCode = "lastException = 0;\n";

			var leaveHandlers = GetLeaveThroughHandlers(inst.Offset, target);
			if (leaveHandlers.IsCollectionValid())
			{
				foreach (var handler in leaveHandlers)
					handler.AddLeaveTarget(target);

				inst.InstCode += string.Format("leaveTarget = {0};\n{1}",
					RegLeaveMap(target),
					GenGoto(leaveHandlers[0].HandlerStart));
			}
			else
			{
				inst.InstCode += GenGoto(target);
			}
		}

		private List<ExHandlerInfo> GetLeaveThroughHandlers(int offset, int target)
		{
			Debug.Assert(CurrMethod.ExHandlerList.IsCollectionValid());

			List<ExHandlerInfo> result = new List<ExHandlerInfo>();
			foreach (var handler in CurrMethod.ExHandlerList)
			{
				if (handler.HandlerType == ExceptionHandlerType.Finally)
				{
					if (offset >= handler.TryStart && offset < handler.TryEnd &&
						!(target >= handler.TryStart && target < handler.TryEnd))
					{
						result.Add(handler);
					}
				}
			}
			return result;
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
			{
				if (Helper.IsEnumType(tySig, out var enumTySig))
					return ToStackType(enumTySig);

				TypeX tyX = GenContext.GetTypeBySig(tySig);
				return new StackType(GenContext.GetTypeName(tyX));
			}

			return StackType.Obj;
		}

		private static string CastType(StackType stype)
		{
			if (stype.Kind == StackTypeKind.ValueType)
				return null;
			else
				return '(' + stype.GetTypeName() + ')';
		}

		private string CastType(TypeSig tySig)
		{
			if (tySig.ElementType == ElementType.ValueType ||
				tySig.ElementType == ElementType.GenericInst && tySig.IsValueType)
				return null;
			else
				return '(' + GenContext.GetTypeName(tySig) + ')';
		}

		private ICorLibTypes GetCorLibTypes()
		{
			return GenContext.TypeMgr.Context.CorLibTypes;
		}

		private void RefTypeDecl(TypeSig tySig)
		{
			TypeX tyX = GenContext.GetTypeBySig(tySig);
			if (tyX != null)
				DeclDepends.Add(GenContext.GetTypeName(tyX));
		}

		private void RefValueTypeDecl(TypeSig tySig)
		{
			if (tySig.IsValueType)
				RefTypeDecl(tySig);
		}

		private void RefTypeImpl(TypeSig tySig)
		{
			TypeX tyX = GenContext.GetTypeBySig(tySig);
			if (tyX != null)
				ImplDepends.Add(GenContext.GetTypeName(tyX));
		}

		private void RefTypeImpl(TypeX tyX)
		{
			ImplDepends.Add(GenContext.GetTypeName(tyX));
		}

		private void RefValueTypeImpl(TypeSig tySig)
		{
			if (tySig.IsValueType)
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

		private string GenGoto(int labelID)
		{
			Debug.Assert(CurrMethod.InstList[labelID].IsBrTarget);
			return "goto " + LabelName(labelID) + ';';
		}

		private string ArgName(int argID)
		{
			Debug.Assert(argID < CurrMethod.ParamTypes.Count);
			return "arg_" + argID;
		}

		private string LocalName(int locID)
		{
			Debug.Assert(locID < CurrMethod.LocalTypes.Count);
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
			if (str.IndexOf(".", StringComparison.Ordinal) != -1 ||
				str.IndexOf("E", StringComparison.Ordinal) != -1)
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
			}
			return false;
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
							case Code.Cgt_Un:
								return true;
						}
					}
					return false;
			}
			return false;
		}

		private static bool IsIntegerOpValid(StackTypeKind op1, StackTypeKind op2, out StackTypeKind retType)
		{
			retType = StackTypeKind.I4;

			switch (op1)
			{
				case StackTypeKind.I4:
					switch (op2)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							retType = op2;
							return true;
					}
					return false;

				case StackTypeKind.I8:
					switch (op2)
					{
						case StackTypeKind.I8:
							retType = StackTypeKind.I8;
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
					}
					return false;
			}
			return false;
		}

		private static bool IsShiftOpValid(StackTypeKind beShift, StackTypeKind shiftBy, out StackTypeKind retType)
		{
			retType = StackTypeKind.I4;

			switch (beShift)
			{
				case StackTypeKind.I4:
					switch (shiftBy)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							retType = StackTypeKind.I4;
							return true;
					}
					return false;

				case StackTypeKind.I8:
					switch (shiftBy)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							retType = StackTypeKind.I8;
							return true;
					}
					return false;

				case StackTypeKind.Ptr:
					switch (shiftBy)
					{
						case StackTypeKind.I4:
						case StackTypeKind.Ptr:
							retType = StackTypeKind.Ptr;
							return true;
					}
					return false;
			}
			return false;
		}
	}
}
