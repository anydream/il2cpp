#include "il2cpp.h"
#include "il2cppGC.h"
#include <gc.h>

void il2cpp_GC_Init()
{
	GC_INIT();
#if defined(GC_THREADS)
	GC_allow_register_threads();
#endif
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

bool il2cpp_GC_RegisterThread(void* basePtr)
{
#if defined(GC_THREADS)
	GC_stack_base sb;
	int res = GC_get_stack_base(&sb);
	if (res != GC_SUCCESS)
		sb.mem_base = basePtr;

	res = GC_register_my_thread(&sb);
	if ((res != GC_SUCCESS) && (res != GC_DUPLICATE))
		return false;
	return true;
#else
	return false;
#endif
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
		IL2CPP_TRAP;
	return nullptr;
}
#endif
