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

.class public auto ansi beforefieldinit Variant_CollapsedInterfaces.HelloWorld
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 
          Main() cil managed
  {
    //.entrypoint
    // Code size       72 (0x48)
    .maxstack  2
    .locals init (string V_0,
             string V_1,
             int32 V_2,
             bool V_3)
    IL_0000:  nop
    IL_0001:  call       string Variant_CollapsedInterfaces.HelloWorld::StepBC()
    IL_0006:  stloc.0
    IL_0007:  call       string Variant_CollapsedInterfaces.HelloWorld::StepCB()
    IL_000c:  stloc.1
    IL_000d:  ldloc.0
    IL_000e:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_0013:  nop
    IL_0014:  ldloc.1
    IL_0015:  pop//call       void [System.Console]System.Console::WriteLine(string)
    IL_001a:  nop
    IL_001b:  ldloc.0
    IL_001c:  ldstr      "B_1"
    IL_0021:  call       bool [mscorlib]System.String::op_Equality(string,
                                                                   string)
    IL_0026:  brfalse.s  IL_0038

    IL_0028:  ldloc.1
    IL_0029:  ldstr      "C_11"
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
  } // end of method Variant_CollapsedInterfaces.HelloWorld::Main

  .method public hidebysig static string 
          StepBC() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A> V_0,
             class Variant_CollapsedInterfaces.A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void Variant_CollapsedInterfaces.XBC::.ctor()
    IL_0006:  castclass  class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "1"
    IL_000e:  callvirt   instance !0 class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A>::Print(string)
    IL_0013:  stloc.1
    IL_0014:  ldloc.1
    IL_0015:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_001a:  stloc.2
    IL_001b:  br.s       IL_001d

    IL_001d:  ldloc.2
    IL_001e:  ret
  } // end of method Variant_CollapsedInterfaces.HelloWorld::StepBC

  .method public hidebysig static string 
          StepCB() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  2
    .locals init (class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A> V_0,
             class Variant_CollapsedInterfaces.A V_1,
             string V_2)
    IL_0000:  nop
    IL_0001:  newobj     instance void Variant_CollapsedInterfaces.XCB::.ctor()
    IL_0006:  castclass  class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A>
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  ldstr "11"
    IL_000f:  callvirt   instance !0 class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.A>::Print(string)
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_001b:  stloc.2
    IL_001c:  br.s       IL_001e

    IL_001e:  ldloc.2
    IL_001f:  ret
  } // end of method Variant_CollapsedInterfaces.HelloWorld::StepCB

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Variant_CollapsedInterfaces.HelloWorld::.ctor

} // end of class Variant_CollapsedInterfaces.HelloWorld

.class interface private abstract auto ansi Variant_CollapsedInterfaces.I`1<+T>
{
  .method public hidebysig newslot abstract virtual 
          instance !T  Print(string index) cil managed
  {
  } // end of method Variant_CollapsedInterfaces.I`1::Print

} // end of class Variant_CollapsedInterfaces.I`1


.class private auto ansi beforefieldinit Variant_CollapsedInterfaces.A
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
    IL_0019:  stfld      string Variant_CollapsedInterfaces.A::'value'
    IL_001e:  nop
    IL_001f:  ret
  } // end of method Variant_CollapsedInterfaces.A::.ctor

  .method public hidebysig specialname instance string 
          get_Value() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (string V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      string Variant_CollapsedInterfaces.A::'value'
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method Variant_CollapsedInterfaces.A::get_Value

  .property instance string Value()
  {
    .get instance string Variant_CollapsedInterfaces.A::get_Value()
  } // end of property Variant_CollapsedInterfaces.A::Value
} // end of class Variant_CollapsedInterfaces.A

.class private auto ansi beforefieldinit Variant_CollapsedInterfaces.B
       extends Variant_CollapsedInterfaces.A
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Variant_CollapsedInterfaces.A::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "B_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Variant_CollapsedInterfaces.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Variant_CollapsedInterfaces.B::.ctor

} // end of class Variant_CollapsedInterfaces.B

.class private auto ansi beforefieldinit Variant_CollapsedInterfaces.C
       extends Variant_CollapsedInterfaces.B
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string index) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void Variant_CollapsedInterfaces.B::.ctor(string)
    IL_0007:  nop
    IL_0008:  nop
    IL_0009:  ldarg.0
    IL_000a:  ldstr      "C_"
    IL_000f:  ldarg.1
    IL_0015:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_001a:  stfld      string Variant_CollapsedInterfaces.A::'value'
    IL_001f:  nop
    IL_0020:  ret
  } // end of method Variant_CollapsedInterfaces.C::.ctor

} // end of class Variant_CollapsedInterfaces.C

.class private auto ansi beforefieldinit Variant_CollapsedInterfaces.XBC
       extends [mscorlib]System.Object
       implements class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.B>,
                  class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.C>
{
  .method public hidebysig newslot virtual final 
          instance class Variant_CollapsedInterfaces.B  Print(string index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init (class Variant_CollapsedInterfaces.B V_0,
             class Variant_CollapsedInterfaces.B V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void Variant_CollapsedInterfaces.B::.ctor(string)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    Variant_CollapsedInterfaces.B Variant_CollapsedInterfaces.XBC.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_0019:  pop//call       void [System.Console]System.Console::WriteLine(string, object, object)
	pop
	pop
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method Variant_CollapsedInterfaces.XBC::Print

  .method public hidebysig newslot virtual final 
          instance class Variant_CollapsedInterfaces.C  Print(string index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init (class Variant_CollapsedInterfaces.C V_0,
             class Variant_CollapsedInterfaces.C V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void Variant_CollapsedInterfaces.C::.ctor(string)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    Variant_CollapsedInterfaces.C Variant_CollapsedInterfaces.XBC.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_0019:  pop//call       void [System.Console]System.Console::WriteLine(string, object, object)
	pop
	pop
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method Variant_CollapsedInterfaces.XBC::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Variant_CollapsedInterfaces.XBC::.ctor

} // end of class Variant_CollapsedInterfaces.XBC

.class private auto ansi beforefieldinit Variant_CollapsedInterfaces.XCB
       extends [mscorlib]System.Object
       implements class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.C>,
                  class Variant_CollapsedInterfaces.I`1<class Variant_CollapsedInterfaces.B>
{
  .method public hidebysig newslot virtual final 
          instance class Variant_CollapsedInterfaces.C  Print(string index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init (class Variant_CollapsedInterfaces.C V_0,
             class Variant_CollapsedInterfaces.C V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void Variant_CollapsedInterfaces.C::.ctor(string)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    Variant_CollapsedInterfaces.C Variant_CollapsedInterfaces.XCB.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_0019:  pop//call       void [System.Console]System.Console::WriteLine(string, object, object)
	pop
	pop
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method Variant_CollapsedInterfaces.XCB::Print

  .method public hidebysig newslot virtual final 
          instance class Variant_CollapsedInterfaces.B  Print(string index) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init (class Variant_CollapsedInterfaces.B V_0,
             class Variant_CollapsedInterfaces.B V_1)
    IL_0000:  nop
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void Variant_CollapsedInterfaces.B::.ctor(string)
    IL_0007:  stloc.0
    IL_0008:  ldstr      "    Variant_CollapsedInterfaces.B Variant_CollapsedInterfaces.XCB.Print(index={0}) = '{1}'"
    IL_000d:  ldarg.1
    IL_0013:  ldloc.0
    IL_0014:  callvirt   instance string Variant_CollapsedInterfaces.A::get_Value()
    IL_0019:  pop//call       void [System.Console]System.Console::WriteLine(string, object, object)
	pop
	pop
    IL_001e:  nop
    IL_001f:  ldloc.0
    IL_0020:  stloc.1
    IL_0021:  br.s       IL_0023

    IL_0023:  ldloc.1
    IL_0024:  ret
  } // end of method Variant_CollapsedInterfaces.XCB::Print2

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Variant_CollapsedInterfaces.XCB::.ctor

} // end of class Variant_CollapsedInterfaces.XCB


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file Variant_CollapsedInterfaces.HelloWorld.res
