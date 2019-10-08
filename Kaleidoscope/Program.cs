using System;
using System.Collections.Generic;
using System.IO;
using Kaleidoscope.Compiler;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;
using Llvm.NET.JIT;
using Llvm.NET.Values;
using static Llvm.NET.Interop.Library;

namespace Kaleidoscope
{
    internal static class Program
    {
        private static ILexer _lexer;
        private static IParser _parser;
        private static Context _ctx;
        static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0])) return;
            var binopPrecedence = new Dictionary<char, int>
            {
                ['<'] = 10,
                ['+'] = 20,
                ['-'] = 20,
                ['*'] = 40
            };
            using (InitializeLLVM())
            {
                RegisterNative();


                _ctx = new Context(false);

                _lexer = new DefaultLexer(File.OpenText(args[0]), binopPrecedence);
                _parser = new DefaultParser(_lexer);

                _lexer.GetNextToken();

                while (true)
                {
                    switch (_lexer.CurrentToken)
                    {
                        case (int) Token.Eof:
                            _ctx.Dispose();
                            return;
                        case ';':
                            _lexer.GetNextToken();
                            break;
                        case (int) Token.Def:
                            HandleDefinition();
                            break;
                        case (int) Token.Extern:
                            HandleExtern();
                            break;
                        default:
                            HandleTopLevelExpression();
                            break;
                    }
                }
            }
        }
        
        static void HandleDefinition()
        {
            var data = _parser.ParseDefinition();
            if (data != null) {
                var value = data.CodeGen(_ctx);
                if (value != null)
                {
                    Console.WriteLine("Read function definition");
                    Console.WriteLine(value);
                    _ctx._jit.AddEagerlyCompiledModule(_ctx.Module);
                    _ctx.InitializeModuleAndPassManager();
                }
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleExtern() {
            var data = _parser.ParseExtern();
            if (data != null) {
                var value = data.CodeGen(_ctx);
                if (value != null)
                {
                    Console.WriteLine("Read function definition");
                    Console.WriteLine(value);
                    _ctx.FunctionProtos[data.Name] = data;
                }
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleTopLevelExpression() {
            var data = _parser.ParseTopLevelExpr();
            if (data != null) {
                var value = data.CodeGen(_ctx);
                if (value != null)
                {
                    var jitHandle = _ctx._jit.AddEagerlyCompiledModule(_ctx.Module);
                    _ctx.InitializeModuleAndPassManager();
                    var nativeFunc = _ctx._jit.GetFunctionDelegate<KaleidoscopeJIT.CallbackHandler0>(data.Proto.Name);
                    var retVal = _ctx.Ctx.CreateConstant(nativeFunc());
                    _ctx._jit.RemoveModule(jitHandle);
                    Console.WriteLine($"Evaluated to {retVal}");
                }
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
    }
}