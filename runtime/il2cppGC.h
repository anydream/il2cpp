#pragma once

struct cls_Object;

void il2cpp_GC_Init();
void* il2cpp_GC_Alloc(uintptr_t sz);
void* il2cpp_GC_AllocAtomic(uintptr_t sz);
void il2cpp_GC_RegisterFinalizer(cls_Object* obj, void(*finalizer)(cls_Object*));
