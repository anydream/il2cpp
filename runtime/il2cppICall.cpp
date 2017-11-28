#include "il2cpp.h"
#include "il2cppGC.h"
#include "il2cppBridge.h"

#if defined(IL2CPP_BRIDGE_HAS_cls_String)
struct cls_String;

cls_String* met_2Rvly4_Environment__GetResourceFromDefault(cls_String* str)
{
	return str;
}
#endif

#if defined(IL2CPP_BRIDGE_HAS_cls_System_Array)
int32_t met_yYD1s1_Array__get_Rank(cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return 1;
	return ary->Rank;
}

int32_t met_im47r1_Array__get_Length(cls_System_Array* ary)
{
	return il2cpp_Array__GetLength(ary);
}

int64_t met_mkmvJ2_Array__get_LongLength(cls_System_Array* ary)
{
	return il2cpp_Array__GetLongLength(ary);
}

int32_t met_Ksden4_Array__GetLength(cls_System_Array* ary, int32_t dim)
{
	return il2cpp_Array__GetLength(ary, dim);
}

int32_t met_ceL9h_Array__GetLowerBound(cls_System_Array* ary, int32_t dim)
{
	return il2cpp_Array__GetLowerBound(ary, dim);
}

int32_t met_OyzG21_Array__GetUpperBound(cls_System_Array* ary, int32_t dim)
{
	return il2cpp_Array__GetUpperBound(ary, dim);
}

void met_ezijB_Array__Copy(cls_System_Array* srcAry, int32_t srcIdx, cls_System_Array* dstAry, int32_t dstIdx, int32_t copyLen, uint8_t reliable)
{
	il2cpp_Array__Copy(srcAry, srcIdx, dstAry, dstIdx, copyLen);
}

void met_mjkfQ2_Array__Clear(cls_System_Array* ary, int32_t idx, int32_t clearLen)
{
	il2cpp_Array__Clear(ary, idx, clearLen);
}
#endif

void met_5lgqh_Monitor__ReliableEnter(cls_Object* obj, uint8_t* lockTaken)
{
	il2cpp_SpinLock(obj->Flags[0]);
	*lockTaken = 1;
}

void met_vcJk_Monitor__Exit(cls_Object* obj)
{
	il2cpp_SpinUnlock(obj->Flags[0]);
}

void met_Jbedr_GC___Collect(int32_t gen, int32_t mode)
{
	il2cpp_GC_Collect();
}

int32_t met_3ECm11_RuntimeHelpers__GetHashCode(cls_Object* obj)
{
	uintptr_t val = (uintptr_t)obj;
	return (int32_t)((uint32_t)val ^ (uint32_t)(val >> 32));
}
