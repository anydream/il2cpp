// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this test is a regression case for VSWhidbey 163172
// Class MethodImplFinal.A has method f()
// Class MethodImplFinal.B extends MethodImplFinal.A and explicitly implements MethodImplFinal.A::f() (with MethodImplFinal.B::g()) MethodImplFinal.B::g() is final 
// Class MethodImplFinal.C extends MethodImplFinal.B and implicitly implements MethodImplFinal.A::f()
// Class MethodImplFinal.D extends MethodImplFinal.A and implicitly implements MethodImplFinal.A::f()

// The final on MethodImplFinal.B::g() causd the target slot (MethodImplFinal.A::f()) to be marked final as well and we got 
// TypeLoadException when instantiating MethodImplFinal.C and MethodImplFinal.D



// Metadata version: v1.2.30801
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .hash = (BB F5 50 CF 5C DC 3F F4 03 2C 76 78 5A 79 37 C7   // ..P.\.?..,vxZy7.
           A4 08 4B CE )                                     // ..K.
  .ver 1:2:3400:0
}

// ================== GLOBAL METHODS =========================
.class public auto ansi beforefieldinit MethodImplFinal.CMain
       extends [mscorlib]System.Object
{
.method public static int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        Main() cil managed
  {
    //.entrypoint

    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             int32 V_1)
    .try
    {
    	
    	newobj     instance void MethodImplFinal.C::.ctor()
  		pop
  		newobj     instance void MethodImplFinal.D::.ctor()
  		pop
      ldstr      "PASS"
      pop//call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   100
      stloc.1
      leave.s    IL_0027

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      stloc.0
      ldstr      "FAIL: Caught unexpected exception: "
      ldloc.0
      call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      pop//call       void [System.Console]System.Console::WriteLine(string)
      nop
      ldc.i4.s   101
      stloc.1
      IL_0025:  leave.s    IL_0027

    }  // end handler
    IL_0027:  nop
    ldloc.1
    ret
} // end of global method main
}

// =============================================================


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi MethodImplFinal.A
       extends [mscorlib]System.Object
{
  .method public newslot virtual instance void 
          f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method MethodImplFinal.A::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method MethodImplFinal.A::.ctor

} // end of class MethodImplFinal.A

.class public auto ansi MethodImplFinal.B
       extends MethodImplFinal.A
{
  .method public newslot virtual final instance void 
          g() cil managed
  {
    .override MethodImplFinal.A::f
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method MethodImplFinal.B::g

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void MethodImplFinal.A::.ctor()
    ret
  } // end of method MethodImplFinal.B::.ctor

} // end of class MethodImplFinal.B

.class public auto ansi MethodImplFinal.C
       extends MethodImplFinal.B
{
  .method public virtual instance void  f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method MethodImplFinal.C::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void MethodImplFinal.B::.ctor()
    ret
  } // end of method MethodImplFinal.C::.ctor

} // end of class MethodImplFinal.C


.class public auto ansi MethodImplFinal.D
       extends MethodImplFinal.A
{
  .method public virtual instance void  f() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  0
    ret
  } // end of method MethodImplFinal.C::f

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    ldarg.0
    call       instance void MethodImplFinal.A::.ctor()
    ret
  } // end of method MethodImplFinal.C::.ctor

} // end of class MethodImplFinal.C


// =============================================================

.data D_00008030 = bytearray (
                 01 00 00 06) 
//*********** DISASSEMBLY COMPLETE ***********************
