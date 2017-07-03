using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp2
{
	// 方法签名
	public class MethodSignature
	{
		public readonly string Name;
		public readonly MethodBaseSig Signature;
		private readonly string SigString;

		public MethodSignature(string name, MethodBaseSig sig)
		{
			Debug.Assert(sig != null);
			Name = name;
			Signature = sig;
			SigString = Name + ": " + Signature.ToString() + "|" + ((int)Signature.CallingConvention).ToString();
		}

		public override int GetHashCode()
		{
			return SigString.GetHashCode();
		}

		public bool Equals(MethodSignature other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return SigString == other.SigString;
		}

		public override bool Equals(object obj)
		{
			return obj is MethodSignature other && Equals(other);
		}

		public override string ToString()
		{
			return SigString;
		}
	}

	// 虚调用入口
	public class VirtualEntry
	{
		// 入口所属的类型
		public readonly string TypeName;
		// 方法签名
		public readonly MethodSignature Signature;

		public VirtualEntry(string typeName, MethodSignature sig)
		{
			Debug.Assert(sig != null);
			TypeName = typeName;
			Signature = sig;
		}

		public override int GetHashCode()
		{
			return TypeName.GetHashCode() ^
				   Signature.GetHashCode();
		}

		public bool Equals(VirtualEntry other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return TypeName == other.TypeName &&
				   Signature.Equals(other.Signature);
		}

		public override bool Equals(object obj)
		{
			return obj is VirtualEntry other && Equals(other);
		}

		public override string ToString()
		{
			return TypeName + "::" + Signature;
		}
	}

	// 方法实现信息
	public class MethodImpl
	{
		// 所属类型
		public readonly TypeX DeclType;
		// 方法定义
		public readonly MethodDef Def;

		public MethodImpl(TypeX declType, MethodDef metDef)
		{
			DeclType = declType;
			Def = metDef;
		}

		public override int GetHashCode()
		{
			return DeclType.GetHashCode() ^
				   Def.Name.GetHashCode();
		}

		public bool Equals(MethodImpl other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return DeclType.Equals(other.DeclType) &&
				   MethodEqualityComparer.DontCompareDeclaringTypes.Equals(Def, other.Def);
		}

		public override bool Equals(object obj)
		{
			return obj is MethodImpl other && Equals(other);
		}

		public override string ToString()
		{
			return Def.FullName + " [" + DeclType + "]";
		}
	}

	// 虚表
	public class VirtualTable
	{
		private class SlotLayer
		{
			public readonly HashSet<string> EntryTypes;
			public MethodImpl ImplMethod;

			public SlotLayer()
			{
				EntryTypes = new HashSet<string>();
			}

			private SlotLayer(HashSet<string> entryTypes, MethodImpl impl)
			{
				EntryTypes = entryTypes;
				ImplMethod = impl;
			}

			public SlotLayer Clone()
			{
				return new SlotLayer(new HashSet<string>(EntryTypes), ImplMethod);
			}
		}

		// 虚表槽层级映射
		private readonly Dictionary<MethodSignature, SlotLayer> VMap =
			new Dictionary<MethodSignature, SlotLayer>();
		// 显式覆盖列表
		private Dictionary<VirtualEntry, MethodImpl> DerivedExplicitMap = new Dictionary<VirtualEntry, MethodImpl>();
		private Dictionary<VirtualEntry, MethodImpl> CurrExplicitMap = new Dictionary<VirtualEntry, MethodImpl>();
		// 展开的虚表
		public Dictionary<VirtualEntry, MethodImpl> Table { get; private set; } =
			new Dictionary<VirtualEntry, MethodImpl>();

		public VirtualTable Clone()
		{
			VirtualTable vtbl = new VirtualTable();

			foreach (var kv in VMap)
				vtbl.VMap.Add(kv.Key, kv.Value.Clone());

			vtbl.DerivedExplicitMap = new Dictionary<VirtualEntry, MethodImpl>(CurrExplicitMap);

			// 克隆展开的虚表
			vtbl.Table = new Dictionary<VirtualEntry, MethodImpl>(Table);

			return vtbl;
		}

		public void NewSlot(MethodSignature sig, MethodImpl impl)
		{
			var layer = new SlotLayer();
			layer.EntryTypes.Add(impl.DeclType.FullName);
			layer.ImplMethod = impl;
			VMap[sig] = layer;
		}

		public void ReuseSlot(MethodSignature sig, MethodImpl impl)
		{
			bool result = VMap.TryGetValue(sig, out var layer);
			Debug.Assert(result);

			layer.EntryTypes.Add(impl.DeclType.FullName);
			layer.ImplMethod = impl;
		}

		public void MergeSlot(TypeX currType, string typeName, MethodSignature sig)
		{
			var entry = new VirtualEntry(typeName, sig);

			// 当前类定义了显式覆盖, 跳过合并
			if (CurrExplicitMap.ContainsKey(entry))
				return;

			bool result = VMap.TryGetValue(sig, out var layer);
			if (!result)
			{
				// 找不到且存在继承的显式覆盖, 跳过
				if (DerivedExplicitMap.ContainsKey(entry))
					return;
				Debug.Fail("MergeSlot " + entry);
			}

			if (layer.ImplMethod.DeclType.Equals(currType))
			{
				// 当前类型存在相同签名的实现, 删除并合并
				DerivedExplicitMap.Remove(entry);
				layer.EntryTypes.Add(typeName);
			}
			else if (!DerivedExplicitMap.ContainsKey(entry))
			{
				// 当前类型不存在实现且不存在显式覆盖, 合并
				layer.EntryTypes.Add(typeName);
			}
		}

		public void ExplicitOverride(string typeName, MethodSignature sig, MethodImpl impl)
		{
			var entry = new VirtualEntry(typeName, sig);
			CurrExplicitMap[entry] = impl;
		}

		public void ExpandTable()
		{
			foreach (var kv in VMap)
			{
				var layer = kv.Value;
				foreach (var typeName in layer.EntryTypes)
				{
					var entry = new VirtualEntry(typeName, kv.Key);
					Table[entry] = layer.ImplMethod;
				}
			}

			// 显式覆盖最后展开
			foreach (var kv in CurrExplicitMap)
			{
				DerivedExplicitMap[kv.Key] = kv.Value;

				// 删除显式覆盖的类型, 防止后续类型覆盖
				if (VMap.TryGetValue(kv.Key.Signature, out var layer))
					layer.EntryTypes.Remove(kv.Key.TypeName);
			}
			CurrExplicitMap = DerivedExplicitMap;

			foreach (var kv in CurrExplicitMap)
			{
				Table[kv.Key] = kv.Value;
			}
		}

		public MethodImpl FindImplementation(VirtualEntry entry)
		{
			if (Table.TryGetValue(entry, out var impl))
				return impl;
			return null;
		}
	}

	// 类型实例的泛型参数
	public class GenericArgs
	{
		private IList<TypeSig> GenArgs_;
		public IList<TypeSig> GenArgs => GenArgs_;
		public bool HasGenArgs => GenArgs_ != null && GenArgs_.Count > 0;

		public void SetGenericArgs(IList<TypeSig> genArgs)
		{
			Debug.Assert(GenArgs_ == null);
			GenArgs_ = genArgs;
		}

		public int GenericHashCode()
		{
			return SigHelper.SigListHashCode(GenArgs_);
		}

		public bool GenericEquals(GenericArgs other)
		{
			return SigHelper.SigListEquals(GenArgs_, other.GenArgs_);
		}

		public string GenericToString()
		{
			if (GenArgs_ == null)
				return "";

			StringBuilder sb = new StringBuilder();

			sb.Append('<');
			bool last = false;
			foreach (var arg in GenArgs_)
			{
				if (last)
					sb.Append(',');
				last = true;
				sb.Append(arg.FullName);
			}
			sb.Append('>');

			return sb.ToString();
		}
	}

	// 展开的类型
	public class TypeX : GenericArgs
	{
		// 类型定义
		public readonly TypeDef Def;

		// 基类
		public TypeX BaseType;
		// 接口列表
		private IList<TypeX> Interfaces_;
		public IList<TypeX> Interfaces => Interfaces_ ?? (Interfaces_ = new List<TypeX>());
		public bool HasInterfaces => Interfaces_ != null && Interfaces_.Count > 0;
		// 方法映射
		private readonly Dictionary<MethodX, MethodX> MethodMap = new Dictionary<MethodX, MethodX>();
		public IList<MethodX> Methods => new List<MethodX>(MethodMap.Values);
		public bool HasMethods => MethodMap.Count > 0;
		// 字段映射
		private readonly Dictionary<FieldX, FieldX> FieldMap = new Dictionary<FieldX, FieldX>();
		public IList<FieldX> Fields => new List<FieldX>(FieldMap.Values);
		public bool HasFields => FieldMap.Count > 0;
		// 运行时类型
		public string RuntimeVersion => Def.Module.RuntimeVersion;

		// 方法签名映射
		public readonly Dictionary<MethodSignature, MethodDef> MethodSigMap = new Dictionary<MethodSignature, MethodDef>();
		// 方法覆盖类型集合映射
		public readonly Dictionary<MethodSignature, HashSet<TypeX>> OverrideImplTypes =
			new Dictionary<MethodSignature, HashSet<TypeX>>();

		// 虚表
		public VirtualTable VTable;

		// 类型全名
		public string FullName => Def.FullName + GenericToString();

		public bool IsEmptyType => !HasMethods && !HasFields;
		// 是否被实例化过
		public bool IsInstanced;
		// 是否生成过静态构造
		public bool CctorGenerated;
		// 是否生成过终结方法
		public bool FinalizerGenerated;

		public TypeX(TypeDef typeDef)
		{
			Def = typeDef;
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode() ^
				   GenericHashCode() ^
				   RuntimeVersion.GetHashCode();
		}

		public bool Equals(TypeX other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return TypeEqualityComparer.Instance.Equals(Def, other.Def) &&
				   GenericEquals(other) &&
				   RuntimeVersion == other.RuntimeVersion;
		}

		public override bool Equals(object obj)
		{
			return obj is TypeX other && Equals(other);
		}

		public override string ToString()
		{
			return FullName;
		}

		public bool AddMethod(MethodX metX, out MethodX ometX)
		{
			if (!MethodMap.TryGetValue(metX, out ometX))
			{
				MethodMap.Add(metX, metX);
				ometX = metX;
				return true;
			}
			return false;
		}

		public bool AddField(FieldX fldX, out FieldX ofldX)
		{
			if (!FieldMap.TryGetValue(fldX, out ofldX))
			{
				FieldMap.Add(fldX, fldX);
				ofldX = fldX;
				return true;
			}
			return false;
		}

		public void AddMethodSig(MethodSignature sig, MethodDef metDef)
		{
			Debug.Assert(!MethodSigMap.ContainsKey(sig));
			Debug.Assert(metDef.DeclaringType == Def);

			MethodSigMap.Add(sig, metDef);
		}

		public void AddOverrideImplType(MethodSignature sig, TypeX implType)
		{
			if (!OverrideImplTypes.TryGetValue(sig, out var implSet))
			{
				implSet = new HashSet<TypeX>();
				OverrideImplTypes.Add(sig, implSet);
			}
			implSet.Add(implType);
		}

		public void CollectInterfaces(HashSet<TypeX> infs)
		{
			if (Def.IsInterface)
			{
				infs.Add(this);
			}

			if (HasInterfaces)
			{
				foreach (var inf in Interfaces_)
				{
					inf.CollectInterfaces(infs);
				}
			}
		}
	}

	// 展开的方法
	public class MethodX : GenericArgs
	{
		// 方法定义
		public readonly MethodDef Def;

		// 所属类型
		public readonly TypeX DeclType;
		// 返回值
		public TypeSig ReturnType;
		// 参数列表
		public IList<TypeSig> ParamTypes;
		// 方法覆盖集合
		private HashSet<MethodX> OverrideImpls_;
		public HashSet<MethodX> OverrideImpls => OverrideImpls_ ?? (OverrideImpls_ = new HashSet<MethodX>(new MethodRefComparer()));
		public List<MethodX> OverrideImplsList => new List<MethodX>(OverrideImpls_);
		public bool HasOverrideImpls => OverrideImpls_ != null && OverrideImpls_.Count > 0;

		// 是否在处理队列中
		public bool IsQueueing = false;

		// 是否为纯虚方法
		private int VirtOnlyStatus_ = 0;
		public bool IsCallVirtOnly
		{
			get => VirtOnlyStatus_ == 1;
			set
			{
				if (VirtOnlyStatus_ == 0 || VirtOnlyStatus_ == 1)
					VirtOnlyStatus_ = value ? 1 : 2;
			}
		}

		public string Name => BuildName(false);
		public string FullName => BuildName(true);

		public MethodX(MethodDef metDef, TypeX declType, IList<TypeSig> genArgs)
		{
			Def = metDef;
			DeclType = declType;
			SetGenericArgs(genArgs);
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode() ^
				   GenericHashCode();
		}

		public int ObjectHashCode()
		{
			return base.GetHashCode();
		}

		public bool Equals(MethodX other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return MethodEqualityComparer.DontCompareDeclaringTypes.Equals(Def, other.Def) &&
				   GenericEquals(other);
		}

		public override bool Equals(object obj)
		{
			return obj is MethodX other && Equals(other);
		}

		public override string ToString()
		{
			return Name;
		}

		private string BuildName(bool hasDeclType)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0} {1}{2}{3}",
				ReturnType != null ? ReturnType.FullName : "<?>",
				hasDeclType ? DeclType.FullName + "::" : "",
				Def.Name,
				GenericToString());

			sb.Append('(');
			if (ParamTypes == null)
				sb.Append("<?>");
			else
			{
				bool last = false;
				foreach (var arg in ParamTypes)
				{
					if (last)
						sb.Append(',');
					last = true;
					sb.Append(arg.FullName);
				}
			}
			sb.Append(')');

			return sb.ToString();
		}
	}

	// 只比较引用的方法比较器
	class MethodRefComparer : IEqualityComparer<MethodX>
	{
		public int GetHashCode(MethodX obj)
		{
			return obj.ObjectHashCode();
		}

		public bool Equals(MethodX x, MethodX y)
		{
			return ReferenceEquals(x, y);
		}
	}

	// 展开的字段
	public class FieldX
	{
		// 字段定义
		public readonly FieldDef Def;

		// 所属类型
		public readonly TypeX DeclType;
		// 字段类型
		public TypeSig FieldType;

		public FieldX(FieldDef fldDef, TypeX declType)
		{
			Def = fldDef;
			DeclType = declType;
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode();
		}

		public bool Equals(FieldX other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return FieldEqualityComparer.DontCompareDeclaringTypes.Equals(Def, other.Def);
		}

		public override bool Equals(object obj)
		{
			return obj is FieldX other && Equals(other);
		}

		public override string ToString()
		{
			return string.Format("{0} {1}",
				FieldType != null ? FieldType.FullName : "<?>",
				Def.Name);
		}
	}

	class VCallInfo : VirtualEntry
	{
		public readonly IList<TypeSig> GenArgs;

		public VCallInfo(string typeName, MethodSignature sig, IList<TypeSig> genArgs)
			: base(typeName, sig)
		{
			GenArgs = genArgs;
		}
	}

	// 类型解析管理器
	public class TypeManager
	{
		// 主模块
		public ModuleDefMD Module { get; private set; }
		// 类型映射
		private readonly Dictionary<TypeX, TypeX> TypeMap = new Dictionary<TypeX, TypeX>();
		private readonly Dictionary<string, List<TypeX>> NameTypeMap = new Dictionary<string, List<TypeX>>();
		public IList<TypeX> Types => new List<TypeX>(TypeMap.Values);
		// 待处理方法队列
		private readonly Queue<MethodX> PendingMets = new Queue<MethodX>();
		// 虚调用
		private readonly Dictionary<string, VCallInfo> VCalls =
			new Dictionary<string, VCallInfo>();

		// 复位
		public void Reset()
		{
			TypeMap.Clear();
			NameTypeMap.Clear();
			PendingMets.Clear();
			VCalls.Clear();
		}

		// 加载模块
		public void Load(string path)
		{
			Reset();

			Module = ModuleDefMD.Load(path);

			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;

			Module.Context = modCtx;
			Module.Context.AssemblyResolver.AddToCache(Module);
		}

		public void AddEntry(MethodDef metDef)
		{
			ResolveMethod(metDef);
		}

		// 处理循环
		public void Process()
		{
			// 添加虚调用 Object.Finalize
			VCalls.Add(
				"System.Void System.Object::Finalize()",
				new VCallInfo("System.Object", new MethodSignature("Finalize",
						new MethodSig(CallingConvention.HasThis, 0, Module.CorLibTypes.Void)),
					null));

			while (PendingMets.Count > 0)
			{
				do
				{
					// 取出一个待处理方法
					MethodX currMetX = PendingMets.Dequeue();
					currMetX.IsQueueing = false;

					// 跳过纯虚方法
					if (currMetX.IsCallVirtOnly)
						continue;

					// 跳过无方法体的方法
					if (!currMetX.Def.HasBody)
						continue;

					// 构建方法内的泛型展开器
					GenericReplacer replacer = new GenericReplacer();
					replacer.SetType(currMetX.DeclType);
					replacer.SetMethod(currMetX);

					// 遍历并解析指令
					foreach (var inst in currMetX.Def.Body.Instructions)
					{
						ResolveInstruction(inst, replacer);
					}
				} while (PendingMets.Count > 0);

				ResolveVCalls();
			}
		}

		// 解析指令
		private void ResolveInstruction(Instruction inst, GenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineMethod:
					{
						MethodX resMetX = null;
						switch (inst.Operand)
						{
							case MethodDef metDef:
								resMetX = ResolveMethod(metDef);
								break;

							case MemberRef memRef:
								resMetX = ResolveMethod(memRef, replacer);
								break;

							case MethodSpec metSpec:
								resMetX = ResolveMethod(metSpec, replacer);
								break;

							default:
								Debug.Fail("InlineMethod " + inst.Operand.GetType().Name);
								break;
						}

						if (resMetX == null)
							break;

						// 添加虚方法入口
						if (resMetX.Def.IsVirtual &&
							(inst.OpCode.Code == Code.Callvirt ||
							 inst.OpCode.Code == Code.Ldvirtftn))
						{
							var metName = resMetX.FullName;
							if (!VCalls.ContainsKey(metName))
							{
								var typeReplacer = new GenericReplacer();
								typeReplacer.SetType(resMetX.DeclType);
								var typeDuplicator = MakeMethodDuplicator(typeReplacer);
								var vSig = MakeMethodSignature(
									resMetX.Def.Name,
									(MethodBaseSig)resMetX.Def.Signature,
									typeDuplicator);

								VCallInfo vInfo = new VCallInfo(resMetX.DeclType.FullName, vSig, resMetX.GenArgs);

								VCalls.Add(metName, vInfo);
							}
							resMetX.IsCallVirtOnly = true;
						}
						else
						{
							// 之前为纯虚方法且未处理过, 需要重新处理
							if (resMetX.IsCallVirtOnly)
							{
								resMetX.IsCallVirtOnly = false;
								AddPendingMethod(resMetX);
							}
						}

						// 遇到静态方法
						if (resMetX.Def.IsStatic)
						{
							// 生成静态构造方法
							GenStaticCctor(resMetX.DeclType);
						}

						// 遇到对象创建
						if (inst.OpCode.Code == Code.Newobj)
						{
							Debug.Assert(!resMetX.Def.IsStatic);
							Debug.Assert(resMetX.Def.IsConstructor);
							resMetX.DeclType.IsInstanced = true;
							// 生成静态构造和终结方法
							GenStaticCctor(resMetX.DeclType);
							GenFinalizer(resMetX.DeclType);
						}

						break;
					}

				case OperandType.InlineField:
					{
						FieldX resFldX = null;
						switch (inst.Operand)
						{
							case FieldDef fldDef:
								resFldX = ResolveField(fldDef);
								break;

							case MemberRef memRef:
								resFldX = ResolveField(memRef, replacer);
								break;

							default:
								Debug.Fail("InlineField " + inst.Operand.GetType().Name);
								break;
						}

						// 遇到静态字段
						if (resFldX.Def.IsStatic)
						{
							// 生成静态构造方法
							GenStaticCctor(resFldX.DeclType);
						}

						break;
					}
			}
		}

		private void GenStaticCctor(TypeX tyX)
		{
			if (tyX.CctorGenerated)
				return;
			tyX.CctorGenerated = true;

			var cctor = tyX.Def.Methods.FirstOrDefault(met => met.IsStatic && met.IsConstructor);
			if (cctor != null)
			{
				// 创建方法包装
				MethodX metX = new MethodX(cctor, tyX, null);
				AddMethod(metX);
			}
		}

		private void GenFinalizer(TypeX tyX)
		{
			if (tyX.FinalizerGenerated)
				return;
			tyX.FinalizerGenerated = true;

			var finalizer = tyX.Def.Methods.FirstOrDefault(met => !met.IsStatic && met.IsFamily && met.Name == "Finalize");
			if (finalizer != null)
			{
				// 创建方法包装
				MethodX metX = new MethodX(finalizer, tyX, null);
				AddMethod(metX);
			}

			//!GenObjectFinalizer();
		}

		private void ResolveVCalls()
		{
			foreach (var vInfo in VCalls.Values)
			{
				VirtualEntry entry = vInfo;
				var typeList = GetTypeList(vInfo.TypeName);
				foreach (var declType in typeList)
				{
					bool result = declType.OverrideImplTypes.TryGetValue(vInfo.Signature, out var implSet);
					// 跳过无实例化类型的虚调用
					if (!result)
						continue;

					foreach (var implType in implSet)
					{
						// 过滤已实例化的类型
						if (!implType.IsInstanced)
							continue;

						// 在类型虚表中查找实现
						MethodImpl impl = implType.VTable.FindImplementation(entry);
						Debug.Assert(impl != null);

						// 构造实现方法
						MethodX metX = new MethodX(impl.Def, impl.DeclType, vInfo.GenArgs);
						metX = AddMethod(metX);

						// 之前为纯虚方法且未处理过, 需要重新处理
						if (metX.IsCallVirtOnly)
						{
							metX.IsCallVirtOnly = false;
							AddPendingMethod(metX);
						}

						// 添加到接口方法的实现集合
						result = declType.MethodSigMap.TryGetValue(vInfo.Signature, out var vmetDef);
						// 不同版本的类型可能方法也不同
						if (!result)
							continue;

						// 添加到接口方法的实现列表
						MethodX vmetX = new MethodX(vmetDef, declType, vInfo.GenArgs);
						vmetX = AddMethod(vmetX);
						vmetX.IsCallVirtOnly = true;
						vmetX.OverrideImpls.Add(metX);
					}
				}
			}
		}

		// 添加类型
		private TypeX AddType(TypeX tyX)
		{
			if (TypeMap.TryGetValue(tyX, out var otyX))
				return otyX;

			TypeMap.Add(tyX, tyX);

			// 添加类型到名称映射
			string typeName = tyX.FullName;
			if (!NameTypeMap.TryGetValue(typeName, out var typeList))
			{
				typeList = new List<TypeX>();
				NameTypeMap.Add(typeName, typeList);
			}
			typeList.Add(tyX);

			ExpandType(tyX);

			return tyX;
		}

		private List<TypeX> GetTypeList(string typeName)
		{
			bool result = NameTypeMap.TryGetValue(typeName, out var typeList);
			Debug.Assert(result);
			return typeList;
		}

		// 展开类型
		private void ExpandType(TypeX tyX)
		{
			// 构建类型内的泛型展开器
			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(tyX);

			// 展开类型内的泛型类型
			if (tyX.Def.BaseType != null)
				tyX.BaseType = ResolveInstanceType(tyX.Def.BaseType, replacer);
			if (tyX.Def.HasInterfaces)
			{
				foreach (var inf in tyX.Def.Interfaces)
					tyX.Interfaces.Add(ResolveInstanceType(inf.Interface, replacer));
			}

			ResolveVTable(tyX, replacer);
		}

		private void ResolveVTable(TypeX currType, GenericReplacer replacer)
		{
			Debug.Assert(currType.VTable == null);

			MethodSigDuplicator duplicator = MakeMethodDuplicator(replacer);

			// 如果当前类型为接口则解析接口方法
			if (currType.Def.IsInterface)
			{
				foreach (var metDef in currType.Def.Methods)
				{
					Debug.Assert(metDef.IsAbstract);

					MethodSignature sig = MakeMethodSignature(
						metDef.Name,
						(MethodBaseSig)metDef.Signature,
						duplicator);

					currType.AddMethodSig(sig, metDef);
				}
				return;
			}

			// 继承虚表
			if (currType.BaseType != null)
				currType.VTable = currType.BaseType.VTable.Clone();
			else
				currType.VTable = new VirtualTable();

			// 遍历方法
			foreach (var metDef in currType.Def.Methods)
			{
				// 跳过不产生虚表的静态和特殊方法
				if (metDef.IsStatic)
					continue;
				if (metDef.IsRuntimeSpecialName)
				{
					Debug.Assert(metDef.Name == ".ctor" || metDef.Name == ".cctor");
					continue;
				}

				if (metDef.HasOverrides)
				{
					// 显式覆盖的方法
					foreach (var overMet in metDef.Overrides)
					{
						var overMetDecl = overMet.MethodDeclaration;
						if (overMetDecl is MethodDef ometDef)
						{
							TypeX oDeclType = ResolveInstanceType(ometDef.DeclaringType);

							MethodSignature oSig = MakeMethodSignature(
								ometDef.Name,
								(MethodBaseSig)ometDef.Signature,
								null);

							currType.VTable.ExplicitOverride(oDeclType.FullName, oSig, new MethodImpl(currType, metDef));
						}
						else if (overMetDecl is MemberRef omemRef)
						{
							Debug.Assert(omemRef.IsMethodRef);

							// 展开目标方法所属的类型
							TypeX oDeclType = null;
							if (omemRef.Class is TypeSpec omemClsSpec)
								oDeclType = ResolveInstanceType(omemClsSpec, replacer);
							else if (omemRef.Class is TypeRef omemClsRef)
								oDeclType = ResolveInstanceType(omemClsRef);
							else
								Debug.Fail("Override MemberRef " + overMetDecl);

							GenericReplacer oReplacer = new GenericReplacer();
							oReplacer.SetType(oDeclType);
							MethodSigDuplicator oDuplicator = MakeMethodDuplicator(oReplacer);

							MethodSignature oSig = MakeMethodSignature(
								omemRef.Name,
								(MethodBaseSig)omemRef.Signature,
								oDuplicator);

							currType.VTable.ExplicitOverride(oDeclType.FullName, oSig, new MethodImpl(currType, metDef));
						}
						else
							Debug.Fail("Override " + overMetDecl.GetType().Name);
					}
				}
				else
				{
					// 展开方法上属于类型的泛型
					MethodSignature sig = MakeMethodSignature(
						metDef.Name,
						(MethodBaseSig)metDef.Signature,
						duplicator);

					currType.AddMethodSig(sig, metDef);

					if (metDef.IsNewSlot ||
						!metDef.IsVirtual ||
						currType.Def.FullName == "System.Object")
					{
						// 新建虚表槽的方法
						currType.VTable.NewSlot(sig, new MethodImpl(currType, metDef));
					}
					else
					{
						// 复用虚表槽的方法
						Debug.Assert(metDef.IsReuseSlot);
						currType.VTable.ReuseSlot(sig, new MethodImpl(currType, metDef));
					}
				}
			}

			// 关联接口方法
			if (currType.HasInterfaces)
			{
				HashSet<TypeX> infs = new HashSet<TypeX>();
				currType.CollectInterfaces(infs);

				foreach (TypeX infType in infs)
				{
					foreach (var kv in infType.MethodSigMap)
					{
						currType.VTable.MergeSlot(currType, infType.FullName, kv.Key);
					}
				}
			}

			// 展开虚表
			currType.VTable.ExpandTable();

			// 追加当前类型到所有虚入口类型
			foreach (var kv in currType.VTable.Table)
			{
				var sig = kv.Key.Signature;
				var typeList = GetTypeList(kv.Key.TypeName);
				foreach (var type in typeList)
					type.AddOverrideImplType(sig, currType);
			}
		}

		private static MethodSigDuplicator MakeMethodDuplicator(GenericReplacer replacer)
		{
			if (replacer.IsValid)
			{
				MethodSigDuplicator duplicator = new MethodSigDuplicator();
				duplicator.GenReplacer = replacer;
				return duplicator;
			}
			return null;
		}

		private static MethodSignature MakeMethodSignature(string name, MethodBaseSig metSig, MethodSigDuplicator duplicator)
		{
			if (duplicator != null)
				metSig = duplicator.Duplicate(metSig);
			return new MethodSignature(name, metSig);
		}

		// 解析实例类型
		private TypeX ResolveInstanceType(ITypeDefOrRef typeDefRef, GenericReplacer replacer = null)
		{
			return AddType(ResolveInstanceTypeImpl(typeDefRef, replacer));
		}

		// 解析实例类型的定义或引用
		private static TypeX ResolveTypeDefOrRefImpl(ITypeDefOrRef typeDefRef)
		{
			switch (typeDefRef)
			{
				case TypeDef typeDef:
					return new TypeX(typeDef);

				case TypeRef typeRef:
					return new TypeX(typeRef.ResolveTypeDef());

				default:
					Debug.Fail("ResolveTypeDefOrRefImpl " + typeDefRef.GetType().Name);
					return null;
			}
		}

		// 解析实例类型的定义引用或高阶类型
		private static TypeX ResolveInstanceTypeImpl(ITypeDefOrRef typeDefRef, GenericReplacer replacer)
		{
			switch (typeDefRef)
			{
				case TypeDef typeDef:
					return new TypeX(typeDef);

				case TypeRef typeRef:
					return new TypeX(typeRef.ResolveTypeDef());

				case TypeSpec typeSpec:
					return ResolveInstanceTypeImpl(typeSpec.TypeSig, replacer);

				default:
					Debug.Fail("ResolveInstanceTypeImpl ITypeDefOrRef " + typeDefRef.GetType().Name);
					return null;
			}
		}

		// 解析实例类型签名
		private static TypeX ResolveInstanceTypeImpl(TypeSig typeSig, GenericReplacer replacer)
		{
			switch (typeSig)
			{
				case TypeDefOrRefSig typeDefRefSig:
					return ResolveTypeDefOrRefImpl(typeDefRefSig.TypeDefOrRef);

				case GenericInstSig genInstSig:
					{
						// 泛型实例类型
						TypeX genType = ResolveTypeDefOrRefImpl(genInstSig.GenericType.TypeDefOrRef);
						genType.SetGenericArgs(ResolveTypeSigList(genInstSig.GenericArguments, replacer));
						return genType;
					}

				default:
					Debug.Fail("ResolveInstanceTypeImpl TypeSig " + typeSig.GetType().Name);
					return null;
			}
		}

		// 展开类型签名
		private static TypeSig ResolveTypeSig(TypeSig typeSig, GenericReplacer replacer)
		{
			if (replacer == null || !replacer.IsValid)
				return typeSig;

			var duplicator = new TypeSigDuplicator();
			duplicator.GenReplacer = replacer;

			return duplicator.Duplicate(typeSig);
		}

		// 展开类型签名列表
		private static IList<TypeSig> ResolveTypeSigList(IList<TypeSig> sigList, GenericReplacer replacer)
		{
			if (replacer == null || !replacer.IsValid)
				return new List<TypeSig>(sigList);

			var duplicator = new TypeSigDuplicator();
			duplicator.GenReplacer = replacer;

			var result = new List<TypeSig>();
			foreach (var typeSig in sigList)
				result.Add(duplicator.Duplicate(typeSig));
			return result;
		}

		// 添加方法
		private MethodX AddMethod(MethodX metX)
		{
			if (metX.DeclType.AddMethod(metX, out var ometX))
				ExpandMethod(metX);

			return ometX;
		}

		// 新方法加入处理队列
		private void AddPendingMethod(MethodX metX)
		{
			if (metX.IsQueueing)
				return;

			metX.IsQueueing = true;
			PendingMets.Enqueue(metX);
		}

		// 展开方法
		private void ExpandMethod(MethodX metX)
		{
			AddPendingMethod(metX);

			// 构建方法内的泛型展开器
			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(metX.DeclType);
			replacer.SetMethod(metX);

			// 展开方法内的泛型类型
			metX.ReturnType = ResolveTypeSig(metX.Def.ReturnType, replacer);
			metX.ParamTypes = ResolveTypeSigList(metX.Def.MethodSig.Params, replacer);
		}

		// 解析无泛型方法
		private MethodX ResolveMethod(MethodDef metDef)
		{
			TypeX declType = ResolveInstanceType(metDef.DeclaringType);

			MethodX metX = new MethodX(metDef, declType, null);
			return AddMethod(metX);
		}

		// 解析所在类型包含泛型实例的方法
		private MethodX ResolveMethod(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsMethodRef);

			// 跳过多维数组构造方法
			if (memRef.DeclaringType.ToTypeSig().IsArray)
				return null;

			TypeX declType = ResolveInstanceType(memRef.DeclaringType, replacer);

			MethodX metX = new MethodX(memRef.ResolveMethod(), declType, null);
			return AddMethod(metX);
		}

		// 解析包含泛型实例的方法
		private MethodX ResolveMethod(MethodSpec metSpec, GenericReplacer replacer)
		{
			TypeX declType = ResolveInstanceType(metSpec.DeclaringType, replacer);

			// 展开方法的泛型参数
			IList<TypeSig> genArgs = null;
			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			if (metGenArgs != null)
				genArgs = ResolveTypeSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(metSpec.ResolveMethodDef(), declType, genArgs);
			return AddMethod(metX);
		}

		// 添加字段
		private FieldX AddField(FieldX fldX)
		{
			if (fldX.DeclType.AddField(fldX, out var ofldX))
				ExpandField(fldX);

			return ofldX;
		}

		private void ExpandField(FieldX fldX)
		{
			// 构建字段内的泛型展开器
			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(fldX.DeclType);

			fldX.FieldType = ResolveTypeSig(fldX.Def.FieldType, replacer);
		}

		private FieldX ResolveField(FieldDef fldDef)
		{
			TypeX declType = ResolveInstanceType(fldDef.DeclaringType);

			FieldX fldX = new FieldX(fldDef, declType);
			return AddField(fldX);
		}

		private FieldX ResolveField(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);
			TypeX declType = ResolveInstanceType(memRef.DeclaringType, replacer);

			FieldX fldX = new FieldX(memRef.ResolveField(), declType);
			return AddField(fldX);
		}
	}
}

