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
        public BitcodeModule Module { get; }

        public Context()
        {
            this.Ctx = new Llvm.NET.Context( );
            this.Module = this.Ctx.CreateBitcodeModule( "Kaleidoscope" );
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

        #region IDisposable

        public void Dispose()
        {
            this.Ctx?.Dispose();
            this.Module?.Dispose();
        }

        #endregion
    }
}