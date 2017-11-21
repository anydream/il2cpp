#include "il2cpp.h"
#include "il2cppGC.h"
#include "il2cppBridge.h"
#include <math.h>

#if defined(_WIN32)
#include <windows.h>
#else
#include <sys/types.h>
#include <sched.h>
#endif

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
			IL2CPP_TRAP();
	}
}

void il2cpp_Trap()
{
#if __has_builtin(__builtin_trap)
	__builtin_trap();
#else
	abort();
#endif
}

void il2cpp_CheckRange(uint64_t lowerBound, uint64_t length, uint64_t index)
{
	if (index < lowerBound || index >= lowerBound + length)
		IL2CPP_TRAP();
}

void il2cpp_CheckRange(uint64_t lowerBound, uint64_t length, uint64_t index, uint64_t rangeLen)
{
	il2cpp_CheckRange(lowerBound, length, index);

	index += rangeLen;
	if (index < lowerBound || index > lowerBound + length)
		IL2CPP_TRAP();
}

float il2cpp_Remainder(float numer, float denom)
{
	return remainderf(numer, denom);
}

double il2cpp_Remainder(double numer, double denom)
{
	return remainder(numer, denom);
}

inline int16_t il2cpp_FDTest(float *ptr)
{
	if ((*((uint16_t*)ptr + 1) & 0x7F80) == 0x7F80)
	{
		if (*((uint16_t*)ptr + 1) & 0x7F || *(uint16_t*)ptr)
			return 2;
		else
			return 1;
	}
	else if (*((uint16_t*)ptr + 1) & 0x7FFF || *(uint16_t*)ptr)
	{
		if (*((uint16_t*)ptr + 1) & 0x7F80)
			return -1;
		else
			return -2;
	}
	return 0;
}

inline int16_t il2cpp_DTest(double *ptr)
{
	if ((*((uint16_t*)ptr + 3) & 0x7FF0) == 0x7FF0)
	{
		if (*((uint16_t*)ptr + 3) & 0xF || *(uint32_t*)((char*)ptr + 2) || *(uint16_t*)ptr)
			return 2;
		else
			return 1;
	}
	else if (*((uint16_t*)ptr + 3) & 0x7FFF || *(uint32_t*)((char*)ptr + 2) || *(uint16_t*)ptr)
	{
		if (*((uint16_t*)ptr + 3) & 0x7FF0)
			return -1;
		else
			return -2;
	}
	return 0;
}

inline bool il2cpp_IsFinite(float num)
{
	return il2cpp_FDTest(&num) <= 0;
}

inline bool il2cpp_IsFinite(double num)
{
	return il2cpp_DTest(&num) <= 0;
}

#if defined(IL2CPP_BRIDGE_HAS_cls_il2cpprt_ThrowHelper)
float il2cpp_Ckfinite(float num)
{
	if (IL2CPP_UNLIKELY(!il2cpp_IsFinite(num)))
		met_4ObKN3_ThrowHelper__Throw_ArithmeticException();
	return num;
}

double il2cpp_Ckfinite(double num)
{
	if (IL2CPP_UNLIKELY(!il2cpp_IsFinite(num)))
		met_4ObKN3_ThrowHelper__Throw_ArithmeticException();
	return num;
}
#endif

#if defined(IL2CPP_BRIDGE_HAS_cls_il2cpprt_ThrowHelper)
void il2cpp_ThrowOverflow()
{
	met_aBjyT3_ThrowHelper__Throw_OverflowException();
}

template <>
uint64_t il2cpp_ConvOverflow<uint64_t>(double from)
{
	if (from < 0 || from >= UINT64_MAX)
		il2cpp_ThrowOverflow();
	return (uint64_t)from;
}

template <>
int64_t il2cpp_ConvOverflow<int64_t>(double from)
{
	int64_t to = (int64_t)from;
	if ((from < (double)INT64_MIN) ||
		(from > (double)INT64_MAX) ||
		(to == INT64_MIN))
		il2cpp_ThrowOverflow();
	return to;
}

template <>
int32_t il2cpp_ConvOverflow<int32_t>(double from)
{
	if (((int64_t)from < INT32_MIN) ||
		((int64_t)from > INT32_MAX))
		il2cpp_ThrowOverflow();
	return (int32_t)from;
}
#endif

#if defined(IL2CPP_BRIDGE_HAS_cls_System_Array)
uint32_t il2cpp_SZArray__LoadLength(cls_System_Array* ary)
{
	IL2CPP_ASSERT(ary->Rank == 0);
	return ((uint32_t*)&ary[1])[0];
}

uint32_t il2cpp_Array__GetLength(cls_System_Array* ary)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
		return ((uint32_t*)&ary[1])[0];
	else
	{
		uint32_t length = 1;
		for (uint32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((uint32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

uint64_t il2cpp_Array__GetLongLength(cls_System_Array* ary)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
		return ((uint32_t*)&ary[1])[0];
	else
	{
		uint64_t length = 1;
		for (uint32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((uint32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}

uint32_t il2cpp_Array__GetLength(cls_System_Array* ary, uint32_t dim)
{
	if (IL2CPP_LIKELY(ary->Rank == 0))
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((uint32_t*)&ary[1])[0];
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((uint32_t*)&ary[1])[dim * 2 + 1];
	}
}

int32_t il2cpp_Array__GetLowerBound(cls_System_Array* ary, uint32_t dim)
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

int32_t il2cpp_Array__GetUpperBound(cls_System_Array* ary, uint32_t dim)
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
		return pBound[dim * 2] + (uint32_t)pBound[dim * 2 + 1] - 1;
	}
}

void il2cpp_Array__Copy(cls_System_Array* srcAry, uint32_t srcIdx, cls_System_Array* dstAry, uint32_t dstIdx, uint32_t copyLen)
{
	uint32_t elemSize = dstAry->ElemSize;
	uint32_t rank = dstAry->Rank;
	IL2CPP_ASSERT(elemSize == srcAry->ElemSize);
	IL2CPP_ASSERT(rank == srcAry->Rank);

	uint64_t srcLen = il2cpp_Array__GetLongLength(srcAry);
	uint64_t dstLen = il2cpp_Array__GetLongLength(dstAry);
	il2cpp_CheckRange(0, srcLen, srcIdx, copyLen);
	il2cpp_CheckRange(0, dstLen, dstIdx, copyLen);

	uint32_t dataOffset = rank == 0 ? sizeof(uint32_t) : rank * sizeof(uint32_t) * 2;
	uint8_t* srcPtr = (uint8_t*)&srcAry[1] + dataOffset + elemSize * srcIdx;
	uint8_t* dstPtr = (uint8_t*)&dstAry[1] + dataOffset + elemSize * dstIdx;

	IL2CPP_MEMCPY(dstPtr, srcPtr, elemSize * copyLen);
}

void il2cpp_Array__Clear(cls_System_Array* ary, uint32_t idx, uint32_t clearLen)
{
	uint32_t elemSize = ary->ElemSize;
	uint32_t rank = ary->Rank;
	uint64_t aryLen = il2cpp_Array__GetLongLength(ary);
	il2cpp_CheckRange(0, aryLen, idx, clearLen);

	uint32_t dataOffset = rank == 0 ? sizeof(uint32_t) : rank * sizeof(uint32_t) * 2;
	uint8_t* ptr = (uint8_t*)&ary[1] + dataOffset + elemSize * idx;

	IL2CPP_MEMSET(ptr, 0, elemSize * clearLen);
}

#endif
