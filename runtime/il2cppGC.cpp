#include "il2cpp.h"
#include "il2cppGC.h"
#include <gc.h>

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

static void GC_CALLBACK FinalizerCallback(void* obj, void* cdata)
{
	((IL2CPP_FINALIZER_FUNC)cdata)((cls_Object*)obj);
}

void il2cpp_GC_RegisterFinalizer(cls_Object* obj, IL2CPP_FINALIZER_FUNC finalizer)
{
	IL2CPP_ASSERT(finalizer != nullptr);
	GC_REGISTER_FINALIZER_NO_ORDER(obj, &FinalizerCallback, (void*)finalizer, nullptr, nullptr);
}

void il2cpp_GC_Collect()
{
	GC_gcollect();
}

#if defined(IL2CPP_PATCH_LLVM)
extern "C" void* _il2cpp_GC_PatchCalloc(uintptr_t nelem, uintptr_t sz)
{
	if (nelem != 1 && sz == 1)
		return il2cpp_GC_AllocAtomic(nelem);
	else if (nelem == 1 && sz != 1)
		return il2cpp_GC_Alloc(sz);
	else
		IL2CPP_TRAP();
	return nullptr;
}
#endif
