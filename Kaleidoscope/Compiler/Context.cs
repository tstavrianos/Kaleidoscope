using System;
using System.Collections.Generic;
using System.Linq;
using Kaleidoscope.Ast;
using Llvm.NET;
using Llvm.NET.Instructions;
using Llvm.NET.Transforms;
using Llvm.NET.Values;
using Llvm.NET.JIT;

namespace Kaleidoscope.Compiler
{
    public class Context: IDisposable
    {
        public readonly Llvm.NET.Context Ctx;
        public readonly InstructionBuilder InstructionBuilder;
        public readonly IDictionary<string, Value> NamedValues = new Dictionary<string, Value>( );
        public readonly IDictionary<string, PrototypeAst> FunctionDeclarations = new Dictionary<string, PrototypeAst>();
        public BitcodeModule Module;
        public FunctionPassManager FunctionPassManager;
        private readonly bool _disableOptimizations;
        internal readonly KaleidoscopeJIT _jit = new KaleidoscopeJIT( );
        private readonly Dictionary<string, ulong> FunctionModuleMap = new Dictionary<string, ulong>( );
        public readonly IDictionary<string, PrototypeAst> FunctionProtos = new Dictionary<string, PrototypeAst>();

        public Context(bool disableOptimizations)
        {
            this._disableOptimizations = disableOptimizations;
            this.Ctx = new Llvm.NET.Context( );
            this.InitializeModuleAndPassManager( );
            this.InstructionBuilder = new InstructionBuilder( this.Ctx );
        }
        
        public IrFunction GetOrDeclareFunction( PrototypeAst prototype )
        {
            var function = this.Module.GetFunction( prototype.Name );
            if( function != null )
            {
                return function;
            }

            var llvmSignature = this.Ctx.GetFunctionType( this.Ctx.DoubleType, prototype.Arguments.Select( _ => this.Ctx.DoubleType ) );
            var retVal = this.Module.AddFunction( prototype.Name, llvmSignature );

            int index = 0;
            foreach( var argId in prototype.Arguments )
            {
                retVal.Parameters[ index ].Name = argId;
                ++index;
            }

            return retVal;
        }

        public IrFunction GetFunction(string name)
        {
            var ret = this.Module.GetFunction(name);
            if (ret != null) return ret;

            if (this.FunctionProtos.TryGetValue(name, out var proto))
            {
                return (IrFunction)proto.CodeGen(this);
            }

            return null;
        }
        
        public void InitializeModuleAndPassManager( )
        {
            this.Module = this.Ctx.CreateBitcodeModule( );
            this.Module.Layout = this._jit.TargetMachine.TargetData;
            this.FunctionPassManager = new FunctionPassManager(this.Module );

            if( !this._disableOptimizations )
            {
                this.FunctionPassManager.AddInstructionCombiningPass( )
                    .AddReassociatePass( )
                    .AddGVNPass( )
                    .AddCFGSimplificationPass( );
            }

            this.FunctionPassManager.Initialize( );
        }


        public void Dispose()
        {
            this.Ctx?.Dispose();
            this.FunctionPassManager?.Dispose();
            this._jit?.Dispose();
            this.Module?.Dispose();
        }
    }
}