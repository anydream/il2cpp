using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace il2cpp
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
			SigString = Name + ':' + Signature.SignatureName() + '|' + (int)Signature.CallingConvention;
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
			return Def.FullName + " [" + DeclType + ']';
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

			// 当前类定义了显式覆盖, 跳过
			if (CurrExplicitMap.ContainsKey(entry))
				return;

			bool result = VMap.TryGetValue(sig, out var layer);
			if (!result)
			{
				// 找不到且存在继承的显式覆盖, 跳过
				if (DerivedExplicitMap.ContainsKey(entry))
					return;
				throw new NotSupportedException("MergeSlot " + entry);
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
		public Dictionary<MethodX, MethodX>.ValueCollection Methods => MethodMap.Values;
		public bool HasMethods => MethodMap.Count > 0;
		// 字段映射
		private readonly Dictionary<FieldX, FieldX> FieldMap = new Dictionary<FieldX, FieldX>();
		public IList<FieldX> Fields => GetSortedFields();
		public bool HasFields => FieldMap.Count > 0;

		// 继承的类型
		private HashSet<TypeX> DerivedTypes_;
		public HashSet<TypeX> DerivedTypes => DerivedTypes_ ?? (DerivedTypes_ = new HashSet<TypeX>());
		public bool HasDerivedTypes => DerivedTypes_ != null && DerivedTypes_.Count > 0;

		// 方法签名映射
		public readonly Dictionary<MethodSignature, MethodDef> MethodSigMap =
			new Dictionary<MethodSignature, MethodDef>();
		// 方法覆盖类型集合映射
		private readonly Dictionary<MethodSignature, HashSet<TypeX>> OverrideImplTypes =
			new Dictionary<MethodSignature, HashSet<TypeX>>();

		// 虚表
		public VirtualTable VTable;

		public MethodX CctorMethod;
		public MethodX FinalizerMethod;

		// 类型全名
		public string FullName => Def.FullName + GenericToString();

		// 是否为空类型
		public bool IsEmptyType => !HasMethods && !HasFields;
		// 是否被实例化过
		public bool IsInstanced;
		// 是否生成过静态构造
		public bool CctorGenerated;
		// 是否生成过终结方法
		public bool FinalizerGenerated;

		public string CppName_;
		public uint CppTypeID_;

		public TypeX(TypeDef typeDef)
		{
			Debug.Assert(typeDef != null);
			Def = typeDef;
		}

		public override int GetHashCode()
		{
			return Def.Name.GetHashCode() ^
				   GenericHashCode();
		}

		public bool Equals(TypeX other)
		{
			if (ReferenceEquals(this, other))
				return true;

			return TypeEqualityComparer.Instance.Equals(Def, other.Def) &&
				   GenericEquals(other);
		}

		public override bool Equals(object obj)
		{
			return obj is TypeX other && Equals(other);
		}

		public override string ToString()
		{
			throw new InvalidOperationException();
		}

		public TypeSig ToTypeSig()
		{
			var sig = Def.ToTypeSig();
			if (HasGenArgs)
			{
				GenericInstSig genInst = new GenericInstSig(
					(ClassOrValueTypeSig)sig,
					new List<TypeSig>(GenArgs));
				return genInst;
			}
			return sig;
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

		public bool GetOverrideImplType(MethodSignature sig, out HashSet<TypeX> implSet)
		{
			return OverrideImplTypes.TryGetValue(sig, out implSet);
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

		public IList<FieldX> GetSortedFields()
		{
			List<FieldX> fldList = new List<FieldX>(FieldMap.Values);
			fldList.Sort((x, y) =>
			{
				return x.Def.Rid.CompareTo(y.Def.Rid);
			});
			return fldList;
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
		// 临时变量映射
		public IList<TypeSig> LocalTypes;
		// 是否需要初始化局部变量
		public bool InitLocals => Def.Body?.InitLocals ?? false;

		// 指令列表
		internal InstructionInfo[] InstList;
		internal bool HasInstList => InstList != null;

		// 异常处理信息列表
		internal ExceptionHandlerInfo[] HandlerList;
		internal bool HasHandlerList => HandlerList != null;

		// 方法覆盖集合
		private HashSet<MethodX> OverrideImpls_;
		public HashSet<MethodX> OverrideImpls => OverrideImpls_ ?? (OverrideImpls_ = new HashSet<MethodX>(new MethodRefComparer()));
		public bool HasOverrideImpls => OverrideImpls_ != null && OverrideImpls_.Count > 0;

		// 是否已处理过
		public bool IsProcessed;

		// 是否为纯虚方法
		private int VirtOnlyStatus_;
		public bool IsCallVirtOnly
		{
			get => VirtOnlyStatus_ == 1;
			set
			{
				if (VirtOnlyStatus_ == 0 || VirtOnlyStatus_ == 1)
					VirtOnlyStatus_ = value ? 1 : 2;
			}
		}

		public string ShortName => BuildName(false);
		public string FullName => BuildName(true);
		public string Name => BuildFuncName(true);

		public string CppName_;

		public MethodX(MethodDef metDef, TypeX declType, IList<TypeSig> genArgs)
		{
			Def = metDef;
			DeclType = declType;
			SetGenericArgs(genArgs);
		}

		internal void BuildInstructions(Func<TypeSig, TypeSig> resolverFunc)
		{
			if (!Def.HasBody)
				return;

			List<int> branchIndices = new List<int>();
			Dictionary<uint, int> offsetMap = new Dictionary<uint, int>();

			var origInstList = Def.Body.Instructions;
			InstList = new InstructionInfo[origInstList.Count];
			// 构造指令列表
			for (int i = 0; i < InstList.Length; ++i)
			{
				var origInst = origInstList[i];

				// 记录分支指令索引
				if (origInst.Operand is Instruction || origInst.Operand is Instruction[])
					branchIndices.Add(i);

				// 记录偏移量映射
				offsetMap.Add(origInst.Offset, i);

				InstList[i] = new InstructionInfo
				{
					OpCode = origInst.OpCode,
					Operand = origInst.Operand,
					Offset = i
				};
			}

			// 重建跳转指令操作数
			foreach (int idx in branchIndices)
			{
				switch (origInstList[idx].Operand)
				{
					case Instruction branchInst:
						{
							InstructionInfo targetInst = InstList[offsetMap[branchInst.Offset]];
							InstList[idx].Operand = targetInst;
						}
						break;

					case Instruction[] branchInstList:
						{
							InstructionInfo[] targets = new InstructionInfo[branchInstList.Length];
							for (int i = 0; i < targets.Length; ++i)
							{
								InstructionInfo targetInst = InstList[offsetMap[branchInstList[i].Offset]];
								targets[i] = targetInst;
							}

							InstList[idx].Operand = targets;
						}
						break;
				}
			}

			// 重建异常处理信息
			if (Def.Body.HasExceptionHandlers)
			{
				var origHandlers = Def.Body.ExceptionHandlers;
				HandlerList = new ExceptionHandlerInfo[origHandlers.Count];
				for (int i = 0; i < HandlerList.Length; ++i)
				{
					var curr = HandlerList[i] = new ExceptionHandlerInfo();
					var orig = origHandlers[i];
					curr.Index = i;
					curr.TryStart = offsetMap[orig.TryStart.Offset];
					curr.TryEnd = offsetMap[orig.TryEnd.Offset];
					if (orig.FilterStart != null)
						curr.FilterStart = offsetMap[orig.FilterStart.Offset];
					curr.HandlerStart = offsetMap[orig.HandlerStart.Offset];
					curr.HandlerEnd = offsetMap[orig.HandlerEnd.Offset];
					if (orig.CatchType != null)
						curr.CatchType = resolverFunc(orig.CatchType.ToTypeSig());
					curr.HandlerType = orig.HandlerType;
				}
			}
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
			throw new InvalidOperationException();
		}

		private string BuildFuncName(bool hasDeclType)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0}{1}{2}",
				hasDeclType ? DeclType.FullName + "::" : "",
				Def.Name,
				GenericToString());

			return sb.ToString();
		}

		private string BuildName(bool hasDeclType)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendFormat("{0} {1}",
				ReturnType != null ? ReturnType.FullName : "<?>",
				BuildFuncName(hasDeclType));

			sb.Append('(');
			if (ParamTypes == null)
				sb.Append("<?>");
			else
			{
				int i = 0;
				if (!Def.IsStatic)
					i = 1;

				bool last = false;
				for (; i < ParamTypes.Count; ++i)
				{
					if (last)
						sb.Append(',');
					last = true;
					var arg = ParamTypes[i];
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

		// 类型全名
		public string FullName => BuildName();

		public string CppName_;

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

		private string BuildName()
		{
			return string.Format("{0} {1}",
				FieldType != null ? FieldType.FullName : "<?>",
				Def.Name);
		}

		public override string ToString()
		{
			throw new InvalidOperationException();
		}
	}

	// 虚调用信息
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
		// 类型名称映射
		private readonly Dictionary<string, TypeX> NameTypeMap = new Dictionary<string, TypeX>();
		public Dictionary<string, TypeX>.ValueCollection Types => NameTypeMap.Values;
		// 待处理方法队列
		private readonly Queue<MethodX> PendingMets = new Queue<MethodX>();
		// 虚调用映射
		private readonly Dictionary<string, VCallInfo> VCalls = new Dictionary<string, VCallInfo>();

		// 复位
		public void Reset()
		{
			NameTypeMap.Clear();
			PendingMets.Clear();
			VCalls.Clear();
		}

		// 加载模块
		public void Load(string path)
		{
			Reset();

			AssemblyResolver asmRes = new AssemblyResolver();
			ModuleContext modCtx = new ModuleContext(asmRes);
			asmRes.DefaultModuleContext = modCtx;
			asmRes.EnableTypeDefCache = true;
			asmRes.FindExactMatch = false;
			asmRes.EnableFrameworkRedirect = true;

			Module = ModuleDefMD.Load(path);
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
				new VCallInfo("System.Object",
					new MethodSignature("Finalize",
						new MethodSig(CallingConvention.HasThis, 0, Module.CorLibTypes.Void)), null));

			while (PendingMets.Count > 0)
			{
				do
				{
					// 取出一个待处理方法
					MethodX currMetX = PendingMets.Dequeue();

					// 跳过已处理过的方法
					if (currMetX.IsProcessed)
						continue;

					// 跳过纯虚方法
					if (currMetX.IsCallVirtOnly)
						continue;

					// 设置为已处理过
					currMetX.IsProcessed = true;

					// 跳过不包含的方法
					if (!currMetX.HasInstList)
						continue;

					// 构建方法内的泛型展开器
					GenericReplacer replacer = new GenericReplacer();
					replacer.SetType(currMetX.DeclType);
					replacer.SetMethod(currMetX);

					// 遍历并解析指令
					foreach (var inst in currMetX.InstList)
					{
						ResolveInstruction(currMetX.DeclType, inst, replacer);
					}

				} while (PendingMets.Count > 0);

				ResolveVCalls();
			}

			// 补齐指令可能抛出的异常
			FixBCLType("System", "NullReferenceException");
			FixBCLType("System", "OutOfMemoryException");
			FixBCLType("System", "StackOverflowException");
			FixBCLType("System", "OverflowException");
			FixBCLType("System", "InvalidCastException");
			FixBCLType("System", "IndexOutOfRangeException");
			FixBCLType("System", "ArrayTypeMismatchException");
			FixBCLType("System", "MethodAccessException");
			FixBCLType("System", "MissingMethodException");
			FixBCLType("System", "FieldAccessException");
			FixBCLType("System", "MissingFieldException");
			FixBCLType("System", "ArithmeticException");
			FixBCLType("System", "DivideByZeroException");
			FixBCLType("System", "InvalidOperationException");

			// 补齐 BCL 中基础类型的字段
			FixBCLFields("System.Boolean");
			FixBCLFields("System.Char");
			FixBCLFields("System.SByte");
			FixBCLFields("System.Byte");
			FixBCLFields("System.Int16");
			FixBCLFields("System.UInt16");
			FixBCLFields("System.Int32");
			FixBCLFields("System.UInt32");
			FixBCLFields("System.Int64");
			FixBCLFields("System.UInt64");
			FixBCLFields("System.Single");
			FixBCLFields("System.Double");
			FixBCLFields("System.IntPtr");
			FixBCLFields("System.UIntPtr");
			FixBCLFields("System.String");
		}

		private void FixBCLType(string ns, string typeName)
		{
			var typeRef = Module.CorLibTypes.GetTypeRef(ns, typeName);
			ResolveInstanceType(typeRef);
		}

		private void FixBCLFields(string typeName)
		{
			TypeX tyX = GetTypeByName(typeName);
			if (tyX == null)
				return;
			foreach (var fld in tyX.Def.Fields)
			{
				if (!fld.IsStatic)
					ResolveField(fld);
			}
		}

		// 解析指令
		private void ResolveInstruction(TypeX currType, InstructionInfo inst, GenericReplacer replacer)
		{
			switch (inst.OpCode.OperandType)
			{
				case OperandType.InlineType:
					{
						ITypeDefOrRef typeDefRef = (ITypeDefOrRef)inst.Operand;
						var duplicator = new TypeSigDuplicator();
						duplicator.GenReplacer = replacer;
						TypeSig resTypeSig = duplicator.Duplicate(typeDefRef.ToTypeSig());

						// 解析实例类型
						TypeSig instSig = resTypeSig.GetLeafSig();
						ResolveInstanceType(instSig.ToTypeDefOrRef());

						inst.Operand = resTypeSig;
						break;
					}

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
								throw new ArgumentOutOfRangeException("InlineMethod " + inst.Operand.GetType().Name);
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

						// 生成静态构造方法
						if (resMetX.Def.IsStatic)
							GenStaticCctor(resMetX.DeclType);

						// 遇到对象创建
						if (inst.OpCode.Code == Code.Newobj)
						{
							Debug.Assert(!resMetX.Def.IsStatic);
							Debug.Assert(resMetX.Def.IsConstructor);
							// 标记类型为已实例化
							resMetX.DeclType.IsInstanced = true;
							// 生成静态构造和终结方法
							GenStaticCctor(resMetX.DeclType);
							GenFinalizer(resMetX.DeclType);
						}

						inst.Operand = resMetX;
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
								throw new ArgumentOutOfRangeException("InlineField " + inst.Operand.GetType().Name);
						}

						// 生成静态构造方法
						if (resFldX.Def.IsStatic)
							GenStaticCctor(resFldX.DeclType);

						inst.Operand = resFldX;
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
				tyX.CctorMethod = AddMethod(metX);
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
				tyX.FinalizerMethod = AddMethod(metX);
			}
		}

		private void ResolveVCalls()
		{
			foreach (var vInfo in VCalls.Values)
			{
				var declType = GetTypeByName(vInfo.TypeName);
				{
					bool result = declType.GetOverrideImplType(vInfo.Signature, out var implSet);
					// 跳过无实例化类型的虚调用
					if (!result)
						continue;

					// 遍历对应方法签名的所有覆盖类型
					foreach (var implType in implSet)
					{
						// 过滤未实例化的类型
						if (!implType.IsInstanced)
							continue;

						// 在类型虚表中查找实现
						VirtualEntry entry = vInfo;
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
						Debug.Assert(result);

						// 添加到接口方法的实现列表
						MethodX vmetX = new MethodX(vmetDef, declType, vInfo.GenArgs);
						vmetX = AddMethod(vmetX);
						vmetX.IsCallVirtOnly = true;
						vmetX.OverrideImpls.Add(metX);
					}
				}
			}
		}

		public TypeX GetTypeByName(string typeName)
		{
			if (NameTypeMap.TryGetValue(typeName, out var tyX))
				return tyX;
			return null;
		}

		// 添加类型
		private TypeX AddType(TypeX tyX)
		{
			string typeName = tyX.FullName;

			if (NameTypeMap.TryGetValue(typeName, out var otyX))
				return otyX;

			NameTypeMap.Add(typeName, tyX);

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

			// 添加当前类型到所有继承的类
			if (!tyX.Def.IsInterface &&
				!tyX.Def.IsAbstract)
			{
				if (tyX.BaseType != null)
					tyX.BaseType.DerivedTypes.Add(tyX);

				if (tyX.HasInterfaces)
				{
					foreach (var inf in tyX.Interfaces)
						inf.DerivedTypes.Add(tyX);
				}
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
								throw new ArgumentOutOfRangeException("Override MemberRef " + overMetDecl);

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
							throw new ArgumentOutOfRangeException("Override " + overMetDecl.GetType().Name);
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
						currType.Def.ToTypeSig().IsObjectSig())
					{
						Debug.Assert(currType.Def.ToTypeSig().ElementType != ElementType.Object ||
									 currType.Def.FullName == "System.Object");

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
				var type = GetTypeByName(kv.Key.TypeName);
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
					throw new ArgumentOutOfRangeException("ResolveTypeDefOrRefImpl " + typeDefRef.GetType().Name);
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
					throw new ArgumentOutOfRangeException("ResolveInstanceTypeImpl ITypeDefOrRef " + typeDefRef.GetType().Name);
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
					throw new ArgumentOutOfRangeException("ResolveInstanceTypeImpl TypeSig " + typeSig.GetType().Name);
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
			// 跳过已处理过的方法
			if (metX.IsProcessed)
				return;

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

			// 构建参数类型列表
			List<TypeSig> paramSigs = new List<TypeSig>();
			if (!metX.Def.IsStatic)
			{
				Debug.Assert(metX.Def.HasThis);

				// 值类型需要加引用
				TypeSig thisSig = metX.DeclType.ToTypeSig();
				if (thisSig.IsValueType)
					thisSig = new ByRefSig(thisSig);

				// 添加 this 类型
				paramSigs.Add(thisSig);
			}
			else
				Debug.Assert(!metX.Def.HasThis);

			paramSigs.AddRange(metX.Def.MethodSig.Params);

			// 展开参数类型
			metX.ParamTypes = ResolveTypeSigList(paramSigs, replacer);

			// 展开临时变量类型
			if (metX.Def.HasBody && metX.Def.Body.HasVariables)
			{
				metX.LocalTypes = new List<TypeSig>();
				foreach (var loc in metX.Def.Body.Variables)
					metX.LocalTypes.Add(ResolveTypeSig(loc.Type, replacer));
			}

			// 构造指令集和异常处理信息
			metX.BuildInstructions(sig =>
			{
				var resTypeSig = ResolveTypeSig(sig, replacer);

				// 解析实例类型
				TypeSig instSig = resTypeSig.GetLeafSig();
				ResolveInstanceType(instSig.ToTypeDefOrRef());

				return resTypeSig;
			});
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
