using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	// 类型管理器
	internal class TypeManager
	{
		public const string NsIl2cppRT = "il2cpprt";

		// 当前环境
		public readonly Il2cppContext Context;
		public ICorLibTypes CorLibTypes => Context.CorLibTypes;

		// 实例类型映射
		private readonly Dictionary<string, TypeX> TypeMap = new Dictionary<string, TypeX>();
		private readonly Dictionary<string, TypeX> RawTypeMap = new Dictionary<string, TypeX>();
		public Dictionary<string, TypeX>.ValueCollection Types => TypeMap.Values;
		// 方法表映射
		private readonly Dictionary<string, MethodTable> MethodTableMap = new Dictionary<string, MethodTable>();
		// 虚调用方法集合
		private readonly HashSet<MethodX> VCallEntries = new HashSet<MethodX>();
		// 方法待处理队列
		private readonly Queue<MethodX> PendingMethods = new Queue<MethodX>();
		// 协逆变映射
		private class VarianceGroup
		{
			public bool IsProcessed;
			public readonly HashSet<TypeX> TypeGroup = new HashSet<TypeX>();
			public List<VarianceType> VarianceTypes;
		}
		private readonly Dictionary<TypeDef, VarianceGroup> VarianceMap = new Dictionary<TypeDef, VarianceGroup>();

		private TypeDef ThrowHelperType;
		private readonly HashSet<string> ResolvedExceptions = new HashSet<string>();

		// 运行时装箱类型原型
		private TypeDef BoxedTypePrototype;
		// 运行时一维数组原型
		private TypeDef SZArrayPrototype;
		// 运行时多维数组原型映射
		private readonly Dictionary<uint, TypeDef> MDArrayProtoMap = new Dictionary<uint, TypeDef>();

		private DelegateProperty DelegateType;

		public TypeX RTTypeHandle;
		public TypeX RTMethodHandle;
		public TypeX RTFieldHandle;
		public TypeX RTTypedRef;

		// 字符串对象是否已经解析
		private bool IsStringTypeResolved;

		public StringBuilder RecordLogs;
#if DEBUG && false
		private void RecordResolvingMethod(MethodX metX)
		{
			if (RecordLogs == null)
				RecordLogs = new StringBuilder();

			string metDeclTypeName = metX.DeclType.GetNameKey();
			string metName = metX.GetNameKey();
			RecordLogs.AppendFormat(" * {0} -> {1}\n", metDeclTypeName, metName);
		}

		private void RecordAddingMethod(MethodX metX)
		{
			if (RecordLogs == null)
				RecordLogs = new StringBuilder();

			if (!PendingMethods.Contains(metX))
			{
				string metDeclTypeName = metX.DeclType.GetNameKey();
				string metName = metX.GetNameKey();
				RecordLogs.AppendFormat("   |- {0} -> {1}\n", metDeclTypeName, metName);
			}
		}
#else
		private void RecordResolvingMethod(MethodX metX)
		{
		}

		private void RecordAddingMethod(MethodX metX)
		{

		}
#endif

		public TypeManager(Il2cppContext context)
		{
			Context = context;
		}

		public TypeX GetTypeByName(string name)
		{
			if (TypeMap.TryGetValue(name, out var tyX))
				return tyX;

			if (RawTypeMap.TryGetValue(name, out var tyXRaw))
				return tyXRaw;

			return null;
		}

		// 解析所有引用
		public void ResolveAll()
		{
			while (PendingMethods.Count > 0)
			{
				do
				{
					MethodX metX = PendingMethods.Dequeue();

					// 跳过不需要处理的方法
					if (metX.IsSkipProcessing)
						continue;

					// 跳过已处理的方法
					if (metX.IsProcessed)
						continue;

					// 设置已处理标记
					metX.IsProcessed = true;

					// 展开指令列表
					BuildInstructions(metX);
				}
				while (PendingMethods.Count > 0);

				// 解析协逆变
				ResolveVariances();
				// 解析虚调用
				ResolveVCalls();
			}
		}

		private void BuildInstructions(MethodX metX)
		{
			Debug.Assert(metX.InstList == null);

			if (!metX.Def.HasBody || !metX.Def.Body.HasInstructions)
				return;

			RecordResolvingMethod(metX);

			IGenericReplacer replacer = new GenericReplacer(metX.DeclType, metX);

			var defInstList = metX.Def.Body.Instructions;
			int numInsts = defInstList.Count;

			InstInfo[] instList = new InstInfo[numInsts];

			Dictionary<uint, int> offsetMap = new Dictionary<uint, int>();
			List<InstInfo> branchInsts = new List<InstInfo>();

			// 构建指令列表
			for (int ip = 0; ip < numInsts; ++ip)
			{
				var defInst = defInstList[ip];
				var inst = instList[ip] = new InstInfo();
				inst.OpCode = defInst.OpCode;
				inst.Operand = defInst.Operand;
				inst.Offset = ip;

				offsetMap.Add(defInst.Offset, ip);
				switch (inst.OpCode.OperandType)
				{
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineSwitch:
						branchInsts.Add(inst);
						break;

					default:
						ResolveOperand(inst, replacer);
						break;
				}
			}

			// 重定向跳转位置
			foreach (var inst in branchInsts)
			{
				if (inst.Operand is Instruction defInst)
				{
					int target;
					inst.Operand = target = offsetMap[defInst.Offset];
					instList[target].IsBrTarget = true;
				}
				else if (inst.Operand is Instruction[] defInsts)
				{
					int[] insts = new int[defInsts.Length];
					for (int i = 0; i < defInsts.Length; ++i)
					{
						int target;
						insts[i] = target = offsetMap[defInsts[i].Offset];
						instList[target].IsBrTarget = true;
					}
					inst.Operand = insts;
				}
			}

			// 展开异常处理信息
			if (metX.Def.Body.HasExceptionHandlers)
			{
				List<Tuple<int, ExHandlerInfo>> sortedHandlers = new List<Tuple<int, ExHandlerInfo>>();
				int idx = 0;
				foreach (var eh in metX.Def.Body.ExceptionHandlers)
				{
					ExHandlerInfo info = new ExHandlerInfo();
					info.TryStart = offsetMap[eh.TryStart.Offset];
					info.TryEnd = offsetMap[eh.TryEnd.Offset];

					if (eh.FilterStart != null)
						info.FilterStart = offsetMap[eh.FilterStart.Offset];
					else
						info.FilterStart = -1;

					info.HandlerStart = offsetMap[eh.HandlerStart.Offset];

					if (eh.HandlerEnd != null)
						info.HandlerEnd = offsetMap[eh.HandlerEnd.Offset];
					else
						info.HandlerEnd = instList.Length;

					if (eh.CatchType != null)
					{
						info.CatchType = ResolveTypeDefOrRef(eh.CatchType, replacer);
						info.CatchType.NeedGenIsType = true;
					}

					info.HandlerType = eh.HandlerType;
					sortedHandlers.Add(new Tuple<int, ExHandlerInfo>(idx++, info));
				}
				// 根据 try 位置排序, 如果相同则根据定义顺序排序
				sortedHandlers.Sort((lhs, rhs) =>
				{
					int cmp = lhs.Item2.TryStart.CompareTo(rhs.Item2.TryStart);
					if (cmp == 0)
					{
						cmp = rhs.Item2.TryEnd.CompareTo(lhs.Item2.TryEnd);
						if (cmp == 0)
							return lhs.Item1.CompareTo(rhs.Item1);
					}
					return cmp;
				});

				// 合并同范围的异常处理器
				List<ExHandlerInfo> handlers = new List<ExHandlerInfo>();

				ExHandlerInfo headInfo = null;
				int count = sortedHandlers.Count;
				for (int i = 0; i < count; ++i)
				{
					ExHandlerInfo currInfo = sortedHandlers[i].Item2;
					if (headInfo != null && headInfo.NeedCombine(currInfo))
						headInfo.CombinedHandlers.Add(currInfo);
					else
					{
						headInfo = currInfo;
						Debug.Assert(headInfo.CombinedHandlers.Count == 0);
						headInfo.CombinedHandlers.Add(headInfo);
						handlers.Add(headInfo);
					}

					instList[currInfo.HandlerOrFilterStart].IsBrTarget = true;
				}

				metX.ExHandlerList = handlers;
			}

			metX.InstList = instList;
		}

		private void ResolveOpCodeException(Code opCode)
		{
			switch (opCode)
			{
				case Code.Ckfinite:
					ResolveExceptionType("ArithmeticException");
					return;

				case Code.Add_Ovf:
				case Code.Add_Ovf_Un:
				case Code.Sub_Ovf:
				case Code.Sub_Ovf_Un:
				case Code.Mul_Ovf:
				case Code.Mul_Ovf_Un:
				case Code.Conv_Ovf_I1:
				case Code.Conv_Ovf_I2:
				case Code.Conv_Ovf_I4:
				case Code.Conv_Ovf_I8:
				case Code.Conv_Ovf_U1:
				case Code.Conv_Ovf_U2:
				case Code.Conv_Ovf_U4:
				case Code.Conv_Ovf_U8:
				case Code.Conv_Ovf_I:
				case Code.Conv_Ovf_U:
				case Code.Conv_Ovf_I1_Un:
				case Code.Conv_Ovf_I2_Un:
				case Code.Conv_Ovf_I4_Un:
				case Code.Conv_Ovf_I8_Un:
				case Code.Conv_Ovf_U1_Un:
				case Code.Conv_Ovf_U2_Un:
				case Code.Conv_Ovf_U4_Un:
				case Code.Conv_Ovf_U8_Un:
				case Code.Conv_Ovf_I_Un:
				case Code.Conv_Ovf_U_Un:
					ResolveExceptionType("OverflowException");
					return;

				case Code.Unbox:
				case Code.Unbox_Any:
				case Code.Castclass:
					ResolveExceptionType("InvalidCastException");
					return;
			}
		}

		private void ResolveOperand(InstInfo inst, IGenericReplacer replacer)
		{
			// 预处理指令
			switch (inst.OpCode.Code)
			{
				case Code.Ldstr:
					ResolveStringType();
					break;

				case Code.Newarr:
					{
						// newobj T[]::.ctor(int)
						TypeSig elemSig = ((ITypeDefOrRef)inst.Operand).ToTypeSig();
						inst.OpCode = OpCodes.Newobj;
						inst.Operand = new MemberRefUser(
							Context.CorLibModule,
							".ctor",
							MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32),
							new TypeSpecUser(new SZArraySig(elemSig)));
					}
					break;

				case Code.Ldelema:
					{
						// call T& T[]::Address(int)
						TypeSig elemSig = ((ITypeDefOrRef)inst.Operand).ToTypeSig();
						inst.OpCode = OpCodes.Call;
						inst.Operand = new MemberRefUser(
							Context.CorLibModule,
							"Address",
							MethodSig.CreateInstance(new ByRefSig(elemSig), CorLibTypes.Int32),
							new TypeSpecUser(new SZArraySig(elemSig)));
					}
					break;

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
					{
						TypeSig elemSig = null;
						switch (inst.OpCode.Code)
						{
							case Code.Ldelem_I1:
								elemSig = CorLibTypes.SByte;
								break;
							case Code.Ldelem_U1:
								elemSig = CorLibTypes.Byte;
								break;
							case Code.Ldelem_I2:
								elemSig = CorLibTypes.Int16;
								break;
							case Code.Ldelem_U2:
								elemSig = CorLibTypes.UInt16;
								break;
							case Code.Ldelem_I4:
								elemSig = CorLibTypes.Int32;
								break;
							case Code.Ldelem_U4:
								elemSig = CorLibTypes.UInt32;
								break;
							case Code.Ldelem_I8:
								elemSig = CorLibTypes.Int64;
								break;
							case Code.Ldelem_I:
								elemSig = CorLibTypes.IntPtr;
								break;
							case Code.Ldelem_R4:
								elemSig = CorLibTypes.Single;
								break;
							case Code.Ldelem_R8:
								elemSig = CorLibTypes.Double;
								break;
							case Code.Ldelem_Ref:
								elemSig = CorLibTypes.Object;
								break;
							case Code.Ldelem:
								elemSig = ((ITypeDefOrRef)inst.Operand).ToTypeSig();
								break;
						}
						// call T T[]::Get(int)
						inst.OpCode = OpCodes.Call;
						inst.Operand = new MemberRefUser(
							Context.CorLibModule,
							"Get",
							MethodSig.CreateInstance(elemSig, CorLibTypes.Int32),
							new TypeSpecUser(new SZArraySig(elemSig)));
					}
					break;

				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_I:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Ref:
				case Code.Stelem:
					{
						TypeSig elemSig = null;
						switch (inst.OpCode.Code)
						{
							case Code.Stelem_I1:
								elemSig = CorLibTypes.SByte;
								break;
							case Code.Stelem_I2:
								elemSig = CorLibTypes.Int16;
								break;
							case Code.Stelem_I4:
								elemSig = CorLibTypes.Int32;
								break;
							case Code.Stelem_I8:
								elemSig = CorLibTypes.Int64;
								break;
							case Code.Stelem_I:
								elemSig = CorLibTypes.IntPtr;
								break;
							case Code.Stelem_R4:
								elemSig = CorLibTypes.Single;
								break;
							case Code.Stelem_R8:
								elemSig = CorLibTypes.Double;
								break;
							case Code.Stelem_Ref:
								elemSig = CorLibTypes.Object;
								break;
							case Code.Stelem:
								elemSig = ((ITypeDefOrRef)inst.Operand).ToTypeSig();
								break;
						}
						// call void T[]::Set(int,T)
						inst.OpCode = OpCodes.Call;
						inst.Operand = new MemberRefUser(
							Context.CorLibModule,
							"Set",
							MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32, elemSig),
							new TypeSpecUser(new SZArraySig(elemSig)));
					}
					break;
			}

			bool isLdtoken = inst.OpCode.Code == Code.Ldtoken;
			var operType = inst.OpCode.OperandType;
			Debug.Assert(operType == OperandType.InlineTok == isLdtoken);

			if (isLdtoken)
			{
				switch (inst.Operand)
				{
					case MemberRef memRef:
						if (memRef.IsMethodRef)
							operType = OperandType.InlineMethod;
						else
						{
							Debug.Assert(memRef.IsFieldRef);
							operType = OperandType.InlineField;
						}
						break;

					case MethodDef _:
					case MethodSpec _:
						operType = OperandType.InlineMethod;
						break;

					case FieldDef _:
						operType = OperandType.InlineField;
						break;

					case TypeDef _:
					case TypeRef _:
					case TypeSpec _:
						operType = OperandType.InlineType;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			switch (operType)
			{
				case OperandType.InlineMethod:
					{
						MethodX resMetX;
						switch (inst.Operand)
						{
							case MethodDef metDef:
								resMetX = ResolveMethodDef(metDef);
								break;
							case MemberRef memRef:
								resMetX = ResolveMethodRef(memRef, replacer);
								break;
							case MethodSpec metSpec:
								resMetX = ResolveMethodSpec(metSpec, replacer);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						bool isReAddMethod = false;

						if (inst.OpCode.Code == Code.Newobj)
						{
							Debug.Assert(!resMetX.Def.IsStatic);
							Debug.Assert(resMetX.Def.IsConstructor);
							// 设置实例化标记
							resMetX.DeclType.IsInstantiated = true;
							// 生成静态构造和终结器
							GenStaticCctor(resMetX.DeclType);
							GenFinalizer(resMetX.DeclType);
						}
						else if (inst.OpCode.Code == Code.Callvirt ||
								 inst.OpCode.Code == Code.Ldvirtftn)
						{
							if (resMetX.IsVirtual)
								AddVCallEntry(resMetX);
							else
							{
								// 非虚方法重定向指令
								inst.OpCode = inst.OpCode.Code == Code.Callvirt ?
									OpCodes.Call : OpCodes.Ldftn;
							}
						}
						else if (resMetX.IsVirtual &&
								(inst.OpCode.Code == Code.Call ||
								 inst.OpCode.Code == Code.Ldftn))
						{
							// 处理方法替换
							if (resMetX.DeclType.QueryCallReplace(this, resMetX.Def, out TypeX implTyX, out var implDef))
							{
								resMetX.IsSkipProcessing = true;

								Debug.Assert(implTyX != null);
								MethodX implMetX = MakeMethodX(implTyX, implDef, resMetX.GenArgs);

								resMetX = implMetX;
							}
							else
								isReAddMethod = true;
						}
						else
							isReAddMethod = true;

						if (isReAddMethod)
						{
							// 尝试重新加入处理队列
							resMetX.IsSkipProcessing = false;
							AddPendingMethod(resMetX);
						}

						if (resMetX.IsStatic)
						{
							// 生成静态构造
							GenStaticCctor(resMetX.DeclType);
						}

						if (isLdtoken)
							resMetX.NeedGenMetadata = true;

						inst.Operand = resMetX;
					}
					break;

				case OperandType.InlineField:
					{
						FieldX resFldX;
						switch (inst.Operand)
						{
							case FieldDef fldDef:
								resFldX = ResolveFieldDef(fldDef);
								break;
							case MemberRef memRef:
								resFldX = ResolveFieldRef(memRef, replacer);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						if (resFldX.IsStatic)
						{
							// 生成静态构造
							GenStaticCctor(resFldX.DeclType);
						}

						if (isLdtoken)
							resFldX.NeedGenMetadata = true;

						inst.Operand = resFldX;
					}
					break;

				case OperandType.InlineType:
					{
						ITypeDefOrRef tyDefRef = (ITypeDefOrRef)inst.Operand;
						bool isKeepToken = false;

						// 无法解析的类型保持原始形态
						switch (tyDefRef)
						{
							case TypeDef tyDef:
								if (tyDef.HasGenericParameters)
									isKeepToken = true;
								break;

							case TypeRef tyRef:
								if (tyRef.Resolve().HasGenericParameters)
									isKeepToken = true;
								break;

							case TypeSpec tySpec:
								switch (tySpec.TypeSig)
								{
									case TypeDefOrRefSig _:
									case GenericInstSig _:
									case SZArraySig _:
									case ArraySig _:
									case GenericVar _:
									case GenericMVar _:
										break;

									case CModReqdSig modReqdSig:
										tyDefRef = modReqdSig.RemoveModifiers().ToTypeDefOrRef();
										break;

									case CModOptSig modOptSig:
										tyDefRef = modOptSig.RemoveModifiers().ToTypeDefOrRef();
										break;

									default:
										isKeepToken = true;
										break;
								}
								break;
						}

						if (!isKeepToken)
						{
							TypeX resTyX = ResolveTypeDefOrRef(tyDefRef, replacer);

							switch (inst.OpCode.Code)
							{
								case Code.Box:
								case Code.Unbox:
								case Code.Unbox_Any:
								case Code.Isinst:
								case Code.Castclass:
								case Code.Constrained:
									if (resTyX.IsValueType)
									{
										if (resTyX.IsNullableType)
										{
											if (resTyX.NullableElem == null)
											{
												// 解析可空类型的所有字段
												ResolveAllFields(resTyX);

												resTyX.NullableElem = ResolveTypeDefOrRef(resTyX.GenArgs[0].ToTypeDefOrRef(), null);
												ResolveBoxedType(resTyX.NullableElem);
											}
										}
										else
											ResolveBoxedType(resTyX);
									}
									break;
							}

							switch (inst.OpCode.Code)
							{
								case Code.Unbox:
								case Code.Unbox_Any:
								case Code.Isinst:
								case Code.Castclass:
									{
										TypeX tempTyX = resTyX;
										if (tempTyX.IsNullableType)
											tempTyX = tempTyX.NullableElem;
										if (tempTyX.IsValueType && tempTyX.BoxedType != null)
											tempTyX = tempTyX.BoxedType;

										tempTyX.NeedGenIsType = true;
									}
									break;
							}

							if (isLdtoken)
								resTyX.NeedGenMetadata = true;

							inst.Operand = resTyX;
						}
					}
					break;

				case OperandType.InlineSig:
					{
						throw new NotImplementedException();
					}
			}

			// 后处理指令
			switch (inst.OpCode.Code)
			{
				case Code.Sizeof:
					ResolveAllFields((TypeX)inst.Operand);
					break;

				case Code.Ldtoken:
					switch (operType)
					{
						case OperandType.InlineType:
							{
								if (inst.Operand is TypeX opTyX)
									ResolveAllFields(opTyX);
								//! ResolveAllMethods

								if (RTTypeHandle == null)
								{
									RTTypeHandle = ResolveTypeDefOrRef(CorLibTypes.GetTypeRef("System", "RuntimeTypeHandle").Resolve(), null);
									ResolveAllFields(RTTypeHandle);
								}
							}
							break;
						case OperandType.InlineMethod:
							if (RTMethodHandle == null)
							{
								RTMethodHandle = ResolveTypeDefOrRef(CorLibTypes.GetTypeRef("System", "RuntimeMethodHandle").Resolve(), null);
								ResolveAllFields(RTMethodHandle);
							}
							break;
						case OperandType.InlineField:
							if (RTFieldHandle == null)
							{
								RTFieldHandle = ResolveTypeDefOrRef(CorLibTypes.GetTypeRef("System", "RuntimeFieldHandle").Resolve(), null);
								ResolveAllFields(RTFieldHandle);
							}
							break;
					}
					break;

				case Code.Mkrefany:
					if (RTTypedRef == null)
					{
						RTTypedRef = ResolveTypeDefOrRef(CorLibTypes.GetTypeRef("System", "TypedReference").Resolve(), null);
						ResolveAllFields(RTTypedRef);
					}
					break;
			}

			ResolveOpCodeException(inst.OpCode.Code);
		}

		private void GenStaticCctor(TypeX tyX)
		{
			if (tyX.IsCctorGenerated)
				return;
			tyX.IsCctorGenerated = true;

			MethodDef cctor = tyX.Def.Methods.FirstOrDefault(met => met.IsStatic && met.IsConstructor);
			if (cctor != null)
			{
				MethodX metX = new MethodX(tyX, cctor);
				tyX.CctorMethod = AddMethod(metX);
			}
		}

		private void GenFinalizer(TypeX tyX)
		{
			tyX.GenFinalizerMethod(this);
		}

		private void AddVCallEntry(MethodX virtMetX)
		{
			// 跳过该方法的处理
			virtMetX.IsSkipProcessing = true;
			// 登记到虚入口
			VCallEntries.Add(virtMetX);
		}

		private void ResolveVCalls()
		{
			foreach (MethodX virtMetX in VCallEntries)
			{
				TypeX entryTyX = virtMetX.DeclType;
				MethodDef entryDef = virtMetX.Def;

				// 在虚入口所在类型内解析虚方法
				if (entryTyX.IsInstantiated)
					ResolveVMethod(virtMetX, entryTyX, entryTyX, entryDef);

				// 在继承类型内解析虚方法
				foreach (TypeX derivedTyX in entryTyX.DerivedTypes)
					ResolveVMethod(virtMetX, derivedTyX, entryTyX, entryDef);
			}
		}

		private void ResolveVMethod(
			MethodX virtMetX,
			TypeX derivedTyX,
			TypeX entryTyX,
			MethodDef entryDef)
		{
			// 跳过没有实例化的类型
			if (!derivedTyX.IsInstantiated)
				return;

			RecordResolvingMethod(virtMetX);

			// 查询虚方法绑定
			derivedTyX.QueryCallVirt(this, entryTyX, entryDef, out TypeX implTyX, out var implDef);

			// 构造实现方法
			Debug.Assert(implTyX != null);
			MethodX implMetX = MakeMethodX(implTyX, implDef, virtMetX.GenArgs);

			// 关联实现方法到虚方法
			virtMetX.AddOverrideImpl(implMetX, derivedTyX);

			// 处理该方法
			implMetX.IsSkipProcessing = false;
			AddPendingMethod(implMetX);
		}

		public FieldX ResolveFieldDef(FieldDef fldDef)
		{
			TypeX declType = ResolveTypeDefOrRef(fldDef.DeclaringType, null);
			FieldX fldX = new FieldX(declType, fldDef);
			return AddField(fldX);
		}

		public FieldX ResolveFieldRef(MemberRef memRef, IGenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);

			TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, replacer);
			FieldX fldX = new FieldX(declType, memRef.ResolveField());
			return AddField(fldX);
		}

		private FieldX AddField(FieldX fldX)
		{
			Debug.Assert(fldX != null);

			// 尝试添加到所属类型
			string nameKey = fldX.GetNameKey();
			if (fldX.DeclType.GetField(nameKey, out var ofldX))
				return ofldX;
			fldX.DeclType.AddField(nameKey, fldX);

			IGenericReplacer replacer = new GenericReplacer(fldX.DeclType, null);
			// 展开字段类型
			fldX.FieldType = Helper.ReplaceGenericSig(fldX.Def.FieldType, replacer);

			return fldX;
		}

		// 解析方法定义并添加
		public MethodX ResolveMethodDef(MethodDef metDef)
		{
			TypeX declType = ResolveTypeDefOrRef(metDef.DeclaringType, null);
			MethodX metX = new MethodX(declType, metDef);
			return AddMethod(metX);
		}

		// 解析方法引用并添加
		public MethodX ResolveMethodRef(MemberRef memRef, IGenericReplacer replacer)
		{
			Debug.Assert(memRef.IsMethodRef);

			TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, replacer);

			MethodDef metDef = declType.Def.FindMethod(memRef.Name, memRef.MethodSig);
			if (metDef == null)
				metDef = declType.Def.FindMethod(memRef.Name);
			if (metDef == null)
				metDef = memRef.ResolveMethod();
			if (metDef == null)
				throw new NotImplementedException();

			if (metDef.DeclaringType != declType.Def)
			{
				// 处理引用类型不包含该方法的情况
				declType = declType.FindBaseType(metDef.DeclaringType);
				Debug.Assert(declType != null);
			}

			MethodX metX = new MethodX(declType, metDef);
			return AddMethod(metX);
		}

		// 解析泛型方法并添加
		public MethodX ResolveMethodSpec(MethodSpec metSpec, IGenericReplacer replacer)
		{
			TypeX declType = ResolveTypeDefOrRef(metSpec.DeclaringType, replacer);
			MethodDef metDef = metSpec.ResolveMethodDef();
			if (metDef.DeclaringType != declType.Def)
			{
				// 处理引用类型不包含该方法的情况
				declType = declType.FindBaseType(metDef.DeclaringType);
				Debug.Assert(declType != null);
			}

			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			Debug.Assert(metGenArgs != null);
			IList<TypeSig> genArgs = Helper.ReplaceGenericSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(declType, metDef);
			metX.GenArgs = genArgs;
			return AddMethod(metX);
		}

		// 添加方法到类型
		public MethodX AddMethod(MethodX metX)
		{
			Debug.Assert(metX != null);

			// 尝试添加到所属类型
			string nameKey = metX.GetNameKey();
			if (metX.DeclType.GetMethod(nameKey, out var ometX))
				return ometX;
			metX.DeclType.AddMethod(nameKey, metX);

			IGenericReplacer replacer = new GenericReplacer(metX.DeclType, metX);

			// 展开返回值和参数类型
			metX.ReturnType = Helper.ReplaceGenericSig(metX.DefSig.RetType, replacer);
			metX.ParamTypes = Helper.ReplaceGenericSigList(metX.DefSig.Params, replacer);
			if (metX.HasThis)
				metX.ParamTypes.Insert(0, Helper.ReplaceGenericSig(metX.DeclType.GetThisTypeSig(), replacer));
			metX.ParamAfterSentinel = Helper.ReplaceGenericSigList(metX.DefSig.ParamsAfterSentinel, replacer);

			MethodDef metDef = metX.Def;
			if (metDef.HasBody)
			{
				if (metDef.Body.HasVariables)
				{
					metX.LocalTypes = new List<TypeSig>();
					foreach (var loc in metDef.Body.Variables)
					{
						Debug.Assert(loc.Index == metX.LocalTypes.Count);
						metX.LocalTypes.Add(loc.Type);
					}
					// 展开局部变量类型
					metX.LocalTypes = Helper.ReplaceGenericSigList(metX.LocalTypes, replacer);
				}
			}

			// 添加到待处理队列
			AddPendingMethod(metX);

			return metX;
		}

		private void AddPendingMethod(MethodX metX)
		{
			if (!metX.IsProcessed)
			{
				RecordAddingMethod(metX);
				PendingMethods.Enqueue(metX);
			}
		}

		private void ExpandType(TypeX tyX)
		{
			IGenericReplacer replacer = new GenericReplacer(tyX, null);

			// 解析基类
			if (tyX.Def.BaseType != null)
			{
				tyX.BaseType = ResolveTypeDefOrRef(tyX.Def.BaseType, replacer);

				string baseName = tyX.BaseType.GetNameKey();
				if (tyX.Def.IsEnum)
				{
					Debug.Assert(baseName == "System.Enum");
					// 枚举类型
					var fldDef = tyX.Def.Fields.FirstOrDefault(f => !f.IsStatic);
					Debug.Assert(fldDef != null);

					FieldX fldX = ResolveFieldDef(fldDef);
					TypeX enumBaseType = ResolveTypeSig(fldX.FieldType, replacer);
					tyX.EnumInfo = new EnumProperty { EnumField = fldX, EnumBaseType = enumBaseType };
					enumBaseType.AddDerivedEnumTypes(tyX);
				}
				else if (tyX.Def.IsDelegate)
				{
					Debug.Assert(baseName == "System.MulticastDelegate");
					// 委托类型
					Debug.Assert(DelegateType != null);
					tyX.DelegateInfo = DelegateType;
				}
			}
			// 解析接口
			if (tyX.Def.HasInterfaces)
			{
				uint lastRid = 0;
				foreach (var inf in tyX.Def.Interfaces)
				{
					Debug.Assert(lastRid == 0 || inf.Rid > lastRid);
					lastRid = inf.Rid;
					tyX.Interfaces.Add(ResolveTypeDefOrRef(inf.Interface, replacer));
				}
			}

			// 解析协逆变
			TryAddVariance(tyX);

			// 更新子类集合
			tyX.UpdateDerivedTypes();

			// 补齐 GetHashCode
			TryAddGetHashCode(tyX);
			// 补齐 Equals
			TryAddEquals(tyX);

			if (tyX.Def.IsExplicitLayout)
			{
				// 递归解析所有的字段
				ResolveAllFields(tyX, true);
			}

			string typeName = tyX.GetNameKey();
			if (typeName == "String")
			{
				// 解析所有的字段
				ResolveAllFields(tyX);
			}
			else if (typeName == "System.Delegate")
			{
				// 解析委托类
				ResolveDelegateType();
			}
		}

		private void TryAddGetHashCode(TypeX tyX)
		{
			const string kGetHashCode = "GetHashCode";

			// 值类型补齐 GetHashCode
			if (!tyX.IsValueType ||
				tyX.Def.FindMethod(kGetHashCode) != null &&
				tyX.Def.FindMethod(kGetHashCode, MethodSig.CreateInstance(CorLibTypes.Int32)) != null)
				return;

			var metGetHashCode = CorLibTypes.Object.TypeRef.ResolveTypeDef().FindMethod(kGetHashCode);
			Debug.Assert(metGetHashCode != null);

			MethodDefUser metDef = new MethodDefUser(metGetHashCode.Name, metGetHashCode.MethodSig, metGetHashCode.Attributes);
			metDef.IsReuseSlot = true;
			tyX.Def.Methods.Add(metDef);

			var body = metDef.Body = new CilBody();
			var insts = body.Instructions;

			List<FieldDef> fldListSorted = new List<FieldDef>(tyX.Def.Fields);
			fldListSorted.Sort((lhs, rhs) => lhs.Rid.CompareTo(rhs.Rid));

			// 筛选需要计算的字段
			var fldList = fldListSorted.Where(fld => fld.FieldType.IsValueType).ToList();

			const int fldLimit = 4;
			if (fldList.Count > fldLimit)
				fldList.RemoveRange(fldLimit, fldList.Count - fldLimit);
			else if (fldList.Count == 0 && fldListSorted.Count > 0)
				fldList.Add(fldListSorted[0]);

			insts.Add(OpCodes.Ldc_I4.ToInstruction(0x14AE055C ^ tyX.GetNameKey().GetHashCode()));

			bool last = false;
			TypeSig tyGenInstSig = tyX.GetDefGenericInstSig();
			foreach (var fldDef in fldList)
			{
				if (!Helper.IsInstanceField(fldDef))
					continue;

				if (last)
				{
					insts.Add(OpCodes.Dup.ToInstruction());
					insts.Add(OpCodes.Ldc_I4_5.ToInstruction());
					insts.Add(OpCodes.Shl.ToInstruction());
					insts.Add(OpCodes.Add.ToInstruction());
				}
				last = true;

				insts.Add(OpCodes.Ldarg_0.ToInstruction());

				MemberRef fldRef = null;
				if (tyGenInstSig != null)
					fldRef = new MemberRefUser(fldDef.Module, fldDef.Name, fldDef.FieldSig, new TypeSpecUser(tyGenInstSig));

				if (fldDef.FieldType.IsValueType ||
					fldDef.FieldType.ElementType == ElementType.Var)
				{
					if (fldRef != null)
						insts.Add(OpCodes.Ldflda.ToInstruction(fldRef));
					else
						insts.Add(OpCodes.Ldflda.ToInstruction(fldDef));
					insts.Add(OpCodes.Constrained.ToInstruction(fldDef.FieldType.ToTypeDefOrRef()));
				}
				else
				{
					if (fldRef != null)
						insts.Add(OpCodes.Ldfld.ToInstruction(fldRef));
					else
						insts.Add(OpCodes.Ldfld.ToInstruction(fldDef));
				}

				insts.Add(OpCodes.Callvirt.ToInstruction(metGetHashCode));
				insts.Add(OpCodes.Xor.ToInstruction());
			}

			insts.Add(OpCodes.Ret.ToInstruction());
			insts.UpdateInstructionOffsets();
		}

		private void TryAddEquals(TypeX tyX)
		{
			const string kEquals = "Equals";

			// 值类型补齐 Equals
			if (!tyX.IsValueType ||
				tyX.Def.FindMethod(kEquals) != null &&
				tyX.Def.FindMethod(kEquals, MethodSig.CreateInstance(CorLibTypes.Boolean, CorLibTypes.Object)) != null)
				return;

			bool isBasicOrEnumType =
				tyX.IsEnumType ||
				Helper.IsBasicValueType(tyX.GetTypeSig().ElementType);

			var objTyDef = CorLibTypes.Object.TypeRef.ResolveTypeDef();
			var valueTypeDef = CorLibTypes.GetTypeRef("System", "ValueType").Resolve();
			var rtHlpDef = CorLibTypes.GetTypeRef("System.Runtime.CompilerServices", "RuntimeHelpers").Resolve();

			var metEquals = objTyDef.FindMethod(kEquals);
			Debug.Assert(metEquals != null);
			var metVTEquals = valueTypeDef.FindMethod(kEquals);
			Debug.Assert(metVTEquals != null);
			var metGetTyID = objTyDef.FindMethod("GetInternalTypeID");
			Debug.Assert(metGetTyID != null);
			var metRtGetTyID = rtHlpDef.FindMethod("GetInternalTypeID");
			Debug.Assert(metRtGetTyID != null);
			var metCanCmpBits = rtHlpDef.FindMethod("CanCompareBits");
			Debug.Assert(metCanCmpBits != null);
			var metFastCmp = rtHlpDef.FindMethod("FastCompareBits");
			Debug.Assert(metFastCmp != null);

			MethodDefUser metDef = new MethodDefUser(metEquals.Name, metEquals.MethodSig, metEquals.Attributes);
			metDef.IsReuseSlot = true;
			tyX.Def.Methods.Add(metDef);

			TypeSig tyGenInstSig = tyX.GetDefGenericInstSig();
			var selfSig = tyGenInstSig ?? tyX.GetDefTypeSig();

			var body = metDef.Body = new CilBody();
			var insts = body.Instructions;
			body.Variables.Add(new Local(selfSig));

			Instruction labelRetFalse = OpCodes.Nop.ToInstruction();
			Instruction labelLoopChk = OpCodes.Nop.ToInstruction();

			insts.Add(OpCodes.Ldarg_1.ToInstruction());
			insts.Add(OpCodes.Brfalse.ToInstruction(labelRetFalse));

			insts.Add(OpCodes.Call.ToInstruction(new MethodSpecUser(metRtGetTyID, new GenericInstMethodSig(selfSig))));
			insts.Add(OpCodes.Ldarg_1.ToInstruction());
			insts.Add(OpCodes.Call.ToInstruction(metGetTyID));
			insts.Add(OpCodes.Bne_Un.ToInstruction(labelRetFalse));

			insts.Add(OpCodes.Ldarg_1.ToInstruction());
			insts.Add(OpCodes.Unbox_Any.ToInstruction(selfSig.ToTypeDefOrRef()));
			insts.Add(OpCodes.Stloc_0.ToInstruction());

			if (!isBasicOrEnumType)
			{
				insts.Add(OpCodes.Call.ToInstruction(new MethodSpecUser(metCanCmpBits, new GenericInstMethodSig(selfSig))));
				insts.Add(OpCodes.Brfalse.ToInstruction(labelLoopChk));
			}

			insts.Add(OpCodes.Ldarg_0.ToInstruction());
			insts.Add(OpCodes.Ldloca.ToInstruction(body.Variables[0]));
			insts.Add(OpCodes.Call.ToInstruction(new MethodSpecUser(metFastCmp, new GenericInstMethodSig(selfSig))));
			insts.Add(OpCodes.Ret.ToInstruction());

			if (!isBasicOrEnumType)
			{
				insts.Add(labelLoopChk);

				List<FieldDef> fldList = new List<FieldDef>(tyX.Def.Fields);
				fldList.Sort((lhs, rhs) => lhs.Rid.CompareTo(rhs.Rid));

				foreach (var fldDef in fldList)
				{
					if (!Helper.IsInstanceField(fldDef))
						continue;

					MemberRef fldRef = null;
					if (tyGenInstSig != null)
						fldRef = new MemberRefUser(fldDef.Module, fldDef.Name, fldDef.FieldSig, new TypeSpecUser(tyGenInstSig));

					if (Helper.IsBasicValueType(fldDef.FieldType.ElementType))
					{
						insts.Add(OpCodes.Ldarg_0.ToInstruction());
						if (fldRef != null)
							insts.Add(OpCodes.Ldfld.ToInstruction(fldRef));
						else
							insts.Add(OpCodes.Ldfld.ToInstruction(fldDef));
						insts.Add(OpCodes.Ldloc_0.ToInstruction());
						if (fldRef != null)
							insts.Add(OpCodes.Ldfld.ToInstruction(fldRef));
						else
							insts.Add(OpCodes.Ldfld.ToInstruction(fldDef));
						insts.Add(OpCodes.Bne_Un.ToInstruction(labelRetFalse));
					}
					else
					{
						if (fldDef.FieldType.IsValueType ||
							fldDef.FieldType.ElementType == ElementType.Var)
						{
							insts.Add(OpCodes.Ldarg_0.ToInstruction());
							if (fldRef != null)
								insts.Add(OpCodes.Ldflda.ToInstruction(fldRef));
							else
								insts.Add(OpCodes.Ldflda.ToInstruction(fldDef));
							insts.Add(OpCodes.Ldloc_0.ToInstruction());
							if (fldRef != null)
								insts.Add(OpCodes.Ldfld.ToInstruction(fldRef));
							else
								insts.Add(OpCodes.Ldfld.ToInstruction(fldDef));

							var fldTyRef = fldDef.FieldType.ToTypeDefOrRef();
							insts.Add(OpCodes.Box.ToInstruction(fldTyRef));

							insts.Add(OpCodes.Constrained.ToInstruction(fldTyRef));
							insts.Add(OpCodes.Callvirt.ToInstruction(metEquals));
							insts.Add(OpCodes.Brfalse.ToInstruction(labelRetFalse));
						}
						else
						{
							Func<Instruction> genLdfld;
							if (fldRef != null)
								genLdfld = () => OpCodes.Ldfld.ToInstruction(fldRef);
							else
								genLdfld = () => OpCodes.Ldfld.ToInstruction(fldDef);

							var labelCheckRhs = OpCodes.Nop.ToInstruction();
							var labelPassed = OpCodes.Nop.ToInstruction();

							insts.Add(OpCodes.Ldarg_0.ToInstruction());
							insts.Add(genLdfld());
							insts.Add(OpCodes.Brfalse.ToInstruction(labelCheckRhs));

							insts.Add(OpCodes.Ldarg_0.ToInstruction());
							insts.Add(genLdfld());

							insts.Add(OpCodes.Ldloc_0.ToInstruction());
							insts.Add(genLdfld());

							insts.Add(OpCodes.Callvirt.ToInstruction(metEquals));
							insts.Add(OpCodes.Brfalse.ToInstruction(labelRetFalse));
							insts.Add(OpCodes.Br.ToInstruction(labelPassed));

							insts.Add(labelCheckRhs);
							insts.Add(OpCodes.Ldloc_0.ToInstruction());
							insts.Add(genLdfld());
							insts.Add(OpCodes.Brtrue.ToInstruction(labelRetFalse));

							insts.Add(labelPassed);
						}
					}
				}

				insts.Add(OpCodes.Ldc_I4_1.ToInstruction());
				insts.Add(OpCodes.Ret.ToInstruction());
			}

			insts.Add(labelRetFalse);
			insts.Add(OpCodes.Ldc_I4_0.ToInstruction());
			insts.Add(OpCodes.Ret.ToInstruction());

			insts.UpdateInstructionOffsets();
		}

		private void TryAddVariance(TypeX tyX)
		{
			if (!tyX.Def.HasGenericParameters)
				return;

			if (tyX.Variances != null)
				return;

			if (!tyX.HasGenArgs)
				return;

			if (VarianceMap.TryGetValue(tyX.Def, out var vgroup))
			{
				if (vgroup != null)
				{
					tyX.Variances = vgroup.VarianceTypes;
					if (vgroup.TypeGroup.Add(tyX))
						vgroup.IsProcessed = false;
				}
			}
			else
			{
				var vaList = new List<VarianceType>();
				bool hasVariance = false;
				foreach (var arg in tyX.Def.GenericParameters)
				{
					if (arg.IsCovariant)
					{
						hasVariance = true;
						vaList.Add(VarianceType.Covariant);
					}
					else if (arg.IsContravariant)
					{
						hasVariance = true;
						vaList.Add(VarianceType.Contravariant);
					}
					else
					{
						Debug.Assert(arg.IsNonVariant);
						vaList.Add(VarianceType.NonVariant);
					}
				}

				if (hasVariance)
				{
					vgroup = new VarianceGroup();
					vgroup.VarianceTypes = vaList;
					vgroup.TypeGroup.Add(tyX);
					tyX.Variances = vaList;
					VarianceMap.Add(tyX.Def, vgroup);
				}
				else
					VarianceMap.Add(tyX.Def, null);
			}
		}

		private void ResolveVariances()
		{
			for (; ; )
			{
				bool isLoop = false;

				var vgList = VarianceMap.Values.ToList();
				foreach (var vgroup in vgList)
				{
					if (vgroup == null || vgroup.IsProcessed)
						continue;
					vgroup.IsProcessed = true;
					isLoop = true;

					if (vgroup.TypeGroup.Count == 1)
						continue;

					var vlist = vgroup.TypeGroup.ToList();
					int len = vlist.Count;
					Debug.Assert(len > 1);

					for (int i = 0; i < len - 1; ++i)
					{
						for (int j = i + 1; j < len; ++j)
						{
							if (!TryLinkVariance(vlist[i], vlist[j]))
								TryLinkVariance(vlist[j], vlist[i]);
						}
					}
				}

				if (!isLoop)
					break;
			}
		}

		private bool TryLinkVariance(TypeX baseTyX, TypeX derivedTyX)
		{
			Debug.Assert(baseTyX.Variances == derivedTyX.Variances);
			Debug.Assert(baseTyX.GenArgs.Count == derivedTyX.GenArgs.Count);

			if (baseTyX.IsDerivedType(derivedTyX))
				return true;

			int len = baseTyX.Variances.Count;
			for (int i = 0; i < len; ++i)
			{
				VarianceType vaType = baseTyX.Variances[i];
				TypeSig baseSig = baseTyX.GenArgs[i];
				TypeSig derivedSig = derivedTyX.GenArgs[i];

				if (vaType == VarianceType.NonVariant)
				{
					// 无协逆变的参数类型不一致, 表示两个类型无关联性
					if (!TypeEqualityComparer.Instance.Equals(baseSig, derivedSig))
						return true;
				}
				else if (vaType == VarianceType.Covariant)
				{
					if (!IsDerivedType(baseSig, derivedSig))
						return false;
				}
				else if (vaType == VarianceType.Contravariant)
				{
					if (!IsDerivedType(derivedSig, baseSig))
						return false;
				}
			}

			derivedTyX.AddVarianceBaseType(baseTyX);

			return true;
		}

		public bool IsDerivedType(TypeSig baseSig, TypeSig derivedSig)
		{
			// 类型相同则允许转换
			if (TypeEqualityComparer.Instance.Equals(baseSig, derivedSig))
				return true;

			var baseElemType = baseSig.ElementType;
			var derivedElemType = derivedSig.ElementType;

			// 子类是 object, 不存在转换
			if (derivedElemType == ElementType.Object)
				return false;

			// 基类是 object
			if (baseElemType == ElementType.Object)
			{
				// 子类非值类型即可转换
				if (!derivedSig.IsValueType)
					return true;
				return false;
			}

			// 基类是值类型, 不存在转换
			if (baseSig.IsValueType)
				return false;

			// 指针类型, 不存在转换
			if (baseSig.IsPointer || derivedSig.IsPointer)
				return false;

			// IList<T> 和 T[] 的赋值
			if (derivedElemType == ElementType.SZArray &&
				baseElemType == ElementType.GenericInst)
			{
				GenericInstSig genInst = (GenericInstSig)baseSig;
				string fullName = genInst.GenericType.FullName;
				if (fullName == "System.Collections.Generic.IList`1" ||
					fullName == "System.Collections.Generic.ICollection`1")
				{
					Debug.Assert(genInst.GenericArguments.Count == 1);
					return IsDerivedType(genInst.GenericArguments[0], derivedSig.Next);
				}
			}

			// 数组类型
			if (baseElemType == ElementType.SZArray || baseElemType == ElementType.Array ||
				derivedElemType == ElementType.SZArray || derivedElemType == ElementType.Array)
			{
				if (baseElemType != derivedElemType)
					return false;

				if (baseElemType == ElementType.Array)
				{
					if (((ArraySig)baseSig).Rank != ((ArraySig)derivedSig).Rank)
						return false;
				}

				return IsDerivedType(baseSig.Next, derivedSig.Next);
			}

			// 解析并判断其他类型
			var baseTyX = ResolveTypeDefOrRef(baseSig.ToTypeDefOrRef(), null);
			var derivedTyX = ResolveTypeDefOrRef(derivedSig.ToTypeDefOrRef(), null);

			return baseTyX.IsDerivedType(derivedTyX);
		}

		private TypeX AddType(TypeX tyX)
		{
			Debug.Assert(tyX != null);

			// 尝试添加到类型映射
			string nameKey = tyX.GetNameKey();
			if (TypeMap.TryGetValue(nameKey, out var otyX))
				return otyX;

			TypeMap.Add(nameKey, tyX);

			string rawNameKey = tyX.GetRawNameKey();
			if (rawNameKey != null)
				RawTypeMap.Add(rawNameKey, tyX);

			// 展开类型
			ExpandType(tyX);

			return tyX;
		}

		// 解析类型定义或引用
		public TypeX ResolveTypeDefOrRef(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			TypeX tyX = ResolveTypeDefOrRefImpl(tyDefRef, replacer);
			return AddType(tyX);
		}

		// 解析类型签名
		public TypeX ResolveTypeSig(TypeSig tySig, IGenericReplacer replacer)
		{
			TypeX tyX = ResolveTypeSigImpl(tySig, replacer);
			return AddType(tyX);
		}

		// 解析类型引用
		private TypeX ResolveTypeDefOrRefImpl(ITypeDefOrRef tyDefRef, IGenericReplacer replacer)
		{
			switch (tyDefRef)
			{
				case TypeDef tyDef:
					return new TypeX(tyDef);

				case TypeRef tyRef:
					{
						TypeDef tyDef = tyRef.Resolve();
						if (tyDef != null)
							return new TypeX(tyDef);

						throw new NotSupportedException(tyRef.ToString());
					}

				case TypeSpec tySpec:
					return ResolveTypeSigImpl(tySpec.TypeSig, replacer);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		// 解析类型签名
		private TypeX ResolveTypeSigImpl(TypeSig tySig, IGenericReplacer replacer)
		{
			switch (tySig)
			{
				case TypeDefOrRefSig tyDefRefSig:
					return ResolveTypeDefOrRefImpl(tyDefRefSig.TypeDefOrRef, null);

				case GenericInstSig genInstSig:
					{
						TypeX genType = ResolveTypeDefOrRefImpl(genInstSig.GenericType.TypeDefOrRef, null);
						genType.GenArgs = Helper.ReplaceGenericSigList(genInstSig.GenericArguments, replacer);
						return genType;
					}

				case SZArraySig szArySig:
					return ResolveSZArrayType(szArySig, replacer);

				case ArraySig arySig:
					return ResolveArrayType(arySig, replacer);

				case GenericVar genVar:
					return ResolveTypeSigImpl(Helper.ReplaceGenericSig(genVar, replacer), null);

				case GenericMVar genMVar:
					return ResolveTypeSigImpl(Helper.ReplaceGenericSig(genMVar, replacer), null);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public MethodTable ResolveMethodTable(ITypeDefOrRef tyDefRef)
		{
			if (tyDefRef is TypeSpec tySpec)
				return ResolveMethodTableSpec(tySpec.TypeSig);
			else
				return ResolveMethodTableDefRef(tyDefRef);
		}

		public MethodTable ResolveMethodTableDefRef(ITypeDefOrRef tyDefRef)
		{
			MethodTable mtable = null;
			if (tyDefRef is TypeDef tyDef)
			{
				mtable = new MethodTable(this, tyDef);
			}
			else if (tyDefRef is TypeRef tyRef)
			{
				TypeDef def = tyRef.Resolve();
				if (def != null)
					mtable = new MethodTable(this, def);
				else
					throw new NotSupportedException();
			}
			else
				throw new ArgumentOutOfRangeException();

			string nameKey = mtable.GetNameKey();
			if (MethodTableMap.TryGetValue(nameKey, out var omtable))
				return omtable;
			MethodTableMap.Add(nameKey, mtable);

			mtable.ResolveTable();

			return mtable;
		}

		public MethodTable ResolveMethodTableSpec(TypeSig tySig)
		{
			if (tySig is GenericInstSig genInstSig)
			{
				return ResolveMethodTableSpec(genInstSig.GenericType.TypeDefOrRef, genInstSig.GenericArguments);
			}
			else if (tySig is TypeDefOrRefSig tyDefRefSig)
			{
				return ResolveMethodTableDefRef(tyDefRefSig.TypeDefOrRef);
			}
			else
				throw new ArgumentOutOfRangeException();
		}

		public MethodTable ResolveMethodTableSpec(ITypeDefOrRef tyDefRef, IList<TypeSig> genArgs)
		{
			MethodTable baseTable = ResolveMethodTableDefRef(tyDefRef);

			string nameKey = baseTable.GetExpandedNameKey(genArgs);
			if (MethodTableMap.TryGetValue(nameKey, out var omtable))
				return omtable;

			MethodTable mtable = baseTable.ExpandTable(genArgs);
			string expNameKey = mtable.GetNameKey();
			Debug.Assert(expNameKey == nameKey);
			MethodTableMap.Add(expNameKey, mtable);

			return mtable;
		}

		private MethodX MakeMethodX(TypeX declType, MethodDef metDef, IList<TypeSig> genArgs)
		{
			MethodX metX = new MethodX(declType, metDef);
			if (genArgs.IsCollectionValid())
				metX.GenArgs = new List<TypeSig>(genArgs);
			return AddMethod(metX);
		}

		private void ResolveAllFields(TypeX tyX, bool isRecursive = false)
		{
			if (tyX.IsAllFieldsResolved)
				return;
			tyX.IsAllFieldsResolved = true;

			foreach (FieldDef fldDef in tyX.Def.Fields)
			{
				if (fldDef.IsStatic)
					continue;

				FieldX fldX = new FieldX(tyX, fldDef);
				AddField(fldX);

				// 递归解析值类型字段
				if (isRecursive &&
					fldX.FieldType.IsValueType)
				{
					TypeX fldTyX = ResolveTypeSig(fldX.FieldType, null);
					ResolveAllFields(fldTyX, true);
				}
			}
		}

		private void ResolveStringType()
		{
			// 解析 System.String 类型
			if (!IsStringTypeResolved)
			{
				IsStringTypeResolved = true;
				if (!TypeMap.ContainsKey("String"))
				{
					TypeX strTyX = ResolveTypeDefOrRef(CorLibTypes.String.ToTypeDefOrRef(), null);
					strTyX.IsInstantiated = true;
				}
			}
		}

		private void ResolveBoxedType(TypeX valueTyX)
		{
			if (!valueTyX.IsValueType)
				return;
			if (valueTyX.BoxedType != null)
				return;
			valueTyX.IsInstantiated = true;

			TypeDef boxedTyDef = GetBoxedTypeDef();
			TypeX tyX = new TypeX(boxedTyDef);
			tyX.GenArgs = new List<TypeSig>() { valueTyX.GetTypeSig() };
			tyX = AddType(tyX);
			tyX.IsInstantiated = true;
			tyX.IsBoxedType = true;

			FieldX fldX = new FieldX(tyX, boxedTyDef.Fields[0]);
			AddField(fldX);

			valueTyX.BoxedType = tyX;
			tyX.UnBoxedType = valueTyX;
		}

		private TypeDef GetBoxedTypeDef()
		{
			if (BoxedTypePrototype != null)
				return BoxedTypePrototype;

			string typeName = "BoxedType";
			var findedDef = CorLibTypes.GetTypeRef(NsIl2cppRT, typeName).Resolve();
			if (findedDef != null)
			{
				BoxedTypePrototype = findedDef;
				return BoxedTypePrototype;
			}

			TypeDefUser tyDef = new TypeDefUser(
				NsIl2cppRT,
				typeName,
				CorLibTypes.Object.ToTypeDefOrRef());
			tyDef.Layout = TypeAttributes.SequentialLayout;
			tyDef.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			var genArgT = new GenericVar(0, tyDef);
			Context.CorLibModule.Types.Add(tyDef);

			FieldDefUser fldDef = new FieldDefUser(
				"value",
				new FieldSig(genArgT),
				FieldAttributes.Public);
			tyDef.Fields.Add(fldDef);

			BoxedTypePrototype = tyDef;
			return BoxedTypePrototype;
		}

		private TypeX ResolveSZArrayType(SZArraySig szArySig, IGenericReplacer replacer)
		{
			TypeSig elemType = szArySig.Next;

			TypeX tyX = new TypeX(GetSZArrayPrototype());
			tyX.GenArgs = new List<TypeSig>() { Helper.ReplaceGenericSig(elemType, replacer) };
			tyX.ArrayInfo = new ArrayProperty() { IsSZArray = true, Rank = 1, Sizes = null, LowerBounds = null };
			return tyX;
		}

		private TypeX ResolveArrayType(ArraySig arySig, IGenericReplacer replacer)
		{
			TypeSig elemType = arySig.Next;

			TypeX tyX = new TypeX(GetMDArrayPrototype(arySig));
			tyX.GenArgs = new List<TypeSig>() { Helper.ReplaceGenericSig(elemType, replacer) };
			tyX.ArrayInfo = new ArrayProperty() { IsSZArray = false, Rank = arySig.Rank, Sizes = arySig.Sizes, LowerBounds = arySig.LowerBounds };
			return tyX;
		}

		private TypeDef GetSZArrayPrototype()
		{
			if (SZArrayPrototype != null)
				return SZArrayPrototype;

			SZArrayPrototype = MakeSZArrayDef();
			return SZArrayPrototype;
		}

		private TypeDef GetMDArrayPrototype(ArraySig arySig)
		{
			uint rank = arySig.Rank;

			if (!MDArrayProtoMap.TryGetValue(rank, out TypeDef mdArrayDef))
			{
				mdArrayDef = MakeMDArrayDef(rank);
				MDArrayProtoMap.Add(rank, mdArrayDef);
			}

			return mdArrayDef;
		}

		private TypeDef MakeSZArrayDef()
		{
			string typeName = "SZArray";
			var findedDef = CorLibTypes.GetTypeRef(NsIl2cppRT, typeName).Resolve();
			if (findedDef != null)
				return findedDef;

			var arrayTyRef = CorLibTypes.GetTypeRef("System", "Array");
			TypeDefUser tyDef = new TypeDefUser(
				NsIl2cppRT,
				typeName,
				arrayTyRef);
			tyDef.Layout = TypeAttributes.SequentialLayout;
			tyDef.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.Covariant, "T"));
			var genArgT = new GenericVar(0, tyDef);
			tyDef.Interfaces.Add(MakeInterfaceImpl("System.Collections.Generic", "IList`1", genArgT));
			tyDef.Interfaces.Add(MakeInterfaceImpl("System.Collections.Generic", "ICollection`1", genArgT));
			tyDef.Interfaces.Add(MakeInterfaceImpl("System.Collections.Generic", "IEnumerable`1", genArgT));
			tyDef.Interfaces.Add(MakeInterfaceImpl("System.Collections.Generic", "IReadOnlyList`1", genArgT));
			tyDef.Interfaces.Add(MakeInterfaceImpl("System.Collections.Generic", "IReadOnlyCollection`1", genArgT));
			Context.CorLibModule.Types.Add(tyDef);

			// .ctor(int)
			MethodDefUser metDef = new MethodDefUser(
				".ctor",
				MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// T Get(int)
			metDef = new MethodDefUser(
				"Get",
				MethodSig.CreateInstance(genArgT, CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// void Set(int,T)
			metDef = new MethodDefUser(
				"Set",
				MethodSig.CreateInstance(CorLibTypes.Void, CorLibTypes.Int32, genArgT),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// T& Address(int)
			metDef = new MethodDefUser(
				"Address",
				MethodSig.CreateInstance(new ByRefSig(genArgT), CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			TypeDef hlpClsDef = CorLibTypes.GetTypeRef("System", "SZArrayHelper").Resolve();

			foreach (var hlpMetDef in hlpClsDef.Methods)
			{
				if (hlpMetDef.IsStatic || hlpMetDef.IsConstructor)
					continue;

				MethodDef aryMetDef = CopyMethodFromSZArrayHelper(hlpClsDef, hlpMetDef, genArgT);
				tyDef.Methods.Add(aryMetDef);
			}

			// 补齐 CopyTo(Array,int)
			metDef = new MethodDefUser(
				"CopyTo",
				MethodSig.CreateInstance(CorLibTypes.Void, arrayTyRef.ToTypeSig(), CorLibTypes.Int32),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.ReuseSlot);
			metDef.Body = tyDef.FindMethod("CopyTo").Body;
			tyDef.Methods.Add(metDef);

			return tyDef;
		}

		private TypeDef MakeMDArrayDef(uint rank)
		{
			string typeName = "MDArray" + rank;
			var findedDef = CorLibTypes.GetTypeRef(NsIl2cppRT, typeName).Resolve();
			if (findedDef != null)
				return findedDef;

			TypeDefUser tyDef = new TypeDefUser(
				NsIl2cppRT,
				typeName,
				CorLibTypes.GetTypeRef("System", "Array"));
			tyDef.Layout = TypeAttributes.SequentialLayout;
			tyDef.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.Covariant, "T"));
			var genArgT = new GenericVar(0, tyDef);
			Context.CorLibModule.Types.Add(tyDef);

			// .ctor(int,int)
			TypeSig[] argSigs = new TypeSig[rank];
			SetAllTypeSig(argSigs, CorLibTypes.Int32);

			MethodDefUser metDef = new MethodDefUser(
				".ctor",
				MethodSig.CreateInstance(CorLibTypes.Void, argSigs),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// .ctor(int,int,int,int)
			argSigs = new TypeSig[rank * 2];
			SetAllTypeSig(argSigs, CorLibTypes.Int32);

			metDef = new MethodDefUser(
				".ctor",
				MethodSig.CreateInstance(CorLibTypes.Void, argSigs),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// T Get(int,int)
			argSigs = new TypeSig[rank];
			SetAllTypeSig(argSigs, CorLibTypes.Int32);

			metDef = new MethodDefUser(
				"Get",
				MethodSig.CreateInstance(genArgT, argSigs),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// void Set(int,int,T)
			argSigs = new TypeSig[rank + 1];
			SetAllTypeSig(argSigs, CorLibTypes.Int32);
			argSigs[argSigs.Length - 1] = genArgT;

			metDef = new MethodDefUser(
				"Set",
				MethodSig.CreateInstance(CorLibTypes.Void, argSigs),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			// T& Address(int,int)
			argSigs = new TypeSig[rank];
			SetAllTypeSig(argSigs, CorLibTypes.Int32);

			metDef = new MethodDefUser(
				"Address",
				MethodSig.CreateInstance(new ByRefSig(genArgT), argSigs),
				MethodAttributes.Public | MethodAttributes.HideBySig);
			metDef.ImplAttributes = MethodImplAttributes.InternalCall;
			tyDef.Methods.Add(metDef);

			return tyDef;
		}

		// 复制方法实现到 SZArray
		private MethodDef CopyMethodFromSZArrayHelper(TypeDef hlpClsDef, MethodDef hlpMetDef, GenericVar genArgT)
		{
			MethodX hlpMetX = new MethodX(new TypeX(hlpClsDef), hlpMetDef);
			hlpMetX.GenArgs = new List<TypeSig>() { genArgT };
			IGenericReplacer replacer = new GenericReplacer(null, hlpMetX);

			var hlpSig = hlpMetDef.MethodSig;

			MethodDef aryMetDef = new MethodDefUser(
				hlpMetDef.Name,
				new MethodSig(CallingConvention.HasThis, 0,
					Helper.ReplaceGenericSig(hlpSig.RetType, replacer),
					Helper.ReplaceGenericSigList(hlpSig.Params, replacer)),
				MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot);

			Debug.Assert(hlpMetDef.HasBody);
			var hlpBody = hlpMetDef.Body;
			var body = aryMetDef.Body = new CilBody();

			foreach (var loc in hlpBody.Variables)
			{
				body.Variables.Add(new Local(Helper.ReplaceGenericSig(loc.Type, replacer)));
			}
			Debug.Assert(!hlpBody.HasExceptionHandlers);

			var offsetMap = new Dictionary<uint, int>();

			for (int i = 0; i < hlpBody.Instructions.Count; ++i)
			{
				Instruction inst = hlpBody.Instructions[i];
				offsetMap[inst.Offset] = i;

				var operand = inst.Operand;
				switch (inst.OpCode.OperandType)
				{
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineSwitch:
						if (operand is Instruction targetInst)
						{
							operand = targetInst.Offset;
						}
						else
						{
							Instruction[] targetInsts = operand as Instruction[];
							uint[] targets = new uint[targetInsts.Length];
							for (int j = 0; j < targetInsts.Length; ++j)
								targets[j] = targetInsts[j].Offset;

							operand = targets;
						}
						break;
				}

				body.Instructions.Add(new Instruction(inst.OpCode, operand));
			}

			for (int i = 0; i < body.Instructions.Count; ++i)
			{
				Instruction inst = body.Instructions[i];

				var operand = inst.Operand;
				switch (inst.OpCode.OperandType)
				{
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
					case OperandType.InlineSwitch:
						if (operand is uint target)
						{
							operand = body.Instructions[offsetMap[target]];
						}
						else
						{
							uint[] targets = operand as uint[];
							Instruction[] targetInsts = new Instruction[targets.Length];
							for (int j = 0; j < targets.Length; ++j)
								targetInsts[j] = body.Instructions[offsetMap[targets[j]]];

							operand = targetInsts;
						}
						break;
				}

				if (operand is TypeSpec typeSpec)
				{
					operand = new TypeSpecUser(Helper.ReplaceGenericSig(typeSpec.TypeSig, replacer));
				}
				else if (operand is MemberRef memRef)
				{
					if (memRef.Class is TypeSpec tySpec)
					{
						tySpec = new TypeSpecUser(Helper.ReplaceGenericSig(tySpec.TypeSig, replacer));
						if (memRef.IsMethodRef)
							operand = new MemberRefUser(memRef.Module, memRef.Name, memRef.MethodSig, tySpec);
						else
							operand = new MemberRefUser(memRef.Module, memRef.Name, memRef.FieldSig, tySpec);
					}
				}
				else if (operand is MethodSpec metSpec)
				{
					if (metSpec.Name == "UnsafeCast" ||
						metSpec.Name == "As")
					{
						inst.OpCode = OpCodes.Nop;
						inst.Operand = null;
						continue;
					}
					else
					{
						var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
						Debug.Assert(metGenArgs != null);
						IList<TypeSig> genArgs = Helper.ReplaceGenericSigList(metGenArgs, replacer);

						operand = new MethodSpecUser(metSpec.Method, new GenericInstMethodSig(genArgs));
					}
				}

				inst.Operand = operand;
			}

			body.UpdateInstructionOffsets();

			return aryMetDef;
		}

		private void ResolveExceptionType(string exName)
		{
			if (!ResolvedExceptions.Contains(exName))
			{
				ResolvedExceptions.Add(exName);
				ResolveExceptionTypeImpl(exName);
			}
		}

		private void ResolveExceptionTypeImpl(string exName)
		{
			if (ThrowHelperType == null)
			{
				string typeName = "ThrowHelper";
				var findedDef = CorLibTypes.GetTypeRef(NsIl2cppRT, typeName).Resolve();
				if (findedDef != null)
				{
					ThrowHelperType = findedDef;
				}
				else
				{
					TypeDef tyDef = new TypeDefUser(
						NsIl2cppRT,
						typeName,
						CorLibTypes.Object.TypeRef);
					Context.CorLibModule.Types.Add(tyDef);
					ThrowHelperType = tyDef;
				}
			}

			string metName = "Throw_" + exName;
			MethodDef metDef = ThrowHelperType.FindMethod(metName);
			if (metDef == null)
			{
				TypeDef exDef = CorLibTypes.GetTypeRef("System", exName).Resolve();
				Debug.Assert(
					exDef != null &&
					exDef.BaseType.Name.Contains("Exception"));
				MethodDef exDefCtor = exDef.FindDefaultConstructor();
				Debug.Assert(exDefCtor != null);

				metDef = new MethodDefUser(
				   metName,
				   MethodSig.CreateStatic(CorLibTypes.Void),
				   MethodAttributes.Public | MethodAttributes.Static);
				ThrowHelperType.Methods.Add(metDef);

				var body = metDef.Body = new CilBody();
				var insts = body.Instructions;
				insts.Add(OpCodes.Newobj.ToInstruction(exDefCtor));
				insts.Add(OpCodes.Throw.ToInstruction());
				body.UpdateInstructionOffsets();
			}
			ResolveMethodDef(metDef);
		}

		private void ResolveDelegateType()
		{
			if (DelegateType != null)
				return;

			TypeDef tyDef = CorLibTypes.GetTypeRef("System", "Delegate").Resolve();
			Debug.Assert(tyDef != null);

			// 使用微软的 BCL 布局
			var fldTarget =
				tyDef.Fields.FirstOrDefault(f => f.Name == "_target" && f.FieldType.ElementType == ElementType.Object);
			var fldMetPtr =
				tyDef.Fields.FirstOrDefault(f => f.Name == "_methodPtr" && f.FieldType.ElementType == ElementType.I);

			if (fldTarget == null || fldMetPtr == null)
				throw new TypeLoadException("Mismatch System.Delegate fields");

			var fldXTarget = ResolveFieldDef(fldTarget);
			var fldXMetPtr = ResolveFieldDef(fldMetPtr);

			DelegateType = new DelegateProperty { TargetField = fldXTarget, MethodPtrField = fldXMetPtr };
		}

		private InterfaceImpl MakeInterfaceImpl(string ns, string name, TypeSig genArg)
		{
			return new InterfaceImplUser(
				new TypeSpecUser(
					new GenericInstSig((ClassOrValueTypeSig)CorLibTypes.GetTypeRef(ns, name).ToTypeSig(), genArg)));
		}

		private static void SetAllTypeSig(TypeSig[] sigList, TypeSig sig)
		{
			for (int i = 0; i < sigList.Length; i++)
				sigList[i] = sig;
		}
	}
}
