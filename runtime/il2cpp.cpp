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

void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index)
{
	if (index < lowerBound || index >= lowerBound + length)
		IL2CPP_UNREACHABLE();
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
			IL2CPP_UNREACHABLE();
	}
}

int32_t il2cpp_ArrayLength(cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return ((int32_t*)&ary[1])[0];
	else
	{
		int32_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

int64_t il2cpp_ArrayLongLength(cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return ((int32_t*)&ary[1])[0];
	else
	{
		int64_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

static void CheckCopyRange(int64_t lowerBound, int64_t length, int64_t index, int64_t copyLen)
{
	if (index < lowerBound || index >= lowerBound + length)
		IL2CPP_UNREACHABLE();

	index += copyLen;
	if (index < lowerBound || index > lowerBound + length)
		IL2CPP_UNREACHABLE();
}

void il2cpp_ArrayCopy(cls_System_Array* srcAry, int32_t srcIdx, cls_System_Array* dstAry, int32_t dstIdx, int32_t copyLen)
{
	int32_t elemSize = dstAry->ElemSize;
	int32_t rank = dstAry->Rank;
	IL2CPP_ASSERT(elemSize == srcAry->ElemSize);
	IL2CPP_ASSERT(rank == srcAry->Rank);

	int64_t srcLen = il2cpp_ArrayLongLength(srcAry);
	int64_t dstLen = il2cpp_ArrayLongLength(dstAry);
	CheckCopyRange(0, srcLen, srcIdx, copyLen);
	CheckCopyRange(0, dstLen, dstIdx, copyLen);

	int32_t dataOffset = rank == 0 ? sizeof(int32_t) : rank * sizeof(int32_t) * 2;
	void* srcPtr = (uint8_t*)&srcAry[1] + dataOffset + elemSize * srcIdx;
	void* dstPtr = (uint8_t*)&dstAry[1] + dataOffset + elemSize * dstIdx;

	IL2CPP_MEMCPY(dstPtr, srcPtr, elemSize * copyLen);
}
