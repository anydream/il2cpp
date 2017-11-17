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

void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index, int64_t rangeLen)
{
	il2cpp_CheckRange(lowerBound, length, index);

	index += rangeLen;
	if (index < lowerBound || index > lowerBound + length)
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

float il2cpp_Ckfinite(float num)
{
	if (IL2CPP_UNLIKELY(isfinite(num)))
		met_4ObKN3_ThrowHelper__Throw_ArithmeticException();
	return num;
}

double il2cpp_Ckfinite(double num)
{
	if (IL2CPP_UNLIKELY(isfinite(num)))
		met_4ObKN3_ThrowHelper__Throw_ArithmeticException();
	return num;
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

float il2cpp_NaNF()
{
	return -nanf("0");
}

float il2cpp_PosInfF()
{
	return INFINITY;
}

float il2cpp_NegInfF()
{
	return -INFINITY;
}

double il2cpp_NaND()
{
	return -nan("0");
}

double il2cpp_PosInfD()
{
	return INFINITY;
}

double il2cpp_NegInfD()
{
	return -INFINITY;
}

#if defined(IL2CPP_BRIDGE_HAS_cls_System_Array)
int32_t il2cpp_SZArray__LoadLength(cls_System_Array* ary)
{
	IL2CPP_ASSERT(ary->Rank == 0);
	return ((int32_t*)&ary[1])[0];
}

int32_t il2cpp_Array__GetLength(cls_System_Array* ary)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
		return ((int32_t*)&ary[1])[0];
	else
	{
		int32_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

int64_t il2cpp_Array__GetLongLength(cls_System_Array* ary)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
		return ((int32_t*)&ary[1])[0];
	else
	{
		int64_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

int32_t il2cpp_Array__GetLength(cls_System_Array* ary, int32_t dim)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((int32_t*)&ary[1])[0];
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2 + 1];
	}
}

int32_t il2cpp_Array__GetLowerBound(cls_System_Array* ary, int32_t dim)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return 0;
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2];
	}
}

int32_t il2cpp_Array__GetUpperBound(cls_System_Array* ary, int32_t dim)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((int32_t*)&ary[1])[0] - 1;
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		int32_t* pBound = (int32_t*)&ary[1];
		return pBound[dim * 2] + pBound[dim * 2 + 1] - 1;
	}
}

void il2cpp_Array__Copy(cls_System_Array* srcAry, int32_t srcIdx, cls_System_Array* dstAry, int32_t dstIdx, int32_t copyLen)
{
	int32_t elemSize = dstAry->ElemSize;
	int32_t rank = dstAry->Rank;
	IL2CPP_ASSERT(elemSize == srcAry->ElemSize);
	IL2CPP_ASSERT(rank == srcAry->Rank);

	int64_t srcLen = il2cpp_Array__GetLongLength(srcAry);
	int64_t dstLen = il2cpp_Array__GetLongLength(dstAry);
	il2cpp_CheckRange(0, srcLen, srcIdx, copyLen);
	il2cpp_CheckRange(0, dstLen, dstIdx, copyLen);

	int32_t dataOffset = rank == 0 ? sizeof(int32_t) : rank * sizeof(int32_t) * 2;
	void* srcPtr = (uint8_t*)&srcAry[1] + dataOffset + elemSize * srcIdx;
	void* dstPtr = (uint8_t*)&dstAry[1] + dataOffset + elemSize * dstIdx;

	IL2CPP_MEMCPY(dstPtr, srcPtr, elemSize * copyLen);
}

void il2cpp_Array__Clear(cls_System_Array* ary, int32_t idx, int32_t clearLen)
{
	int32_t elemSize = ary->ElemSize;
	int32_t rank = ary->Rank;
	int64_t aryLen = il2cpp_Array__GetLongLength(ary);
	il2cpp_CheckRange(0, aryLen, idx, clearLen);

	int32_t dataOffset = rank == 0 ? sizeof(int32_t) : rank * sizeof(int32_t) * 2;
	void* ptr = (uint8_t*)&ary[1] + dataOffset + elemSize * idx;

	IL2CPP_MEMSET(ptr, 0, elemSize * clearLen);
}

#endif
