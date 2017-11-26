using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public class GenerateResult
	{
		private readonly GeneratorContext GenContext;
		public readonly List<CompileUnit> UnitList;
		public readonly Dictionary<string, string> TransMap;

		internal GenerateResult(GeneratorContext genContext, List<CompileUnit> unitList, Dictionary<string, string> transMap)
		{
			GenContext = genContext;
			UnitList = unitList;
			TransMap = transMap;
		}

		public void GenerateIncludes()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var unit in UnitList)
			{
				if (unit.DeclDepends.IsCollectionValid() ||
					!string.IsNullOrEmpty(unit.DeclCode))
				{
					sb.Append("#pragma once\n");
					sb.Append("#include \"il2cpp.h\"\n");
					foreach (var dep in unit.DeclDepends)
						sb.AppendFormat("#include \"{0}.h\"\n", dep);
					sb.Append(unit.DeclCode);
					unit.DeclCode = sb.ToString();
					sb.Clear();

					sb.AppendFormat("#include \"{0}.h\"\n", unit.Name);
				}

				if (!string.IsNullOrEmpty(unit.ImplCode))
				{
					foreach (var dep in unit.ImplDepends)
						sb.AppendFormat("#include \"{0}.h\"\n", dep);
					sb.Append(unit.ImplCode);
					unit.ImplCode = sb.ToString();
				}
				sb.Clear();
			}
		}

		public string GetMethodName(MethodDef metDef, out string unitName)
		{
			MethodX metX = GenContext.TypeMgr.ResolveMethodDef(metDef);
			string typeName = GenContext.GetTypeName(metX.DeclType);
			unitName = TransMap[typeName];
			return GenContext.GetMethodName(metX, "met_");
		}
	}

	public class CompileUnit
	{
		public string Name;
		public string DeclCode;
		public string ImplCode;
		public HashSet<string> DeclDepends = new HashSet<string>();
		public HashSet<string> ImplDepends = new HashSet<string>();
		public HashSet<string> StringDepends = new HashSet<string>();
		public ulong DependOrder;

		public void Optimize(Dictionary<string, CompileUnit> unitMap)
		{
			ImplDepends.ExceptWith(DeclDepends);

			DeclDepends.Remove(Name);
			ImplDepends.Remove(Name);

			DeclDepends.RemoveWhere(item => !unitMap.ContainsKey(item));
			ImplDepends.RemoveWhere(item => !unitMap.ContainsKey(item));
		}

		public void Append(CompileUnit unit)
		{
			DeclCode += unit.DeclCode;
			DeclDepends.UnionWith(unit.DeclDepends);
			ImplCode += unit.ImplCode;
			ImplDepends.UnionWith(unit.ImplDepends);
			StringDepends.UnionWith(unit.StringDepends);
		}

		public bool IsEmpty()
		{
			return DeclCode == null && ImplCode == null ||
				   DeclCode?.Length == 0 && ImplCode?.Length == 0;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	internal class CompileUnitMerger
	{
		private uint UnitCounter;
		public readonly Dictionary<string, CompileUnit> UnitMap;
		private static readonly HashSet<string> BridgeTypes = new HashSet<string>
		{
			"cls_Object",
			"cls_String",
			"cls_System_Array",
			"cls_System_Threading_Monitor",
			"cls_il2cpprt_ThrowHelper",
			"cls_System_GC",
		};

		public CompileUnitMerger(Dictionary<string, CompileUnit> units)
		{
			UnitMap = units;
		}

		public Dictionary<string, string> Merge()
		{
			// 排序编译单元
			var sortedUnits = UnitMap.Values.ToList();
			sortedUnits.Sort((lhs, rhs) => GetDependOrder(lhs).CompareTo(GetDependOrder(rhs)));

			// 合并编译单元
			UnitMap.Clear();
			var transMap = new Dictionary<string, string>();

			CompileUnit bridgeUnit = new CompileUnit();
			bridgeUnit.Name = "il2cppBridge";
			UnitMap.Add(bridgeUnit.Name, bridgeUnit);

			// 收集桥接代码的所有依赖项
			List<CompileUnit> tmpList = new List<CompileUnit>();
			for (; ; )
			{
				bool changed = false;
				foreach (var unit in sortedUnits)
				{
					if (BridgeTypes.Contains(unit.Name) ||
						bridgeUnit.DeclDepends.Contains(unit.Name))
					{
						bridgeUnit.DeclCode += "#define IL2CPP_BRIDGE_HAS_" + unit.Name + '\n';
						bridgeUnit.Append(unit);
						transMap[unit.Name] = bridgeUnit.Name;
						changed = true;
					}
					else
						tmpList.Add(unit);
				}

				if (changed)
				{
					sortedUnits = tmpList;
					tmpList = new List<CompileUnit>();
				}
				else
					break;
			}

			CompileUnit currUnit = NewUnit();
			foreach (var unit in sortedUnits)
			{
				Debug.Assert(!bridgeUnit.DeclDepends.Contains(unit.Name));
				currUnit.Append(unit);
				transMap[unit.Name] = currUnit.Name;
				if (IsUnitFull(currUnit))
					currUnit = NewUnit();
			}

			if (currUnit.IsEmpty())
				UnitMap.Remove(currUnit.Name);

			foreach (var unit in UnitMap.Values)
			{
				var declDeps = new HashSet<string>();
				foreach (string dep in unit.DeclDepends)
					declDeps.Add(transMap[dep]);
				unit.DeclDepends = declDeps;

				var implDeps = new HashSet<string>();
				foreach (string dep in unit.ImplDepends)
					implDeps.Add(transMap[dep]);
				unit.ImplDepends = implDeps;

				unit.Optimize(UnitMap);
			}

			return transMap;
		}

		private CompileUnit NewUnit()
		{
			var unit = new CompileUnit();
			unit.Name = "il2cppUnit_" + ++UnitCounter;
			UnitMap.Add(unit.Name, unit);
			return unit;
		}

		private bool IsUnitFull(CompileUnit unit)
		{
			// 生成一个 .cpp 文件比什么 LTO 都好使
#if false
			return !unit.IsEmpty();
#else
			return unit.DeclCode.Length > 100000 ||
				   unit.ImplCode.Length > 1000000;
#endif
		}

		private ulong GetDependOrder(CompileUnit unit, HashSet<string> stackUnitNames = null)
		{
			ulong depOrder = unit.DependOrder;
			if (depOrder != 0)
				return depOrder;

			string unitName = unit.Name;
			if (stackUnitNames != null && stackUnitNames.Contains(unitName))
				return 0;
			if (stackUnitNames == null)
				stackUnitNames = new HashSet<string>();
			stackUnitNames.Add(unitName);

			unit.Optimize(UnitMap);
			foreach (string dep in unit.DeclDepends)
			{
				var depUnit = GetUnitFromMap(dep);
				if (depUnit != null)
					depOrder += GetDependOrder(depUnit, stackUnitNames);
			}

			++depOrder;
			unit.DependOrder = depOrder;
			return depOrder;
		}

		private CompileUnit GetUnitFromMap(string key)
		{
			if (UnitMap.TryGetValue(key, out var result))
				return result;
			return null;
		}
	}

	internal class GeneratorContext
	{
		public readonly TypeManager TypeMgr;
		public readonly StringGenerator StrGen = new StringGenerator();
		private uint TypeIDCounter;

		public GeneratorContext(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
		}

		public GenerateResult Generate()
		{
			var units = new Dictionary<string, CompileUnit>();

			// 生成类型代码
			var types = TypeMgr.Types;
			foreach (TypeX tyX in types)
			{
				CompileUnit unit = new TypeGenerator(this, tyX).Generate();
				units.Add(unit.Name, unit);
			}

			var transMap = new CompileUnitMerger(units).Merge();

			TypeX strTyX = TypeMgr.GetTypeByName("String");
			if (strTyX != null)
				StrGen.Generate(units, GetTypeID(strTyX));

			return new GenerateResult(this, units.Values.ToList(), transMap);
		}

		public int GetTypeLayoutOrder(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
				case ElementType.I1:
				case ElementType.U1:
				case ElementType.Boolean:
					return 1;
				case ElementType.I2:
				case ElementType.U2:
				case ElementType.Char:
					return 2;
				case ElementType.I4:
				case ElementType.U4:
				case ElementType.R4:
					return 4;
				case ElementType.I8:
				case ElementType.U8:
				case ElementType.R8:
					return 8;

				case ElementType.I:
				case ElementType.U:
				case ElementType.Ptr:
				case ElementType.ByRef:
				case ElementType.Object:
				case ElementType.Class:
				case ElementType.TypedByRef:
				case ElementType.SZArray:
				case ElementType.Array:
				case ElementType.String:
					return 10;

				case ElementType.ValueType:
				case ElementType.GenericInst:
					if (tySig.IsValueType)
					{
						if (Helper.IsEnumType(tySig, out var enumTySig))
							return GetTypeLayoutOrder(enumTySig);

						TypeX tyX = GetTypeBySig(tySig);
						if (tyX == null)
							return 1;

						if (tyX.AccumOrderSize >= 0)
							return tyX.AccumOrderSize;

						// 值类型需要累加所有字段的长度
						int size = 0;
						foreach (var fldX in tyX.Fields)
						{
							if (fldX.IsStatic)
								continue;

							size += GetTypeLayoutOrder(fldX.FieldType);
						}
						tyX.AccumOrderSize = size;
						return size;
					}
					else
						return 10;

				case ElementType.CModReqd:
					return GetTypeLayoutOrder(tySig.Next);

				default:
					throw new NotImplementedException();
			}
		}

		private bool RefToTypeIsNoRef(TypeX tyX)
		{
			if (tyX == null)
				return true;

			if (tyX.IsValueType)
				return IsNoRefType(tyX);
			return false;
		}

		public bool IsNoRefType(TypeX tyX)
		{
			if (tyX == null)
				return true;

			if (tyX.NoRefFlag != 0)
				return tyX.NoRefFlag == 1;

			if (tyX.IsArrayType)
			{
				// 数组取决于其元素类型的属性
				Debug.Assert(tyX.HasGenArgs && tyX.GenArgs.Count == 1);
				TypeX elemType = GetTypeBySig(tyX.GenArgs[0]);
				tyX.NoRefFlag = (byte)(RefToTypeIsNoRef(elemType) ? 1 : 2);
			}
			else
			{
				tyX.NoRefFlag = 1;
				// 检查对象的字段
				foreach (var fldX in tyX.Fields)
				{
					if (fldX.IsStatic)
						continue;

					TypeX fldType = GetTypeBySig(fldX.FieldType);
					if (!RefToTypeIsNoRef(fldType))
					{
						// 存在包含引用的字段
						tyX.NoRefFlag = 2;
						break;
					}
				}
			}

			return tyX.NoRefFlag == 1;
		}

		public string GetTypeDefaultValue(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
				case ElementType.Boolean:
				case ElementType.Char:
				case ElementType.I1:
				case ElementType.I2:
				case ElementType.I4:
				case ElementType.I8:
				case ElementType.U1:
				case ElementType.U2:
				case ElementType.U4:
				case ElementType.U8:
				case ElementType.R4:
				case ElementType.R8:
				case ElementType.I:
				case ElementType.U:
				case ElementType.Ptr:
				case ElementType.ByRef:
					return "0";

				case ElementType.Object:
					return "nullptr";
			}

			if (tySig.IsValueType)
			{
				if (Helper.IsEnumType(tySig, out var enumTySig))
					return GetTypeDefaultValue(enumTySig);

				return "{}";
			}

			return "nullptr";
		}

		public string GetTypeName(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
				case ElementType.Void:
					return "void";

				case ElementType.Boolean:
					return "uint8_t";
				case ElementType.Char:
					return "uint16_t";
				case ElementType.I1:
					return "int8_t";
				case ElementType.I2:
					return "int16_t";
				case ElementType.I4:
					return "int32_t";
				case ElementType.I8:
					return "int64_t";
				case ElementType.U1:
					return "uint8_t";
				case ElementType.U2:
					return "uint16_t";
				case ElementType.U4:
					return "uint32_t";
				case ElementType.U8:
					return "uint64_t";
				case ElementType.R4:
					return "float";
				case ElementType.R8:
					return "double";
				case ElementType.I:
					return "intptr_t";
				case ElementType.U:
					return "uintptr_t";

				case ElementType.Ptr:
				case ElementType.ByRef:
					return GetTypeName(tySig.Next) + '*';

				case ElementType.Object:
					return "cls_Object*";

				case ElementType.Class:
				case ElementType.ValueType:
				case ElementType.TypedByRef:
				case ElementType.GenericInst:
				case ElementType.SZArray:
				case ElementType.Array:
				case ElementType.String:
					{
						if (Helper.IsEnumType(tySig, out var enumTySig))
							return GetTypeName(enumTySig);

						bool isValueType = tySig.IsValueType;
						TypeX tyX = GetTypeBySig(tySig);
						if (tyX == null)
						{
							if (isValueType)
								return "il2cppDummy";
							else
								return "cls_Object*";
						}

						return "struct " +
							GetTypeName(tyX) +
							(isValueType ? null : "*");
					}

				case ElementType.Pinned:
					return GetTypeName(tySig.Next);

				case ElementType.CModReqd:
					{
						CModReqdSig modReqdSig = (CModReqdSig)tySig;
						if (modReqdSig.Modifier.FullName == "System.Runtime.CompilerServices.IsVolatile")
							return "volatile " + GetTypeName(tySig.Next);
					}
					break;
			}

			throw new NotImplementedException(tySig.ElementType.ToString());
		}

		public string GetTypeName(TypeX tyX, bool expandEnum = true)
		{
			if (tyX == null)
				return "il2cppDummy";
			if (expandEnum)
			{
				if (tyX.IsEnumType)
					return GetTypeName(tyX.EnumTypeSig);
			}

			string strName = tyX.GeneratedTypeName;
			if (strName == null)
			{
				strName = tyX.IsValueType ? "stru_" : "cls_";

				string nameKey = tyX.GetNameKey();
				if (!tyX.HasGenArgs && tyX.Def.DefinitionAssembly.IsCorLib())
					strName += nameKey;
				else
					strName += NameHash(nameKey.GetHashCode()) + '_' + GetNameWithGen(tyX.Def.Name, tyX.GenArgs);

				tyX.GeneratedTypeName = strName = EscapeName(strName);
			}

			return strName;
		}

		public uint GetTypeID(TypeX tyX)
		{
			if (tyX.HasBoxedType)
				return GetTypeID(tyX.BoxedType);

			if (tyX.GeneratedTypeID != 0)
				return tyX.GeneratedTypeID;

			tyX.GeneratedTypeID = ++TypeIDCounter;
			return tyX.GeneratedTypeID;
		}

		public TypeX GetTypeBySig(TypeSig tySig)
		{
			StringBuilder sb = new StringBuilder();
			Helper.TypeSigName(sb, tySig, true);
			return TypeMgr.GetTypeByName(sb.ToString());
		}

		public string GetMethodName(MethodX metX, string prefix)
		{
			string strName = metX.GeneratedMethodName;
			if (strName == null)
			{
				int hashCode = Helper.CombineHash(
					metX.GetNameKey().GetHashCode(),
					metX.GetReplacedNameKey().GetHashCode(),
					metX.DeclType.GetNameKey().GetHashCode());

				strName = NameHash(hashCode) + '_' +
					GetNameWithGen(metX.DeclType.Def.Name, metX.DeclType.GenArgs) + "__" +
					GetNameWithGen(metX.Def.Name, metX.GenArgs);

				metX.GeneratedMethodName = strName = EscapeName(strName);
			}
			return prefix + strName;
		}

		public string GetFieldName(FieldX fldX)
		{
			string strName = fldX.GeneratedFieldName;
			if (strName == null)
			{
				string prefix = fldX.IsStatic ? "sfld_" : "fld_";

				string middle = null;
				if (fldX.IsStatic)
				{
					int hashCode = Helper.CombineHash(
						fldX.GetNameKey().GetHashCode(),
						fldX.GetReplacedNameKey().GetHashCode(),
						fldX.DeclType.GetNameKey().GetHashCode(),
						(int)fldX.Def.Rid);

					middle = NameHash(hashCode) + '_' +
							 GetNameWithGen(fldX.DeclType.Def.Name, fldX.DeclType.GenArgs);
				}
				else
				{
					middle = NameHash((int)fldX.Def.Rid);
				}

				strName = prefix + middle + "__" + fldX.Def.Name;

				fldX.GeneratedFieldName = strName = EscapeName(strName);
			}
			return strName;
		}

		private static string GetNameWithGen(string name, IList<TypeSig> genArgs)
		{
			if (genArgs.IsCollectionValid())
			{
				foreach (var arg in genArgs)
				{
					name += '_';
					name += arg.TypeName;
				}
			}
			return name;
		}

		private static string EscapeName(string fullName)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < fullName.Length; ++i)
			{
				char ch = fullName[i];
				if (IsLegalIdentChar(ch))
					sb.Append(ch);
				else if (ch >= 0x7F)
					sb.AppendFormat("{0:X}", ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}

		private static bool IsLegalIdentChar(char ch)
		{
			return ch >= 'a' && ch <= 'z' ||
				   ch >= 'A' && ch <= 'Z' ||
				   ch >= '0' && ch <= '9' ||
				   ch == '_';
		}

		public static string NameHash(int hashCode)
		{
			return ToRadix((uint)hashCode, (uint)DigMap.Length);
		}

		private const string DigMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private static string ToRadix(uint value, uint radix)
		{
			StringBuilder sb = new StringBuilder();
			do
			{
				uint dig = value % radix;
				value /= radix;
				sb.Append(DigMap[(int)dig]);
			} while (value != 0);

			return sb.ToString();
		}
	}
}
