// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft (R) .NET Framework IL Disassembler.  Version 2.0.50727.4926
//  Copyright (c) Microsoft Corporation.  All rights reserved.

// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi InterfaceFolding.Ambiguous.I`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Foo() cil managed
  {
  } // end of method InterfaceFolding.Ambiguous.I`1::Foo

} // end of class InterfaceFolding.Ambiguous.I`1

.class private auto ansi beforefieldinit InterfaceFolding.Ambiguous.A`1<U>
       extends [mscorlib]System.Object
       implements class InterfaceFolding.Ambiguous.I`1<!U>
{
  .method private hidebysig newslot virtual final 
          instance string  'I<U>.Foo'() cil managed
  {
    .override  method instance string class InterfaceFolding.Ambiguous.I`1<!U>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "A::Foo"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method InterfaceFolding.Ambiguous.A`1::'I<U>.Foo'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceFolding.Ambiguous.A`1::.ctor

} // end of class InterfaceFolding.Ambiguous.A`1

.class private auto ansi beforefieldinit InterfaceFolding.Ambiguous.B`2<V,W>
       extends class InterfaceFolding.Ambiguous.A`1<!V>
       implements class InterfaceFolding.Ambiguous.I`1<!W>
{
  .method private hidebysig newslot virtual final instance string  'I<W>.Foo'() cil managed
  {
    .override  method instance string class InterfaceFolding.Ambiguous.I`1<!W>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B::Foo1"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method InterfaceFolding.Ambiguous.B`2::'I<W>.Foo'

  .method private hidebysig newslot virtual final instance string  'I<V>.Foo'() cil managed
  {
    .override  method instance string class InterfaceFolding.Ambiguous.I`1<!V>::Foo()
    // Code size       11 (0xb)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "B::Foo2"
    IL_0006:  stloc.0
    IL_0007:  br.s       IL_0009

    IL_0009:  ldloc.0
    IL_000a:  ret
  } // end of method InterfaceFolding.Ambiguous.B`2::'I<W>.Foo'

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void class InterfaceFolding.Ambiguous.A`1<!V>::.ctor()
    IL_0006:  ret
  } // end of method InterfaceFolding.Ambiguous.B`2::.ctor

} // end of class InterfaceFolding.Ambiguous.B`2

.class public auto ansi beforefieldinit InterfaceFolding.Ambiguous.Test
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    
    // Code size       96 (0x60)
    .maxstack  2
    .locals init (class InterfaceFolding.Ambiguous.B`2<int32,int32> V_0,
             class InterfaceFolding.Ambiguous.B`2<string,string> V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void class InterfaceFolding.Ambiguous.B`2<int32,int32>::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  callvirt   instance string class InterfaceFolding.Ambiguous.I`1<int32>::Foo()
    IL_000d:  ldstr      "B::Foo2"
    IL_0012:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0017:  ldc.i4.0
    IL_0018:  ceq
    IL_001a:  stloc.3
    IL_001b:  ldloc.3
    IL_001c:  brtrue.s   IL_0020

    IL_001e:  br.s       IL_004f

    IL_0020:  newobj     instance void class InterfaceFolding.Ambiguous.B`2<string,string>::.ctor()
    IL_0025:  stloc.1
    IL_0026:  ldloc.1
    IL_0027:  callvirt   instance string class InterfaceFolding.Ambiguous.I`1<string>::Foo()
    IL_002c:  ldstr      "B::Foo2"
    IL_0031:  call       bool [mscorlib]System.String::op_Inequality(string,
                                                                     string)
    IL_0036:  ldc.i4.0
    IL_0037:  ceq
    IL_0039:  stloc.3
    IL_003a:  ldloc.3
    IL_003b:  brtrue.s   IL_003f

    IL_003d:  br.s       IL_004f

    IL_003f:  ldstr      "Pass"
    IL_0044:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0049:  nop
    IL_004a:  ldc.i4.s   100
    IL_004c:  stloc.2
    IL_004d:  br.s       IL_005e

    IL_004f:  ldstr      "Failed!"
    IL_0054:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0059:  nop
    IL_005a:  ldc.i4.m1
    IL_005b:  stloc.2
    IL_005c:  br.s       IL_005e

    IL_005e:  ldloc.2
    IL_005f:  ret
  } // end of method InterfaceFolding.Ambiguous.Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method InterfaceFolding.Ambiguous.Test::.ctor

} // end of class InterfaceFolding.Ambiguous.Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Ambiguous.res
