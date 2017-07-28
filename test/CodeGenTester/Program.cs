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
    // call System.Int32 CodeGenTester.TestBasicInst::Fibonacci(System.Int32)
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

	class Program
	{
		static void Main()
		{

		}
	}
}
