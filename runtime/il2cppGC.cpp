#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <gc.h>

void il2cpp_InitGC()
{
	GC_INIT();
}

extern "C" void* _il2cpp_PatchCalloc(uintptr_t nelem, uintptr_t sz)
{
	if (nelem != 1 && sz == 1)
	{
		void* ptr = GC_MALLOC_ATOMIC(nelem);
		memset(ptr, 0, nelem);
		return ptr;
	}
	else if (nelem == 1 && sz != 1)
		return GC_MALLOC(sz);
	else
		abort();
}
