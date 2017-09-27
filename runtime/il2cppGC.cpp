#include <stdlib.h>
#include <string.h>
#include <gc.h>
#include "il2cppGC.h"

void il2cpp_GC_Init()
{
	GC_INIT();
}

void* il2cpp_GC_Alloc(uintptr_t sz)
{
	return GC_MALLOC(sz);
}

void* il2cpp_GC_AllocAtomic(uintptr_t sz)
{
	void* ptr = GC_MALLOC_ATOMIC(sz);
	memset(ptr, 0, sz);
	return ptr;
}

#if defined(IL2CPP_PATCH_LLVM)
extern "C" void* _il2cpp_GC_PatchCalloc(uintptr_t nelem, uintptr_t sz)
{
	if (nelem != 1 && sz == 1)
		return il2cpp_GC_AllocAtomic(nelem);
	else if (nelem == 1 && sz != 1)
		return il2cpp_GC_Alloc(sz);
	else
		abort();
}
#endif
