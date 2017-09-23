#include <stdlib.h>
#include <thread>
#include "il2cpp.h"

#if defined(_WIN32)
#include <windows.h>
#else
#include <sys/types.h>
#endif

#include "il2cppBridge.h"

void* il2cpp_New(uint32_t sz, uint32_t typeID, int isNoRef)
{
	cls_Object* obj = (cls_Object*)calloc(1, sz);
	obj->TypeID = typeID;
	return obj;
}

void il2cpp_Yield()
{
#if defined(_WIN32)
	Sleep(0);
#else
	std::this_thread::yield();
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
