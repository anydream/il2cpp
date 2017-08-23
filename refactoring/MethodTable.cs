using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;

namespace il2cpp
{
	// 虚表槽
	internal class VirtualSlot
	{
		// 入口签名集合
		public readonly HashSet<string> EntryNames = new HashSet<string>();

		// 实现方法定义
		public MethodDef ImplMethod;

		public VirtualSlot()
		{
		}

		public VirtualSlot(HashSet<string> entryNames, MethodDef impl)
		{
			EntryNames.UnionWith(entryNames);
			ImplMethod = impl;
		}
	}

	// 方法表
	internal class MethodTable
	{
		// 虚表槽覆盖映射. 展开的方法签名:虚表槽
		private readonly Dictionary<string, VirtualSlot> VSlotMap = new Dictionary<string, VirtualSlot>();

		// 展平的虚表. 入口类型:{入口签名:方法定义}
		private readonly Dictionary<string, Dictionary<string, MethodDef>> VTable =
			new Dictionary<string, Dictionary<string, MethodDef>>();

		// 解析方法绑定
		public void ResolveBindings(TypeX tyX, List<string> metSigList)
		{
			if (tyX.Def.BaseType != null)
			{
				// 解析原始基类型
				TypeX baseTyX = tyX.Context.TypeMgr.ResolveITypeDefOrRef(tyX.Def.BaseType, null);

				// 继承基类的方法表
				DerivedFrom(baseTyX.GetExpandedMethodTable());
			}


			string entryTySigName = NameManager.TypeXSigName(tyX);

			// 显式覆盖方法列表
			List<MethodDef> explicitOverrides = new List<MethodDef>();

			StringBuilder sb = new StringBuilder();
			// 解析虚方法绑定
			for (int i = 0; i < metSigList.Count; ++i)
			{
				string cmpMetSigName = metSigList[i];
				MethodDef metDef = tyX.GetInstanceMethodDef(i);

				// 构建虚入口名称
				string entrySigName = entryTySigName + '|' + tyX.GetNotExpandedSigName(i);

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
						NewSlot(cmpMetSigName, entrySigName, metDef);
					}
					else
					{
						Debug.Assert(metDef.IsReuseSlot);
						ReuseSlot(cmpMetSigName, entrySigName, metDef);
					}
				}
				else
				{
					// 普通方法视作新建虚槽
					NewSlotChecked(cmpMetSigName, entrySigName, metDef);
				}
			}

			// 解析接口
			Dictionary<string, HashSet<string>> infSigMap = new Dictionary<string, HashSet<string>>();
			CollectInterfaceSigNames(tyX.Context.TypeMgr, tyX.Def.Interfaces, infSigMap);

			// 没有对应实现的接口
			Dictionary<string, HashSet<string>> noImplSigMap = new Dictionary<string, HashSet<string>>();

			foreach (var kv in infSigMap)
			{
				string cmpMetSigName = kv.Key;
				foreach (string entrySigName in kv.Value)
				{
					if (VSlotMap.TryGetValue(cmpMetSigName, out var vslot))
					{
						// 添加接口入口到虚表槽的入口集合
						vslot.EntryNames.Add(entrySigName);
					}
					else
					{
						// 记录没有对应实现的接口签名
						if (!noImplSigMap.TryGetValue(cmpMetSigName, out var entrySet))
						{
							entrySet = new HashSet<string>();
							noImplSigMap.Add(cmpMetSigName, entrySet);
						}
						entrySet.Add(entrySigName);
					}
				}
			}

			// 展平虚表
			/*foreach (var kv in VSlotMap)
			{
				string metSigName = kv.Key;
				var vslot = kv.Value;
			}*/

			// 处理显式覆盖方法

			// 检查是否存在没有对应实现的接口
		}

		private void SetVTable(string entryType, string metSigName, MethodDef metDef)
		{
			if (!VTable.TryGetValue(entryType, out var sigMap))
			{
				sigMap = new Dictionary<string, MethodDef>();
				VTable.Add(entryType, sigMap);
			}
			sigMap[metSigName] = metDef;
		}

		private void DerivedFrom(MethodTable mtable)
		{
			// 拷贝虚表槽映射
			foreach (var kv in mtable.VSlotMap)
			{
				VSlotMap.Add(kv.Key, new VirtualSlot(kv.Value.EntryNames, kv.Value.ImplMethod));
			}

			// 拷贝展平的虚表
			foreach (var kv in mtable.VTable)
			{
				VTable.Add(kv.Key, new Dictionary<string, MethodDef>(kv.Value));
			}
		}

		private void NewSlot(string cmpMetSigName, string entrySigName, MethodDef metDef)
		{
			var vslot = VSlotMap[cmpMetSigName] = new VirtualSlot();
			vslot.EntryNames.Add(entrySigName);
			vslot.ImplMethod = metDef;
		}

		private void NewSlotChecked(string cmpMetSigName, string entrySigName, MethodDef metDef)
		{
			var vslot = new VirtualSlot();
			VSlotMap.Add(cmpMetSigName, vslot);
			vslot.EntryNames.Add(entrySigName);
			vslot.ImplMethod = metDef;
		}

		private void ReuseSlot(string cmpMetSigName, string entrySigName, MethodDef metDef)
		{
			bool res = VSlotMap.TryGetValue(cmpMetSigName, out var vslot);
			Debug.Assert(res);
			vslot.EntryNames.Add(entrySigName);
			vslot.ImplMethod = metDef;
		}

		private void CollectInterfaceSigNames(
			TypeManager typeMgr,
			IList<InterfaceImpl> infs,
			Dictionary<string, HashSet<string>> sigNameMap)
		{
			foreach (var inf in infs)
			{
				// 解析原始接口类型
				TypeX infTyX = typeMgr.ResolveITypeDefOrRef(inf.Interface, null);
				CollectInterfaceSigNames(typeMgr, infTyX.Def.Interfaces, sigNameMap);

				string entryTySigName = NameManager.TypeXSigName(infTyX);

				var cmpMetSigNames = infTyX.GetExpandedSigNames();
				for (int i = 0; i < cmpMetSigNames.Count; ++i)
				{
					string cmpMetSigName = cmpMetSigNames[i];
					string entrySigName = entryTySigName + '|' + infTyX.GetNotExpandedSigName(i);

					if (!sigNameMap.TryGetValue(cmpMetSigName, out var entrySet))
					{
						entrySet = new HashSet<string>();
						sigNameMap.Add(cmpMetSigName, entrySet);
					}
					entrySet.Add(entrySigName);
				}
			}
		}
	}
}
