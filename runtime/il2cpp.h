#pragma once

#define NOMINMAX

#include <stdint.h>
#include <malloc.h>
#include <type_traits>
#include <limits>

#ifndef __has_builtin
#define __has_builtin(_x) 0
#endif

#if defined(__clang__) || defined(__GNUC__)
#define IL2CPP_GNUC_LIKE
#elif defined(_MSC_VER)
#include <intrin.h>
#define IL2CPP_MSVC_LIKE
#else
#error Cannot detect your compiler environment!
#endif

#if defined(IL2CPP_GNUC_LIKE)
#define IL2CPP_TRAP								__builtin_trap()
#define IL2CPP_UNREACHABLE						__builtin_unreachable()
#define IL2CPP_ATOMIC_CAS_8(_dst, _cmp, _new)	__sync_val_compare_and_swap(_dst, _cmp, _new)
#define IL2CPP_LIKELY(_x)						__builtin_expect(!!(_x), 1)
#define IL2CPP_UNLIKELY(_x)						__builtin_expect(!!(_x), 0)
#define IL2CPP_PACKED_TAIL(_x)					__attribute__((packed, aligned(_x)))
#else
#define IL2CPP_TRAP								abort()
#define IL2CPP_UNREACHABLE						abort()
#define IL2CPP_ATOMIC_CAS_8(_dst, _cmp, _new)	_InterlockedCompareExchange8((volatile char*)_dst, _new, _cmp)
#define IL2CPP_LIKELY(_x)						_x
#define IL2CPP_UNLIKELY(_x)						_x
#define IL2CPP_PACKED_TAIL(_x)
#endif

#define IL2CPP_ASSERT(_x)				do { if (!(_x)) IL2CPP_TRAP; } while(0)
#define IL2CPP_MEMCPY					memcpy
#define IL2CPP_MEMMOVE					memmove
#define IL2CPP_MEMSET					memset
#define IL2CPP_MEMCMP					memcmp
#define IL2CPP_ALLOCA					alloca
#define IL2CPP_NEW						il2cpp_New
#define IL2CPP_ADD_ROOT(_x)				il2cppRootItem(&(_x), sizeof(_x))
#define IL2CPP_THROW(_ex)				throw il2cppException(_ex)
#define IL2CPP_THROW_INVALIDCAST		do { il2cpp_ThrowInvalidCast(); IL2CPP_UNREACHABLE; } while(0)

#define IL2CPP_MIN(_x, _y)				il2cpp_Min(_x, _y)
#define IL2CPP_MAX(_x, _y)				il2cpp_Max(_x, _y)
#define IL2CPP_OFFSETOF(_fld)			il2cpp_OffsetOf(_fld)
#define IL2CPP_NANF						il2cpp_NaNF()
#define IL2CPP_NAND						il2cpp_NaND()
#define IL2CPP_POS_INF					(1E+300 * 1E+300)
#define IL2CPP_NEG_INF					(-IL2CPP_POS_INF)

#define IL2CPP_CHECK_RANGE				il2cpp_CheckRange
#define IL2CPP_REMAINDER				il2cpp_Remainder
#define IL2CPP_CKFINITE					il2cpp_Ckfinite

#define IL2CPP_ADD						il2cpp_SafeAdd
#define IL2CPP_SUB						il2cpp_SafeSub
#define IL2CPP_ADD_OVF					il2cpp_AddOverflow
#define IL2CPP_SUB_OVF					il2cpp_SubOverflow
#define IL2CPP_MUL_OVF					il2cpp_MulOverflow
#define IL2CPP_CONV_OVF(_t, _s, _val)	il2cpp_ConvOverflow<_t, _s>((_s)_val)

#define IL2CPP_SZARRAY_LEN(_x)			il2cpp_SZArray__LoadLength((cls_System_Array*)(_x))

#if defined(IL2CPP_DISABLE_THREADSAFE_CALL_CCTOR)
#define IL2CPP_CALL_CCTOR(_pfn) \
	static bool s_IsCalled = false; \
	if (!s_IsCalled) \
	{ \
		s_IsCalled = true; \
		_pfn(); \
	}
#else
#define IL2CPP_CALL_CCTOR(_pfn) \
	static uintptr_t s_LockTid = 0; \
	static uint8_t s_OnceFlag = 0; \
	il2cpp_CallOnce(s_OnceFlag, s_LockTid, &_pfn);
#endif

#define IL2CPP_CHECK_ADD_OVERFLOW(a,b) \
	(int32_t)(b) >= 0 ? (int32_t)(INT32_MAX) - (int32_t)(b) < (int32_t)(a) ? -1 : 0	\
	: (int32_t)(INT32_MIN) - (int32_t)(b) > (int32_t)(a) ? +1 : 0

#define IL2CPP_CHECK_SUB_OVERFLOW(a,b) \
	(int32_t)(b) < 0 ? (int32_t)(INT32_MAX) + (int32_t)(b) < (int32_t)(a) ? -1 : 0	\
	: (int32_t)(INT32_MIN) + (int32_t)(b) > (int32_t)(a) ? +1 : 0

#define IL2CPP_CHECK_ADD_OVERFLOW_UN(a,b) \
	(uint32_t)(UINT32_MAX) - (uint32_t)(b) < (uint32_t)(a) ? -1 : 0

#define IL2CPP_CHECK_SUB_OVERFLOW_UN(a,b) \
	(uint32_t)(a) < (uint32_t)(b) ? -1 : 0

#define IL2CPP_CHECK_ADD_OVERFLOW64(a,b) \
	(int64_t)(b) >= 0 ? (int64_t)(INT64_MAX) - (int64_t)(b) < (int64_t)(a) ? -1 : 0	\
	: (int64_t)(INT64_MIN) - (int64_t)(b) > (int64_t)(a) ? +1 : 0

#define IL2CPP_CHECK_SUB_OVERFLOW64(a,b) \
	(int64_t)(b) < 0 ? (int64_t)(INT64_MAX) + (int64_t)(b) < (int64_t)(a) ? -1 : 0	\
	: (int64_t)(INT64_MIN) + (int64_t)(b) > (int64_t)(a) ? +1 : 0

#define IL2CPP_CHECK_ADD_OVERFLOW64_UN(a,b) \
	(uint64_t)(UINT64_MAX) - (uint64_t)(b) < (uint64_t)(a) ? -1 : 0

#define IL2CPP_CHECK_SUB_OVERFLOW64_UN(a,b) \
	(uint64_t)(a) < (uint64_t)(b) ? -1 : 0

#define IL2CPP_CHECK_MUL_OVERFLOW(a,b) \
	((int32_t)(a) == 0) || ((int32_t)(b) == 0) ? 0 : \
	(((int32_t)(a) > 0) && ((int32_t)(b) == -1)) ? 0 : \
	(((int32_t)(a) < 0) && ((int32_t)(b) == -1)) ? (a == - INT32_MAX) : \
	(((int32_t)(a) > 0) && ((int32_t)(b) > 0)) ? (int32_t)(a) > ((INT32_MAX) / (int32_t)(b)) : \
	(((int32_t)(a) > 0) && ((int32_t)(b) < 0)) ? (int32_t)(a) > ((INT32_MIN) / (int32_t)(b)) : \
	(((int32_t)(a) < 0) && ((int32_t)(b) > 0)) ? (int32_t)(a) < ((INT32_MIN) / (int32_t)(b)) : \
	(int32_t)(a) < ((INT32_MAX) / (int32_t)(b))

#define IL2CPP_CHECK_MUL_OVERFLOW_UN(a,b) \
	((uint32_t)(a) == 0) || ((uint32_t)(b) == 0) ? 0 : \
	(uint32_t)(b) > ((UINT32_MAX) / (uint32_t)(a))

#define IL2CPP_CHECK_MUL_OVERFLOW64(a,b) \
	((int64_t)(a) == 0) || ((int64_t)(b) == 0) ? 0 : \
	(((int64_t)(a) > 0) && ((int64_t)(b) == -1)) ? 0 : \
	(((int64_t)(a) < 0) && ((int64_t)(b) == -1)) ? (a == - INT64_MAX) : \
	(((int64_t)(a) > 0) && ((int64_t)(b) > 0)) ? (int64_t)(a) > ((INT64_MAX) / (int64_t)(b)) : \
	(((int64_t)(a) > 0) && ((int64_t)(b) < 0)) ? (int64_t)(a) > ((INT64_MIN) / (int64_t)(b)) : \
	(((int64_t)(a) < 0) && ((int64_t)(b) > 0)) ? (int64_t)(a) < ((INT64_MIN) / (int64_t)(b)) : \
	(int64_t)(a) < ((INT64_MAX) / (int64_t)(b))

#define IL2CPP_CHECK_MUL_OVERFLOW64_UN(a,b) \
	((uint64_t)(a) == 0) || ((uint64_t)(b) == 0) ? 0 : \
	(uint64_t)(b) > ((UINT64_MAX) / (uint64_t)(a))

struct cls_Object;

struct il2cppDummy {};

struct il2cppException
{
	cls_Object* ExceptionPtr;
	il2cppException(cls_Object* ptr) : ExceptionPtr(ptr) {}
};

struct il2cppMetaBuffer
{
	const uint8_t* Data;
	uint32_t Length;
};

struct il2cppTypeInfo
{
	const uint16_t* Name;
	const uint16_t* Namespace;
};

struct il2cppMethodInfo
{
	const uint16_t* Name;
};

struct il2cppCustomAttr
{
	il2cppMethodInfo* AttrCtor;
	il2cppMetaBuffer AttrData;
};

struct il2cppFieldInfo
{
	const uint16_t* Name;
	il2cppTypeInfo* DeclType;
	il2cppTypeInfo* FieldType;
	il2cppCustomAttr** CustomAttrs;
	il2cppMetaBuffer FieldInit;
	uint32_t FieldAttr;
	uint32_t Offset;
};

struct il2cppRootItem
{
	uint8_t* Ptr;
	uint32_t Length;

	il2cppRootItem(const void* ptr, uint32_t len)
		: Ptr((uint8_t*)ptr)
		, Length(len)
	{}
};

using IL2CPP_FINALIZER_FUNC = void(*)(cls_Object*);

void il2cpp_GC_Init();
void* il2cpp_GC_Alloc(uintptr_t sz);
void* il2cpp_GC_AllocAtomic(uintptr_t sz);
void il2cpp_GC_AddRoots(void* low, void* high);
bool il2cpp_GC_RegisterThread();
bool il2cpp_GC_UnregisterThread();
void il2cpp_GC_RegisterFinalizer(cls_Object* obj, IL2CPP_FINALIZER_FUNC finalizer);
void il2cpp_GC_Collect();

void il2cpp_Init();
void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef);
void* il2cpp_New(uint32_t sz, uint32_t typeID, uint8_t isNoRef, IL2CPP_FINALIZER_FUNC finalizer);
void il2cpp_CommitRoots(il2cppRootItem* roots, uint32_t num);
void il2cpp_Yield();
void il2cpp_SleepMS(uint32_t ms);
uintptr_t il2cpp_ThreadID();
void il2cpp_CallOnce(uint8_t &onceFlag, uintptr_t &lockTid, void(*invokeFunc)());
void il2cpp_SpinLock(uint8_t &flag);
void il2cpp_SpinUnlock(uint8_t &flag);
int32_t il2cpp_HashString(const uint16_t* str, int32_t len);
double il2cpp_Abs(double n);
double il2cpp_Sqrt(double n);
double il2cpp_Sin(double n);
double il2cpp_Cos(double n);
double il2cpp_Tan(double n);
double il2cpp_Exp(double n);
double il2cpp_Pow(double n, double m);

template <class T>
inline T il2cpp_Min(T lhs, T rhs)
{
	return lhs < rhs ? lhs : rhs;
}

template <class T>
inline T il2cpp_Max(T lhs, T rhs)
{
	return lhs > rhs ? lhs : rhs;
}

template <typename TField, typename TCls>
constexpr uintptr_t il2cpp_OffsetOf(TField TCls::*member)
{
	const char obj_dummy[sizeof(TCls)] = {};
	const TCls *obj = reinterpret_cast<const TCls*>(obj_dummy);
	return reinterpret_cast<uintptr_t>(&(obj->*member)) - reinterpret_cast<uintptr_t>(obj);
}

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

template <class T, class U>
std::common_type_t<T, U> il2cpp_SafeAdd(T lhs, U rhs)
{
	using common_t = std::common_type_t<T, U>;
	using unsigned_t = std::make_unsigned_t<common_t>;
	return static_cast<common_t>(static_cast<unsigned_t>(lhs) + static_cast<unsigned_t>(rhs));
}

inline float il2cpp_SafeAdd(float lhs, float rhs)
{
	return lhs + rhs;
}

inline double il2cpp_SafeAdd(double lhs, double rhs)
{
	return lhs + rhs;
}

template <class T, class U>
std::common_type_t<T, U> il2cpp_SafeSub(T lhs, U rhs)
{
	using common_t = std::common_type_t<T, U>;
	using unsigned_t = std::make_unsigned_t<common_t>;
	return static_cast<common_t>(static_cast<unsigned_t>(lhs) - static_cast<unsigned_t>(rhs));
}

inline float il2cpp_SafeSub(float lhs, float rhs)
{
	return lhs - rhs;
}

inline double il2cpp_SafeSub(double lhs, double rhs)
{
	return lhs - rhs;
}

inline bool il2cpp_CheckAdd(int32_t lhs, int32_t rhs)
{
	return IL2CPP_CHECK_ADD_OVERFLOW(lhs, rhs);
}

inline bool il2cpp_CheckAdd(uint32_t lhs, uint32_t rhs)
{
	return IL2CPP_CHECK_ADD_OVERFLOW_UN(lhs, rhs);
}

inline bool il2cpp_CheckAdd(int64_t lhs, int64_t rhs)
{
	return IL2CPP_CHECK_ADD_OVERFLOW64(lhs, rhs);
}

inline bool il2cpp_CheckAdd(uint64_t lhs, uint64_t rhs)
{
	return IL2CPP_CHECK_ADD_OVERFLOW64_UN(lhs, rhs);
}

template <class T>
bool il2cpp_AddOverflow(T lhs, T rhs, T &result)
{
#if __has_builtin(__builtin_add_overflow)
	return __builtin_add_overflow(lhs, rhs, &result);
#else
	result = IL2CPP_ADD(lhs, rhs);
	return il2cpp_CheckAdd(lhs, rhs) != 0;
#endif
}

inline bool il2cpp_CheckSub(int32_t lhs, int32_t rhs)
{
	return IL2CPP_CHECK_SUB_OVERFLOW(lhs, rhs);
}

inline bool il2cpp_CheckSub(uint32_t lhs, uint32_t rhs)
{
	return IL2CPP_CHECK_SUB_OVERFLOW_UN(lhs, rhs);
}

inline bool il2cpp_CheckSub(int64_t lhs, int64_t rhs)
{
	return IL2CPP_CHECK_SUB_OVERFLOW64(lhs, rhs);
}

inline bool il2cpp_CheckSub(uint64_t lhs, uint64_t rhs)
{
	return IL2CPP_CHECK_SUB_OVERFLOW64_UN(lhs, rhs);
}

template <class T>
bool il2cpp_SubOverflow(T lhs, T rhs, T &result)
{
#if __has_builtin(__builtin_sub_overflow)
	return __builtin_sub_overflow(lhs, rhs, &result);
#else
	result = IL2CPP_SUB(lhs, rhs);
	return il2cpp_CheckSub(lhs, rhs) != 0;
#endif
}

inline bool il2cpp_CheckMul(int32_t lhs, int32_t rhs)
{
	return IL2CPP_CHECK_MUL_OVERFLOW(lhs, rhs);
}

inline bool il2cpp_CheckMul(uint32_t lhs, uint32_t rhs)
{
	return IL2CPP_CHECK_MUL_OVERFLOW_UN(lhs, rhs);
}

inline bool il2cpp_CheckMul(int64_t lhs, int64_t rhs)
{
	return IL2CPP_CHECK_MUL_OVERFLOW64(lhs, rhs);
}

inline bool il2cpp_CheckMul(uint64_t lhs, uint64_t rhs)
{
	return IL2CPP_CHECK_MUL_OVERFLOW64_UN(lhs, rhs);
}

template <class T>
bool il2cpp_MulOverflow(T lhs, T rhs, T &result)
{
#if __has_builtin(__builtin_mul_overflow)
	return __builtin_mul_overflow(lhs, rhs, &result);
#else
	result = lhs * rhs;
	return il2cpp_CheckMul(lhs, rhs) != 0;
#endif
}

template <class T, class U>
std::common_type_t<T, U> il2cpp_AddOverflow(T lhs, U rhs)
{
	using common_t = std::common_type_t<T, U>;
	common_t result;
	if (IL2CPP_UNLIKELY(il2cpp_AddOverflow(static_cast<common_t>(lhs), static_cast<common_t>(rhs), result)))
		il2cpp_ThrowOverflow();
	return result;
}

template <class T, class U>
std::common_type_t<T, U> il2cpp_SubOverflow(T lhs, U rhs)
{
	using common_t = std::common_type_t<T, U>;
	common_t result;
	if (IL2CPP_UNLIKELY(il2cpp_SubOverflow(static_cast<common_t>(lhs), static_cast<common_t>(rhs), result)))
		il2cpp_ThrowOverflow();
	return result;
}

template <class T, class U>
std::common_type_t<T, U> il2cpp_MulOverflow(T lhs, U rhs)
{
	using common_t = std::common_type_t<T, U>;
	common_t result;
	if (IL2CPP_UNLIKELY(il2cpp_MulOverflow(static_cast<common_t>(lhs), static_cast<common_t>(rhs), result)))
		il2cpp_ThrowOverflow();
	return result;
}

template <class T, class S>
T il2cpp_ConvOverflow(S s)
{
	const bool isToReal = std::is_floating_point<T>::value;
	const bool isToSmaller = sizeof(T) < sizeof(S);
	const bool isEquals = sizeof(T) == sizeof(S);
	const bool isToUnsigned = std::is_unsigned<T>::value;
	const bool isFromReal = std::is_floating_point<S>::value;
	const bool isFromUnsigned = std::is_unsigned<S>::value;

	if (isToReal && isFromReal && isToSmaller)
	{
		if (s > static_cast<S>(std::numeric_limits<T>::max()) ||
			s < static_cast<S>(std::numeric_limits<T>::lowest()))
		{
			il2cpp_ThrowOverflow();
		}
	}
	else if (!isToReal)
	{
		if (isFromReal || isToSmaller)
		{
			if (s >= static_cast<S>(std::numeric_limits<T>::max()) + 1)
			{
				il2cpp_ThrowOverflow();
			}
			if (!isFromUnsigned &&
				s <= static_cast<S>(std::numeric_limits<T>::lowest()) - 1)
			{
				il2cpp_ThrowOverflow();
			}
		}
		else
		{
			using common_t = std::common_type_t<T, S>;
			if (isEquals &&
				static_cast<common_t>(s) > static_cast<common_t>(std::numeric_limits<T>::max()))
			{
				il2cpp_ThrowOverflow();
			}
			if (isToUnsigned && s < 0)
			{
				il2cpp_ThrowOverflow();
			}
		}
	}

	return static_cast<T>(s);
}

void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index);
void il2cpp_CheckRange(int64_t lowerBound, int64_t length, int64_t index, int64_t rangeLen);
float il2cpp_Remainder(float numer, float denom);
double il2cpp_Remainder(double numer, double denom);
float il2cpp_Ckfinite(float num);
double il2cpp_Ckfinite(double num);
void il2cpp_ThrowInvalidCast();
void il2cpp_ThrowOverflow();

struct cls_System_Array;

uint32_t il2cpp_SZArray__LoadLength(cls_System_Array* ary);
uint32_t il2cpp_Array__GetLength(cls_System_Array* ary);
uint64_t il2cpp_Array__GetLongLength(cls_System_Array* ary);
uint32_t il2cpp_Array__GetLength(cls_System_Array* ary, uint32_t dim);
int32_t il2cpp_Array__GetLowerBound(cls_System_Array* ary, uint32_t dim);
int32_t il2cpp_Array__GetUpperBound(cls_System_Array* ary, uint32_t dim);
void il2cpp_Array__Copy(cls_System_Array* srcAry, uint32_t srcIdx, cls_System_Array* dstAry, uint32_t dstIdx, uint32_t copyLen);
void il2cpp_Array__Clear(cls_System_Array* ary, uint32_t idx, uint32_t clearLen);
void il2cpp_Array__Init(cls_System_Array* ary, il2cppFieldInfo* fldInfo);
