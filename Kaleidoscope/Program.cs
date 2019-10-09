using System.Collections.Generic;
using System.IO;
using Kaleidoscope.Compiler;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;
using static Llvm.NET.Interop.Library;

namespace Kaleidoscope
{
    internal static class Program
    {
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


                var visitor = new CodeGenVisitor(false);
                var listener = new CodeGenParserListener(visitor);

                var lexer = new DefaultLexer(File.OpenText(args[0]), binopPrecedence);
                var parser = new DefaultParser(lexer, listener);

                lexer.GetNextToken();

                while (true)
                {
                    switch (lexer.CurrentToken)
                    {
                        case (int) Token.Eof:
                            visitor.Dispose();
                            return;
                        case ';':
                            lexer.GetNextToken();
                            break;
                        case (int) Token.Def:
                            parser.HandleDefinition();
                            break;
                        case (int) Token.Extern:
                            parser.HandleExtern();
                            break;
                        default:
                            parser.HandleTopLevelExpression();
                            break;
                    }
                }
            }
        }
    }
}