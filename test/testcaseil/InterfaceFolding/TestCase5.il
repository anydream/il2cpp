// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

// =============== CLASS MEMBERS DECLARATION ===================
.class interface private abstract auto ansi InterfaceFolding.TestCase5.I`1<T>
{ .method public hidebysig newslot abstract virtual instance string  Foo() cil managed {} }

.class interface private abstract auto ansi InterfaceFolding.TestCase5.J
{ 
  .method public hidebysig newslot abstract virtual instance string  Bar1() cil managed {}
  .method public hidebysig newslot abstract virtual instance string  Bar2() cil managed {}
}

.class private auto ansi beforefieldinit InterfaceFolding.TestCase5.A`1<U> 
		implements class InterfaceFolding.TestCase5.I`1<!U>
{
  .method public hidebysig newslot virtual instance string  Foo() cil managed
  {
    ldstr      "A::Foo"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar1() cil managed
  {
    .maxstack  8
    ldstr      "A::Bar1"
    ret
  }
  .method public hidebysig newslot virtual instance string  Bar2() cil managed
  {
    .maxstack  8
    ldstr      "A::Bar2"
    ret
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}

.class private auto ansi beforefieldinit InterfaceFolding.TestCase5.B`2<V,W>
       extends class InterfaceFolding.TestCase5.A`1<!V>
       implements class InterfaceFolding.TestCase5.I`1<!W>, InterfaceFolding.TestCase5.J
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}

.class private auto ansi beforefieldinit InterfaceFolding.TestCase5.C extends class InterfaceFolding.TestCase5.B`2<class InterfaceFolding.TestCase5.C,class InterfaceFolding.TestCase5.C>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}

.class public auto ansi beforefieldinit InterfaceFolding.TestCase5.Test extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    

    newobj     instance void InterfaceFolding.TestCase5.C::.ctor()
	callvirt   instance string class InterfaceFolding.TestCase5.C::Foo()
    ldstr      "A::Foo"
    call       bool [mscorlib]System.String::op_Inequality(string, string)
    brtrue     FAILURE

    newobj     instance void InterfaceFolding.TestCase5.C::.ctor()
    callvirt   instance string class InterfaceFolding.TestCase5.C::Bar1()
    ldstr      "A::Bar1"
    call       bool [mscorlib]System.String::op_Inequality(string,string)
    brtrue     FAILURE    
	
	newobj     instance void InterfaceFolding.TestCase5.C::.ctor()
    callvirt   instance string class InterfaceFolding.TestCase5.C::Bar2()
    ldstr      "A::Bar2"
    call       bool [mscorlib]System.String::op_Inequality(string,string)
    brtrue     FAILURE
	
	
PASS:
    ldstr      "Pass"
    pop
    ldc.i4.s   100
    ret

FAILURE:
    ldstr      "Failed!"
    pop
    ldc.i4.m1
    ret
  }
}
