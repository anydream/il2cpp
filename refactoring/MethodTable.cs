using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;

namespace il2cpp
{
	// 虚表槽
	internal class VirtualSlot
	{
		// 入口类型集合
		public readonly HashSet<string> EntryTypes = new HashSet<string>();
		// 实现方法定义
		public MethodDef ImplMethod;
	}

	// 方法表
	internal class MethodTable
	{
		// 虚表槽覆盖映射. 展开的方法签名:虚表槽
		private readonly Dictionary<string, VirtualSlot> VSlotMap = new Dictionary<string, VirtualSlot>();
		// 展平的虚表. 入口类型:{入口签名:方法定义}
		private readonly Dictionary<string, Dictionary<string, MethodDef>> VTable = new Dictionary<string, Dictionary<string, MethodDef>>();

		// 解析方法绑定
		public void ResolveBindings(TypeX tyX, Dictionary<string, MethodDef> metDefMap)
		{
			if (tyX.BaseType != null)
			{
				// 继承基类的方法表
				DerivedFrom(tyX.BaseType.GetExpandedMethodTable());
			}

			// 当前类型入口名
			string entryTypeName = tyX.GetNameKey();

			// 显式覆盖方法列表
			List<MethodDef> explicitOverrides = new List<MethodDef>();

			// 解析虚方法绑定
			foreach (var kv in metDefMap)
			{
				string metSigName = kv.Key;
				MethodDef metDef = kv.Value;

				if (metDef.HasOverrides)
				{
					Debug.Assert(!metDef.IsVirtual);
					// 延迟处理显式覆盖方法
					explicitOverrides.Add(metDef);
				}
				else if (metDef.IsVirtual)
				{
					if (metDef.IsNewSlot)
					{
						NewSlot(metSigName, entryTypeName, metDef);
					}
					else
					{
						Debug.Assert(metDef.IsReuseSlot);
						ReuseSlot(metSigName, entryTypeName, metDef);
					}
				}
				else
				{
					// 普通方法视作新建虚槽
					NewSlot(metSigName, entryTypeName, metDef);
				}
			}

			// 解析接口
			Dictionary<string, HashSet<string>> infSigMap = new Dictionary<string, HashSet<string>>();
			CollectInterfaceSigNames(tyX.Interfaces, infSigMap);

			// 没有对应实现的接口
			Dictionary<string, HashSet<string>> noImplSigMap = new Dictionary<string, HashSet<string>>();

			foreach (var kv in infSigMap)
			{
				string infEntryType = kv.Key;
				foreach (string infSigName in kv.Value)
				{
					if (VSlotMap.TryGetValue(infSigName, out var vslot))
					{
						// 添加接口类型到虚表槽的入口类型
						vslot.EntryTypes.Add(infEntryType);
					}
					else
					{
						// 记录没有对应实现的接口签名
						if (!noImplSigMap.TryGetValue(infEntryType, out var sigSet))
						{
							sigSet = new HashSet<string>();
							noImplSigMap.Add(infEntryType, sigSet);
						}
						sigSet.Add(infSigName);
					}
				}
			}

			// 处理显式覆盖方法

			// 检查是否存在没有对应实现的接口
		}

		private void DerivedFrom(MethodTable mtable)
		{
			// 拷贝虚槽
			// 拷贝展平的虚表
		}

		private void NewSlot(string metSigName, string entryTypeName, MethodDef metDef)
		{
			var vslot = VSlotMap[metSigName] = new VirtualSlot();
			vslot.EntryTypes.Add(entryTypeName);
			vslot.ImplMethod = metDef;
		}

		private void ReuseSlot(string metSigName, string entryTypeName, MethodDef metDef)
		{
			bool res = VSlotMap.TryGetValue(metSigName, out var vslot);
			Debug.Assert(res);
			vslot.EntryTypes.Add(entryTypeName);
			vslot.ImplMethod = metDef;
		}

		private void CollectInterfaceSigNames(IList<TypeX> infs, Dictionary<string, HashSet<string>> sigNameMap)
		{
			foreach (var inf in infs)
			{
				CollectInterfaceSigNames(inf.Interfaces, sigNameMap);

				string entryTypeName = inf.GetNameKey();
				if (!sigNameMap.TryGetValue(entryTypeName, out var sigSet))
				{
					sigSet = new HashSet<string>();
					sigNameMap.Add(entryTypeName, sigSet);
				}
				sigSet.UnionWith(inf.GetExpandedSigNames());
			}
		}
	}
}
