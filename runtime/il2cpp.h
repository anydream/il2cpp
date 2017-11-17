#pragma once

#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>

#if defined(__clang__) || defined(__GNUC__)
#define GNU_LIKE
#elif defined(_MSC_VER)
#include <intrin.h>
#define MSVC_LIKE
#else
#error Cannot detect your compiler environment!
#endif

#ifdef GNU_LIKE
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	__sync_val_compare_and_swap(_dst, _cmp, _new)
#define IL2CPP_LIKELY(x)					__builtin_expect((x), 1)
#define IL2CPP_UNLIKELY(x)					__builtin_expect((x), 0)
#else
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	_InterlockedCompareExchange8((volatile char*)_dst, _new, _cmp)
#define IL2CPP_LIKELY(x)					x
#define IL2CPP_UNLIKELY(x)					x
#endif

#define IL2CPP_ASSERT(_x)		if (!(_x)) abort()
#define IL2CPP_UNREACHABLE		abort
#define IL2CPP_MEMCPY			memcpy
#define IL2CPP_MEMSET			memset
#define IL2CPP_NEW				il2cpp_New
#define IL2CPP_CHECK_RANGE		il2cpp_CheckRange
#define IL2CPP_REMAINDER		il2cpp_Remainder
#define IL2CPP_CKFINITE			il2cpp_Ckfinite
#define IL2CPP_SZARRAY_LEN(_x)	il2cpp_SZArray__LoadLength((cls_System_Array*)(_x))

#define IL2CPP_CALL_ONCE		il2cpp_CallOnce
#define IL2CPP_THROW(_ex)		throw il2cppException(_ex)

struct cls_Object;

struct il2cppDummy {};

struct il2cppException
{
	cls_Object* ExceptionPtr;
	il2cppException(cls_Object* ptr) : ExceptionPtr(ptr) {}
};

void il2cpp_Init();

void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef);
void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index);
void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index, int64_t rangeLen);
float il2cpp_Remainder(float numer, float denom);
double il2cpp_Remainder(double numer, double denom);
float il2cpp_Ckfinite(float num);
double il2cpp_Ckfinite(double num);

void il2cpp_Yield();
uintptr_t il2cpp_ThreadID();
void il2cpp_CallOnce(int8_t &onceFlag, uintptr_t &lockTid, void(*invokeFunc)());

struct cls_System_Array;
int32_t il2cpp_SZArray__LoadLength(cls_System_Array* ary);
int32_t il2cpp_Array__GetLength(cls_System_Array* ary);
int64_t il2cpp_Array__GetLongLength(cls_System_Array* ary);
int32_t il2cpp_Array__GetLength(cls_System_Array* ary, int32_t dim);
int32_t il2cpp_Array__GetLowerBound(cls_System_Array* ary, int32_t dim);
int32_t il2cpp_Array__GetUpperBound(cls_System_Array* ary, int32_t dim);
void il2cpp_Array__Copy(cls_System_Array* srcAry, int32_t srcIdx, cls_System_Array* dstAry, int32_t dstIdx, int32_t copyLen);
void il2cpp_Array__Clear(cls_System_Array* ary, int32_t idx, int32_t clearLen);
