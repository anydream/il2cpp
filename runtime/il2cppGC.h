#pragma once

void il2cpp_GC_Init();
void* il2cpp_GC_Alloc(uintptr_t sz);
void* il2cpp_GC_AllocAtomic(uintptr_t sz);
void il2cpp_GC_RegisterFinalizer(cls_Object* obj, IL2CPP_FINALIZER_FUNC finalizer);
