#include <stdlib.h>

#if defined(_WIN32)
#include <windows.h>
#else
#include <sys/types.h>
#include <sched.h>
#endif

#if !defined(IL2CPP_LLVM)
#include <gc.h>
#endif

#include "il2cpp.h"
#include "il2cppBridge.h"

void il2cpp_InitGC();

void il2cpp_Init()
{
	il2cpp_InitGC();
}

void* il2cpp_New(uint32_t sz, uint32_t typeID, int32_t isNoRef)
{
	if (sz < 4)
		sz = 4;

	cls_Object* obj;
#if defined(IL2CPP_LLVM)
	if (isNoRef)
		obj = (cls_Object*)calloc(sz, 1);
	else
		obj = (cls_Object*)calloc(1, sz);
#else
	if (isNoRef)
		obj = (cls_Object*)GC_MALLOC_ATOMIC(sz);
	else
		obj = (cls_Object*)GC_MALLOC(sz);
#endif
	obj->TypeID = typeID;
	return obj;
}

void il2cpp_CheckRange(int32_t lowerBound, int32_t length, int32_t index)
{
	if (index < lowerBound || index >= lowerBound + length)
		abort();
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

int32_t met_WvfER_Array__get_Rank(struct cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return 1;
	return ary->Rank;
}

int32_t met_5SoFe3_Array__get_Length(struct cls_System_Array* ary)
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

int64_t met_afGVQ1_Array__get_LongLength(struct cls_System_Array* ary)
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

int32_t met_5o7RW3_Array__GetLength(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
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

int32_t met_y01YS2_Array__GetLowerBound(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
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

int32_t met_ivEBm1_Array__GetUpperBound(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((int32_t*)&ary[1])[0] - 1;
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2] + ((int32_t*)&ary[1])[dim * 2 + 1] - 1;
	}
}
