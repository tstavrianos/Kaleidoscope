using System.Collections.Generic;
using System.IO;
using Kaleidoscope.Compiler;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;
using LLVMSharp;

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
            
            var module = LLVM.ModuleCreateWithName("my cool jit");
            var builder = LLVM.CreateBuilder();
            _ctx = new Context(module, builder);

            _lexer = new DefaultLexer(File.OpenText(args[0]), binopPrecedence);
            _parser = new DefaultParser(_lexer);
            
            _lexer.GetNextToken();
            
            while (true)
            {
                switch (_lexer.CurrentToken)
                {
                    case (int)Token.Eof:
                        return;
                    case ';':
                        _lexer.GetNextToken();
                        break;
                    case (int)Token.Def:
                        HandleDefinition();
                        break;
                    case (int)Token.Extern:
                        HandleExtern();
                        break;
                    default:
                        HandleTopLevelExpression();
                        break;
                }
            }
        }
        
        static void HandleDefinition()
        {
            var data = _parser.ParseDefinition();
            if (data != null) {
                data.CodeGen(_ctx);
                LLVM.DumpValue(_ctx.ValueStack.Pop());
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleExtern() {
            var data = _parser.ParseExtern();
            if (data != null) {
                data.CodeGen(_ctx);
                LLVM.DumpValue(_ctx.ValueStack.Pop());
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleTopLevelExpression() {
            var data = _parser.ParseTopLevelExpr();
            if (data != null) {
                data.CodeGen(_ctx);
                LLVM.DumpValue(_ctx.ValueStack.Pop());
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
    }
}