#if defined(_WIN32)
#include <windows.h>
#else
#include <sys/types.h>
#include <sched.h>
#endif

#include <math.h>

#include "il2cpp.h"
#include "il2cppGC.h"
#include "il2cppBridge.h"

void il2cpp_Init()
{
	il2cpp_GC_Init();
}

void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef)
{
	if (sz < 4)
		sz = 4;

	cls_Object* obj;
#if defined(IL2CPP_PATCH_LLVM)
	if (isNoRef)
		obj = (cls_Object*)calloc(sz, 1);
	else
		obj = (cls_Object*)calloc(1, sz);
#else
	if (isNoRef)
		obj = (cls_Object*)il2cpp_GC_AllocAtomic(sz);
	else
		obj = (cls_Object*)il2cpp_GC_Alloc(sz);
#endif
	obj->TypeID = typeID;
	return obj;
}

void il2cpp_CheckRange(int32_t lowerBound, int32_t length, int32_t index)
{
	if (index < lowerBound || index >= lowerBound + length)
		abort();
}

float il2cpp_Remainder(float numer, float denom)
{
	return remainderf(numer, denom);
}

double il2cpp_Remainder(double numer, double denom)
{
	return remainder(numer, denom);
}

void il2cpp_Yield()
{
#if defined(_WIN32)
	Sleep(0);
#else
	sched_yield();
#endif
}

uintptr_t il2cpp_ThreadID()
{
#if defined(_WIN32)
	return (uintptr_t)GetCurrentThreadId();
#else
	return (uintptr_t)gettid();
#endif
}

void il2cpp_CallOnce(int8_t &onceFlag, uintptr_t &lockTid, void(*invokeFunc)())
{
	if (IL2CPP_UNLIKELY(onceFlag != -1))
	{
		if (IL2CPP_ATOMIC_CAS(&onceFlag, 0, 1) == 0)
		{
			lockTid = il2cpp_ThreadID();
			invokeFunc();
			IL2CPP_ATOMIC_CAS(&onceFlag, 1, -1);
		}
		else if (lockTid != il2cpp_ThreadID())
		{
			while (onceFlag != -1)
				il2cpp_Yield();
		}
		else if (onceFlag != 1)
			abort();
	}
}
