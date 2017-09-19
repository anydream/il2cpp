using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	public class CompileUnit
	{
		public string Name;
		public string DeclCode;
		public string ImplCode;
		public HashSet<string> DeclDepends = new HashSet<string>();
		public HashSet<string> ImplDepends = new HashSet<string>();
		public uint DependOrder;

		public void Optimize()
		{
			ImplDepends.ExceptWith(DeclDepends);
			ImplDepends.Remove(Name);
			DeclDepends.Remove(Name);
		}

		public void Append(CompileUnit unit)
		{
			DeclCode += unit.DeclCode;
			DeclDepends.UnionWith(unit.DeclDepends);
			ImplCode += unit.ImplCode;
			ImplDepends.UnionWith(unit.ImplDepends);
		}

		public bool IsEmpty()
		{
			return DeclCode.Length == 0 &&
				   ImplCode.Length == 0;
		}
	}

	internal class CompileUnitMerger
	{
		private uint UnitCounter;
		public readonly Dictionary<string, CompileUnit> UnitMap;
		private static readonly HashSet<string> BridgeTypes = new HashSet<string>
		{
			"cls_Object"
		};

		public CompileUnitMerger(Dictionary<string, CompileUnit> units)
		{
			UnitMap = units;
		}

		public void Merge()
		{
			// 排序编译单元
			var sortedUnits = UnitMap.Values.ToList();
			sortedUnits.Sort((lhs, rhs) =>
			{
				uint l = GetDependOrder(lhs);
				uint r = GetDependOrder(rhs);
				if (l < r)
					return -1;
				else if (l > r)
					return 1;
				return 0;
			});

			// 合并编译单元
			UnitMap.Clear();
			var transMap = new Dictionary<string, string>();

			CompileUnit bridgeUnit = new CompileUnit();
			bridgeUnit.Name = "il2cppBridge";
			UnitMap.Add(bridgeUnit.Name, bridgeUnit);

			CompileUnit currUnit = NewUnit();

			foreach (var unit in sortedUnits)
			{
				if (BridgeTypes.Contains(unit.Name))
				{
					bridgeUnit.Append(unit);
					transMap[unit.Name] = bridgeUnit.Name;
				}
				else
				{
					currUnit.Append(unit);
					transMap[unit.Name] = currUnit.Name;
					if (IsUnitFull(currUnit))
						currUnit = NewUnit();
				}
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

				unit.Optimize();
			}
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
			return unit.DeclCode.Length > 30000 ||
				   unit.ImplCode.Length > 100000;
		}

		private uint GetDependOrder(CompileUnit unit)
		{
			uint depOrder = unit.DependOrder;
			if (depOrder != 0)
				return depOrder;

			foreach (string dep in unit.DeclDepends)
				depOrder += GetDependOrder(UnitMap[dep]);

			++depOrder;
			unit.DependOrder = depOrder;
			return depOrder;
		}
	}

	internal class GeneratorContext
	{
		private readonly TypeManager TypeMgr;

		public GeneratorContext(TypeManager typeMgr)
		{
			TypeMgr = typeMgr;
		}

		public List<CompileUnit> Generate()
		{
			var units = new Dictionary<string, CompileUnit>();

			// 生成类型代码
			var types = TypeMgr.Types;
			foreach (TypeX tyX in types)
			{
				CompileUnit unit = new TypeGenerator(this, tyX).Generate();
				units.Add(unit.Name, unit);
			}

			var merger = new CompileUnitMerger(units);
			merger.Merge();

			StringBuilder sb = new StringBuilder();
			var unitList = merger.UnitMap.Values.ToList();
			foreach (var unit in unitList)
			{
				sb.Append("#pragma once\n");
				sb.Append("#include \"il2cpp.h\"\n");
				foreach (var dep in unit.DeclDepends)
					sb.AppendFormat("#include \"{0}.h\"\n", dep);
				sb.Append(unit.DeclCode);
				unit.DeclCode = sb.ToString();
				sb.Clear();

				sb.AppendFormat("#include \"{0}.h\"\n", unit.Name);
				foreach (var dep in unit.ImplDepends)
					sb.AppendFormat("#include \"{0}.h\"\n", dep);
				sb.Append(unit.ImplCode);
				unit.ImplCode = sb.ToString();
				sb.Clear();
			}
			return unitList;
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
					return 10;

				case ElementType.ValueType:
				case ElementType.GenericInst:
					return tySig.IsValueType ? 12 : 10;

				default:
					throw new NotImplementedException();
			}
		}

		public string GetTypeName(TypeSig tySig)
		{
			switch (tySig.ElementType)
			{
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
				case ElementType.Boolean:
					return "uint8_t";
				case ElementType.Char:
					return "uint16_t";
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
				case ElementType.GenericInst:
					{
						bool isValueType = tySig.IsValueType;
						return (isValueType ? null : "struct ") +
							GetTypeName(FindType(tySig)) +
							(isValueType ? null : "*");
					}

				default:
					throw new NotImplementedException();
			}

			return null;
		}

		public string GetTypeName(TypeX tyX)
		{
			string strName = tyX.GenTypeName;
			if (strName != null)
				return strName;

			strName = tyX.Def.IsValueType ? "stru_" : "cls_";

			string nameKey = tyX.GetNameKey();
			if (tyX.Def.DefinitionAssembly.IsCorLib())
				strName += EscapeName(nameKey);
			else
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(tyX.Def.Name);
				var genArgs = tyX.GenArgs;
				if (genArgs.IsCollectionValid())
				{
					sb.Append('_');
					Helper.TypeSigListName(sb, genArgs, false);
				};
				strName += NameHash(nameKey.GetHashCode()) + '_' + EscapeName(sb.ToString());
			}

			tyX.GenTypeName = strName;
			return strName;
		}

		private TypeX FindType(TypeSig tySig)
		{
			StringBuilder sb = new StringBuilder();
			Helper.TypeSigName(sb, tySig, false);
			return TypeMgr.GetTypeByName(sb.ToString());
		}

		public string GetMethodName(MethodX metX, string prefix)
		{
			string strName = metX.GenMethodName;
			if (strName == null)
			{
				int hashCode = metX.GetNameKey().GetHashCode() ^ metX.DeclType.GetNameKey().GetHashCode();
				strName = NameHash(hashCode) + '_' + EscapeName(metX.DeclType.Def.Name) + "__" + EscapeName(metX.Def.Name);
				metX.GenMethodName = strName;
			}
			return prefix + strName;
		}

		public string GetFieldName(FieldX fldX)
		{
			string strName = fldX.GenFieldName;
			if (strName == null)
			{
				if (fldX.DeclType.Def.DefinitionAssembly.IsCorLib())
					strName = "fld_" + EscapeName(fldX.Def.Name);
				else
					strName = "fld_" + NameHash((int)fldX.Def.Rid) + '_' + EscapeName(fldX.Def.Name);
			}
			return strName;
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

		private static string NameHash(int hashCode)
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
