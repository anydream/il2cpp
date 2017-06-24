using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
{
	public class CodeMarker
	{
		private ModuleDefMD Module_;
		public ModuleDefMD Module => Module_;
		private readonly Queue<MethodX> PendingMets_ = new Queue<MethodX>();
		private readonly Dictionary<MethodX, MethodX> ProcessedMets_ = new Dictionary<MethodX, MethodX>();
		private readonly Dictionary<FieldX, FieldX> ProcessedFlds_ = new Dictionary<FieldX, FieldX>();
		private readonly Dictionary<TypeX, TypeX> ProcessedTypes_ = new Dictionary<TypeX, TypeX>();
		private readonly HashSet<TypeX> NewObjs_ = new HashSet<TypeX>();
		private readonly Dictionary<TypeX, HashSet<MethodX>> VirtCallMap_ = new Dictionary<TypeX, HashSet<MethodX>>();
		public List<TypeX> AllTypes => new List<TypeX>(ProcessedTypes_.Keys);

		public void Reset()
		{
			PendingMets_.Clear();
			ProcessedMets_.Clear();
			ProcessedFlds_.Clear();
			ProcessedTypes_.Clear();
			NewObjs_.Clear();
			VirtCallMap_.Clear();
		}

		public void Load(string path)
		{
			Reset();

			Module_ = ModuleDefMD.Load(path);

			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;

			Module_.Context = modCtx;
			Module_.Context.AssemblyResolver.AddToCache(Module_);
		}

		public void AddEntry(MethodDef metDef, IGenericResolver genResolver = null)
		{
			ResolveMethodDef(metDef, genResolver);
		}

		public void Process()
		{
			for (;;)
			{
				if (PendingMets_.Count == 0)
					break;

				while (PendingMets_.Count > 0)
				{
					MethodX currMetX = PendingMets_.Dequeue();

					if (currMetX.IsSkipped)
						continue;

					if (!currMetX.Def.HasBody)
						continue;

					MethodGenericResolver metGenResolver = new MethodGenericResolver(currMetX);

					CilBody body = currMetX.Def.Body;
					foreach (var inst in body.Instructions)
					{
						if (inst.OpCode.OperandType == OperandType.InlineMethod)
						{
							MethodX resMetX = null;
							switch (inst.Operand)
							{
								case MethodDef metDef:
									resMetX = ResolveMethodDef(metDef, metGenResolver);
									break;

								case MemberRef memRef:
									resMetX = ResolveMethodRef(memRef, metGenResolver);
									break;

								case MethodSpec metSpec:
									resMetX = ResolveMethodSpec(metSpec, metGenResolver);
									break;

								default:
									Debug.Fail("InlineMethod " + inst.Operand.GetType());
									break;
							}

							if (resMetX == null)
								continue;

							// 对于已跳过的方法, 如果当前指令不是虚调用, 则重新处理
							if (resMetX.IsSkipped && inst.OpCode.Code != Code.Callvirt)
							{
								resMetX.IsSkipped = false;
								// 加入处理队列
								PendingMets_.Enqueue(resMetX);
							}

							// 跳过已标记的方法
							if (resMetX.IsMarked)
								continue;

							resMetX.IsMarked = true;

							// 遇到虚调用以及虚方法
							if (inst.OpCode.Code == Code.Callvirt &&
								resMetX.Def.IsVirtual)
							{
								resMetX.IsSkipped = true;
								// 添加到虚调用映射
								if (!VirtCallMap_.TryGetValue(resMetX.DeclType, out var metSet))
								{
									metSet = new HashSet<MethodX>();
									VirtCallMap_.Add(resMetX.DeclType, metSet);
								}
								metSet.Add(resMetX);
							}

							// 遇到静态方法
							if (resMetX.Def.IsStatic)
							{
								// 解析该对象的静态构造方法
								GenStaticCctor(resMetX.DeclType);
							}

							// 遇到对象创建
							if (inst.OpCode.Code == Code.Newobj)
							{
								Debug.Assert(!resMetX.Def.IsStatic);
								Debug.Assert(resMetX.Def.IsConstructor);
								// 解析该对象的静态构造和终结方法
								GenStaticCctor(resMetX.DeclType);
								GenFinalizer(resMetX.DeclType);

								// 记录新建的对象类型
								NewObjs_.Add(resMetX.DeclType);
							}
						}
						else if (inst.OpCode.OperandType == OperandType.InlineField)
						{
							FieldX resFldX = null;
							switch (inst.Operand)
							{
								case FieldDef fldDef:
									resFldX = ResolveFieldDef(fldDef, metGenResolver);
									break;

								case MemberRef memRef:
									resFldX = ResolveFieldRef(memRef, metGenResolver);
									break;

								default:
									Debug.Fail("InlineField " + inst.Operand.GetType());
									break;
							}

							// 遇到静态字段
							if (resFldX.Def.IsStatic)
							{
								// 解析该对象的静态构造方法
								GenStaticCctor(resFldX.DeclType);
							}
						}
						else if (inst.OpCode.OperandType == OperandType.InlineType)
						{
							TypeX resTyX = null;
							switch (inst.Operand)
							{
								case ITypeDefOrRef tyDefRef:
									resTyX = ResolveTypeDefOrRef(tyDefRef, metGenResolver);
									break;

								default:
									Debug.Fail("InlineType " + inst.Operand.GetType());
									break;
							}
						}
					}
				}

				ResolveVTables();
			}
		}

		private void ResolveVTables()
		{
			foreach (var obj in NewObjs_)
			{
				obj.InitVTable();
				foreach (var kv in VirtCallMap_)
				{
					if (!obj.IsDerivedOrEqual(kv.Key))
						continue;

					foreach (var vmet in kv.Value)
					{
						MethodDef metDef = obj.GetImplMethod(vmet.ToString(true), vmet.Def);
						if (metDef == null)
						{
							// 解析失败的情况下, 检查虚方法与对象实例的版本
							if (vmet.Def.Module.RuntimeVersion != obj.Def.Module.RuntimeVersion)
								continue;
							else
								Debug.Fail("GetImplMethod failed. " + obj + ", " + vmet);
						}

						// 解析所属类型
						TypeX declType = obj.GetImplType(metDef.DeclaringType, vmet.DeclType, vmet.Def);
						// 创建方法包装
						MethodX resMetX = new MethodX(metDef, declType);
						resMetX.GenArgs = vmet.GenArgs;
						resMetX = TryAppendMethod(resMetX);

						// 添加覆盖的方法
						vmet.OverrideImpls.Add(resMetX);

						if (resMetX.IsSkipped)
						{
							resMetX.IsSkipped = false;
							// 加入处理队列
							PendingMets_.Enqueue(resMetX);
						}
					}
				}
			}
		}

		private FieldX AddExpandField(FieldX fldX)
		{
			// 过滤
			if (ProcessedFlds_.TryGetValue(fldX, out FieldX ofldX))
				return ofldX;
			ProcessedFlds_.Add(fldX, fldX);

			// 解析字段类型
			fldX.FieldType = ResolveTypeSig(fldX.Def.FieldType, new TypeGenericResolver(fldX.DeclType));

			// 添加到所属类型
			fldX.DeclType.Fields.Add(fldX);

			return fldX;
		}

		private FieldX ResolveFieldDef(FieldDef fldDef, IGenericResolver genResolver)
		{
			// 解析所属类型
			TypeX declType = ResolveTypeDefOrRef(fldDef.DeclaringType, genResolver);
			// 创建字段包装
			FieldX fldX = new FieldX(fldDef, declType);

			return AddExpandField(fldX);
		}

		private FieldX ResolveFieldRef(MemberRef memRef, IGenericResolver genResolver)
		{
			Debug.Assert(memRef.IsFieldRef);

			// 解析所属类型
			TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, genResolver);
			// 创建字段包装
			FieldX fldX = new FieldX(memRef.ResolveField(), declType);

			return AddExpandField(fldX);
		}

		private void ExpandMethod(MethodX metX)
		{
			MethodGenericResolver metGenResolver = new MethodGenericResolver(metX);

			metX.ReturnType = ResolveTypeSig(metX.Def.ReturnType, metGenResolver);

			int i = -1;
			foreach (var p in metX.Def.Parameters)
			{
				++i;
				if (p.IsHiddenThisParameter)
				{
					Debug.Assert(i == 0);
					continue;
				}
				metX.Args.Add(ResolveTypeSig(p.Type, metGenResolver));
			}

			// 添加到所属类型
			metX.DeclType.Methods.Add(metX);
		}

		private MethodX TryAppendMethod(MethodX metX)
		{
			// 过滤
			if (ProcessedMets_.TryGetValue(metX, out MethodX ometX))
				return ometX;
			ProcessedMets_.Add(metX, metX);

			// 解析
			ExpandMethod(metX);

			// 加入处理队列
			PendingMets_.Enqueue(metX);

			return metX;
		}

		private MethodX ResolveMethodDef(MethodDef metDef, IGenericResolver genResolver)
		{
			// 解析所属类型
			TypeX declType = ResolveTypeDefOrRef(metDef.DeclaringType, genResolver);
			// 创建方法包装
			MethodX metX = new MethodX(metDef, declType);

			return TryAppendMethod(metX);
		}

		private MethodX ResolveMethodRef(MemberRef memRef, IGenericResolver genResolver)
		{
			Debug.Assert(memRef.IsMethodRef);

			// 解析所属类型
			TypeX declType = ResolveTypeDefOrRef(memRef.DeclaringType, genResolver);

			// 创建方法包装
			MethodDef metDef = memRef.ResolveMethod();
			if (metDef == null)
				return null;

			MethodX metX = new MethodX(metDef, declType);

			return TryAppendMethod(metX);
		}

		private MethodX ResolveMethodSpec(MethodSpec metSpec, IGenericResolver genResolver)
		{
			// 解析所属类型
			TypeX declType = ResolveTypeDefOrRef(metSpec.DeclaringType, genResolver);
			// 创建方法包装
			MethodX metX = new MethodX(metSpec.ResolveMethodDef(), declType);

			// 解析方法泛型类型列表 Func<...>
			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			if (metGenArgs != null)
			{
				List<TypeX> genArgs = new List<TypeX>();
				foreach (var p in metGenArgs)
					genArgs.Add(ResolveTypeSig(p, genResolver));

				metX.GenArgs = genArgs;
			}

			return TryAppendMethod(metX);
		}

		private void GenStaticCctor(TypeX declType)
		{
			if (declType.IsCctorGenerated)
				return;
			declType.SetCctorGenerated();

			var cctor = declType.Def.Methods.FirstOrDefault(met => met.IsStatic && met.IsConstructor);
			if (cctor != null)
			{
				// 创建方法包装
				MethodX metX = new MethodX(cctor, declType);
				TryAppendMethod(metX);
			}
		}

		private void GenFinalizer(TypeX declType)
		{
			if (declType.IsFinalizerGenerated)
				return;
			declType.SetFinalizerGenerated();

			var finalizer = declType.Def.Methods.FirstOrDefault(met => !met.IsStatic && met.IsFamily && met.Name == "Finalize");
			if (finalizer != null)
			{
				// 创建方法包装
				MethodX metX = new MethodX(finalizer, declType);
				TryAppendMethod(metX);
			}
		}

		private void ExpandType(TypeX tyX)
		{
			// 跳过包含修饰链的类型, 只展开原始类型
			if (tyX.HasModifierList)
				return;

			TypeGenericResolver tyGenResolver = new TypeGenericResolver(tyX);

			// 解析基类
			if (tyX.Def.BaseType != null)
				tyX.BaseType = ResolveTypeDefOrRef(tyX.Def.BaseType, tyGenResolver);
			// 解析接口
			if (tyX.Def.HasInterfaces)
			{
				foreach (var inf in tyX.Def.Interfaces)
					tyX.Interfaces.Add(ResolveTypeDefOrRef(inf.Interface, tyGenResolver));
			}
		}

		private TypeX TypeFilter(TypeX tyX)
		{
			// 过滤
			if (ProcessedTypes_.TryGetValue(tyX, out TypeX otyX))
				return otyX;
			ProcessedTypes_.Add(tyX, tyX);

			// 解析
			ExpandType(tyX);

			return tyX;
		}

		private static List<NonLeafSig> ModifierListReverse(List<NonLeafSig> modList)
		{
			modList?.Reverse();
			return modList;
		}

		private TypeX ResolveTypeSig(TypeSig tySig, IGenericResolver genResolver)
		{
			return TypeFilter(_ResolveTypeSigImpl(tySig, genResolver));
		}

		private TypeX _ResolveTypeSigImpl(TypeSig tySig, IGenericResolver genResolver)
		{
			List<NonLeafSig> modList = null;

			for (;;)
			{
				switch (tySig)
				{
					case TypeDefOrRefSig defRefSig:
						{
							TypeX tyX = _ResolveTypeDefOrRefImpl(defRefSig.TypeDefOrRef, genResolver);
							tyX.ModifierList = ModifierListReverse(modList);
							return tyX;
						}

					case GenericInstSig genInstSig:
						{
							// 解析泛型类型参数 Class<...>
							List<TypeX> genArgs = new List<TypeX>();
							foreach (var p in genInstSig.GenericArguments)
								genArgs.Add(ResolveTypeSig(p, genResolver));

							// 解析泛型类型
							TypeX tyX = _ResolveTypeSigImpl(genInstSig.GenericType, genResolver);
							tyX.ModifierList = ModifierListReverse(modList);
							tyX.GenArgs = genArgs;
							return tyX;
						}

					case GenericSig genSig:
						{
							TypeX tyX = genResolver.ResolveGenericSig(genSig).Clone(ModifierListReverse(modList));
							return tyX;
						}

					case SZArraySig _:
					case PtrSig _:
					case ByRefSig _:
					case ArraySig _:
					case CModOptSig _:
					case CModReqdSig _:
						{
							if (modList == null)
								modList = new List<NonLeafSig>();
							var cloned = (tySig as NonLeafSig).ModifierClone();
							modList.Add(cloned);
							tySig = tySig.Next;
							continue;
						}

					default:
						Debug.Fail("ResolveTypeSig " + tySig.GetType());
						return null;
				}
			}
		}

		private TypeX ResolveTypeDefOrRef(ITypeDefOrRef tyDefRef, IGenericResolver genResolver)
		{
			return TypeFilter(_ResolveTypeDefOrRefImpl(tyDefRef, genResolver));
		}

		private TypeX _ResolveTypeDefOrRefImpl(ITypeDefOrRef tyDefRef, IGenericResolver genResolver)
		{
			switch (tyDefRef)
			{
				case TypeDef tyDef:
					return new TypeX(tyDef);

				case TypeRef tyRef:
					return new TypeX(tyRef.Resolve());

				case TypeSpec tySpec:
					return _ResolveTypeSigImpl(tySpec.TypeSig, genResolver);

				default:
					Debug.Fail("ResolveTypeDefOrRef " + tyDefRef.GetType());
					return null;
			}
		}
	}
}
