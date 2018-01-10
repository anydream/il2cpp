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

.class public auto ansi beforefieldinit Covariant_InherittedCollision.HelloWorld
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    //.entrypoint
    // Code size       129 (0x81)
    .maxstack  2
    .locals init (string V_0,
             string V_1,
             string V_2,
             string V_3,
             int32 V_4,
             bool V_5)
    IL_0000:  nop
    IL_0001:  call       string Covariant_InherittedCollision.HelloWorld::StepAB_1()
    IL_0006:  stloc.0
    IL_0007:  call       string Covariant_InherittedCollision.HelloWorld::StepAB_2()
    IL_000c:  stloc.1
    IL_000d:  call       string Covariant_InherittedCollision.HelloWorld::StepBA_1()
    IL_0012:  stloc.2
    IL_0013:  call       string Covariant_InherittedCollision.HelloWorld::StepBA_2()
    IL_0018:  stloc.3
    IL_0019:  ldloc.0
    IL_001a:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_001f:  nop
    IL_0020:  ldloc.1
    IL_0021:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldloc.2
    IL_0028:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_002d:  nop
    IL_002e:  ldloc.3
    IL_002f:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0034:  nop
    IL_0035:  ldloc.0
    IL_0036:  ldstr      "Covariant_InherittedCollision.XIAB_1.Print(Covariant_InherittedCollision.A='C_1')"
    IL_003b:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0040:  brfalse.s  IL_006c

    IL_0042:  ldloc.1
    IL_0043:  ldstr      "Covariant_InherittedCollision.XIAB_2_1.Print(Covariant_InherittedCollision.B='C_2')"
    IL_0048:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_004d:  brfalse.s  IL_006c

    IL_004f:  ldloc.2
    IL_0050:  ldstr      "Covariant_InherittedCollision.XIBA_1.Print(Covariant_InherittedCollision.B='C_11')"
    IL_0055:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_005a:  brfalse.s  IL_006c

    IL_005c:  ldloc.3
    IL_005d:  ldstr      "Covariant_InherittedCollision.XIBA_2.Print(Covariant_InherittedCollision.A='C_12')"
    IL_0062:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0067:  ldc.i4.0
    IL_0068:  ceq
    IL_006a:  br.s       IL_006d

    IL_006c:  ldc.i4.1
    IL_006d:  stloc.s    V_5
    IL_006f:  ldloc.s    V_5
    IL_0071:  brtrue.s   IL_0079

    IL_0073:  ldc.i4.s   100
    IL_0075:  stloc.s    V_4
    IL_0077:  br.s       IL_007e

    IL_0079:  ldc.i4.m1
    IL_007a:  stloc.s    V_4
    IL_007c:  br.s       IL_007e

    IL_007e:  ldloc.s    V_4
    IL_0080:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::Main

  .method public hidebysig static string 
          StepAB_1() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_InherittedCollision.XIAB_1::.ctor()
    IL_0006:  castclass  class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "1"
    IL_000e:  newobj     instance void Covariant_InherittedCollision.C::.ctor(string)
    IL_0013:  callvirt   instance string class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::StepAB_1

  .method public hidebysig static string 
          StepAB_2() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_InherittedCollision.XIAB_2_1::.ctor()
    IL_0006:  castclass  class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "2"
    IL_000e:  newobj     instance void Covariant_InherittedCollision.C::.ctor(string)
    IL_0013:  callvirt   instance string class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::StepAB_2

  .method public hidebysig static string 
          StepBA_1() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    .locals init (class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_InherittedCollision.XIBA_1::.ctor()
    IL_0006:  castclass  class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "11"
    IL_000f:  newobj     instance void Covariant_InherittedCollision.C::.ctor(string)
    IL_0014:  callvirt   instance string class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>::Print(!0)
    IL_0019:  stloc.1
    IL_001a:  br.s       IL_001c

    IL_001c:  ldloc.1
    IL_001d:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::StepBA_1

  .method public hidebysig static string 
          StepBA_2() cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  3
    .locals init (class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_InherittedCollision.XIBA_2::.ctor()
    IL_0006:  castclass  class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "12"
    IL_000f:  newobj     instance void Covariant_InherittedCollision.C::.ctor(string)
    IL_0014:  callvirt   instance string class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.C>::Print(!0)
    IL_0019:  stloc.1
    IL_001a:  br.s       IL_001c

    IL_001c:  ldloc.1
    IL_001d:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::StepBA_2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_InherittedCollision.HelloWorld::.ctor

} // end of class Covariant_InherittedCollision.HelloWorld

.class interface private abstract auto ansi Covariant_InherittedCollision.I`1<-T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method Covariant_InherittedCollision.I`1::Print

} // end of class Covariant_InherittedCollision.I`1

.class private auto ansi beforefieldinit Covariant_InherittedCollision.A
       extends [mscorlib]System.Object
{
  .field family string 'value'
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       32 (0x20)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  nop
    IL_0008:  ldarg.0
    IL_0009:  ldstr      "A_"
    IL_000e:  ldarg.1
    IL_0014:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0019:  stfld      string Covariant_InherittedCollision.A::'value'
    IL_001e:  nop
    IL_001f:  ret
  } // end of method Covariant_InherittedCollision.A::.ctor

  .method public hidebysig specialname instance string 
          get_Value() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      string Covariant_InherittedCollision.A::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Covariant_InherittedCollision.A::get_Value

  .property instance string Value()
  {
    .get instance string Covariant_InherittedCollision.A::get_Value()
  } // end of property Covariant_InherittedCollision.A::Value
} // end of class Covariant_InherittedCollision.A

.class private auto ansi beforefieldinit Covariant_InherittedCollision.B
       extends Covariant_InherittedCollision.A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Covariant_InherittedCollision.A::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "B_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Covariant_InherittedCollision.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Covariant_InherittedCollision.B::.ctor

} // end of class Covariant_InherittedCollision.B

.class private auto ansi beforefieldinit Covariant_InherittedCollision.C
       extends Covariant_InherittedCollision.B
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Covariant_InherittedCollision.B::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "C_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Covariant_InherittedCollision.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Covariant_InherittedCollision.C::.ctor

} // end of class Covariant_InherittedCollision.C

.class private auto ansi beforefieldinit Covariant_InherittedCollision.XIAB_1
       extends [mscorlib]System.Object
       implements class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_InherittedCollision.A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_InherittedCollision.XIAB_1.Print(Covariant_InherittedCollision.A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_InherittedCollision.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_InherittedCollision.XIAB_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_InherittedCollision.XIAB_1::.ctor

} // end of class Covariant_InherittedCollision.XIAB_1

.class private auto ansi beforefieldinit Covariant_InherittedCollision.XIAB_2_1
       extends Covariant_InherittedCollision.XIAB_1
       implements class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_InherittedCollision.B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_InherittedCollision.XIAB_2_1.Print(Covariant_InherittedCollision.B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_InherittedCollision.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_InherittedCollision.XIAB_2_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Covariant_InherittedCollision.XIAB_1::.ctor()
    IL_0006:  ret
  } // end of method Covariant_InherittedCollision.XIAB_2_1::.ctor

} // end of class Covariant_InherittedCollision.XIAB_2_1

.class private auto ansi beforefieldinit Covariant_InherittedCollision.XIBA_1
       extends [mscorlib]System.Object
       implements class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_InherittedCollision.B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_InherittedCollision.XIBA_1.Print(Covariant_InherittedCollision.B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_InherittedCollision.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_InherittedCollision.XIBA_1::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_InherittedCollision.XIBA_1::.ctor

} // end of class Covariant_InherittedCollision.XIBA_1

.class private auto ansi beforefieldinit Covariant_InherittedCollision.XIBA_2
       extends Covariant_InherittedCollision.XIBA_1
       implements class Covariant_InherittedCollision.I`1<class Covariant_InherittedCollision.A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_InherittedCollision.A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_InherittedCollision.XIBA_2.Print(Covariant_InherittedCollision.A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_InherittedCollision.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_InherittedCollision.XIBA_2::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void Covariant_InherittedCollision.XIBA_1::.ctor()
    IL_0006:  ret
  } // end of method Covariant_InherittedCollision.XIBA_2::.ctor

} // end of class Covariant_InherittedCollision.XIBA_2


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Covariant_InherittedCollision.HelloWorld.res
