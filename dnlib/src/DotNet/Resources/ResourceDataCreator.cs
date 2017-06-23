﻿// dnlib: See LICENSE.txt for more info

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace dnlib.DotNet.Resources {
	/// <summary>
	/// Creates resource data
	/// </summary>
	public class ResourceDataCreator {
	    readonly ModuleDefMD moduleMD;
		readonly Dictionary<string, UserResourceType> dict = new Dictionary<string, UserResourceType>(StringComparer.Ordinal);
		readonly Dictionary<string, string> asmNameToAsmFullName = new Dictionary<string, string>(StringComparer.Ordinal);

		/// <summary>
		/// Gets the owner module
		/// </summary>
		protected ModuleDef Module { get; }

	    /// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Owner module</param>
		public ResourceDataCreator(ModuleDef module) {
			this.Module = module;
			this.moduleMD = module as ModuleDefMD;
		}

		/// <summary>
		/// Gets number of user data types
		/// </summary>
		public int Count => dict.Count;

	    /// <summary>
		/// Create null data
		/// </summary>
		/// <returns></returns>
		public BuiltInResourceData CreateNull() {
			return new BuiltInResourceData(ResourceTypeCode.Null, null);
		}

		/// <summary>
		/// Creates <see cref="string"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(string value) {
			return new BuiltInResourceData(ResourceTypeCode.String, value);
		}

		/// <summary>
		/// Creates <see cref="bool"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(bool value) {
			return new BuiltInResourceData(ResourceTypeCode.Boolean, value);
		}

		/// <summary>
		/// Creates <see cref="char"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(char value) {
			return new BuiltInResourceData(ResourceTypeCode.Char, value);
		}

		/// <summary>
		/// Creates <see cref="byte"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(byte value) {
			return new BuiltInResourceData(ResourceTypeCode.Byte, value);
		}

		/// <summary>
		/// Creates <see cref="sbyte"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(sbyte value) {
			return new BuiltInResourceData(ResourceTypeCode.SByte, value);
		}

		/// <summary>
		/// Creates <see cref="short"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(short value) {
			return new BuiltInResourceData(ResourceTypeCode.Int16, value);
		}

		/// <summary>
		/// Creates <see cref="ushort"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(ushort value) {
			return new BuiltInResourceData(ResourceTypeCode.UInt16, value);
		}

		/// <summary>
		/// Creates <see cref="int"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(int value) {
			return new BuiltInResourceData(ResourceTypeCode.Int32, value);
		}

		/// <summary>
		/// Creates <see cref="uint"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(uint value) {
			return new BuiltInResourceData(ResourceTypeCode.UInt32, value);
		}

		/// <summary>
		/// Creates <see cref="long"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(long value) {
			return new BuiltInResourceData(ResourceTypeCode.Int64, value);
		}

		/// <summary>
		/// Creates <see cref="ulong"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(ulong value) {
			return new BuiltInResourceData(ResourceTypeCode.UInt64, value);
		}

		/// <summary>
		/// Creates <see cref="float"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(float value) {
			return new BuiltInResourceData(ResourceTypeCode.Single, value);
		}

		/// <summary>
		/// Creates <see cref="double"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(double value) {
			return new BuiltInResourceData(ResourceTypeCode.Double, value);
		}

		/// <summary>
		/// Creates <see cref="decimal"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(decimal value) {
			return new BuiltInResourceData(ResourceTypeCode.Decimal, value);
		}

		/// <summary>
		/// Creates <see cref="DateTime"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(DateTime value) {
			return new BuiltInResourceData(ResourceTypeCode.DateTime, value);
		}

		/// <summary>
		/// Creates <see cref="TimeSpan"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(TimeSpan value) {
			return new BuiltInResourceData(ResourceTypeCode.TimeSpan, value);
		}

		/// <summary>
		/// Creates <see cref="byte"/> array data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData Create(byte[] value) {
			return new BuiltInResourceData(ResourceTypeCode.ByteArray, value);
		}

		/// <summary>
		/// Creates <see cref="Stream"/> data
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		public BuiltInResourceData CreateStream(byte[] value) {
			return new BuiltInResourceData(ResourceTypeCode.Stream, value);
		}

		/// <summary>
		/// Creates serialized data
		/// </summary>
		/// <param name="value">Serialized data</param>
		/// <param name="type">Type of serialized data</param>
		/// <returns></returns>
		public BinaryResourceData CreateSerialized(byte[] value, UserResourceType type) {
			return new BinaryResourceData(CreateUserResourceType(type.Name, true), value);
		}

		/// <summary>
		/// Creates serialized data
		/// </summary>
		/// <param name="value">Serialized data</param>
		/// <returns></returns>
		public BinaryResourceData CreateSerialized(byte[] value) {
            if (!GetSerializedTypeAndAssemblyName(value, out string assemblyName, out string typeName))
                throw new ApplicationException("Could not get serialized type name");
            string fullName = $"{typeName}, {assemblyName}";
			return new BinaryResourceData(CreateUserResourceType(fullName), value);
		}

		sealed class MyBinder : SerializationBinder {
			public class OkException : Exception {
				public string AssemblyName { get; set; }
				public string TypeName { get; set; }
			}

			public override Type BindToType(string assemblyName, string typeName) {
				throw new OkException {
					AssemblyName = assemblyName,
					TypeName = typeName,
				};
			}
		}

		bool GetSerializedTypeAndAssemblyName(byte[] value, out string assemblyName, out string typeName) {
			try {
                var formatter = new BinaryFormatter()
                {
                    Binder = new MyBinder()
                };
                formatter.Deserialize(new MemoryStream(value));
			}
			catch (MyBinder.OkException ex) {
				assemblyName = ex.AssemblyName;
				typeName = ex.TypeName;
				return true;
			}
			catch {
			}

			assemblyName = null;
			typeName = null;
			return false;
		}

		/// <summary>
		/// Creates a user type. If the type already exists, the existing value is returned.
		/// </summary>
		/// <param name="fullName">Full name of type</param>
		/// <returns></returns>
		public UserResourceType CreateUserResourceType(string fullName) {
			return CreateUserResourceType(fullName, false);
		}

		/// <summary>
		/// Creates a user type. If the type already exists, the existing value is returned.
		/// </summary>
		/// <param name="fullName">Full name of type</param>
		/// <param name="useFullName">Use <paramref name="fullName"/> without converting it to a
		/// type in an existing assembly reference</param>
		/// <returns></returns>
		UserResourceType CreateUserResourceType(string fullName, bool useFullName) {
            if (dict.TryGetValue(fullName, out UserResourceType type))
                return type;

            var newFullName = useFullName ? fullName : GetRealTypeFullName(fullName);
			type = new UserResourceType(newFullName, ResourceTypeCode.UserTypes + dict.Count);
			dict[fullName] = type;
			dict[newFullName] = type;
			return type;
		}

		string GetRealTypeFullName(string fullName) {
			var tr = TypeNameParser.ParseReflection(Module, fullName, null);
			if (tr == null)
				return fullName;
			var asmRef = tr.DefinitionAssembly;
			if (asmRef == null)
				return fullName;

			var newFullName = fullName;

			string assemblyName = GetRealAssemblyName(asmRef);
			if (!string.IsNullOrEmpty(assemblyName))
				newFullName = $"{tr.ReflectionFullName}, {assemblyName}";

			return newFullName;
		}

		string GetRealAssemblyName(IAssembly asm) {
			string assemblyName = asm.FullName;
            if (!asmNameToAsmFullName.TryGetValue(assemblyName, out string newAsmName))
                asmNameToAsmFullName[assemblyName] = newAsmName = TryGetRealAssemblyName(asm);
            return newAsmName;
		}

		string TryGetRealAssemblyName(IAssembly asm) {
			var simpleName = asm.Name;
			if (simpleName == Module.CorLibTypes.AssemblyRef.Name)
				return Module.CorLibTypes.AssemblyRef.FullName;

		    var asmRef = moduleMD?.GetAssemblyRef(simpleName);
		    return asmRef != null ? asmRef.FullName : GetAssemblyFullName(simpleName);
		}

		/// <summary>
		/// Converts an assembly simple name (eg. mscorlib) to the full name of the assembly,
		/// which includes the version, public key token, etc. Returns <c>null</c> if it's
		/// unknown.
		/// </summary>
		/// <param name="simpleName">Simple name of assembly</param>
		/// <returns></returns>
		protected virtual string GetAssemblyFullName(string simpleName) {
			return null;
		}

		/// <summary>
		/// Gets all types sorted by <see cref="UserResourceType"/>
		/// </summary>
		/// <returns></returns>
		public List<UserResourceType> GetSortedTypes() {
			var list = new List<UserResourceType>(dict.Values);
			list.Sort((a, b) => ((int)a.Code).CompareTo((int)b.Code));
			return list;
		}
	}
}
