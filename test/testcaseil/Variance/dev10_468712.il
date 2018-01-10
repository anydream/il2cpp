// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.10707.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.

// Metadata version: v4.0.10707
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

// =============== CLASS MEMBERS DECLARATION ===================


.class interface private abstract auto ansi ConsoleApplicationVB.ICovariant`1<+ T>
{
  .method public newslot abstract strict virtual instance void  f<U>() cil managed  { }

} 


.class private auto ansi ConsoleApplicationVB.C`1<T> extends [mscorlib]System.Object implements class ConsoleApplicationVB.ICovariant`1<!T>
{
  .method public specialname rtspecialname instance void  .ctor() cil managed
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerNonUserCodeAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ret
  }

  .method public newslot strict virtual final instance void  f<U>() cil managed
  {
    .override  method instance void class ConsoleApplicationVB.ICovariant`1<!T>::f<[1]>()
    .maxstack  8
    IL_0000:  nop
    IL_0001:  nop
    IL_0002:  ret
  } 
} 


.class public auto ansi sealed ConsoleApplicationVB.Module1 extends [mscorlib]System.Object
{
  .method public static int32  Main() cil managed
  {
    //.entrypoint
    .custom instance void [mscorlib]System.STAThreadAttribute::.ctor() = ( 01 00 00 00 ) 
    .maxstack  1
    .locals init ([0] class ConsoleApplicationVB.ICovariant`1<object> y)
    IL_0000:  nop
    IL_0001:  newobj     instance void class ConsoleApplicationVB.C`1<string>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance void class ConsoleApplicationVB.ICovariant`1<object>::f<class [mscorlib]System.Exception>()
    IL_000d:  nop
    
	ldstr "PASS"
	pop//call void class [System.Console]System.Console::WriteLine(string)
	
    ldc.i4.s 100
	ret
  }
} 


// =============================================================

