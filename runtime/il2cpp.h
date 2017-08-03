#pragma once

#include <stdint.h>

#if defined(__clang__) || defined(__GNUC__)
#define GNU_LIKE
#elif defined(_MSC_VER)
#define MSVC_LIKE
#endif

#ifdef GNU_LIKE
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	__sync_val_compare_and_swap(_dst, _cmp, _new)
#define IL2CPP_UNLIKELY(x)					__builtin_expect(!!(x), 0)
#else
#define IL2CPP_ATOMIC_CAS(_dst, _cmp, _new)	_InterlockedCompareExchange8((volatile char*)_dst, _new, _cmp)
#define IL2CPP_UNLIKELY(x)					x
#endif

#define IL2CPP_CALL_ONCE(_flag, _func) \
		if (IL2CPP_UNLIKELY(_flag != -1)) \
		{ \
			if (IL2CPP_ATOMIC_CAS(&_flag, 0, 1) == 0) \
			{ \
				_func(); \
			} \
			else \
			{ \
				while (_flag != -1) \
					il2cpp_Yield(); \
			} \
			_flag = -1; \
		}

void* il2cpp_New(uint32_t sz, uint32_t typeID);
void il2cpp_Yield();
