// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// MethodImpl.Desktop.self_override1.MyBar is an abstract class which declares an abstract method MethodImpl.Desktop.self_override1.MyBar::DoBar().
// MethodImpl.Desktop.self_override1.MyBar also DoSelfBar() that explicitly overrides DoBar().
// BarImpl extends MethodImpl.Desktop.self_override1.MyBar and also overrides DoBar().
// Expect that b.DoBar() will invoke BarImpl::DoBar() if called virtually, whether b is a BarImpl reference
// or a MethodImpl.Desktop.self_override1.MyBar reference.  Expect that b.DoBar() will invoke MethodImpl.Desktop.self_override1.MyBar::DoSelfBar() if b is 
// a MethodImpl.Desktop.self_override1.MyBar reference and DoBar() is invoke non-virtually.

.assembly extern mscorlib{}

.class public abstract auto ansi beforefieldinit MethodImpl.Desktop.self_override1.MyBar
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot abstract virtual 
          instance int32  DoBar() cil managed
  {
  } // end of method MethodImpl.Desktop.self_override1.MyBar::DoBar

  .method public hidebysig virtual instance int32 
          DoSelfBar() cil managed
  {
    .override MethodImpl.Desktop.self_override1.MyBar::DoBar
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "In MethodImpl.Desktop.self_override1.MyBar.DoSelfBar"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.1
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method BarImpl::DoBar


  .method family hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MethodImpl.Desktop.self_override1.MyBar::.ctor

} // end of class MethodImpl.Desktop.self_override1.MyBar

.class public auto ansi beforefieldinit BarImpl
       extends MethodImpl.Desktop.self_override1.MyBar
{
  .method public hidebysig virtual instance int32 
          DoBar() cil managed
  {
    // Code size       16 (0x10)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  ldstr      "In BarImpl.DoBar"
    IL_0005:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.2
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_000e

    IL_000e:  ldloc.0
    IL_000f:  ret
  } // end of method BarImpl::DoBar

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void MethodImpl.Desktop.self_override1.MyBar::.ctor()
    IL_0006:  ret
  } // end of method BarImpl::.ctor

} // end of class BarImpl

.class public auto ansi beforefieldinit MethodImpl.Desktop.self_override1.CMain
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    //.entrypoint
    // Code size       66 (0x42)
    .maxstack  2
    .locals init (bool V_0,
             class BarImpl V_1,
             class MethodImpl.Desktop.self_override1.MyBar V_2,
             int32 V_3)


    // Invoke b.DoBar() virtually where b is a BarImpl reference.
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  newobj     instance void BarImpl::.ctor()
    IL_0007:  stloc.1
    IL_0008:  ldc.i4.2
    IL_0009:  ldloc.1
    IL_000a:  callvirt   instance int32 MethodImpl.Desktop.self_override1.MyBar::DoBar()
    IL_000f:  beq.s      IL_001d

    IL_0011:  ldstr      "FAIL: expected BarImpl.DoBar to execute, but anoth"
    + "er method was executed instead."
    IL_0016:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001b:  ldc.i4.0
    IL_001c:  stloc.0

    // Invoke b.DoBar() virtually where b is a MethodImpl.Desktop.self_override1.MyBar reference.
    IL_001d:  ldloc.1
    IL_001e:  stloc.2
    IL_001f:  ldc.i4.2
    IL_0020:  ldloc.2
    IL_0021:  callvirt   instance int32 MethodImpl.Desktop.self_override1.MyBar::DoBar()
    IL_0026:  beq.s      IL_0033b
    IL_0028:  ldstr      "FAIL: expected BarImpl.DoBar to execute, but ano"
    + "ther method was executed instead."
    IL_002d:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0032:  ldc.i4.0
    IL_0033:  stloc.0


    // Invoke b.DoBar() non-virtually where b is a MethodImpl.Desktop.self_override1.MyBar reference.
    IL_0033b: ldc.i4.1
    ldloc.2
    call instance int32 MethodImpl.Desktop.self_override1.MyBar::DoBar()
    beq.s IL_0034
    ldstr      "FAIL: expected MethodImpl.Desktop.self_override1.MyBar.DoSelfBar to execute, but ano"
    + "ther method was executed instead."
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.0

    // return a status
    IL_0034:  ldloc.0
    IL_0035:  brtrue.s   IL_003b

    IL_0037:  ldc.i4.s   101
    stloc.3
    ldstr "FAIL"
    call void [mscorlib]System.Console::WriteLine(string)
    IL_0039:  br.s       IL_0040

    IL_003b:  ldc.i4.s   100
    IL_003d:  stloc.3
    ldstr "PASS"
    call void [mscorlib]System.Console::WriteLine(string)
    IL_003e:  br.s       IL_0040

    IL_0040:  ldloc.3
    IL_0041:  ret
  } // end of method MethodImpl.Desktop.self_override1.CMain::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method MethodImpl.Desktop.self_override1.CMain::.ctor

} // end of class MethodImpl.Desktop.self_override1.CMain

