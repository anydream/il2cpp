using System;

namespace CodeGenTester
{
	class TestClassAttribute : Attribute
	{
		public string Result;

		public TestClassAttribute(string result)
		{
			Result = result;
		}
	}

	[TestClass(@"[CppUnit_0.h]
#pragma once
#include ""il2cpp.h""
// object, v4.0.30319
struct cls_0_System_Object
{
};
// CodeGenTester.TestBasicInst, v4.0.30319
struct cls_1_CodeGenTester_TestBasicInst : cls_0_System_Object
{
};
// static void CodeGenTester.TestBasicInst::Entry()
void met_3_CodeGenTester_TestBasicInst__Entry();
// static int CodeGenTester.TestBasicInst::Fibonacci(int)
int32_t met_2_CodeGenTester_TestBasicInst__Fibonacci(int32_t arg_0);

[CppUnit_0.cpp]
#include ""CppUnit_0.h""
// static void CodeGenTester.TestBasicInst::Entry()
void met_3_CodeGenTester_TestBasicInst__Entry()
{
	// locals
	int32_t loc_0;
	int32_t loc_1;

	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// nop
	// ldc.i4.0
	tmp_0_i4 = 0;
	// stloc.0
	loc_0 = tmp_0_i4;
	// br.s label_13
	goto label_13;
	// nop
label_4:
	// ldloc.0
	tmp_0_i4 = loc_0;
	// call static int CodeGenTester.TestBasicInst::Fibonacci(int)
	tmp_0_i4 = met_2_CodeGenTester_TestBasicInst__Fibonacci(tmp_0_i4);
	// pop
	// nop
	// ldloc.0
	tmp_0_i4 = loc_0;
	// ldc.i4.1
	tmp_1_i4 = 1;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stloc.0
	loc_0 = tmp_0_i4;
	// ldloc.0
label_13:
	tmp_0_i4 = loc_0;
	// ldc.i4.s 15
	tmp_1_i4 = 15;
	// clt
	tmp_0_i4 = tmp_0_i4 < tmp_1_i4 ? 1 : 0;
	// stloc.1
	loc_1 = tmp_0_i4;
	// ldloc.1
	tmp_0_i4 = loc_1;
	// brtrue.s label_4
	if (tmp_0_i4) goto label_4;
	// ret
	return;
}
// static int CodeGenTester.TestBasicInst::Fibonacci(int)
int32_t met_2_CodeGenTester_TestBasicInst__Fibonacci(int32_t arg_0)
{
	// locals
	int32_t loc_0;
	int32_t loc_1;
	int32_t loc_2;
	int32_t loc_3;
	int32_t loc_4;
	int32_t loc_5;

	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// nop
	// ldc.i4.0
	tmp_0_i4 = 0;
	// stloc.0
	loc_0 = tmp_0_i4;
	// ldc.i4.1
	tmp_0_i4 = 1;
	// stloc.1
	loc_1 = tmp_0_i4;
	// ldc.i4.0
	tmp_0_i4 = 0;
	// stloc.2
	loc_2 = tmp_0_i4;
	// br.s label_22
	goto label_22;
	// nop
label_8:
	// ldloc.0
	tmp_0_i4 = loc_0;
	// stloc.3
	loc_3 = tmp_0_i4;
	// ldloc.1
	tmp_0_i4 = loc_1;
	// stloc.0
	loc_0 = tmp_0_i4;
	// ldloc.3
	tmp_0_i4 = loc_3;
	// ldloc.1
	tmp_1_i4 = loc_1;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stloc.1
	loc_1 = tmp_0_i4;
	// nop
	// ldloc.2
	tmp_0_i4 = loc_2;
	// ldc.i4.1
	tmp_1_i4 = 1;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stloc.2
	loc_2 = tmp_0_i4;
	// ldloc.2
label_22:
	tmp_0_i4 = loc_2;
	// ldarg.0
	tmp_1_i4 = arg_0;
	// clt
	tmp_0_i4 = tmp_0_i4 < tmp_1_i4 ? 1 : 0;
	// stloc.s loc_4
	loc_4 = tmp_0_i4;
	// ldloc.s loc_4
	tmp_0_i4 = loc_4;
	// brtrue.s label_8
	if (tmp_0_i4) goto label_8;
	// ldloc.0
	tmp_0_i4 = loc_0;
	// stloc.s loc_5
	loc_5 = tmp_0_i4;
	// br.s label_31
	goto label_31;
	// ldloc.s loc_5
label_31:
	tmp_0_i4 = loc_5;
	// ret
	return tmp_0_i4;
}

")]
	class TestBasicInst
	{
		public static int Fibonacci(int n)
		{
			int a = 0;
			int b = 1;

			for (int i = 0; i < n; i++)
			{
				int temp = a;
				a = b;
				b = temp + b;
			}
			return a;
		}

		static void Entry()
		{
			for (int i = 0; i < 15; ++i)
			{
				Fibonacci(i);
			}
		}
	}

	[TestClass(@"[CppUnit_0.h]
#pragma once
#include ""il2cpp.h""
// object, v4.0.30319
struct cls_0_System_Object
{
};
// CodeGenTester.TestBasicTypes, v4.0.30319
struct cls_1_CodeGenTester_TestBasicTypes : cls_0_System_Object
{
};
// static void CodeGenTester.TestBasicTypes::Entry()
void met_3_CodeGenTester_TestBasicTypes__Entry();
// static char CodeGenTester.TestBasicTypes::Foo(sbyte,byte,short,ushort,int,uint,long,ulong,float,double,char,bool,[valuetype]System.IntPtr,[valuetype]System.UIntPtr,object)
uint16_t met_2_CodeGenTester_TestBasicTypes__Foo(int8_t arg_0, uint8_t arg_1, int16_t arg_2, uint16_t arg_3, int32_t arg_4, uint32_t arg_5, int64_t arg_6, uint64_t arg_7, float arg_8, double arg_9, uint16_t arg_10, int32_t arg_11, intptr_t arg_12, uintptr_t arg_13, struct cls_0_System_Object* arg_14);

[CppUnit_0.cpp]
#include ""CppUnit_0.h""
// static void CodeGenTester.TestBasicTypes::Entry()
void met_3_CodeGenTester_TestBasicTypes__Entry()
{
	// locals
	uint16_t loc_0;

	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;
	int32_t tmp_2_i4;
	int32_t tmp_3_i4;
	int32_t tmp_4_i4;
	int32_t tmp_5_i4;
	int64_t tmp_6_i8;
	int32_t tmp_7_i4;
	int64_t tmp_7_i8;
	float tmp_8_r4;
	double tmp_9_r8;
	int32_t tmp_10_i4;
	int32_t tmp_11_i4;
	int32_t tmp_12_i4;
	void* tmp_12_ptr;
	int32_t tmp_13_i4;
	void* tmp_13_ptr;
	void* tmp_14_obj;

	// nop
	// ldc.i4.s -128
	tmp_0_i4 = -128;
	// ldc.i4.0
	tmp_1_i4 = 0;
	// ldc.i4 -32768
	tmp_2_i4 = -32768;
	// ldc.i4.0
	tmp_3_i4 = 0;
	// ldc.i4 -2147483648
	tmp_4_i4 = -2147483648;
	// ldc.i4.0
	tmp_5_i4 = 0;
	// ldc.i8 -9223372036854775808
	tmp_6_i8 = -9223372036854775808;
	// ldc.i4.0
	tmp_7_i4 = 0;
	// conv.i8
	tmp_7_i8 = tmp_7_i4;
	// ldc.r4 -3.402823E+38
	tmp_8_r4 = -3.402823E+38;
	// ldc.r8 -1.79769313486232E+308
	tmp_9_r8 = -1.79769313486232E+308;
	// ldc.i4.0
	tmp_10_i4 = 0;
	// ldc.i4.0
	tmp_11_i4 = 0;
	// ldc.i4.0
	tmp_12_i4 = 0;
	// conv.i
	tmp_12_ptr = (void*)(intptr_t)tmp_12_i4;
	// ldc.i4.0
	tmp_13_i4 = 0;
	// conv.u
	tmp_13_ptr = (void*)(uintptr_t)tmp_13_i4;
	// ldnull
	tmp_14_obj = nullptr;
	// call static char CodeGenTester.TestBasicTypes::Foo(sbyte,byte,short,ushort,int,uint,long,ulong,float,double,char,bool,[valuetype]System.IntPtr,[valuetype]System.UIntPtr,object)
	tmp_0_i4 = met_2_CodeGenTester_TestBasicTypes__Foo((int8_t)tmp_0_i4, (uint8_t)tmp_1_i4, (int16_t)tmp_2_i4, (uint16_t)tmp_3_i4, tmp_4_i4, (uint32_t)tmp_5_i4, tmp_6_i8, (uint64_t)tmp_7_i8, tmp_8_r4, tmp_9_r8, (uint16_t)tmp_10_i4, tmp_11_i4, (intptr_t)tmp_12_ptr, (uintptr_t)tmp_13_ptr, (struct cls_0_System_Object*)tmp_14_obj);
	// stloc.0
	loc_0 = (uint16_t)tmp_0_i4;
	// ldc.i4.s 127
	tmp_0_i4 = 127;
	// ldc.i4 255
	tmp_1_i4 = 255;
	// ldc.i4 32767
	tmp_2_i4 = 32767;
	// ldc.i4 65535
	tmp_3_i4 = 65535;
	// ldc.i4 2147483647
	tmp_4_i4 = 2147483647;
	// ldc.i4.m1
	tmp_5_i4 = -1;
	// ldc.i8 9223372036854775807
	tmp_6_i8 = 9223372036854775807;
	// ldc.i4.m1
	tmp_7_i4 = -1;
	// conv.i8
	tmp_7_i8 = tmp_7_i4;
	// ldc.r4 3.402823E+38
	tmp_8_r4 = 3.402823E+38;
	// ldc.r8 1.79769313486232E+308
	tmp_9_r8 = 1.79769313486232E+308;
	// ldc.i4 65535
	tmp_10_i4 = 65535;
	// ldc.i4.1
	tmp_11_i4 = 1;
	// ldc.i4.0
	tmp_12_i4 = 0;
	// conv.i
	tmp_12_ptr = (void*)(intptr_t)tmp_12_i4;
	// ldc.i4.0
	tmp_13_i4 = 0;
	// conv.u
	tmp_13_ptr = (void*)(uintptr_t)tmp_13_i4;
	// ldnull
	tmp_14_obj = nullptr;
	// call static char CodeGenTester.TestBasicTypes::Foo(sbyte,byte,short,ushort,int,uint,long,ulong,float,double,char,bool,[valuetype]System.IntPtr,[valuetype]System.UIntPtr,object)
	tmp_0_i4 = met_2_CodeGenTester_TestBasicTypes__Foo((int8_t)tmp_0_i4, (uint8_t)tmp_1_i4, (int16_t)tmp_2_i4, (uint16_t)tmp_3_i4, tmp_4_i4, (uint32_t)tmp_5_i4, tmp_6_i8, (uint64_t)tmp_7_i8, tmp_8_r4, tmp_9_r8, (uint16_t)tmp_10_i4, tmp_11_i4, (intptr_t)tmp_12_ptr, (uintptr_t)tmp_13_ptr, (struct cls_0_System_Object*)tmp_14_obj);
	// stloc.0
	loc_0 = (uint16_t)tmp_0_i4;
	// ret
	return;
}
// static char CodeGenTester.TestBasicTypes::Foo(sbyte,byte,short,ushort,int,uint,long,ulong,float,double,char,bool,[valuetype]System.IntPtr,[valuetype]System.UIntPtr,object)
uint16_t met_2_CodeGenTester_TestBasicTypes__Foo(int8_t arg_0, uint8_t arg_1, int16_t arg_2, uint16_t arg_3, int32_t arg_4, uint32_t arg_5, int64_t arg_6, uint64_t arg_7, float arg_8, double arg_9, uint16_t arg_10, int32_t arg_11, intptr_t arg_12, uintptr_t arg_13, struct cls_0_System_Object* arg_14)
{
	// locals
	uint16_t loc_0;

	// temps
	int32_t tmp_0_i4;

	// nop
	// ldarg.s arg_10
	tmp_0_i4 = arg_10;
	// stloc.0
	loc_0 = (uint16_t)tmp_0_i4;
	// br.s label_4
	goto label_4;
	// ldloc.0
label_4:
	tmp_0_i4 = loc_0;
	// ret
	return tmp_0_i4;
}

")]
	class TestBasicTypes
	{
		private static char Foo(
			sbyte i8,
			byte u8,
			short i16,
			ushort u16,
			int i32,
			uint u32,
			long i64,
			ulong u64,
			float f32,
			double f64,
			char ch,
			bool bl,
			IntPtr ptr,
			UIntPtr uptr,
			object obj)
		{
			return ch;
		}

		static unsafe void Entry()
		{
			char ch = Foo(
				sbyte.MinValue,
				byte.MinValue,
				short.MinValue,
				ushort.MinValue,
				int.MinValue,
				uint.MinValue,
				long.MinValue,
				ulong.MinValue,
				float.MinValue,
				double.MinValue,
				char.MinValue,
				false,
				default(IntPtr),
				default(UIntPtr),
				null);

			ch = Foo(
				sbyte.MaxValue,
				byte.MaxValue,
				short.MaxValue,
				ushort.MaxValue,
				int.MaxValue,
				uint.MaxValue,
				long.MaxValue,
				ulong.MaxValue,
				float.MaxValue,
				double.MaxValue,
				char.MaxValue,
				true,
				default(IntPtr),
				default(UIntPtr),
				null);
		}
	}

	class Program
	{
		static void Main()
		{

		}
	}
}
