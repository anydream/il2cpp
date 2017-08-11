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
	internal struct StackType
	{
		public string TypeName;

		public StackType(string name)
		{
			TypeName = name;
		}

		public override string ToString()
		{
			return TypeName;
		}

		public const string I4 = "i4";
		public const string I8 = "i8";
		public const string R4 = "r4";
		public const string R8 = "r8";
		public const string Ptr = "ptr";
		public const string Ref = "ref";
		public const string Obj = "obj";

		public static implicit operator StackType(string name)
		{
			return new StackType(name);
		}

		public static implicit operator string(StackType stype)
		{
			return stype.TypeName;
		}
	}

	// 栈槽信息
	internal struct SlotInfo
	{
		public StackType SlotType;
		public int StackIndex;
	}

	// 包装的指令
	internal class InstructionInfo
	{
		public OpCode OpCode;
		public object Operand;
		public int Offset;

		public bool IsBrTarget;
		public bool IsProcessed;
		public string CppCode;

		public override string ToString()
		{
			string strOperand = null;
			switch (Operand)
			{
				case InstructionInfo inst:
					strOperand = "label_" + inst.Offset;
					break;

				case InstructionInfo[] instList:
					{
						foreach (var inst in instList)
						{
							strOperand += "label_" + inst.Offset + ' ';
						}
					}
					break;

				case TypeX tyX:
					strOperand = tyX.PrettyName();
					break;

				case MethodX metX:
					strOperand = metX.PrettyName(true);
					break;

				case FieldX fldX:
					strOperand = fldX.PrettyName(true);
					break;

				case Parameter arg:
					strOperand = "arg_" + arg.Index;
					break;

				case Local loc:
					strOperand = "loc_" + loc.Index;
					break;

				default:
					strOperand = Operand?.ToString();
					break;
			}

			return string.Format("{0}{1}", OpCode, strOperand != null ? ' ' + strOperand : "");
		}
	}

	// 异常处理器信息
	internal class ExceptionHandlerInfo
	{
		public int Index;
		public int TryStart;
		public int TryEnd;
		public int? FilterStart;
		public int HandlerStart;
		public int HandlerEnd;
		public TypeSig CatchType;
		public ExceptionHandlerType HandlerType;

		// 跳转离开的目标指令位置
		public SortedSet<int> LeaveTargets;
	}

	internal class InsertCode
	{
		public string CppCode;
		public int Indent;

		public InsertCode(string code, int indent)
		{
			CppCode = code;
			Indent = indent;
		}
	}

	internal class ExceptionInsertCode
	{
		private readonly SortedDictionary<int, InsertCode> PreInsert = new SortedDictionary<int, InsertCode>(Comparer<int>.Create((x, y) => y.CompareTo(x)));
		private readonly SortedDictionary<int, InsertCode> PostInsert = new SortedDictionary<int, InsertCode>();

		public void AddPreInsert(int handlerIdx, string code, int indent)
		{
			PreInsert.Add(handlerIdx, new InsertCode(code, indent));
		}

		public void AddPostInsert(int handlerIdx, string code, int indent)
		{
			PostInsert.Add(handlerIdx, new InsertCode(code, indent));
		}

		public void PreAppendTo(CodePrinter prt)
		{
			foreach (var code in PreInsert.Values)
			{
				prt.Indents += code.Indent;
				prt.Append(code.CppCode);
			}
		}

		public void PostAppendTo(CodePrinter prt)
		{
			foreach (var code in PostInsert.Values)
			{
				prt.Append(code.CppCode);
				prt.Indents += code.Indent;
			}
		}
	}

	internal class ExceptionInsertMap
	{
		private readonly Dictionary<int, ExceptionInsertCode> CodeMap = new Dictionary<int, ExceptionInsertCode>();

		public ExceptionInsertCode this[int offset]
		{
			get
			{
				if (CodeMap.TryGetValue(offset, out var val))
					return val;
				return null;
			}
		}

		public void Clear()
		{
			CodeMap.Clear();
		}

		public ExceptionInsertCode GetOrAdd(int offset)
		{
			if (!CodeMap.TryGetValue(offset, out var code))
			{
				code = new ExceptionInsertCode();
				CodeMap.Add(offset, code);
			}
			return code;
		}
	}

	// 方法生成器
	public class MethodGenerator
	{
		// 类型管理器
		private readonly TypeManager TypeMgr;
		// 字符串生成器
		private readonly StringGenerator StringGen;
		// 类型生成器
		private readonly TypeGenerator TypeGen;

		// 当前方法
		private MethodX CurrMethod;

		// 当前类型栈
		private Stack<StackType> TypeStack = new Stack<StackType>();
		// 栈槽类型映射
		private readonly Dictionary<int, HashSet<StackType>> SlotMap = new Dictionary<int, HashSet<StackType>>();
		// 待处理的分支
		private readonly Queue<Tuple<int, Stack<StackType>>> Branches = new Queue<Tuple<int, Stack<StackType>>>();
		// 跳出异常目标映射
		private readonly Dictionary<int, int> LeaveMap = new Dictionary<int, int>();
		private int LeaveCounter;
		private readonly ExceptionInsertMap ExpInsertMap = new ExceptionInsertMap();

		// 声明代码
		public readonly StringBuilder DeclCode = new StringBuilder();
		// 实现代码
		public readonly StringBuilder ImplCode = new StringBuilder();
		// 声明依赖的类型
		public readonly HashSet<string> DeclDependNames = new HashSet<string>();
		// 实现依赖的类型
		public readonly HashSet<string> ImplDependNames = new HashSet<string>();
		// 实现依赖的字符串常量
		public readonly HashSet<string> DependStrings = new HashSet<string>();

		internal MethodGenerator(TypeManager typeMgr, StringGenerator strGen, TypeGenerator typeGen)
		{
			TypeMgr = typeMgr;
			StringGen = strGen;
			TypeGen = typeGen;
		}

		private void RegisterSlotInfo(ref SlotInfo sinfo)
		{
			if (!SlotMap.TryGetValue(sinfo.StackIndex, out var typeSet))
			{
				typeSet = new HashSet<StackType>();
				SlotMap.Add(sinfo.StackIndex, typeSet);
			}
			typeSet.Add(sinfo.SlotType);
		}

		private SlotInfo Push(StackType stype)
		{
			SlotInfo sinfo = new SlotInfo();
			sinfo.StackIndex = TypeStack.Count;
			sinfo.SlotType = stype;
			TypeStack.Push(stype);
			RegisterSlotInfo(ref sinfo);
			return sinfo;
		}

		private SlotInfo Pop()
		{
			Debug.Assert(TypeStack.Count > 0);
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
			Branches.Enqueue(new Tuple<int, Stack<StackType>>(
				targetIP,
				new Stack<StackType>(TypeStack)));
		}

		private void Reset()
		{
			CurrMethod = null;
			TypeStack.Clear();
			SlotMap.Clear();
			Branches.Clear();
			LeaveMap.Clear();
			LeaveCounter = 0;
			ExpInsertMap.Clear();
		}

		public void Process(MethodX metX)
		{
			// 重置数据
			Reset();
			CurrMethod = metX;
			DeclCode.Clear();
			ImplCode.Clear();
			DeclDependNames.Clear();
			ImplDependNames.Clear();
			DependStrings.Clear();

			string codeDecl, codeImpl;
			if (metX.HasInstList)
			{
				ProcessLoop(0);
				ProcessHandlers();

				// 生成实现代码
				GenMetCode(out codeDecl, out codeImpl);
				DeclCode.Append(codeDecl);
				ImplCode.Append(codeImpl);
			}

			// 生成虚查询代码
			GenVFtnCode(out codeDecl, out codeImpl);
			DeclCode.Append(codeDecl);
			ImplCode.Append(codeImpl);

			// 生成虚调用代码
			GenVMetCode(out codeDecl, out codeImpl);
			DeclCode.Append(codeDecl);
			ImplCode.Append(codeImpl);

			Reset();
		}

		private void ProcessLoop(int begin)
		{
			// 模拟执行
			int currIP = begin;
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

			Debug.Assert(TypeStack.Count == 0);
			TypeStack.Clear();
		}

		// 生成实现方法
		private void GenMetCode(out string codeDecl, out string codeImpl)
		{
			CodePrinter prt = new CodePrinter();
			CodePrinter prtOnce = null;

			string retTypeName = CurrMethod.ReturnType.GetCppName(TypeMgr);

			if (CurrMethod.ReturnType.IsValueType)
				DeclDependNames.Add(retTypeName);

			string metName = CurrMethod.GetCppName(PrefixMet);
			// 构造声明
			prt.AppendFormat("// {0}\n{1} {2}(",
				CurrMethod.PrettyName(true),
				retTypeName,
				metName);

			// 构造参数列表
			int argNum = CurrMethod.ParamTypes.Count;
			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					prt.Append(", ");

				var arg = CurrMethod.ParamTypes[i];
				string argTypeName = arg.GetCppName(TypeMgr);

				prt.AppendFormat("{0} {1}",
					argTypeName,
					ArgName(i));

				if (arg.IsValueType)
					DeclDependNames.Add(argTypeName);
			}

			prt.Append(")");
			codeDecl = prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			if (CurrMethod.Def.IsStatic)
			{
				if (CurrMethod.Def.IsConstructor)
				{
					Debug.Assert(CurrMethod.DeclType.CctorMethod == CurrMethod);

					// 静态构造内部防止多次调用
					string declTypeName = CurrMethod.DeclType.GetCppName();
					prt.AppendFormatLine("IL2CPP_CALL_ONCE(\n\tonceflag_{0},\n\tlocktid_{0},\n\tonce_{1});",
						declTypeName,
						metName);

					--prt.Indents;
					prt.AppendLine("}");

					// 构造单次调用的实现代码
					prtOnce = prt;
					prt = new CodePrinter();

					prt.AppendFormat("static {0} once_{1}()",
						retTypeName,
						metName);

					prt.AppendLine("\n{");
					++prt.Indents;
				}
				// 静态方法内部调用静态构造
				else if (CurrMethod.DeclType.CctorMethod != null)
					prt.AppendFormatLine("{0}();\n", CurrMethod.DeclType.CctorMethod.GetCppName(PrefixMet));
			}
			else if (CurrMethod.Def.IsConstructor)
			{
				// 非静态构造内部调用静态构造
				if (CurrMethod.DeclType.CctorMethod != null)
					prt.AppendFormatLine("{0}();\n", CurrMethod.DeclType.CctorMethod.GetCppName(PrefixMet));
			}

			// 构造局部变量
			var localList = CurrMethod.LocalTypes;
			if (localList != null)
			{
				prt.AppendLine("// locals");
				for (int i = 0; i < localList.Count; ++i)
				{
					var loc = localList[i];
					string locTypeName = loc.GetCppName(TypeMgr);

					prt.AppendFormatLine("{0} {1}{2};",
						locTypeName,
						LocalName(i),
						CurrMethod.InitLocals ? " = " + loc.GetInitValue(TypeMgr) : "");

					if (loc.IsValueType)
						ImplDependNames.Add(locTypeName);
				}
				prt.AppendLine();
			}

			// 构造临时变量
			if (SlotMap.Count > 0)
			{
				prt.AppendLine("// temps");
				foreach (var kv in SlotMap)
				{
					foreach (var type in kv.Value)
					{
						prt.AppendFormatLine("{0} {1};",
							StackTypeCppName(type),
							TempName(kv.Key, type));
					}
				}
				prt.AppendLine();
			}

			// 构造异常辅助变量
			if (CurrMethod.HasHandlerList)
			{
				prt.AppendLine("// exceptions");
				prt.AppendLine("il2cppObject* lastException = nullptr;");
				prt.AppendLine("int leaveTarget = 0;");
				prt.AppendLine();
			}

			// 构造指令代码
			foreach (var inst in CurrMethod.InstList)
			{
				var insertCode = ExpInsertMap[inst.Offset];
				if (insertCode != null)
					insertCode.PreAppendTo(prt);

				// 指令注释
				prt.AppendFormatLine("// {0}", inst);

				// 跳转标签
				if (inst.IsBrTarget)
				{
					bool isDec = false;
					if (prt.Indents > 0)
					{
						isDec = true;
						--prt.Indents;
					}

					prt.AppendLine(LabelName(inst.Offset) + ':');

					if (isDec)
						++prt.Indents;
				}

				if (insertCode != null)
					insertCode.PostAppendTo(prt);

				// 指令代码
				if (inst.CppCode != null)
				{
					prt.AppendLine(inst.CppCode);
				}
			}

			--prt.Indents;
			prt.AppendLine("}");

			codeImpl = prt.ToString();
			if (prtOnce != null)
				codeImpl += prtOnce.ToString();
		}

		// 生成虚调用方法
		private void GenVMetCode(out string codeDecl, out string codeImpl)
		{
			codeDecl = null;
			codeImpl = null;

			if (!CurrMethod.Def.IsVirtual)
				return;
			Debug.Assert(!CurrMethod.Def.IsStatic);

			CodePrinter prt = new CodePrinter();

			// 构造声明
			string retTypeName = CurrMethod.ReturnType.GetCppName(TypeMgr);
			prt.AppendFormat("{0} {1}(",
				retTypeName,
				CurrMethod.GetCppName(PrefixVMet));

			// 构造函数指针类型
			StringBuilder sbFuncPtr = new StringBuilder();
			sbFuncPtr.AppendFormat("{0}(*)(",
				retTypeName);

			// 构造参数列表
			int argNum = CurrMethod.ParamTypes.Count;
			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
				{
					prt.Append(", ");
					sbFuncPtr.Append(", ");
				}

				string argTypeName = CurrMethod.ParamTypes[i].GetCppName(TypeMgr);
				prt.AppendFormat("{0} {1}",
					argTypeName,
					ArgName(i));
				sbFuncPtr.Append(argTypeName);
			}

			prt.Append(")");
			sbFuncPtr.Append(')');
			codeDecl = prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;

			// 构造获得函数指针代码
			prt.AppendFormatLine("void* pfn = {0}({1}->objectTypeID);",
				CurrMethod.GetCppName(PrefixVFtn),
				ArgName(0));

			// 构造调用代码
			if (!CurrMethod.ReturnType.IsVoidSig())
				prt.Append("return ");

			prt.AppendFormat("(({0})pfn)(", sbFuncPtr.ToString());
			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					prt.Append(", ");
				prt.Append(ArgName(i));
			}
			prt.AppendLine(");");

			--prt.Indents;
			prt.AppendLine("}");

			codeImpl = prt.ToString();
		}

		// 生成虚查询方法
		private void GenVFtnCode(out string codeDecl, out string codeImpl)
		{
			codeDecl = null;
			codeImpl = null;

			// 跳过无覆盖方法
			if (!CurrMethod.HasOverrideImpls)
				return;

			CodePrinter prt = new CodePrinter();
			// 构造声明
			prt.AppendFormat("void* {0}(uint32_t typeID)",
				CurrMethod.GetCppName(PrefixVFtn));
			codeDecl = prt + ";\n";

			prt.AppendLine("\n{");
			++prt.Indents;
			prt.AppendLine("switch (typeID)\n{");
			++prt.Indents;

			// 构造查询跳转分支
			foreach (var metX in CurrMethod.OverrideImpls)
			{
				prt.AppendFormatLine("case {0}: return (void*)&{1};",
					metX.DeclType.GetCppTypeID(),
					metX.GetCppName(PrefixMet));
			}

			--prt.Indents;
			// 找不到则返回空指针
			prt.AppendLine("}\nreturn nullptr;");
			--prt.Indents;
			prt.AppendLine("}");

			codeImpl = prt.ToString();
		}

		private int AddLeaveTarget(int target)
		{
			if (!LeaveMap.TryGetValue(target, out int idx))
			{
				idx = ++LeaveCounter;
				LeaveMap.Add(target, idx);
			}
			return idx;
		}

		private List<ExceptionHandlerInfo> GetLeaveThroughHandlers(int offset, int target)
		{
			Debug.Assert(CurrMethod.Def.HasBody);
			if (!CurrMethod.HasHandlerList)
				return null;

			List<ExceptionHandlerInfo> result = new List<ExceptionHandlerInfo>();
			foreach (var handler in CurrMethod.HandlerList)
			{
				// 只收集 finally 信息
				if (handler.HandlerType == ExceptionHandlerType.Finally)
				{
					if (offset >= handler.TryStart && offset < handler.TryEnd &&
						!(target >= handler.TryStart && target < handler.TryEnd))
						result.Add(handler);
				}
			}

			if (result.Count > 0)
				return result;
			return null;
		}

		private void ProcessHandlers()
		{
			if (!CurrMethod.HasHandlerList)
				return;

			// 处理异常块内的指令
			foreach (var handler in CurrMethod.HandlerList)
			{
				if (handler.HandlerType == ExceptionHandlerType.Filter)
				{
					Push(StackType.Obj);
					ProcessLoop((int)handler.FilterStart);
				}
				else if (handler.HandlerType == ExceptionHandlerType.Catch)
				{
					Push(StackType.Obj);
					ProcessLoop(handler.HandlerStart);
				}
				else
				{
					Debug.Assert(handler.HandlerType == ExceptionHandlerType.Finally ||
								 handler.HandlerType == ExceptionHandlerType.Fault);
					ProcessLoop(handler.HandlerStart);
				}
			}

			// 合并连续的 catch/filter 链
			Dictionary<Tuple<int, int>, List<ExceptionHandlerInfo>> mergeMap =
				new Dictionary<Tuple<int, int>, List<ExceptionHandlerInfo>>();
			List<List<ExceptionHandlerInfo>> mergeList = new List<List<ExceptionHandlerInfo>>();

			bool isNew = true;
			foreach (var handler in CurrMethod.HandlerList)
			{
				Tuple<int, int> key = new Tuple<int, int>(handler.TryStart, handler.TryEnd);
				if (handler.HandlerType == ExceptionHandlerType.Catch ||
					handler.HandlerType == ExceptionHandlerType.Filter)
				{
					if (!mergeMap.TryGetValue(key, out var lst))
					{
						Debug.Assert(isNew);
						lst = new List<ExceptionHandlerInfo>();
						mergeMap.Add(key, lst);
						mergeList.Add(lst);
					}
					else
						Debug.Assert(!isNew);

					isNew = false;

					lst.Add(handler);
				}
				else if (handler.HandlerType == ExceptionHandlerType.Finally ||
						 handler.HandlerType == ExceptionHandlerType.Fault)
				{
					isNew = true;

					Debug.Assert(!mergeMap.ContainsKey(key));

					var lst = new List<ExceptionHandlerInfo>();
					mergeMap.Add(key, lst);
					mergeList.Add(lst);

					lst.Add(handler);
				}
				else
					throw new ArgumentOutOfRangeException();
			}

			// 构造异常插入代码
			foreach (var lst in mergeList)
			{
				var firstHandler = lst[0];

				var tryStart = ExpInsertMap.GetOrAdd(firstHandler.TryStart);
				tryStart.AddPostInsert(
					firstHandler.Index,
					"try\n{\n",
					1);

				var tryEnd = ExpInsertMap.GetOrAdd(firstHandler.TryEnd);
				int entry = firstHandler.FilterStart ?? firstHandler.HandlerStart;
				tryEnd.AddPreInsert(
					firstHandler.Index,
					"}\n" +
					"catch (il2cppException& ex)\n" +
					"{\n" +
					"\tlastException = ex.obj;\n" +
					"}\n" +
					"goto " + LabelName(entry) + ";\n",
					-1);

				if (firstHandler.HandlerType == ExceptionHandlerType.Catch ||
					firstHandler.HandlerType == ExceptionHandlerType.Filter)
				{
					for (int i = 0; i < lst.Count; ++i)
					{
						var handler = lst[i];

						string endCode = null;
						int endIndent = 0;

						var hStart = ExpInsertMap.GetOrAdd(handler.HandlerStart);
						SlotInfo tmp0 = new SlotInfo { StackIndex = 0, SlotType = StackType.Obj };
						string storeLastExp = LoadPushed(ref tmp0, null) + "lastException;\n";

						if (handler.HandlerType == ExceptionHandlerType.Catch)
						{
							TypeX catchTyX = TypeMgr.GetNamedType(handler.CatchType.FullName);
							Debug.Assert(catchTyX != null);
							string catchTypeName = catchTyX.GetCppName();
							ImplDependNames.Add(catchTypeName);

							hStart.AddPostInsert(
								handler.Index,
								string.Format(
									"if (isinst_{0}(lastException->objectTypeID))\n{{\n\t",
									catchTypeName) +
								storeLastExp,
								1);

							endCode = "}\n";
							endIndent = -1;
						}
						else
						{
							Debug.Assert(handler.HandlerType == ExceptionHandlerType.Filter);

							hStart.AddPostInsert(
								handler.Index,
								storeLastExp,
								0);
						}

						if (i == lst.Count - 1)
						{
							// 异常处理链最后一项末尾向上抛出异常
							endCode += "IL2CPP_THROW(lastException);\n";
						}
						else
						{
							// 跳到下一个异常处理代码
							var next = lst[i + 1];
							int nextEntry = next.FilterStart ?? next.HandlerStart;

							endCode += "goto " + LabelName(nextEntry) + ";\n";
						}

						var hEnd = ExpInsertMap.GetOrAdd(handler.HandlerEnd);
						hEnd.AddPreInsert(
							handler.Index,
							endCode,
							endIndent);
					}
				}
				else
				{
					Debug.Assert(lst.Count == 1);

					var hEnd = ExpInsertMap.GetOrAdd(firstHandler.HandlerEnd);

					if (firstHandler.HandlerType == ExceptionHandlerType.Finally)
					{
						StringBuilder sb = new StringBuilder();
						sb.Append("if (lastException) IL2CPP_THROW(lastException);\n");
						sb.Append("switch (leaveTarget)\n{\n");
						foreach (int leaveTarget in firstHandler.LeaveTargets)
						{
							sb.AppendFormat("\tcase {0}: goto {1};\n",
								LeaveMap[leaveTarget],
								LabelName(leaveTarget));
						}
						sb.Append("}\nabort();\n");

						hEnd.AddPreInsert(
							firstHandler.Index,
							sb.ToString(),
							0);
					}
					else
					{
						Debug.Assert(firstHandler.HandlerType == ExceptionHandlerType.Fault);
						hEnd.AddPreInsert(
							firstHandler.Index,
							"IL2CPP_THROW(lastException);\n",
							0);
					}
				}
			}
		}

		private bool ProcessStep(int currIP, ref int nextIP)
		{
			var inst = CurrMethod.InstList[currIP];
			if (inst.IsProcessed)
				return false;
			inst.IsProcessed = true;

			OpCode opCode = inst.OpCode;
			switch (opCode.Code)
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
					ProcessInstruction(inst);
					break;
			}

			object operand = inst.Operand;
			// 计算下一指令位置
			switch (opCode.FlowControl)
			{
				case FlowControl.Branch:
					nextIP = ((InstructionInfo)operand).Offset;
					return true;

				case FlowControl.Cond_Branch:
					if (opCode.Code == Code.Switch)
					{
						InstructionInfo[] targets = (InstructionInfo[])operand;
						foreach (InstructionInfo targetInst in targets)
							AddBranch(targetInst.Offset);
					}
					else
					{
						AddBranch(((InstructionInfo)operand).Offset);
					}
					nextIP = currIP + 1;
					return true;

				case FlowControl.Break:
				case FlowControl.Call:
				case FlowControl.Next:
					nextIP = currIP + 1;
					return true;

				case FlowControl.Return:
				case FlowControl.Throw:
					return false;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ProcessInstruction(InstructionInfo inst)
		{
			OpCode opCode = inst.OpCode;
			object operand = inst.Operand;

			switch (opCode.Code)
			{
				case Code.Ldnull:
					Load(inst, StackType.Obj, "nullptr");
					return;

				case Code.Ldstr:
					{
						string str = (string)operand;
						Ldstr(inst, str);
					}
					return;

				case Code.Ldc_I4_M1:
					Load(inst, StackType.I4, "-1");
					return;
				case Code.Ldc_I4_0:
					Load(inst, StackType.I4, "0");
					return;
				case Code.Ldc_I4_1:
					Load(inst, StackType.I4, "1");
					return;
				case Code.Ldc_I4_2:
					Load(inst, StackType.I4, "2");
					return;
				case Code.Ldc_I4_3:
					Load(inst, StackType.I4, "3");
					return;
				case Code.Ldc_I4_4:
					Load(inst, StackType.I4, "4");
					return;
				case Code.Ldc_I4_5:
					Load(inst, StackType.I4, "5");
					return;
				case Code.Ldc_I4_6:
					Load(inst, StackType.I4, "6");
					return;
				case Code.Ldc_I4_7:
					Load(inst, StackType.I4, "7");
					return;
				case Code.Ldc_I4_8:
					Load(inst, StackType.I4, "8");
					return;
				case Code.Ldc_I4_S:
					Load(inst, StackType.I4, ((sbyte)operand).ToString());
					return;
				case Code.Ldc_I4:
					Load(inst, StackType.I4, ((int)operand).ToString());
					return;
				case Code.Ldc_I8:
					Load(inst, StackType.I8, ((long)operand).ToString());
					return;
				case Code.Ldc_R4:
					Load(inst, StackType.R4, ((float)operand).ToString(CultureInfo.InvariantCulture));
					return;
				case Code.Ldc_R8:
					Load(inst, StackType.R8, ((double)operand).ToString(CultureInfo.InvariantCulture));
					return;

				case Code.Ldloc_0:
					Load(inst, ToStackType(CurrMethod.LocalTypes[0]), LocalName(0));
					return;
				case Code.Ldloc_1:
					Load(inst, ToStackType(CurrMethod.LocalTypes[1]), LocalName(1));
					return;
				case Code.Ldloc_2:
					Load(inst, ToStackType(CurrMethod.LocalTypes[2]), LocalName(2));
					return;
				case Code.Ldloc_3:
					Load(inst, ToStackType(CurrMethod.LocalTypes[3]), LocalName(3));
					return;
				case Code.Ldloc:
				case Code.Ldloc_S:
					{
						Local loc = (Local)operand;
						Load(inst, ToStackType(CurrMethod.LocalTypes[loc.Index]), LocalName(loc.Index));
					}
					return;
				case Code.Ldloca:
				case Code.Ldloca_S:
					{
						Local loc = (Local)operand;
						Load(inst, StackType.Ptr, '&' + LocalName(loc.Index));
					}
					return;

				case Code.Stloc_0:
					Store(inst, LocalName(0), CurrMethod.LocalTypes[0].GetCppName(TypeMgr));
					return;
				case Code.Stloc_1:
					Store(inst, LocalName(1), CurrMethod.LocalTypes[1].GetCppName(TypeMgr));
					return;
				case Code.Stloc_2:
					Store(inst, LocalName(2), CurrMethod.LocalTypes[2].GetCppName(TypeMgr));
					return;
				case Code.Stloc_3:
					Store(inst, LocalName(3), CurrMethod.LocalTypes[3].GetCppName(TypeMgr));
					return;
				case Code.Stloc:
				case Code.Stloc_S:
					{
						Local loc = (Local)operand;
						Store(inst, LocalName(loc.Index), CurrMethod.LocalTypes[loc.Index].GetCppName(TypeMgr));
					}
					return;

				case Code.Ldarg_0:
					Load(inst, ToStackType(CurrMethod.ParamTypes[0]), ArgName(0));
					return;
				case Code.Ldarg_1:
					Load(inst, ToStackType(CurrMethod.ParamTypes[1]), ArgName(1));
					return;
				case Code.Ldarg_2:
					Load(inst, ToStackType(CurrMethod.ParamTypes[2]), ArgName(2));
					return;
				case Code.Ldarg_3:
					Load(inst, ToStackType(CurrMethod.ParamTypes[3]), ArgName(3));
					return;
				case Code.Ldarg:
				case Code.Ldarg_S:
					{
						Parameter arg = (Parameter)operand;
						Load(inst, ToStackType(CurrMethod.ParamTypes[arg.Index]), ArgName(arg.Index));
					}
					return;
				case Code.Ldarga:
				case Code.Ldarga_S:
					{
						Parameter arg = (Parameter)operand;
						Load(inst, StackType.Ptr, '&' + ArgName(arg.Index));
					}
					return;

				case Code.Starg:
				case Code.Starg_S:
					{
						Parameter arg = (Parameter)operand;
						Store(inst, ArgName(arg.Index), CurrMethod.ParamTypes[arg.Index].GetCppName(TypeMgr));
					}
					return;

				case Code.Ldfld:
					{
						FieldX fldX = (FieldX)operand;
						Ldfld(inst, fldX, false);
					}
					return;
				case Code.Ldflda:
					{
						FieldX fldX = (FieldX)operand;
						Ldfld(inst, fldX, true);
					}
					return;
				case Code.Ldsfld:
					{
						FieldX fldX = (FieldX)operand;
						InvokeCctor(inst, fldX.DeclType);
						Load(inst, ToStackType(fldX.FieldType), fldX.GetCppName(), fldX.FieldType.GetCppName(TypeMgr));

						ImplDependNames.Add(fldX.DeclType.GetCppName());
					}
					return;
				case Code.Ldsflda:
					{
						FieldX fldX = (FieldX)operand;
						InvokeCctor(inst, fldX.DeclType);
						Load(inst, StackType.Ptr, '&' + fldX.GetCppName());

						ImplDependNames.Add(fldX.DeclType.GetCppName());
					}
					return;

				case Code.Stfld:
					{
						FieldX fldX = (FieldX)operand;
						Stfld(inst, fldX);
					}
					return;

				case Code.Stsfld:
					{
						FieldX fldX = (FieldX)operand;
						InvokeCctor(inst, fldX.DeclType);
						Store(inst, fldX.GetCppName(), fldX.FieldType.GetCppName(TypeMgr));

						ImplDependNames.Add(fldX.DeclType.GetCppName());
					}
					return;

				case Code.Conv_I1:
				case Code.Conv_I2:
				case Code.Conv_I4:
				case Code.Conv_I8:
				case Code.Conv_U1:
				case Code.Conv_U2:
				case Code.Conv_U4:
				case Code.Conv_U8:
				case Code.Conv_R4:
				case Code.Conv_R8:
				case Code.Conv_I:
				case Code.Conv_U:
					Conv(inst);
					return;

				/*case Code.Conv_R_Un:
					return;*/

				case Code.Add:
					BinOp(inst, "+");
					return;
				case Code.Sub:
					BinOp(inst, "-");
					return;

				case Code.Ceq:
				case Code.Cgt:
				case Code.Cgt_Un:
				case Code.Clt:
				case Code.Clt_Un:
					Cmp(inst);
					return;

				case Code.Br:
				case Code.Br_S:
					{
						int target = ((InstructionInfo)operand).Offset;
						inst.CppCode = "goto " + LabelName(target) + ';';
					}
					return;
				case Code.Brfalse:
				case Code.Brfalse_S:
					BrCond(inst, false, ((InstructionInfo)operand).Offset);
					return;
				case Code.Brtrue:
				case Code.Brtrue_S:
					BrCond(inst, true, ((InstructionInfo)operand).Offset);
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
					BrCmp(inst, ((InstructionInfo)operand).Offset);
					return;

				case Code.Switch:
					{
						InstructionInfo[] targets = (InstructionInfo[])operand;
						Switch(inst, targets);
					}
					return;

				case Code.Ret:
					Ret(inst);
					return;

				case Code.Call:
				case Code.Callvirt:
					{
						MethodX metX = (MethodX)operand;

						string prefix;
						if (opCode.Code == Code.Callvirt && metX.Def.IsVirtual)
							prefix = PrefixVMet;
						else
							prefix = PrefixMet;

						Call(inst, metX, prefix);
					}
					return;

				case Code.Ldftn:
					{
						MethodX metX = (MethodX)operand;
						Ldftn(inst, metX);
					}
					return;
				case Code.Ldvirtftn:
					{
						MethodX metX = (MethodX)operand;
						Ldvirtftn(inst, metX);
					}
					return;

				case Code.Calli:
					{
						MethodSig metSig = (MethodSig)operand;
						Calli(inst, metSig);
					}
					return;

				case Code.Jmp:
					{
						MethodX metX = (MethodX)operand;
						Jmp(inst, metX);
					}
					return;

				case Code.Newobj:
					{
						if (operand is MethodX metX)
						{
							NewObj(inst, metX);
						}
						else
						{
							throw new NotImplementedException();
						}
					}
					return;

				case Code.Initobj:
					{
						TypeSig sig = (TypeSig)operand;
						InitObj(inst, sig);
					}
					return;
				case Code.Ldobj:
					{
						TypeSig sig = (TypeSig)operand;
						Ldobj(inst, sig);
					}
					return;
				case Code.Stobj:
					{
						TypeSig sig = (TypeSig)operand;
						Stobj(inst, sig);
					}
					return;
				case Code.Cpobj:
					{
						TypeSig sig = (TypeSig)operand;
						Cpobj(inst, sig);
					}
					return;

				case Code.Initblk:
					{
						Initblk(inst);
					}
					return;
				case Code.Cpblk:
					{
						Cpblk(inst);
					}
					return;
				case Code.Sizeof:
					{
						TypeSig sig = (TypeSig)operand;
						Sizeof(inst, sig);
					}
					return;
				case Code.Localloc:
					{
						Localloc(inst);
					}
					return;

				case Code.Ldind_I1:
				case Code.Ldind_I2:
				case Code.Ldind_I4:
				case Code.Ldind_I8:
				case Code.Ldind_U1:
				case Code.Ldind_U2:
				case Code.Ldind_U4:
				case Code.Ldind_R4:
				case Code.Ldind_R8:
				case Code.Ldind_I:
				case Code.Ldind_Ref:
					Ldind(inst, opCode.Code);
					return;

				case Code.Stind_I1:
				case Code.Stind_I2:
				case Code.Stind_I4:
				case Code.Stind_I8:
				case Code.Stind_R4:
				case Code.Stind_R8:
				case Code.Stind_I:
				case Code.Stind_Ref:
					Stind(inst, opCode.Code);
					return;

				case Code.Throw:
					{
						SlotInfo poped = Pop();
						Debug.Assert(poped.SlotType == StackType.Obj);
						inst.CppCode = string.Format("IL2CPP_THROW({0});",
							StorePoped(ref poped, null));
					}
					return;
				case Code.Rethrow:
					inst.CppCode = "IL2CPP_THROW(lastException);";
					return;

				case Code.Leave:
				case Code.Leave_S:
					{
						int target = ((InstructionInfo)operand).Offset;

						inst.CppCode = "lastException = nullptr;\n";

						var leaveHandlers = GetLeaveThroughHandlers(inst.Offset, target);
						if (leaveHandlers != null)
						{
							foreach (var handler in leaveHandlers)
							{
								if (handler.LeaveTargets == null)
									handler.LeaveTargets = new SortedSet<int>();
								handler.LeaveTargets.Add(target);
							}

							int targetIdx = AddLeaveTarget(target);
							inst.CppCode += string.Format("leaveTarget = {0};\ngoto {1};",
								targetIdx,
								LabelName(leaveHandlers[0].HandlerStart));
						}
						else
						{
							inst.CppCode += "goto " + LabelName(target) + ';';
						}
					}
					return;

				case Code.Endfinally:
					return;

				default:
					throw new NotImplementedException("OpCode: " + opCode);
			}
		}

		private const string PrefixMet = "met_";
		private const string PrefixVMet = "vmet_";
		private const string PrefixVFtn = "vftn_";

		private static string ArgName(int argID)
		{
			return "arg_" + argID;
		}

		private static string LocalName(int localID)
		{
			return "loc_" + localID;
		}

		private string LabelName(int offset)
		{
			CurrMethod.InstList[offset].IsBrTarget = true;
			return "label_" + offset;
		}

		private static string TempName(int stackIndex, StackType stype)
		{
			return string.Format("tmp_{0}_{1}", stackIndex, stype);
		}

		private static string SlotInfoName(ref SlotInfo sinfo)
		{
			return TempName(sinfo.StackIndex, sinfo.SlotType);
		}

		private static string StackTypeCppName(StackType stype)
		{
			switch (stype)
			{
				case StackType.I4:
					return "int32_t";
				case StackType.I8:
					return "int64_t";
				case StackType.R4:
					return "float";
				case StackType.R8:
					return "double";
				case StackType.Ptr:
				case StackType.Ref:
				case StackType.Obj:
					return "void*";
				default:
					return stype;
			}
		}

		private static string StackTypeUnsignedCppName(StackType stype)
		{
			switch (stype)
			{
				case StackType.I4:
					return "uint32_t";
				case StackType.I8:
					return "uint64_t";
				case StackType.Ptr:
					return "uintptr_t";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static bool StackTypeIsReal(StackType stype)
		{
			return stype == StackType.R4 ||
				   stype == StackType.R8;
		}

		private static bool StackTypeIsInt(StackType stype)
		{
			return stype == StackType.I4 ||
				   stype == StackType.I8 ||
				   stype == StackType.Ptr;
		}

		private static bool StackTypeIsPointer(StackType stype)
		{
			return stype == StackType.Ptr ||
				   stype == StackType.Ref;
		}

		private static bool StackTypeIsPtrOrObj(StackType stype)
		{
			return stype == StackType.Ptr ||
				   stype == StackType.Ref ||
				   stype == StackType.Obj;
		}

		private StackType ToStackType(TypeSig sig)
		{
			if (sig.IsByRef)
			{
				return StackType.Ref;
			}

			var sigType = sig.ElementType;
			if (sig.IsPointer ||
				sigType == ElementType.I ||
				sigType == ElementType.U)
			{
				return StackType.Ptr;
			}
			if (sigType == ElementType.I1 ||
				sigType == ElementType.U1 ||
				sigType == ElementType.I2 ||
				sigType == ElementType.U2 ||
				sigType == ElementType.I4 ||
				sigType == ElementType.U4 ||
				sigType == ElementType.Char ||
				sigType == ElementType.Boolean)
			{
				return StackType.I4;
			}
			if (sigType == ElementType.I8 ||
				sigType == ElementType.U8)
			{
				return StackType.I8;
			}
			if (sigType == ElementType.R4)
			{
				return StackType.R4;
			}
			if (sigType == ElementType.R8)
			{
				return StackType.R8;
			}

			if (sig.IsValueType)
			{
				return sig.GetCppName(TypeMgr);
			}

			return StackType.Obj;
		}

		private void InvokeCctor(InstructionInfo inst, TypeX declType)
		{
			// 如果需要调用静态构造的类型就是当前方法所在的类型, 则不生成
			//! 当前方法类型的父类型也无需生成
			if (CurrMethod.DeclType.Equals(declType))
				return;

			if (declType.CctorMethod != null)
				inst.CppCode += declType.CctorMethod.GetCppName(PrefixMet) + "();\n";
		}

		private string LoadPushed(ref SlotInfo pushed, string rvalType, string castType = null)
		{
			if (castType != null)
			{
				if (IsCppTypeConvertValid(castType, rvalType))
					castType = null;

				rvalType = castType;
			}

			string tempType = null;
			if (rvalType != null)
			{
				tempType = StackTypeCppName(pushed.SlotType);
				if (IsCppTypeConvertValid(tempType, rvalType))
					tempType = null;
			}

			return string.Format("{0} = {1}{2}",
				SlotInfoName(ref pushed),
				tempType != null ? '(' + tempType + ')' : "",
				castType != null ? '(' + castType + ')' : "");
		}

		private void Load(InstructionInfo inst, StackType stype, string rval, string rvalType = null, string castType = null)
		{
			SlotInfo pushed = Push(stype);
			inst.CppCode += LoadPushed(ref pushed, rvalType, castType) + rval + ';';
		}

		private string StorePoped(ref SlotInfo poped, string cast)
		{
			// 如果类型相同则无需转换
			if (cast != null)
			{
				string tempType = StackTypeCppName(poped.SlotType);
				if (IsCppTypeConvertValid(cast, tempType))
					cast = null;
			}

			return (cast != null ? '(' + cast + ')' : "") +
				   SlotInfoName(ref poped);
		}

		private void Store(InstructionInfo inst, string lval, string cast = null)
		{
			SlotInfo poped = Pop();
			inst.CppCode += string.Format("{0} = {1};",
				lval,
				StorePoped(ref poped, cast));
		}

		private void Ldstr(InstructionInfo inst, string str)
		{
			int idx = StringGen.AddString(str);

			string rval = string.Format("(il2cppString*)&str_{0}",
				idx);
			Load(inst, StackType.Obj, rval);

			DependStrings.Add(str);
		}

		private void Conv(InstructionInfo inst)
		{
			string cast;
			StackType stype = StackType.I4;

			OpCode opCode = inst.OpCode;
			switch (opCode.Code)
			{
				case Code.Conv_I1:
					cast = "int8_t";
					break;
				case Code.Conv_I2:
					cast = "int16_t";
					break;
				case Code.Conv_I4:
					cast = "int32_t";
					break;
				case Code.Conv_I8:
					cast = "int64_t";
					stype = StackType.I8;
					break;
				case Code.Conv_U1:
					cast = "uint8_t";
					break;
				case Code.Conv_U2:
					cast = "uint16_t";
					break;
				case Code.Conv_U4:
					cast = "uint32_t";
					break;
				case Code.Conv_U8:
					cast = "uint64_t";
					stype = StackType.I8;
					break;
				case Code.Conv_R4:
					cast = "float";
					stype = StackType.R4;
					break;
				case Code.Conv_R8:
					cast = "double";
					stype = StackType.R8;
					break;
				case Code.Conv_I:
					cast = "intptr_t";
					stype = StackType.Ptr;
					break;
				case Code.Conv_U:
					cast = "uintptr_t";
					stype = StackType.Ptr;
					break;
				default:
					throw new ArgumentOutOfRangeException("Code error " + opCode);
			}

			SlotInfo val = Pop();
			Load(inst, stype, SlotInfoName(ref val), StackTypeCppName(val.SlotType), cast);
		}

		private void BrCond(InstructionInfo inst, bool cond, int target)
		{
			SlotInfo poped = Pop();
			inst.CppCode += string.Format("if ({0}{1}) goto {2};", cond ? "" : "!", SlotInfoName(ref poped), LabelName(target));
		}

		private void BrCmp(InstructionInfo inst, int target)
		{
			inst.CppCode += string.Format("if ({0}) goto {1};", CmpCode(inst.OpCode.Code), LabelName(target));
		}

		private void Cmp(InstructionInfo inst)
		{
			string str = CmpCode(inst.OpCode.Code);
			SlotInfo pushed = Push(StackType.I4);
			inst.CppCode += string.Format("{0} = {1} ? 1 : 0;", SlotInfoName(ref pushed), str);
		}

		private string CmpCode(Code code)
		{
			SlotInfo[] popList = Pop(2);

			if (!IsBinaryCompareValid(popList[0].SlotType, popList[1].SlotType, code))
			{
				Debug.Fail("Compare Invalid");
			}

			bool isNeg = false;
			bool isUn = false;
			string oper;

			switch (code)
			{
				case Code.Ceq:
				case Code.Beq:
				case Code.Beq_S:
					oper = "==";
					break;

				case Code.Cgt:
				case Code.Bgt:
				case Code.Bgt_S:
					oper = ">";
					break;

				case Code.Cgt_Un:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
					oper = "<=";
					isNeg = true;
					isUn = true;
					break;

				case Code.Clt:
				case Code.Blt:
				case Code.Blt_S:
					oper = "<";
					break;

				case Code.Clt_Un:
				case Code.Blt_Un:
				case Code.Blt_Un_S:
					oper = ">=";
					isNeg = true;
					isUn = true;
					break;

				case Code.Bge:
				case Code.Bge_S:
					oper = ">=";
					break;

				case Code.Ble:
				case Code.Ble_S:
					oper = "<=";
					break;

				case Code.Bge_Un:
				case Code.Bge_Un_S:
					oper = "<";
					isNeg = true;
					isUn = true;
					break;

				case Code.Ble_Un:
				case Code.Ble_Un_S:
					oper = ">";
					isNeg = true;
					isUn = true;
					break;

				case Code.Bne_Un:
				case Code.Bne_Un_S:
					oper = "!=";
					isUn = true;
					break;

				default:
					throw new ArgumentOutOfRangeException("Code error " + code);
			}

			bool isUnsigned = false;
			if (isUn)
			{
				if (StackTypeIsInt(popList[0].SlotType) && StackTypeIsInt(popList[1].SlotType))
				{
					isUnsigned = true;
				}
				else if (StackTypeIsReal(popList[0].SlotType) && StackTypeIsReal(popList[1].SlotType))
				{
				}
				else
					return "0";
			}

			return string.Format("{0}{1} {2} {3}{4}",
				isNeg ? "!(" : "",
				(isUnsigned ? '(' + StackTypeUnsignedCppName(popList[0].SlotType) + ')' : "") + SlotInfoName(ref popList[0]),
				oper,
				(isUnsigned ? '(' + StackTypeUnsignedCppName(popList[1].SlotType) + ')' : "") + SlotInfoName(ref popList[1]),
				isNeg ? ")" : "");
		}

		private void Switch(InstructionInfo inst, InstructionInfo[] targets)
		{
			SlotInfo poped = Pop();
			Debug.Assert(poped.SlotType == StackType.I4);

			CodePrinter prt = new CodePrinter();
			prt.AppendFormatLine("switch ((uint32_t){0})",
				SlotInfoName(ref poped));

			prt.AppendLine("{");
			++prt.Indents;

			for (int i = 0; i < targets.Length; ++i)
			{
				prt.AppendFormatLine("case {0}: goto {1};",
					i,
					LabelName(targets[i].Offset));
			}

			--prt.Indents;
			prt.Append("}");

			inst.CppCode = prt.ToString();
		}

		private void BinOp(InstructionInfo inst, string oper)
		{
			SlotInfo[] popList = Pop(2);

			if (!IsBinaryOpValid(popList[0].SlotType, popList[1].SlotType, out var retType, inst.OpCode.Code))
			{
				Debug.Fail("Binary Oper Invalid");
			}

			SlotInfo pushed = Push(retType);
			inst.CppCode += string.Format("{0} = {1} {2} {3};",
				SlotInfoName(ref pushed),
				SlotInfoName(ref popList[0]),
				oper,
				SlotInfoName(ref popList[1]));
		}

		private void Ret(InstructionInfo inst)
		{
			if (TypeStack.Count > 0)
			{
				Debug.Assert(TypeStack.Count == 1);
				SlotInfo poped = Pop();
				inst.CppCode += "return " + StorePoped(ref poped, CurrMethod.ReturnType.GetCppName(TypeMgr)) + ';';
			}
			else
			{
				inst.CppCode += "return;";
			}
		}

		private void Call(InstructionInfo inst, MethodX metX, string prefix)
		{
			int argNum = metX.ParamTypes.Count;
			SlotInfo[] popList = Pop(argNum);

			StringBuilder sb = new StringBuilder();
			var retType = metX.ReturnType;
			if (!retType.IsVoidSig())
			{
				SlotInfo pushed = Push(ToStackType(retType));
				sb.Append(LoadPushed(ref pushed, retType.GetCppName(TypeMgr)));
			}

			sb.AppendFormat("{0}(", metX.GetCppName(prefix));

			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(StorePoped(ref popList[i], metX.ParamTypes[i].GetCppName(TypeMgr)));
			}

			sb.Append(");");

			inst.CppCode += sb.ToString();

			ImplDependNames.Add(metX.DeclType.GetCppName());
		}

		private void Ldftn(InstructionInfo inst, MethodX metX)
		{
			string rval = string.Format("(void*)&{0}",
				metX.GetCppName(PrefixMet));
			Load(inst, StackType.Ptr, rval);

			ImplDependNames.Add(metX.DeclType.GetCppName());
		}

		private void Ldvirtftn(InstructionInfo inst, MethodX metX)
		{
			SlotInfo poped = Pop();
			Debug.Assert(poped.SlotType == StackType.Obj);

			string rval = string.Format("{0}(((il2cppObject*){1})->objectTypeID)",
				metX.GetCppName(PrefixVFtn),
				SlotInfoName(ref poped));
			Load(inst, StackType.Ptr, rval);

			ImplDependNames.Add(metX.DeclType.GetCppName());
		}

		private void Calli(InstructionInfo inst, MethodBaseSig metSig)
		{
			SlotInfo ftnPtr = Pop();
			Debug.Assert(ftnPtr.SlotType == StackType.Ptr);

			int argNum = metSig.Params.Count;
			SlotInfo[] popList = Pop(argNum);

			StringBuilder sb = new StringBuilder();
			var retType = metSig.RetType;
			var retTypeName = retType.GetCppName(TypeMgr);
			if (!retType.IsVoidSig())
			{
				SlotInfo pushed = Push(ToStackType(retType));
				sb.Append(LoadPushed(ref pushed, retTypeName));
			}

			string[] paramTypeName = new string[argNum];

			sb.AppendFormat("(({0}(*)(", retTypeName);
			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					sb.Append(',');

				paramTypeName[i] = metSig.Params[i].GetCppName(TypeMgr);
				sb.Append(paramTypeName[i]);
			}
			sb.AppendFormat(")){0})(",
				SlotInfoName(ref ftnPtr));

			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(StorePoped(ref popList[i], paramTypeName[i]));
			}

			sb.Append(");");

			inst.CppCode += sb.ToString();
		}

		private void Jmp(InstructionInfo inst, MethodX metX)
		{
			TypeStack.Clear();

			int argNum = metX.ParamTypes.Count;
			Debug.Assert(CurrMethod.ParamTypes.Count == argNum);

			StringBuilder sb = new StringBuilder();
			var retType = metX.ReturnType;
			if (!retType.IsVoidSig())
			{
				SlotInfo pushed = Push(ToStackType(retType));
				sb.Append(LoadPushed(ref pushed, retType.GetCppName(TypeMgr)));
			}

			sb.AppendFormat("{0}(", metX.GetCppName(PrefixMet));

			for (int i = 0; i < argNum; ++i)
			{
				if (i != 0)
					sb.Append(", ");
				sb.Append(ArgName(i));
			}

			sb.Append(");");

			inst.CppCode += sb.ToString();
			Ret(inst);

			ImplDependNames.Add(metX.DeclType.GetCppName());
		}

		private void NewObj(InstructionInfo inst, MethodX metX)
		{
			Debug.Assert(metX.Def.IsConstructor);
			Debug.Assert(!metX.Def.IsStatic);
			Debug.Assert(metX.ReturnType.IsVoidSig());

			StringBuilder sb = new StringBuilder();

			int argNum = metX.ParamTypes.Count - 1;
			SlotInfo[] popList = Pop(argNum);

			TypeX declTyX = metX.DeclType;
			string declTypeName = declTyX.GetCppName(true);

			if (declTyX.Def.IsValueType)
			{
				SlotInfo self = Push(declTypeName);

				sb.AppendFormat("{0}(({1})&{2}",
					metX.GetCppName(PrefixMet),
					metX.ParamTypes[0].GetCppName(TypeMgr),
					SlotInfoName(ref self));
			}
			else
			{
				SlotInfo self = Push(StackType.Obj);

				sb.AppendFormat("{0} = il2cpp_New(sizeof({1}), {2});\n",
					SlotInfoName(ref self),
					declTypeName,
					declTyX.GetCppTypeID());

				sb.AppendFormat("{0}({1}",
					metX.GetCppName(PrefixMet),
					StorePoped(ref self, metX.ParamTypes[0].GetCppName(TypeMgr)));
			}

			for (int i = 0; i < argNum; ++i)
			{
				sb.Append(", ");
				sb.Append(StorePoped(ref popList[i], metX.ParamTypes[i + 1].GetCppName(TypeMgr)));
			}

			sb.Append(");");

			inst.CppCode += sb.ToString();

			ImplDependNames.Add(declTyX.GetCppName());
		}

		private void Ldfld(InstructionInfo inst, FieldX fldX, bool isLoadAddr)
		{
			SlotInfo self = Pop();

			string fldDeclTypeName = fldX.DeclType.GetCppName();

			string rval;
			if (StackTypeIsPtrOrObj(self.SlotType))
			{
				rval = string.Format("{0}(({1}*){2})->{3}",
					isLoadAddr ? "&" : "",
					fldDeclTypeName,
					SlotInfoName(ref self),
					fldX.GetCppName());
			}
			else
			{
				if (fldDeclTypeName == self.SlotType)
				{
					rval = string.Format("{0}{1}.{2}",
						isLoadAddr ? "&" : "",
						SlotInfoName(ref self),
						fldX.GetCppName());
				}
				else
				{
					Debug.Assert(fldX.DeclType.GetCppName(true) == self.SlotType);
					rval = string.Format("{0}(({1}*)&{2})->{3}",
										isLoadAddr ? "&" : "",
										fldDeclTypeName,
										SlotInfoName(ref self),
										fldX.GetCppName());
				}
			}

			StackType stype;
			string rvalType = null;
			if (isLoadAddr)
				stype = StackType.Ptr;
			else
			{
				stype = ToStackType(fldX.FieldType);
				rvalType = fldX.FieldType.GetCppName(TypeMgr);
			}

			Load(inst, stype, rval, rvalType);

			ImplDependNames.Add(fldDeclTypeName);
		}

		private void Stfld(InstructionInfo inst, FieldX fldX)
		{
			SlotInfo val = Pop();
			SlotInfo self = Pop();

			Debug.Assert(StackTypeIsPtrOrObj(self.SlotType));

			string fldDeclTypeName = fldX.DeclType.GetCppName();
			inst.CppCode = string.Format("(({0}*){1})->{2} = {3};",
				fldDeclTypeName,
				SlotInfoName(ref self),
				fldX.GetCppName(),
				StorePoped(ref val, fldX.FieldType.GetCppName(TypeMgr)));

			ImplDependNames.Add(fldDeclTypeName);
		}

		private void InitObj(InstructionInfo inst, TypeSig sig)
		{
			SlotInfo poped = Pop();

			if (sig.IsValueType)
			{
				Debug.Assert(StackTypeIsPointer(poped.SlotType));
				string typeName = sig.GetCppName(TypeMgr);
				inst.CppCode = string.Format("*({0}*){1} = {0}();",
					typeName,
					SlotInfoName(ref poped));

				ImplDependNames.Add(typeName);
			}
			else
			{
				Debug.Assert(poped.SlotType == StackType.Obj);
				inst.CppCode = string.Format("{0} = nullptr;",
					SlotInfoName(ref poped));
			}
		}

		private void Ldobj(InstructionInfo inst, TypeSig sig)
		{
			Debug.Assert(sig.IsValueType);

			SlotInfo poped = Pop();
			Debug.Assert(StackTypeIsPointer(poped.SlotType));

			string typeName = sig.GetCppName(TypeMgr);
			string rval = string.Format("*({0}*){1}",
				typeName,
				SlotInfoName(ref poped));

			//! typeName 是否需要转换基础类型名为简化的别名
			//! 是否需要转换为栈类型名
			Load(inst, typeName, rval);

			ImplDependNames.Add(typeName);
		}

		private void Stobj(InstructionInfo inst, TypeSig sig)
		{
			Debug.Assert(sig.IsValueType);

			SlotInfo srcVal = Pop();
			SlotInfo dstAddr = Pop();
			Debug.Assert(StackTypeIsPointer(dstAddr.SlotType));

			string typeName = sig.GetCppName(TypeMgr);
			Debug.Assert(typeName == srcVal.SlotType);

			inst.CppCode = string.Format("*({0}*){1} = {2};",
				typeName,
				SlotInfoName(ref dstAddr),
				SlotInfoName(ref srcVal));

			ImplDependNames.Add(typeName);
		}

		private void Ldind(InstructionInfo inst, Code code)
		{
			SlotInfo poped = Pop();
			Debug.Assert(StackTypeIsPointer(poped.SlotType));

			StackType stype = null;
			string typeName = null;

			switch (code)
			{
				case Code.Ldind_I1:
					stype = StackType.I4;
					typeName = "int8_t";
					break;

				case Code.Ldind_I2:
					stype = StackType.I4;
					typeName = "int16_t";
					break;

				case Code.Ldind_I4:
					stype = StackType.I4;
					typeName = "int32_t";
					break;

				case Code.Ldind_I8:
					stype = StackType.I8;
					typeName = "int64_t";
					break;

				case Code.Ldind_U1:
					stype = StackType.I4;
					typeName = "uint8_t";
					break;

				case Code.Ldind_U2:
					stype = StackType.I4;
					typeName = "uint16_t";
					break;

				case Code.Ldind_U4:
					stype = StackType.I4;
					typeName = "uint32_t";
					break;

				case Code.Ldind_R4:
					stype = StackType.R4;
					typeName = "float";
					break;

				case Code.Ldind_R8:
					stype = StackType.R8;
					typeName = "double";
					break;

				case Code.Ldind_I:
					stype = StackType.Ptr;
					typeName = "intptr_t";
					break;

				case Code.Ldind_Ref:
					stype = StackType.Obj;
					typeName = StackTypeCppName(stype);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			string rval = string.Format("*({0}*){1}",
				typeName,
				SlotInfoName(ref poped));
			Load(inst, stype, rval, typeName);
		}

		private void Stind(InstructionInfo inst, Code code)
		{
			SlotInfo srcVal = Pop();
			SlotInfo dstAddr = Pop();
			Debug.Assert(StackTypeIsPointer(dstAddr.SlotType));

			string typeName = null;
			switch (code)
			{
				case Code.Stind_I1:
					typeName = "int8_t";
					break;

				case Code.Stind_I2:
					typeName = "int16_t";
					break;

				case Code.Stind_I4:
					typeName = "int32_t";
					break;

				case Code.Stind_I8:
					typeName = "int64_t";
					break;

				case Code.Stind_R4:
					typeName = "float";
					break;

				case Code.Stind_R8:
					typeName = "double";
					break;

				case Code.Stind_I:
					typeName = "intptr_t";
					break;

				case Code.Stind_Ref:
					typeName = StackTypeCppName(StackType.Obj);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			inst.CppCode = string.Format("*({0}*){1} = {2};",
				typeName,
				SlotInfoName(ref dstAddr),
				StorePoped(ref srcVal, typeName));
		}

		private void Cpobj(InstructionInfo inst, TypeSig sig)
		{
			if (!sig.IsValueType)
				throw new NotImplementedException();

			SlotInfo srcAddr = Pop();
			SlotInfo dstAddr = Pop();
			Debug.Assert(StackTypeIsPointer(srcAddr.SlotType));
			Debug.Assert(StackTypeIsPointer(dstAddr.SlotType));

			string typeName = sig.GetCppName(TypeMgr);

			inst.CppCode = string.Format("*({0}*){1} = *({0}*){2};",
				typeName,
				SlotInfoName(ref dstAddr),
				SlotInfoName(ref srcAddr));

			ImplDependNames.Add(typeName);
		}

		private void Initblk(InstructionInfo inst)
		{
			SlotInfo size = Pop();
			SlotInfo val = Pop();
			SlotInfo dstAddr = Pop();

			Debug.Assert(StackTypeIsPointer(dstAddr.SlotType));

			inst.CppCode = string.Format("memset({0}, (uint8_t){1}, (uint32_t){2});",
				SlotInfoName(ref dstAddr),
				SlotInfoName(ref val),
				SlotInfoName(ref size));
		}

		private void Cpblk(InstructionInfo inst)
		{
			SlotInfo size = Pop();
			SlotInfo srcAddr = Pop();
			SlotInfo dstAddr = Pop();

			Debug.Assert(StackTypeIsPointer(srcAddr.SlotType));
			Debug.Assert(StackTypeIsPointer(dstAddr.SlotType));

			inst.CppCode = string.Format("memcpy({0}, {1}, (uint32_t){2});",
				SlotInfoName(ref dstAddr),
				SlotInfoName(ref srcAddr),
				SlotInfoName(ref size));
		}

		private void Sizeof(InstructionInfo inst, TypeSig sig)
		{
			if (sig.IsValueType)
			{
				string typeName = sig.GetCppName(TypeMgr);
				string rval = string.Format("sizeof({0})",
					typeName);

				Load(inst, StackType.I4, rval);

				ImplDependNames.Add(typeName);
			}
			else
			{
				Load(inst, StackType.I4, "sizeof(void*)");
			}
		}

		private void Localloc(InstructionInfo inst)
		{
			SlotInfo size = Pop();

			Debug.Assert(StackTypeIsInt(size.SlotType));

			string rval = string.Format("il2cpp_Localloc((uintptr_t){0}, {1})",
				SlotInfoName(ref size),
				CurrMethod.InitLocals ? "true" : "false");

			Load(inst, StackType.Ptr, rval);
		}

		private static bool IsCppTypeConvertValid(string ltype, string rtype)
		{
			if (ltype == rtype)
				return true;

			switch (ltype)
			{
				case "int16_t":
					{
						switch (rtype)
						{
							case "int8_t":
							case "uint8_t":
								return true;
						}
						return false;
					}

				case "uint16_t":
					{
						switch (rtype)
						{
							case "uint8_t":
								return true;
						}
						return false;
					}

				case "int32_t":
					{
						switch (rtype)
						{
							case "int16_t":
							case "uint16_t":
							case "int8_t":
							case "uint8_t":
								return true;
						}
						return false;
					}

				case "uint32_t":
					{
						switch (rtype)
						{
							case "uint16_t":
							case "uint8_t":
								return true;
						}
						return false;
					}

				case "int64_t":
					{
						switch (rtype)
						{
							case "int32_t":
							case "uint32_t":
							case "int16_t":
							case "uint16_t":
							case "int8_t":
							case "uint8_t":
								return true;
						}
						return false;
					}

				case "uint64_t":
					{
						switch (rtype)
						{
							case "uint32_t":
							case "uint16_t":
							case "uint8_t":
								return true;
						}
						return false;
					}
			}
			return false;
		}

		private static bool IsBinaryOpValid(StackType op1, StackType op2, out StackType retType, Code code)
		{
			retType = StackType.I4;
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
					return false;

				case StackType.I8:
					if (op2 == StackType.I8)
					{
						retType = StackType.I8;
						return true;
					}
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
					return false;

				case StackType.R8:
					if (op2 == StackType.R4 || op2 == StackType.R8)
					{
						retType = StackType.R8;
						return true;
					}
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
					return false;

				case StackType.Obj:
					return false;

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}

		private static bool IsUnaryOpValid(StackType op, out StackType retType)
		{
			retType = op;
			switch (op)
			{
				case StackType.I4:
				case StackType.I8:
				case StackType.R4:
				case StackType.R8:
				case StackType.Ptr:
					return true;
			}
			return false;
		}

		private static bool IsBinaryCompareValid(StackType op1, StackType op2, Code code)
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

				default:
					throw new ArgumentOutOfRangeException(nameof(op1), op1, null);
			}
		}

		private static bool IsIntegerOpValid(StackType op1, StackType op2, out StackType retType)
		{
			retType = op2;
			switch (op1)
			{
				case StackType.I4:
					return op2 == StackType.I4 || op2 == StackType.Ptr;

				case StackType.I8:
					return op2 == StackType.I8;

				case StackType.Ptr:
					if (op2 == StackType.I4 || op2 == StackType.Ptr)
					{
						retType = StackType.Ptr;
						return true;
					}
					return false;
			}
			return false;
		}
	}
}
