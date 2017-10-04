#pragma once

#include <stdint.h>
#include <stdlib.h>
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
#define IL2CPP_UNLIKELY(x)					__builtin_expect(!!(x), 0)
#else
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	_InterlockedCompareExchange8((volatile char*)_dst, _new, _cmp)
#define IL2CPP_UNLIKELY(x)					x
#endif

#define IL2CPP_ASSERT		assert
#define IL2CPP_NEW			il2cpp_New
#define IL2CPP_CHECK_RANGE	il2cpp_CheckRange
#define IL2CPP_REMAINDER	il2cpp_Remainder

#define IL2CPP_CALL_ONCE	il2cpp_CallOnce

#define IL2CPP_THROW(_ex)	throw il2cppException(_ex)

struct cls_Object;

struct il2cppDummy {};

struct il2cppException
{
	cls_Object* ExceptionPtr;
	il2cppException(cls_Object* ptr) : ExceptionPtr(ptr) {}
};

void il2cpp_Init();

void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef);
void il2cpp_CheckRange(int32_t lowerBound, int32_t length, int32_t index);
float il2cpp_Remainder(float numer, float denom);
double il2cpp_Remainder(double numer, double denom);

void il2cpp_Yield();
uintptr_t il2cpp_ThreadID();
void il2cpp_CallOnce(int8_t &onceFlag, uintptr_t &lockTid, void(*invokeFunc)());
