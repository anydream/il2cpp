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
// object
struct cls_0_System_Object
{
	uint32_t objectTypeID;
};
// CodeGenTester.TestBasicInst
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
	int32_t loc_0 = 0;
	int32_t loc_1 = 0;

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
	int32_t loc_0 = 0;
	int32_t loc_1 = 0;
	int32_t loc_2 = 0;
	int32_t loc_3 = 0;
	int32_t loc_4 = 0;
	int32_t loc_5 = 0;

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
// object
struct cls_0_System_Object
{
	uint32_t objectTypeID;
};
// CodeGenTester.TestBasicTypes
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
	uint16_t loc_0 = 0;

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
	uint16_t loc_0 = 0;

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
	return (uint16_t)tmp_0_i4;
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

	[TestClass(@"[CppUnit_0.h]
#pragma once
#include ""il2cpp.h""
// object
struct cls_0_System_Object
{
	uint32_t objectTypeID;
};
// void object::.ctor()
void met_1_System_Object___ctor(struct cls_0_System_Object* arg_0);
// void object::Finalize()
void met_2_System_Object__Finalize(struct cls_0_System_Object* arg_0);
void* vftn_2_System_Object__Finalize(uint32_t typeID);
void vmet_2_System_Object__Finalize(struct cls_0_System_Object* arg_0);
// CodeGenTester.TestStatic/Cls
struct cls_8_CodeGenTester_TestStatic_Cls : cls_0_System_Object
{
	// uint f_uint
	uint32_t fld_a_f_uint;
};
// static uint CodeGenTester.TestStatic/Cls::s_uint
extern uint32_t sfld_7_CodeGenTester_TestStatic_Cls__s_uint;
// static byte CodeGenTester.TestStatic/Cls::s_byte
extern uint8_t sfld_b_CodeGenTester_TestStatic_Cls__s_byte;
extern int8_t onceflag_cls_8_CodeGenTester_TestStatic_Cls;
extern uintptr_t locktid_cls_8_CodeGenTester_TestStatic_Cls;
// static void CodeGenTester.TestStatic/Cls::.cctor()
void met_6_CodeGenTester_TestStatic_Cls___cctor();
// void CodeGenTester.TestStatic/Cls::.ctor()
void met_9_CodeGenTester_TestStatic_Cls___ctor(struct cls_8_CodeGenTester_TestStatic_Cls* arg_0);
// void CodeGenTester.TestStatic/Cls::Setup()
void met_e_CodeGenTester_TestStatic_Cls__Setup(struct cls_8_CodeGenTester_TestStatic_Cls* arg_0);
// CodeGenTester.TestStatic
struct cls_3_CodeGenTester_TestStatic : cls_0_System_Object
{
};
// static uint CodeGenTester.TestStatic::s_uint
extern uint32_t sfld_4_CodeGenTester_TestStatic__s_uint;
// static byte CodeGenTester.TestStatic::s_byte
extern uint8_t sfld_5_CodeGenTester_TestStatic__s_byte;
extern int8_t onceflag_cls_3_CodeGenTester_TestStatic;
extern uintptr_t locktid_cls_3_CodeGenTester_TestStatic;
// static uint CodeGenTester.TestStatic::Entry()
uint32_t met_c_CodeGenTester_TestStatic__Entry();
// static void CodeGenTester.TestStatic::.cctor()
void met_d_CodeGenTester_TestStatic___cctor();
void il2cpp_InitStaticVars();

[CppUnit_0.cpp]
#include ""CppUnit_0.h""
// void object::.ctor()
void met_1_System_Object___ctor(struct cls_0_System_Object* arg_0)
{
	// ret
	return;
}
// void object::Finalize()
void met_2_System_Object__Finalize(struct cls_0_System_Object* arg_0)
{
	// ret
	return;
}
void* vftn_2_System_Object__Finalize(uint32_t typeID)
{
	switch (typeID)
	{
		case 1: return (void*)&met_2_System_Object__Finalize;
	}
	return nullptr;
}
void vmet_2_System_Object__Finalize(struct cls_0_System_Object* arg_0)
{
	void* pfn = vftn_2_System_Object__Finalize(arg_0->objectTypeID);
	((void(*)(struct cls_0_System_Object*))pfn)(arg_0);
}
// static uint CodeGenTester.TestStatic/Cls::s_uint
uint32_t sfld_7_CodeGenTester_TestStatic_Cls__s_uint;
// static byte CodeGenTester.TestStatic/Cls::s_byte
uint8_t sfld_b_CodeGenTester_TestStatic_Cls__s_byte;
int8_t onceflag_cls_8_CodeGenTester_TestStatic_Cls;
uintptr_t locktid_cls_8_CodeGenTester_TestStatic_Cls;
static void once_met_6_CodeGenTester_TestStatic_Cls___cctor()
{
	// temps
	int32_t tmp_0_i4;

	// ldc.i4 123456
	tmp_0_i4 = 123456;
	// stsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	sfld_7_CodeGenTester_TestStatic_Cls__s_uint = (uint32_t)tmp_0_i4;
	// nop
	// ldc.i4.s 42
	tmp_0_i4 = 42;
	// stsfld static byte CodeGenTester.TestStatic/Cls::s_byte
	sfld_b_CodeGenTester_TestStatic_Cls__s_byte = (uint8_t)tmp_0_i4;
	// ret
	return;
}
// static void CodeGenTester.TestStatic/Cls::.cctor()
void met_6_CodeGenTester_TestStatic_Cls___cctor()
{
	IL2CPP_CALL_ONCE(
		onceflag_cls_8_CodeGenTester_TestStatic_Cls,
		locktid_cls_8_CodeGenTester_TestStatic_Cls,
		once_met_6_CodeGenTester_TestStatic_Cls___cctor);
}
// void CodeGenTester.TestStatic/Cls::.ctor()
void met_9_CodeGenTester_TestStatic_Cls___ctor(struct cls_8_CodeGenTester_TestStatic_Cls* arg_0)
{
	met_6_CodeGenTester_TestStatic_Cls___cctor();

	// temps
	void* tmp_0_obj;
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// ldarg.0
	tmp_0_obj = arg_0;
	// ldc.i4 789
	tmp_1_i4 = 789;
	// stfld uint CodeGenTester.TestStatic/Cls::f_uint
	((cls_8_CodeGenTester_TestStatic_Cls*)tmp_0_obj)->fld_a_f_uint = (uint32_t)tmp_1_i4;
	// ldarg.0
	tmp_0_obj = arg_0;
	// call void object::.ctor()
	met_1_System_Object___ctor((struct cls_0_System_Object*)tmp_0_obj);
	// nop
	// nop
	// ldc.i4 123455
	tmp_0_i4 = 123455;
	// stsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	sfld_7_CodeGenTester_TestStatic_Cls__s_uint = (uint32_t)tmp_0_i4;
	// ldarg.0
	tmp_0_obj = arg_0;
	// call void CodeGenTester.TestStatic/Cls::Setup()
	met_e_CodeGenTester_TestStatic_Cls__Setup((struct cls_8_CodeGenTester_TestStatic_Cls*)tmp_0_obj);
	// nop
	// ret
	return;
}
// void CodeGenTester.TestStatic/Cls::Setup()
void met_e_CodeGenTester_TestStatic_Cls__Setup(struct cls_8_CodeGenTester_TestStatic_Cls* arg_0)
{
	// temps
	int32_t tmp_0_i4;

	// nop
	// ldc.i4 123456
	tmp_0_i4 = 123456;
	// stsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	sfld_7_CodeGenTester_TestStatic_Cls__s_uint = (uint32_t)tmp_0_i4;
	// ret
	return;
}
// static uint CodeGenTester.TestStatic::s_uint
uint32_t sfld_4_CodeGenTester_TestStatic__s_uint;
// static byte CodeGenTester.TestStatic::s_byte
uint8_t sfld_5_CodeGenTester_TestStatic__s_byte;
int8_t onceflag_cls_3_CodeGenTester_TestStatic;
uintptr_t locktid_cls_3_CodeGenTester_TestStatic;
// static uint CodeGenTester.TestStatic::Entry()
uint32_t met_c_CodeGenTester_TestStatic__Entry()
{
	met_d_CodeGenTester_TestStatic___cctor();

	// locals
	uint32_t loc_0 = 0;

	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;
	void* tmp_2_obj;
	int32_t tmp_2_i4;

	// nop
	// ldsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	met_6_CodeGenTester_TestStatic_Cls___cctor();
	tmp_0_i4 = (int32_t)sfld_7_CodeGenTester_TestStatic_Cls__s_uint;
	// ldsfld static uint CodeGenTester.TestStatic::s_uint
	tmp_1_i4 = (int32_t)sfld_4_CodeGenTester_TestStatic__s_uint;
	// newobj void CodeGenTester.TestStatic/Cls::.ctor()
	tmp_2_obj = il2cpp_New(sizeof(cls_8_CodeGenTester_TestStatic_Cls), 2);
	met_9_CodeGenTester_TestStatic_Cls___ctor((struct cls_8_CodeGenTester_TestStatic_Cls*)tmp_2_obj);
	// ldfld uint CodeGenTester.TestStatic/Cls::f_uint
	tmp_2_i4 = (int32_t)((cls_8_CodeGenTester_TestStatic_Cls*)tmp_2_obj)->fld_a_f_uint;
	// sub
	tmp_1_i4 = tmp_1_i4 - tmp_2_i4;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	met_6_CodeGenTester_TestStatic_Cls___cctor();
	sfld_7_CodeGenTester_TestStatic_Cls__s_uint = (uint32_t)tmp_0_i4;
	// ldsfld static byte CodeGenTester.TestStatic::s_byte
	tmp_0_i4 = sfld_5_CodeGenTester_TestStatic__s_byte;
	// ldsfld static uint CodeGenTester.TestStatic::s_uint
	tmp_1_i4 = (int32_t)sfld_4_CodeGenTester_TestStatic__s_uint;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// ldsfld static byte CodeGenTester.TestStatic/Cls::s_byte
	met_6_CodeGenTester_TestStatic_Cls___cctor();
	tmp_1_i4 = sfld_b_CodeGenTester_TestStatic_Cls__s_byte;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// ldsfld static uint CodeGenTester.TestStatic/Cls::s_uint
	met_6_CodeGenTester_TestStatic_Cls___cctor();
	tmp_1_i4 = (int32_t)sfld_7_CodeGenTester_TestStatic_Cls__s_uint;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stloc.0
	loc_0 = (uint32_t)tmp_0_i4;
	// br.s label_17
	goto label_17;
	// ldloc.0
label_17:
	tmp_0_i4 = loc_0;
	// ret
	return (uint32_t)tmp_0_i4;
}
static void once_met_d_CodeGenTester_TestStatic___cctor()
{
	// temps
	int32_t tmp_0_i4;

	// nop
	// ldc.i4.1
	tmp_0_i4 = 1;
	// stsfld static byte CodeGenTester.TestStatic::s_byte
	sfld_5_CodeGenTester_TestStatic__s_byte = (uint8_t)tmp_0_i4;
	// ret
	return;
}
// static void CodeGenTester.TestStatic::.cctor()
void met_d_CodeGenTester_TestStatic___cctor()
{
	IL2CPP_CALL_ONCE(
		onceflag_cls_3_CodeGenTester_TestStatic,
		locktid_cls_3_CodeGenTester_TestStatic,
		once_met_d_CodeGenTester_TestStatic___cctor);
}
void il2cpp_InitStaticVars()
{
	sfld_4_CodeGenTester_TestStatic__s_uint = 0;
	sfld_5_CodeGenTester_TestStatic__s_byte = 0;
	onceflag_cls_3_CodeGenTester_TestStatic = 0;
	locktid_cls_3_CodeGenTester_TestStatic = 0;
	sfld_7_CodeGenTester_TestStatic_Cls__s_uint = 0;
	sfld_b_CodeGenTester_TestStatic_Cls__s_byte = 0;
	onceflag_cls_8_CodeGenTester_TestStatic_Cls = 0;
	locktid_cls_8_CodeGenTester_TestStatic_Cls = 0;
}

")]
	class TestStatic
	{
		private static byte s_byte;
		private static uint s_uint;

		class Cls
		{
			public static byte s_byte;
			public static uint s_uint = 123456;
			public uint f_uint = 789;

			public Cls()
			{
				s_uint = 123455;
				Setup();
			}

			static Cls()
			{
				s_byte = 42;
			}

			private void Setup()
			{
				s_uint = 123456;
			}
		}

		static TestStatic()
		{
			s_byte = 1;
		}

		public static uint Entry()
		{
			Cls.s_uint += s_uint - new Cls().f_uint;
			return s_byte + s_uint + Cls.s_byte + Cls.s_uint;
		}
	}

	[TestClass(@"[CppUnit_0.h]
#pragma once
#include ""il2cpp.h""
// [valuetype]CodeGenTester.TestValueType/MyStru
struct stru_3_CodeGenTester_TestValueType_MyStru
{
	// float f_float
	float fld_5_f_float;
	// long f_long
	int64_t fld_6_f_long;
};
// void [valuetype]CodeGenTester.TestValueType/MyStru::.ctor(float,long)
void met_2_CodeGenTester_TestValueType_MyStru___ctor(stru_3_CodeGenTester_TestValueType_MyStru* arg_0, float arg_1, int64_t arg_2);
// object
struct cls_0_System_Object
{
	uint32_t objectTypeID;
};
// CodeGenTester.TestValueType
struct cls_1_CodeGenTester_TestValueType : cls_0_System_Object
{
};
// static bool CodeGenTester.TestValueType::Entry()
int32_t met_7_CodeGenTester_TestValueType__Entry();
// static [valuetype]CodeGenTester.TestValueType/MyStru CodeGenTester.TestValueType::Foo([valuetype]CodeGenTester.TestValueType/MyStru,[valuetype]CodeGenTester.TestValueType/MyStru&,[valuetype]CodeGenTester.TestValueType/MyStru&)
stru_3_CodeGenTester_TestValueType_MyStru met_4_CodeGenTester_TestValueType__Foo(stru_3_CodeGenTester_TestValueType_MyStru arg_0, stru_3_CodeGenTester_TestValueType_MyStru* arg_1, stru_3_CodeGenTester_TestValueType_MyStru* arg_2);
// System.ValueType
struct cls_8_System_ValueType : cls_0_System_Object
{
};

[CppUnit_0.cpp]
#include ""CppUnit_0.h""
// void [valuetype]CodeGenTester.TestValueType/MyStru::.ctor(float,long)
void met_2_CodeGenTester_TestValueType_MyStru___ctor(stru_3_CodeGenTester_TestValueType_MyStru* arg_0, float arg_1, int64_t arg_2)
{
	// temps
	void* tmp_0_ref;
	float tmp_1_r4;
	int64_t tmp_1_i8;

	// nop
	// ldarg.0
	tmp_0_ref = arg_0;
	// ldarg.1
	tmp_1_r4 = arg_1;
	// stfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	((stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref)->fld_5_f_float = tmp_1_r4;
	// ldarg.0
	tmp_0_ref = arg_0;
	// ldarg.2
	tmp_1_i8 = arg_2;
	// stfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	((stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref)->fld_6_f_long = tmp_1_i8;
	// ret
	return;
}
// static bool CodeGenTester.TestValueType::Entry()
int32_t met_7_CodeGenTester_TestValueType__Entry()
{
	// locals
	stru_3_CodeGenTester_TestValueType_MyStru loc_0 = stru_3_CodeGenTester_TestValueType_MyStru();
	stru_3_CodeGenTester_TestValueType_MyStru loc_1 = stru_3_CodeGenTester_TestValueType_MyStru();
	stru_3_CodeGenTester_TestValueType_MyStru loc_2 = stru_3_CodeGenTester_TestValueType_MyStru();
	stru_3_CodeGenTester_TestValueType_MyStru loc_3 = stru_3_CodeGenTester_TestValueType_MyStru();
	int32_t loc_4 = 0;
	int32_t loc_5 = 0;

	// temps
	void* tmp_0_ptr;
	stru_3_CodeGenTester_TestValueType_MyStru tmp_0_stru_3_CodeGenTester_TestValueType_MyStru;
	float tmp_0_r4;
	int64_t tmp_0_i8;
	int32_t tmp_0_i4;
	float tmp_1_r4;
	void* tmp_1_ptr;
	stru_3_CodeGenTester_TestValueType_MyStru tmp_1_stru_3_CodeGenTester_TestValueType_MyStru;
	int64_t tmp_1_i8;
	int32_t tmp_2_i4;
	int64_t tmp_2_i8;
	void* tmp_2_ptr;

	// nop
	// ldloca.s loc_0
	tmp_0_ptr = &loc_0;
	// ldc.r4 777
	tmp_1_r4 = 777;
	// ldc.i4 888
	tmp_2_i4 = 888;
	// conv.i8
	tmp_2_i8 = tmp_2_i4;
	// call void [valuetype]CodeGenTester.TestValueType/MyStru::.ctor(float,long)
	met_2_CodeGenTester_TestValueType_MyStru___ctor((stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ptr, tmp_1_r4, tmp_2_i8);
	// ldloca.s loc_3
	tmp_0_ptr = &loc_3;
	// initobj CodeGenTester.TestValueType/MyStru
	*(stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ptr = stru_3_CodeGenTester_TestValueType_MyStru();
	// ldloc.3
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_3;
	// ldloca.s loc_0
	tmp_1_ptr = &loc_0;
	// ldloca.s loc_1
	tmp_2_ptr = &loc_1;
	// call static [valuetype]CodeGenTester.TestValueType/MyStru CodeGenTester.TestValueType::Foo([valuetype]CodeGenTester.TestValueType/MyStru,[valuetype]CodeGenTester.TestValueType/MyStru&,[valuetype]CodeGenTester.TestValueType/MyStru&)
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = met_4_CodeGenTester_TestValueType__Foo(tmp_0_stru_3_CodeGenTester_TestValueType_MyStru, (stru_3_CodeGenTester_TestValueType_MyStru*)tmp_1_ptr, (stru_3_CodeGenTester_TestValueType_MyStru*)tmp_2_ptr);
	// stloc.2
	loc_2 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru;
	// ldloc.0
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_0;
	// ldfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	tmp_0_r4 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru.fld_5_f_float;
	// ldloc.1
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = loc_1;
	// ldfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	tmp_1_r4 = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru.fld_5_f_float;
	// bne.un.s label_34
	if (tmp_0_r4 != tmp_1_r4) goto label_34;
	// ldloc.0
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_0;
	// ldfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	tmp_0_i8 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru.fld_6_f_long;
	// ldloc.1
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = loc_1;
	// ldfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	tmp_1_i8 = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru.fld_6_f_long;
	// bne.un.s label_34
	if ((uint64_t)tmp_0_i8 != (uint64_t)tmp_1_i8) goto label_34;
	// ldloc.2
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_2;
	// ldfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	tmp_0_r4 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru.fld_5_f_float;
	// ldloc.1
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = loc_1;
	// ldfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	tmp_1_r4 = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru.fld_5_f_float;
	// bne.un.s label_34
	if (tmp_0_r4 != tmp_1_r4) goto label_34;
	// ldloc.2
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_2;
	// ldfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	tmp_0_i8 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru.fld_6_f_long;
	// ldloc.1
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = loc_1;
	// ldfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	tmp_1_i8 = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru.fld_6_f_long;
	// ceq
	tmp_0_i4 = tmp_0_i8 == tmp_1_i8 ? 1 : 0;
	// br.s label_35
	goto label_35;
	// ldc.i4.0
label_34:
	tmp_0_i4 = 0;
	// stloc.s loc_4
label_35:
	loc_4 = tmp_0_i4;
	// ldloc.s loc_4
	tmp_0_i4 = loc_4;
	// brfalse.s label_42
	if (!tmp_0_i4) goto label_42;
	// nop
	// ldc.i4.1
	tmp_0_i4 = 1;
	// stloc.s loc_5
	loc_5 = tmp_0_i4;
	// br.s label_45
	goto label_45;
	// ldc.i4.0
label_42:
	tmp_0_i4 = 0;
	// stloc.s loc_5
	loc_5 = tmp_0_i4;
	// br.s label_45
	goto label_45;
	// ldloc.s loc_5
label_45:
	tmp_0_i4 = loc_5;
	// ret
	return tmp_0_i4;
}
// static [valuetype]CodeGenTester.TestValueType/MyStru CodeGenTester.TestValueType::Foo([valuetype]CodeGenTester.TestValueType/MyStru,[valuetype]CodeGenTester.TestValueType/MyStru&,[valuetype]CodeGenTester.TestValueType/MyStru&)
stru_3_CodeGenTester_TestValueType_MyStru met_4_CodeGenTester_TestValueType__Foo(stru_3_CodeGenTester_TestValueType_MyStru arg_0, stru_3_CodeGenTester_TestValueType_MyStru* arg_1, stru_3_CodeGenTester_TestValueType_MyStru* arg_2)
{
	// locals
	stru_3_CodeGenTester_TestValueType_MyStru loc_0 = stru_3_CodeGenTester_TestValueType_MyStru();

	// temps
	void* tmp_0_ref;
	stru_3_CodeGenTester_TestValueType_MyStru tmp_0_stru_3_CodeGenTester_TestValueType_MyStru;
	stru_3_CodeGenTester_TestValueType_MyStru tmp_1_stru_3_CodeGenTester_TestValueType_MyStru;
	float tmp_1_r4;
	int32_t tmp_1_i4;
	int64_t tmp_1_i8;
	void* tmp_1_ref;
	float tmp_2_r4;

	// nop
	// ldarg.1
	tmp_0_ref = arg_1;
	// ldarg.0
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = arg_0;
	// ldfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	tmp_1_r4 = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru.fld_5_f_float;
	// ldc.r4 100
	tmp_2_r4 = 100;
	// add
	tmp_1_r4 = tmp_1_r4 + tmp_2_r4;
	// stfld float [valuetype]CodeGenTester.TestValueType/MyStru::f_float
	((stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref)->fld_5_f_float = tmp_1_r4;
	// ldarg.1
	tmp_0_ref = arg_1;
	// ldc.i4 789
	tmp_1_i4 = 789;
	// conv.i8
	tmp_1_i8 = tmp_1_i4;
	// stfld long [valuetype]CodeGenTester.TestValueType/MyStru::f_long
	((stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref)->fld_6_f_long = tmp_1_i8;
	// ldarg.2
	tmp_0_ref = arg_2;
	// ldarg.1
	tmp_1_ref = arg_1;
	// ldobj CodeGenTester.TestValueType/MyStru
	tmp_1_stru_3_CodeGenTester_TestValueType_MyStru = *(stru_3_CodeGenTester_TestValueType_MyStru*)tmp_1_ref;
	// stobj CodeGenTester.TestValueType/MyStru
	*(stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref = tmp_1_stru_3_CodeGenTester_TestValueType_MyStru;
	// ldarg.2
	tmp_0_ref = arg_2;
	// ldobj CodeGenTester.TestValueType/MyStru
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = *(stru_3_CodeGenTester_TestValueType_MyStru*)tmp_0_ref;
	// stloc.0
	loc_0 = tmp_0_stru_3_CodeGenTester_TestValueType_MyStru;
	// br.s label_19
	goto label_19;
	// ldloc.0
label_19:
	tmp_0_stru_3_CodeGenTester_TestValueType_MyStru = loc_0;
	// ret
	return tmp_0_stru_3_CodeGenTester_TestValueType_MyStru;
}

")]
	class TestValueType
	{
		struct MyStru
		{
			public float f_float;
			public long f_long;

			public MyStru(float f, long l)
			{
				f_float = f;
				f_long = l;
			}
		}

		static MyStru Foo(MyStru val, ref MyStru rval, out MyStru oval)
		{
			rval.f_float = val.f_float + 100;
			rval.f_long = 789;
			oval = rval;
			return oval;
		}

		public static bool Entry()
		{
			MyStru stru = new MyStru(777, 888);
			MyStru stru2;
			var res = Foo(new MyStru(), ref stru, out stru2);
			if (stru.f_float == stru2.f_float &&
				stru.f_long == stru2.f_long &&
				res.f_float == stru2.f_float &&
				res.f_long == stru2.f_long)
			{
				return true;
			}
			return false;
		}
	}

	[TestClass(@"[CppUnit_0.h]
#pragma once
#include ""il2cpp.h""
// object
struct cls_0_System_Object
{
	uint32_t objectTypeID;
};
// CodeGenTester.TestCycleCctor/A
struct cls_4_CodeGenTester_TestCycleCctor_A : cls_0_System_Object
{
};
// static int CodeGenTester.TestCycleCctor/A::sfld
extern int32_t sfld_3_CodeGenTester_TestCycleCctor_A__sfld;
extern int8_t onceflag_cls_4_CodeGenTester_TestCycleCctor_A;
extern uintptr_t locktid_cls_4_CodeGenTester_TestCycleCctor_A;
// static void CodeGenTester.TestCycleCctor/A::.cctor()
void met_2_CodeGenTester_TestCycleCctor_A___cctor();
// CodeGenTester.TestCycleCctor/B
struct cls_7_CodeGenTester_TestCycleCctor_B : cls_0_System_Object
{
};
// static int CodeGenTester.TestCycleCctor/B::sfld
extern int32_t sfld_6_CodeGenTester_TestCycleCctor_B__sfld;
extern int8_t onceflag_cls_7_CodeGenTester_TestCycleCctor_B;
extern uintptr_t locktid_cls_7_CodeGenTester_TestCycleCctor_B;
// static void CodeGenTester.TestCycleCctor/B::.cctor()
void met_5_CodeGenTester_TestCycleCctor_B___cctor();
// CodeGenTester.TestCycleCctor
struct cls_1_CodeGenTester_TestCycleCctor : cls_0_System_Object
{
};
// static bool CodeGenTester.TestCycleCctor::Entry()
int32_t met_8_CodeGenTester_TestCycleCctor__Entry();
void il2cpp_InitStaticVars();

[CppUnit_0.cpp]
#include ""CppUnit_0.h""
// static int CodeGenTester.TestCycleCctor/A::sfld
int32_t sfld_3_CodeGenTester_TestCycleCctor_A__sfld;
int8_t onceflag_cls_4_CodeGenTester_TestCycleCctor_A;
uintptr_t locktid_cls_4_CodeGenTester_TestCycleCctor_A;
static void once_met_2_CodeGenTester_TestCycleCctor_A___cctor()
{
	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// nop
	// ldsfld static int CodeGenTester.TestCycleCctor/B::sfld
	met_5_CodeGenTester_TestCycleCctor_B___cctor();
	tmp_0_i4 = sfld_6_CodeGenTester_TestCycleCctor_B__sfld;
	// ldc.i4.1
	tmp_1_i4 = 1;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stsfld static int CodeGenTester.TestCycleCctor/A::sfld
	sfld_3_CodeGenTester_TestCycleCctor_A__sfld = tmp_0_i4;
	// ret
	return;
}
// static void CodeGenTester.TestCycleCctor/A::.cctor()
void met_2_CodeGenTester_TestCycleCctor_A___cctor()
{
	IL2CPP_CALL_ONCE(
		onceflag_cls_4_CodeGenTester_TestCycleCctor_A,
		locktid_cls_4_CodeGenTester_TestCycleCctor_A,
		once_met_2_CodeGenTester_TestCycleCctor_A___cctor);
}
// static int CodeGenTester.TestCycleCctor/B::sfld
int32_t sfld_6_CodeGenTester_TestCycleCctor_B__sfld;
int8_t onceflag_cls_7_CodeGenTester_TestCycleCctor_B;
uintptr_t locktid_cls_7_CodeGenTester_TestCycleCctor_B;
static void once_met_5_CodeGenTester_TestCycleCctor_B___cctor()
{
	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// nop
	// ldsfld static int CodeGenTester.TestCycleCctor/A::sfld
	met_2_CodeGenTester_TestCycleCctor_A___cctor();
	tmp_0_i4 = sfld_3_CodeGenTester_TestCycleCctor_A__sfld;
	// ldc.i4.1
	tmp_1_i4 = 1;
	// add
	tmp_0_i4 = tmp_0_i4 + tmp_1_i4;
	// stsfld static int CodeGenTester.TestCycleCctor/B::sfld
	sfld_6_CodeGenTester_TestCycleCctor_B__sfld = tmp_0_i4;
	// ret
	return;
}
// static void CodeGenTester.TestCycleCctor/B::.cctor()
void met_5_CodeGenTester_TestCycleCctor_B___cctor()
{
	IL2CPP_CALL_ONCE(
		onceflag_cls_7_CodeGenTester_TestCycleCctor_B,
		locktid_cls_7_CodeGenTester_TestCycleCctor_B,
		once_met_5_CodeGenTester_TestCycleCctor_B___cctor);
}
// static bool CodeGenTester.TestCycleCctor::Entry()
int32_t met_8_CodeGenTester_TestCycleCctor__Entry()
{
	// locals
	int32_t loc_0 = 0;

	// temps
	int32_t tmp_0_i4;
	int32_t tmp_1_i4;

	// nop
	// ldsfld static int CodeGenTester.TestCycleCctor/A::sfld
	met_2_CodeGenTester_TestCycleCctor_A___cctor();
	tmp_0_i4 = sfld_3_CodeGenTester_TestCycleCctor_A__sfld;
	// ldsfld static int CodeGenTester.TestCycleCctor/B::sfld
	met_5_CodeGenTester_TestCycleCctor_B___cctor();
	tmp_1_i4 = sfld_6_CodeGenTester_TestCycleCctor_B__sfld;
	// sub
	tmp_0_i4 = tmp_0_i4 - tmp_1_i4;
	// ldc.i4.1
	tmp_1_i4 = 1;
	// ceq
	tmp_0_i4 = tmp_0_i4 == tmp_1_i4 ? 1 : 0;
	// stloc.0
	loc_0 = tmp_0_i4;
	// br.s label_8
	goto label_8;
	// ldloc.0
label_8:
	tmp_0_i4 = loc_0;
	// ret
	return tmp_0_i4;
}
void il2cpp_InitStaticVars()
{
	sfld_3_CodeGenTester_TestCycleCctor_A__sfld = 0;
	onceflag_cls_4_CodeGenTester_TestCycleCctor_A = 0;
	locktid_cls_4_CodeGenTester_TestCycleCctor_A = 0;
	sfld_6_CodeGenTester_TestCycleCctor_B__sfld = 0;
	onceflag_cls_7_CodeGenTester_TestCycleCctor_B = 0;
	locktid_cls_7_CodeGenTester_TestCycleCctor_B = 0;
}

")]
	class TestCycleCctor
	{
		class A
		{
			public static int sfld;

			static A()
			{
				sfld = B.sfld + 1;
			}
		}

		class B
		{
			public static int sfld;

			static B()
			{
				sfld = A.sfld + 1;
			}
		}

		public static bool Entry()
		{
			return A.sfld - B.sfld == 1;
		}
	}

	class Program
	{
		static void Main()
		{
			bool res = TestCycleCctor.Entry();
		}
	}
}
