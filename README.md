il2cpp  *A MSIL/C# to C++ converter*
===

> 「  在享受C#开发效率的同时, 获得C++编译器往死里优化的执行速度  」

![alt tag](https://github.com/anydream/il2cpp/raw/master/il2cpp-schematic.png)

## How to test
  - Pre-requirements:
    1. Windows7 and later, 64-bit system;
    2. Visual Studio **2017**, C# and C++ desktop dev environments;
  - Open ``il2cpp.sln``;
  - Set ``test`` as startup project;
  - Run.

## 已实现的特性
- [x] 类型/方法/字段的引用分析, 提取最小依赖子集
- [x] 虚方法调用与虚表绑定
- [x] 接口与基类方法的显式重写
- [x] 协变/逆变分析
- [x] 内嵌保守式垃圾回收器
- [x] 静态构造函数
- [x] try/catch/finally/fault 异常块的解析与代码生成
- [x] 一维数组/多维数组的代码生成
- [x] 枚举类型处理
- [x] 字符串常量代码生成
- [x] 可空类型代码生成
- [x] 显式字段布局和结构体长度
- [x] 方法委托
- [x] C++ 代码编译工具
- [x] 数组读写指令
- [x] 栈操作指令
- [x] 常量载入指令
- [x] 方法调用指令
- [x] 变量/参数/字段读写指令
- [x] 条件与分支指令
- [x] 比较指令
- [x] 数值转换指令
- [x] 数值运算指令
- [x] 引用和值类型对象操作指令
- [x] 指针读写指令
- [x] 异常处理指令
- [x] 装箱/拆箱指令
- [x] 溢出检查指令

## 明确不支持的特性
- [x] 运行时创建新类型 (TypeBuilder.CreateType)
- [x] 运行时加载 .NET DLL 并实例化其中的类型
- [x] 运行时实例化不存在的泛型展开 (只支持编译期存在的泛型展开)
- [x] 运行时增加/删除/修改反射信息
- [x] 递归的泛型参数类型展开
- [x] Marshaling 非静态方法委托


正在如火如荼地开发中...

求 star 求测试~~
