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

.class public auto ansi beforefieldinit Covariant_CollapsedInterfaces.HelloWorld
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    // Code size       72 (0x48)
    .maxstack  2
    .locals init (string V_0,
             string V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  call       string Covariant_CollapsedInterfaces.HelloWorld::StepAB()
    IL_0006:  stloc.0
    IL_0007:  call       string Covariant_CollapsedInterfaces.HelloWorld::StepBA()
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_001a:  nop
    IL_001b:  ldloc.0
    IL_001c:  ldstr      "Covariant_CollapsedInterfaces.XAB.Print(Covariant_CollapsedInterfaces.A='C_1')"
    IL_0021:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0026:  brfalse.s  IL_0038

    IL_0028:  ldloc.1
    IL_0029:  ldstr      "Covariant_CollapsedInterfaces.XBA.Print(Covariant_CollapsedInterfaces.B='C_2')"
    IL_002e:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0033:  ldc.i4.0
    IL_0034:  ceq
    IL_0036:  br.s       IL_0039

    IL_0038:  ldc.i4.1
    IL_0039:  stloc.3
    IL_003a:  ldloc.3
    IL_003b:  brtrue.s   IL_0042

    IL_003d:  ldc.i4.s   100
    IL_003f:  stloc.2
    IL_0040:  br.s       IL_0046

    IL_0042:  ldc.i4.m1
    IL_0043:  stloc.2
    IL_0044:  br.s       IL_0046

    IL_0046:  ldloc.2
    IL_0047:  ret
  } // end of method Covariant_CollapsedInterfaces.HelloWorld::Main

  .method public hidebysig static string 
          StepAB() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_CollapsedInterfaces.XAB::.ctor()
    IL_0006:  castclass  class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "1"
    IL_000e:  newobj     instance void Covariant_CollapsedInterfaces.C::.ctor(string)
    IL_0013:  callvirt   instance string class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method Covariant_CollapsedInterfaces.HelloWorld::StepAB

  .method public hidebysig static string 
          StepBA() cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C> V_0,
             string V_1)
    IL_0000:  nop
    IL_0001:  newobj     instance void Covariant_CollapsedInterfaces.XBA::.ctor()
    IL_0006:  castclass  class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "2"
    IL_000e:  newobj     instance void Covariant_CollapsedInterfaces.C::.ctor(string)
    IL_0013:  callvirt   instance string class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.C>::Print(!0)
    IL_0018:  stloc.1
    IL_0019:  br.s       IL_001b

    IL_001b:  ldloc.1
    IL_001c:  ret
  } // end of method Covariant_CollapsedInterfaces.HelloWorld::StepBA

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_CollapsedInterfaces.HelloWorld::.ctor

} // end of class Covariant_CollapsedInterfaces.HelloWorld

.class interface private abstract auto ansi Covariant_CollapsedInterfaces.I`1<-T>
{
  .method public hidebysig newslot abstract virtual 
          instance string  Print(!T t) cil managed
  {
  } // end of method Covariant_CollapsedInterfaces.I`1::Print

} // end of class Covariant_CollapsedInterfaces.I`1


.class private auto ansi beforefieldinit Covariant_CollapsedInterfaces.A
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
    IL_0019:  stfld      string Covariant_CollapsedInterfaces.A::'value'
    IL_001e:  nop
    IL_001f:  ret
  } // end of method Covariant_CollapsedInterfaces.A::.ctor

  .method public hidebysig specialname instance string 
          get_Value() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      string Covariant_CollapsedInterfaces.A::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Covariant_CollapsedInterfaces.A::get_Value

  .property instance string Value()
  {
    .get instance string Covariant_CollapsedInterfaces.A::get_Value()
  } // end of property Covariant_CollapsedInterfaces.A::Value
} // end of class Covariant_CollapsedInterfaces.A

.class private auto ansi beforefieldinit Covariant_CollapsedInterfaces.B
       extends Covariant_CollapsedInterfaces.A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Covariant_CollapsedInterfaces.A::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "B_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Covariant_CollapsedInterfaces.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Covariant_CollapsedInterfaces.B::.ctor

} // end of class Covariant_CollapsedInterfaces.B

.class private auto ansi beforefieldinit Covariant_CollapsedInterfaces.C
       extends Covariant_CollapsedInterfaces.B
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Covariant_CollapsedInterfaces.B::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "C_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Covariant_CollapsedInterfaces.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Covariant_CollapsedInterfaces.C::.ctor

} // end of class Covariant_CollapsedInterfaces.C

.class private auto ansi beforefieldinit Covariant_CollapsedInterfaces.XAB
       extends [mscorlib]System.Object
       implements class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.A>,
                  class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.B>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_CollapsedInterfaces.A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_CollapsedInterfaces.XAB.Print(Covariant_CollapsedInterfaces.A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_CollapsedInterfaces.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_CollapsedInterfaces.XAB::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_CollapsedInterfaces.B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_CollapsedInterfaces.XAB.Print(Covariant_CollapsedInterfaces.B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_CollapsedInterfaces.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_CollapsedInterfaces.XAB::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_CollapsedInterfaces.XAB::.ctor

} // end of class Covariant_CollapsedInterfaces.XAB

.class private auto ansi beforefieldinit Covariant_CollapsedInterfaces.XBA
       extends [mscorlib]System.Object
       implements class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.B>,
                  class Covariant_CollapsedInterfaces.I`1<class Covariant_CollapsedInterfaces.A>
{
  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_CollapsedInterfaces.B b) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_CollapsedInterfaces.XBA.Print(Covariant_CollapsedInterfaces.B='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_CollapsedInterfaces.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_CollapsedInterfaces.XBA::Print

  .method public hidebysig newslot virtual final 
          instance string  Print(class Covariant_CollapsedInterfaces.A a) cil managed
  {
    // Code size       27 (0x1b)
    .maxstack  3
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "Covariant_CollapsedInterfaces.XBA.Print(Covariant_CollapsedInterfaces.A='"
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string Covariant_CollapsedInterfaces.A::get_Value()
    IL_000c:  ldstr      "')"
    IL_0011:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0016:  stloc.0
    IL_0017:  br.s       IL_0019

    IL_0019:  ldloc.0
    IL_001a:  ret
  } // end of method Covariant_CollapsedInterfaces.XBA::Print

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Covariant_CollapsedInterfaces.XBA::.ctor

} // end of class Covariant_CollapsedInterfaces.XBA


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Covariant_CollapsedInterfaces.HelloWorld.res
