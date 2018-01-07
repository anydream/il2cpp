// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.0.20512.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.

// Metadata version: v4.0.20512
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}

// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit SameMethodImpl.Override.HelloWorld
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    // Code size       171 (0xab)
    .maxstack  3
    .locals init (bool V_0,
             class [mscorlib]System.Reflection.MethodInfo V_1,
             class [mscorlib]System.Type V_2,
             int32 V_3,
             class [mscorlib]System.Reflection.MethodInfo[] V_4,
             int32 V_5,
             bool V_6)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  nop
    IL_0004:  ldtoken    SameMethodImpl.Override.HelloWorld
    IL_0009:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000e:  ldc.i4.s   24
    IL_0010:  callvirt   instance class [mscorlib]System.Reflection.MethodInfo[] [mscorlib]System.Type::GetMethods(valuetype [mscorlib]System.Reflection.BindingFlags)
    IL_0015:  stloc.s    V_4
    IL_0017:  ldc.i4.0
    IL_0018:  stloc.s    V_5
    IL_001a:  br.s       IL_0088

    IL_001c:  ldloc.s    V_4
    IL_001e:  ldloc.s    V_5
    IL_0020:  ldelem.ref
    IL_0021:  stloc.1

    IL_0033:  nop
    .try
    {
      IL_0034:  nop
      IL_0035:  ldloc.1
      IL_0036:  ldnull
      IL_0037:  ldc.i4.0
      IL_0038:  newarr     [mscorlib]System.Object
      IL_003d:  callvirt   instance object [mscorlib]System.Reflection.MethodBase::Invoke(object,
                                                                                          object[])
      IL_0042:  castclass  [mscorlib]System.Type
      IL_0047:  stloc.2

      IL_005e:  ldc.i4.0
      IL_005f:  stloc.0
      IL_0060:  nop
      IL_0061:  leave.s    IL_0080

    }  // end .try
    catch [mscorlib]System.Object 
    {
      IL_0063:  pop
      IL_0064:  nop

      IL_007e:  leave.s    IL_0080

    }  // end handler
    IL_0080:  nop
    IL_0081:  nop
    IL_0082:  ldloc.s    V_5
    IL_0084:  ldc.i4.1
    IL_0085:  add
    IL_0086:  stloc.s    V_5
    IL_0088:  ldloc.s    V_5
    IL_008a:  ldloc.s    V_4
    IL_008c:  ldlen
    IL_008d:  conv.i4
    IL_008e:  clt
    IL_0090:  stloc.s    V_6
    IL_0092:  ldloc.s    V_6
    IL_0094:  brtrue.s   IL_001c

    IL_0096:  ldloc.0
    IL_0097:  ldc.i4.0
    IL_0098:  ceq
    IL_009a:  stloc.s    V_6
    IL_009c:  ldloc.s    V_6
    IL_009e:  brtrue.s   IL_00a5

    IL_00a0:  ldc.i4.s   100
    IL_00a2:  stloc.3
    IL_00a3:  br.s       IL_00a9

    IL_00a5:  ldc.i4.m1
    IL_00a6:  stloc.3
    IL_00a7:  br.s       IL_00a9

    IL_00a9:  ldloc.3
    IL_00aa:  ret
  } // end of method SameMethodImpl.Override.HelloWorld::Main

  .method public hidebysig static class [mscorlib]System.Type 
          Test_B() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    SameMethodImpl.Override.B
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method SameMethodImpl.Override.HelloWorld::Test_B

  .method public hidebysig static class [mscorlib]System.Type 
          Test_C() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (class [mscorlib]System.Type V_0)
    IL_0000:  nop
    IL_0001:  ldtoken    SameMethodImpl.Override.C
    IL_0006:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method SameMethodImpl.Override.HelloWorld::Test_C

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method SameMethodImpl.Override.HelloWorld::.ctor

} // end of class SameMethodImpl.Override.HelloWorld

.class private auto ansi beforefieldinit SameMethodImpl.Override.A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  Print() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.A::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method SameMethodImpl.Override.A::.ctor

} // end of class SameMethodImpl.Override.A

.class private auto ansi beforefieldinit SameMethodImpl.Override.B
       extends SameMethodImpl.Override.A
{
  .method public hidebysig newslot virtual 
          instance void  Print1() cil managed
  {
	.override SameMethodImpl.Override.A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.B::Print1

  .method public hidebysig newslot virtual 
          instance void  Print2() cil managed
  {
	.override SameMethodImpl.Override.A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.B::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void SameMethodImpl.Override.A::.ctor()
    IL_0006:  ret
  } // end of method SameMethodImpl.Override.B::.ctor

} // end of class SameMethodImpl.Override.B

.class private auto ansi beforefieldinit SameMethodImpl.Override.CBase
       extends SameMethodImpl.Override.A
{
  .method public hidebysig virtual instance void 
          Print() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.CBase::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void SameMethodImpl.Override.A::.ctor()
    IL_0006:  ret
  } // end of method SameMethodImpl.Override.CBase::.ctor

} // end of class SameMethodImpl.Override.CBase

.class private auto ansi beforefieldinit SameMethodImpl.Override.C
       extends SameMethodImpl.Override.CBase
{
  .method public hidebysig newslot virtual 
          instance void  Print1() cil managed
  {
	.override SameMethodImpl.Override.A::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.C::Print1

  .method public hidebysig newslot virtual 
          instance void  Print2() cil managed
  {
	.override SameMethodImpl.Override.CBase::Print
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  nop

    IL_000c:  ret
  } // end of method SameMethodImpl.Override.C::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void SameMethodImpl.Override.CBase::.ctor()
    IL_0006:  ret
  } // end of method SameMethodImpl.Override.C::.ctor

} // end of class SameMethodImpl.Override.C


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file SameMethodImpl.Override.HelloWorld.res
