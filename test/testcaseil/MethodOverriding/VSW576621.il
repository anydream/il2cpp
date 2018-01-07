// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {}

.class public MethodOverriding.VSW576621.C1
extends [mscorlib]System.Object
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void [mscorlib]System.Object::.ctor() 
      ret
   }
   .method public   virtual instance int32 M1()
   {
      ldc.i4 1
      ret
   }
   .method public   virtual instance int32 M2()
   {
      ldc.i4 2
      ret
   }
   .method public   virtual instance int32 M3()
   {
      ldc.i4 3
      ret
   }
}

.class public MethodOverriding.VSW576621.C2
extends MethodOverriding.VSW576621.C1
{
   .method public hidebysig specialname rtspecialname 
   instance void  .ctor() cil managed
   {
      ldarg.0
      call instance void MethodOverriding.VSW576621.C1::.ctor() 
      ret
   }
   .method public   virtual instance int32 M3()
   {
    .override MethodOverriding.VSW576621.C1::M2
      ldc.i4 4
      ret
   }
   .method public   virtual instance int32 M4()
   {
    .override MethodOverriding.VSW576621.C1::M1
      ldc.i4 5
      ret
   }
   .method public virtual instance int32 M1()
   {
    .override MethodOverriding.VSW576621.C1::M3
      ldc.i4 6
      ret
   }
}
