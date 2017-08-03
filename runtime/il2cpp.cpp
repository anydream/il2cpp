#include <stdlib.h>
#include <thread>
#include "il2cpp.h"

struct il2cppObject
{
	uint32_t objectTypeID;
};

void* il2cpp_New(uint32_t sz, uint32_t typeID)
{
	il2cppObject* obj = (il2cppObject*)calloc(1, sz);
	obj->objectTypeID = typeID;
	return obj;
}

void il2cpp_Yield()
{
	std::this_thread::yield();
}
