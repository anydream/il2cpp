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

		public MethodSignature(string name, MethodBaseSig sig)
		{
			Debug.Assert(sig != null);
			Name = name;
			Signature = sig;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public bool Equals(MethodSignature other)
		{
			return Name == other.Name &&
				   new SigComparer().Equals(Signature, other.Signature);
		}

		public override bool Equals(object obj)
		{
			return obj is MethodSignature other && Equals(other);
		}

		public override string ToString()
		{
			return Name + ": " + Signature;
		}
	}

	// 虚调用入口
	public class VirtualEntry
	{
		// 入口所属的类型
		public readonly TypeX DeclType;
		// 方法签名
		public readonly MethodSignature Signature;

		public VirtualEntry(TypeX declType, MethodSignature sig)
		{
			Debug.Assert(sig != null);
			DeclType = declType;
			Signature = sig;
		}

		public override int GetHashCode()
		{
			return DeclType.GetHashCode() ^
				   Signature.GetHashCode();
		}

		public bool Equals(VirtualEntry other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return DeclType.Equals(other.DeclType) &&
				   Signature.Equals(other.Signature);
		}

		public override bool Equals(object obj)
		{
			return obj is VirtualEntry other && Equals(other);
		}

		public override string ToString()
		{
			return DeclType + "::" + Signature;
		}
	}

	// 方法实现信息
	public class MethodImpl
	{
		// 方法定义
		public readonly MethodDef Def;
		// 所属类型
		public readonly TypeX DeclType;

		public MethodImpl(MethodDef metDef, TypeX declType)
		{
			Def = metDef;
			DeclType = declType;
		}

		public override string ToString()
		{
			return Def.FullName + " [" + DeclType + "]";
		}
	}

	public class VirtualTable
	{
		private class SlotLayer
		{
			public readonly List<TypeX> Entries;
			public MethodImpl ImplMethod;

			public SlotLayer()
			{
				Entries = new List<TypeX>();
			}

			private SlotLayer(List<TypeX> entries, MethodImpl impl)
			{
				Entries = entries;
				ImplMethod = impl;
			}

			public SlotLayer Clone()
			{
				return new SlotLayer(new List<TypeX>(Entries), ImplMethod);
			}
		}

		private class ExplicitItem : VirtualEntry
		{
			public readonly MethodImpl ImplMethod;

			public ExplicitItem(TypeX declType, MethodSignature sig, MethodImpl impl)
				: base(declType, sig)
			{
				ImplMethod = impl;
			}

			public override string ToString()
			{
				return base.ToString() + " => " + ImplMethod;
			}
		}

		// 虚表槽层级映射
		private readonly Dictionary<MethodSignature, List<SlotLayer>> VMap =
			new Dictionary<MethodSignature, List<SlotLayer>>();
		// 显式覆盖列表
		private readonly List<ExplicitItem> ExplicitList = new List<ExplicitItem>();
		private readonly HashSet<VirtualEntry> ExplicitSet = new HashSet<VirtualEntry>();
		// 展开的虚表
		public Dictionary<VirtualEntry, MethodImpl> Table { get; private set; }
			= new Dictionary<VirtualEntry, MethodImpl>();

		public VirtualTable Clone()
		{
			VirtualTable vtbl = new VirtualTable();

			// 只需要克隆最后一层虚槽
			foreach (var kv in VMap)
				vtbl.VMap.Add(kv.Key, new List<SlotLayer> { kv.Value.Last().Clone() });

			// 克隆展开的虚表
			vtbl.Table = new Dictionary<VirtualEntry, MethodImpl>(Table);

			return vtbl;
		}

		public void NewSlot(MethodSignature sig, MethodImpl impl)
		{
			if (!VMap.TryGetValue(sig, out var layerList))
			{
				layerList = new List<SlotLayer>();
				VMap.Add(sig, layerList);
			}

			var layer = new SlotLayer();
			layer.Entries.Add(impl.DeclType);
			layer.ImplMethod = impl;
			layerList.Add(layer);
		}

		public void ReuseSlot(MethodSignature sig, MethodImpl impl)
		{
			bool result = VMap.TryGetValue(sig, out var layerList);
			Debug.Assert(result);
			Debug.Assert(layerList.Count > 0);

			var layer = layerList.Last();
			layer.Entries.Add(impl.DeclType);
			layer.ImplMethod = impl;
		}

		public void MergeSlot(MethodSignature sig, TypeX declType)
		{
			// 跳过已覆盖的签名
			var entry = new VirtualEntry(declType, sig);
			if (ExplicitSet.Contains(entry))
				return;
			if (Table.ContainsKey(entry))
				return;

			bool result = VMap.TryGetValue(sig, out var layerList);
			Debug.Assert(result);
			Debug.Assert(layerList.Count > 0);

			var layer = layerList.Last();
			layer.Entries.Add(declType);
		}

		public void ExplicitOverride(TypeX declType, MethodSignature sig, MethodImpl impl)
		{
			var expItem = new ExplicitItem(declType, sig, impl);
			ExplicitList.Add(expItem);
			ExplicitSet.Add(expItem);
		}

		public void ExpandTable()
		{
			foreach (var kv in VMap)
			{
				foreach (var layer in kv.Value)
				{
					foreach (var entryType in layer.Entries)
					{
						var entry = new VirtualEntry(entryType, kv.Key);
						Table[entry] = layer.ImplMethod;
					}
				}
			}
			// 显式覆盖最后展开
			foreach (var expInfo in ExplicitList)
			{
				var entry = new VirtualEntry(expInfo.DeclType, expInfo.Signature);
				Table[entry] = expInfo.ImplMethod;
			}
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
			if (GenArgs_ == null)
				return 0;
			return ~(GenArgs_.Count + 1);
		}

		public bool GenericEquals(GenericArgs other)
		{
			if (GenArgs_ == null && other.GenArgs_ == null)
				return true;
			if (GenArgs_ == null || other.GenArgs_ == null)
				return false;
			if (GenArgs_.Count != other.GenArgs_.Count)
				return false;

			var comparer = new SigComparer();
			for (int i = 0; i < GenArgs_.Count; ++i)
			{
				if (!comparer.Equals(GenArgs_[i], other.GenArgs_[i]))
					return false;
			}
			return true;
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
		public IList<MethodX> Methods => new List<MethodX>(MethodMap.Keys);
		// 方法签名映射
		public readonly Dictionary<MethodSignature, MethodDef> MethodSigMap = new Dictionary<MethodSignature, MethodDef>();
		// 字段映射
		private readonly Dictionary<FieldX, FieldX> FieldMap = new Dictionary<FieldX, FieldX>();
		public IList<FieldX> Fields => new List<FieldX>(FieldMap.Keys);
		// 运行时类型
		public string RuntimeVersion => Def.Module.RuntimeVersion;

		public VirtualTable VTable;

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
			return Def.FullName + GenericToString();
		}

		public bool AddMethod(MethodX metX)
		{
			if (!MethodMap.ContainsKey(metX))
			{
				MethodMap.Add(metX, metX);
				return true;
			}
			return false;
		}

		public bool AddField(FieldX fldX)
		{
			if (!FieldMap.ContainsKey(fldX))
			{
				FieldMap.Add(fldX, fldX);
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
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0} {1}{2}",
				ReturnType != null ? ReturnType.FullName : "<?>",
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

	public class TypeManager
	{
		// 主模块
		public ModuleDefMD Module { get; private set; }
		// 类型映射
		private readonly Dictionary<TypeX, TypeX> TypeMap = new Dictionary<TypeX, TypeX>();
		public IList<TypeX> Types => new List<TypeX>(TypeMap.Keys);
		// 待处理方法队列
		private readonly Queue<MethodX> PendingMets = new Queue<MethodX>();

		// 复位
		public void Reset()
		{
			Module = null;
			TypeMap.Clear();
			PendingMets.Clear();
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

		// 处理循环
		public void Process()
		{
			while (PendingMets.Count > 0)
			{
				// 取出一个待处理方法
				MethodX currMetX = PendingMets.Dequeue();

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
			}
		}

		// 解析指令
		private void ResolveInstruction(Instruction inst, GenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineMethod:
					{
						switch (inst.Operand)
						{
							case MethodDef metDef:
								ResolveMethod(metDef);
								break;

							case MemberRef memRef:
								ResolveMethod(memRef, replacer);
								break;

							case MethodSpec metSpec:
								ResolveMethod(metSpec, replacer);
								break;
						}

						break;
					}

				case OperandType.InlineField:
					{
						switch (inst.Operand)
						{
							case FieldDef fldDef:
								ResolveField(fldDef);
								break;

							case MemberRef memRef:
								ResolveField(memRef, replacer);
								break;
						}

						break;
					}
			}
		}

		// 添加类型
		private TypeX AddType(TypeX tyX)
		{
			if (TypeMap.TryGetValue(tyX, out var otyX))
				return otyX;

			TypeMap.Add(tyX, tyX);
			ExpandType(tyX);

			return tyX;
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
				if (metDef.IsStatic || metDef.IsSpecialName)
					continue;

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

							currType.VTable.ExplicitOverride(oDeclType, oSig, new MethodImpl(metDef, currType));
						}
						else if (overMetDecl is MemberRef omemRef)
						{
							Debug.Assert(omemRef.IsMethodRef);

							// 展开目标方法所属的类型
							TypeX oDeclType = null;
							if (omemRef.Class is TypeSpec omemClsSpec)
								oDeclType = ResolveInstanceType(omemClsSpec, replacer);
							else
								Debug.Fail("Override MemberRef " + overMetDecl);
							//oDeclType = ResolveInstanceType(omemRef.DeclaringType);

							GenericReplacer oReplacer = new GenericReplacer();
							oReplacer.SetType(oDeclType);
							MethodSigDuplicator oDuplicator = MakeMethodDuplicator(oReplacer);

							MethodSignature oSig = MakeMethodSignature(
								omemRef.Name,
								(MethodBaseSig)omemRef.Signature,
								oDuplicator);

							currType.VTable.ExplicitOverride(oDeclType, oSig, new MethodImpl(metDef, currType));
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
						currType.VTable.NewSlot(sig, new MethodImpl(metDef, currType));
					}
					else
					{
						// 复用虚表槽的方法
						Debug.Assert(metDef.IsReuseSlot);
						currType.VTable.ReuseSlot(sig, new MethodImpl(metDef, currType));
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
						currType.VTable.MergeSlot(kv.Key, infType);
					}
				}
			}

			// 展开虚表
			currType.VTable.ExpandTable();
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
		private void AddMethod(TypeX declType, MethodX metX)
		{
			if (declType.AddMethod(metX))
				ExpandMethod(metX);
		}

		// 新方法加入处理队列
		private void AddPendingMethod(MethodX metX)
		{
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
		public void ResolveMethod(MethodDef metDef)
		{
			TypeX declType = ResolveInstanceType(metDef.DeclaringType);

			MethodX metX = new MethodX(metDef, declType, null);
			AddMethod(declType, metX);
		}

		// 解析所在类型包含泛型实例的方法
		private void ResolveMethod(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsMethodRef);
			TypeX declType = ResolveInstanceType(memRef.DeclaringType, replacer);

			MethodX metX = new MethodX(memRef.ResolveMethod(), declType, null);
			AddMethod(declType, metX);
		}

		// 解析包含泛型实例的方法
		private void ResolveMethod(MethodSpec metSpec, GenericReplacer replacer)
		{
			TypeX declType = ResolveInstanceType(metSpec.DeclaringType, replacer);

			// 展开方法的泛型参数
			IList<TypeSig> genArgs = null;
			var metGenArgs = metSpec.GenericInstMethodSig?.GenericArguments;
			if (metGenArgs != null)
				genArgs = ResolveTypeSigList(metGenArgs, replacer);

			MethodX metX = new MethodX(metSpec.ResolveMethodDef(), declType, genArgs);
			AddMethod(declType, metX);
		}

		// 添加字段
		private void AddField(TypeX declType, FieldX fldX)
		{
			if (declType.AddField(fldX))
				ExpandField(fldX);
		}

		private void ExpandField(FieldX fldX)
		{
			// 构建字段内的泛型展开器
			GenericReplacer replacer = new GenericReplacer();
			replacer.SetType(fldX.DeclType);

			fldX.FieldType = ResolveTypeSig(fldX.Def.FieldType, replacer);
		}

		private void ResolveField(FieldDef fldDef)
		{
			TypeX declType = ResolveInstanceType(fldDef.DeclaringType);

			FieldX fldX = new FieldX(fldDef, declType);
			AddField(declType, fldX);
		}

		private void ResolveField(MemberRef memRef, GenericReplacer replacer)
		{
			Debug.Assert(memRef.IsFieldRef);
			TypeX declType = ResolveInstanceType(memRef.DeclaringType, replacer);

			FieldX fldX = new FieldX(memRef.ResolveField(), declType);
			AddField(declType, fldX);
		}
	}
}
