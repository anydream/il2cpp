#pragma once

#define NOMINMAX

#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <limits>

#ifndef __has_builtin
#define __has_builtin(_x) 0
#endif

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
#define IL2CPP_LIKELY(_x)					__builtin_expect((_x), 1)
#define IL2CPP_UNLIKELY(_x)					__builtin_expect((_x), 0)
#else
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	_InterlockedCompareExchange8((volatile char*)_dst, _new, _cmp)
#define IL2CPP_LIKELY(_x)					_x
#define IL2CPP_UNLIKELY(_x)					_x
#endif

#define IL2CPP_NANF					il2cpp_NaNF()
#define IL2CPP_NAND					il2cpp_NaND()
#define IL2CPP_POS_INF				(1E+300 * 1E+300)
#define IL2CPP_NEG_INF				(-IL2CPP_POS_INF)
#define IL2CPP_ADD_OVF				il2cpp_AddOvf
#define IL2CPP_ADD_OVF_UN			il2cpp_AddOvfUn
#define IL2CPP_SUB_OVF				il2cpp_SubOvf
#define IL2CPP_SUB_OVF_UN			il2cpp_SubOvfUn
#define IL2CPP_MUL_OVF				il2cpp_MulOvf
#define IL2CPP_MUL_OVF_UN			il2cpp_MulOvfUn
#define IL2CPP_CONV_OVF(_to, _from)	il2cpp_ConvOvf<_to>(_from)

#define IL2CPP_TRAP					il2cpp_Trap
#define IL2CPP_ASSERT(_x)			if (!(_x)) IL2CPP_TRAP()
#define IL2CPP_MEMCPY				memcpy
#define IL2CPP_MEMSET				memset
#define IL2CPP_NEW					il2cpp_New
#define IL2CPP_CHECK_RANGE			il2cpp_CheckRange
#define IL2CPP_REMAINDER			il2cpp_Remainder
#define IL2CPP_CKFINITE				il2cpp_Ckfinite
#define IL2CPP_SZARRAY_LEN(_x)		il2cpp_SZArray__LoadLength((cls_System_Array*)(_x))
#define IL2CPP_CALL_ONCE			il2cpp_CallOnce
#define IL2CPP_THROW(_ex)			throw il2cppException(_ex)

struct cls_Object;

struct il2cppDummy {};

struct il2cppException
{
	cls_Object* ExceptionPtr;
	il2cppException(cls_Object* ptr) : ExceptionPtr(ptr) {}
};

void il2cpp_Init();
void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef);
void il2cpp_Yield();
uintptr_t il2cpp_ThreadID();
void il2cpp_CallOnce(int8_t &onceFlag, uintptr_t &lockTid, void(*invokeFunc)());

inline float il2cpp_NaNF()
{
	uint32_t n = 0xFFC00000;
	return *(float*)&n;
}

inline double il2cpp_NaND()
{
	uint64_t n = 0xFFF8000000000000;
	return *(double*)&n;
}

template <class ToType, class FromType>
inline ToType il2cpp_ConvOvf(FromType from)
{
	if ((std::numeric_limits<ToType>::min() == 0 && from < 0) ||
		from < std::numeric_limits<ToType>::min() ||
		from > std::numeric_limits<ToType>::max())
		il2cpp_ThrowOverflow();
	return (ToType)from;
}

void il2cpp_Trap();
void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index);
void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index, int64_t rangeLen);
float il2cpp_Remainder(float numer, float denom);
double il2cpp_Remainder(double numer, double denom);
float il2cpp_Ckfinite(float num);
double il2cpp_Ckfinite(double num);
void il2cpp_ThrowOverflow();
int32_t il2cpp_AddOvf(int32_t lhs, int32_t rhs);
int64_t il2cpp_AddOvf(int64_t lhs, int64_t rhs);
uint32_t il2cpp_AddOvfUn(uint32_t lhs, uint32_t rhs);
uint64_t il2cpp_AddOvfUn(uint64_t lhs, uint64_t rhs);
int32_t il2cpp_SubOvf(int32_t lhs, int32_t rhs);
int64_t il2cpp_SubOvf(int64_t lhs, int64_t rhs);
uint32_t il2cpp_SubOvfUn(uint32_t lhs, uint32_t rhs);
uint64_t il2cpp_SubOvfUn(uint64_t lhs, uint64_t rhs);
int32_t il2cpp_MulOvf(int32_t lhs, int32_t rhs);
int64_t il2cpp_MulOvf(int64_t lhs, int64_t rhs);
uint32_t il2cpp_MulOvfUn(uint32_t lhs, uint32_t rhs);
uint64_t il2cpp_MulOvfUn(uint64_t lhs, uint64_t rhs);

struct cls_System_Array;
int32_t il2cpp_SZArray__LoadLength(cls_System_Array* ary);
int32_t il2cpp_Array__GetLength(cls_System_Array* ary);
int64_t il2cpp_Array__GetLongLength(cls_System_Array* ary);
int32_t il2cpp_Array__GetLength(cls_System_Array* ary, int32_t dim);
int32_t il2cpp_Array__GetLowerBound(cls_System_Array* ary, int32_t dim);
int32_t il2cpp_Array__GetUpperBound(cls_System_Array* ary, int32_t dim);
void il2cpp_Array__Copy(cls_System_Array* srcAry, int32_t srcIdx, cls_System_Array* dstAry, int32_t dstIdx, int32_t copyLen);
void il2cpp_Array__Clear(cls_System_Array* ary, int32_t idx, int32_t clearLen);
