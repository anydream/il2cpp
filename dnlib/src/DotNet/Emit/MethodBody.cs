// dnlib: See LICENSE.txt for more info

﻿using System.Collections.Generic;
using dnlib.DotNet.Pdb;
using dnlib.PE;
using dnlib.Threading;

#if THREAD_SAFE
using ThreadSafe = dnlib.Threading.Collections;
#else
using ThreadSafe = System.Collections.Generic;
#endif

namespace dnlib.DotNet.Emit {
	/// <summary>
	/// Method body base class
	/// </summary>
	public abstract class MethodBody {
	}

	/// <summary>
	/// A native method body
	/// </summary>
	public sealed class NativeMethodBody : MethodBody {
	    /// <summary>
		/// Gets/sets the RVA of the native method body
		/// </summary>
		public RVA RVA { get; set; }

	    /// <summary>
		/// Default constructor
		/// </summary>
		public NativeMethodBody() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="rva">RVA of method body</param>
		public NativeMethodBody(RVA rva) {
			this.RVA = rva;
		}
	}

	/// <summary>
	/// CIL (managed code) body
	/// </summary>
	public sealed class CilBody : MethodBody {
	    /// <summary>
		/// Size of a small header
		/// </summary>
		public const byte SMALL_HEADER_SIZE = 1;

		/// <summary>
		/// Gets/sets a flag indicating whether the original max stack value should be used.
		/// </summary>
		public bool KeepOldMaxStack { get; set; }

	    /// <summary>
		/// Gets/sets the init locals flag. This is only valid if the method has any locals.
		/// </summary>
		public bool InitLocals { get; set; }

	    /// <summary>
		/// Gets/sets the size in bytes of the method body header. The instructions immediately follow
		/// the header.
		/// </summary>
		public byte HeaderSize { get; set; }

	    /// <summary>
		/// <c>true</c> if it was a small body header (<see cref="HeaderSize"/> is <c>1</c>)
		/// </summary>
		public bool IsSmallHeader => HeaderSize == SMALL_HEADER_SIZE;

	    /// <summary>
		/// <c>true</c> if it was a big body header
		/// </summary>
		public bool IsBigHeader => HeaderSize != SMALL_HEADER_SIZE;

	    /// <summary>
		/// Gets/sets max stack value from the fat method header.
		/// </summary>
		public ushort MaxStack { get; set; }

	    /// <summary>
		/// Gets/sets the locals metadata token
		/// </summary>
		public uint LocalVarSigTok { get; set; }

	    /// <summary>
		/// <c>true</c> if <see cref="Instructions"/> is not empty
		/// </summary>
		public bool HasInstructions => Instructions.Count > 0;

	    /// <summary>
		/// Gets the instructions
		/// </summary>
		public ThreadSafe.IList<Instruction> Instructions { get; }

	    /// <summary>
		/// <c>true</c> if <see cref="ExceptionHandlers"/> is not empty
		/// </summary>
		public bool HasExceptionHandlers => ExceptionHandlers.Count > 0;

	    /// <summary>
		/// Gets the exception handlers
		/// </summary>
		public ThreadSafe.IList<ExceptionHandler> ExceptionHandlers { get; }

	    /// <summary>
		/// <c>true</c> if <see cref="Variables"/> is not empty
		/// </summary>
		public bool HasVariables => Variables.Count > 0;

	    /// <summary>
		/// Gets the locals
		/// </summary>
		public LocalList Variables
		{
// Only called Variables for compat w/ older code. Locals is a better and more accurate name
		    get;
	    }

	    /// <summary>
		/// Gets/sets the PDB scope. This is <c>null</c> if no PDB has been loaded or if there's
		/// no PDB scope for this method.
		/// </summary>
		public PdbScope Scope { get; set; }

	    /// <summary>
		/// <c>true</c> if <see cref="Scope"/> is not <c>null</c>
		/// </summary>
		public bool HasScope => Scope != null;

	    /// <summary>
		/// Default constructor
		/// </summary>
		public CilBody() {
			this.InitLocals = true;
			this.Instructions = ThreadSafeListCreator.Create<Instruction>();
			this.ExceptionHandlers = ThreadSafeListCreator.Create<ExceptionHandler>();
			this.Variables = new LocalList();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="initLocals">Init locals flag</param>
		/// <param name="instructions">All instructions. This instance will own the list.</param>
		/// <param name="exceptionHandlers">All exception handlers. This instance will own the list.</param>
		/// <param name="locals">All locals. This instance will own the locals in the list.</param>
		public CilBody(bool initLocals, IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers, IList<Local> locals) {
			this.InitLocals = initLocals;
			this.Instructions = ThreadSafeListCreator.MakeThreadSafe(instructions);
			this.ExceptionHandlers = ThreadSafeListCreator.MakeThreadSafe(exceptionHandlers);
			this.Variables = new LocalList(locals);
		}

		/// <summary>
		/// Shorter instructions are converted to the longer form, eg. <c>Ldc_I4_1</c> is
		/// converted to <c>Ldc_I4</c> with a <c>1</c> as the operand.
		/// </summary>
		/// <param name="parameters">All method parameters, including the hidden 'this' parameter
		/// if it's an instance method. Use <see cref="MethodDef.Parameters"/>.</param>
		public void SimplifyMacros(IList<Parameter> parameters) {
			Instructions.SimplifyMacros(Variables, parameters);
		}

		/// <summary>
		/// Optimizes instructions by using the shorter form if possible. Eg. <c>Ldc_I4</c> <c>1</c>
		/// will be replaced with <c>Ldc_I4_1</c>.
		/// </summary>
		public void OptimizeMacros() {
			Instructions.OptimizeMacros();
		}

		/// <summary>
		/// Short branch instructions are converted to the long form, eg. <c>Beq_S</c> is
		/// converted to <c>Beq</c>.
		/// </summary>
		public void SimplifyBranches() {
			Instructions.SimplifyBranches();
		}

		/// <summary>
		/// Optimizes branches by using the smallest possible branch
		/// </summary>
		public void OptimizeBranches() {
			Instructions.OptimizeBranches();
		}

		/// <summary>
		/// Updates each instruction's offset
		/// </summary>
		/// <returns>Total size in bytes of all instructions</returns>
		public uint UpdateInstructionOffsets() {
			return Instructions.UpdateInstructionOffsets();
		}
	}
}
