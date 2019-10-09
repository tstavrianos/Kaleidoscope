using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Kaleidoscope.Compiler;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;
using static Llvm.NET.Interop.Library;

namespace Kaleidoscope
{
    internal static class Program
    {
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)] 
        internal static bool IsAscii(this int c)
        {
            return c >= 0 && c < 128;
        }

        static void Main(string[] args)
        {
            if (args.Length != 1 || !File.Exists(args[0])) return;
            using (InitializeLLVM())
            {
                RegisterNative();


                var session = new Session();
                var visitor = new CodeGenVisitor(false, session);
                var listener = new CodeGenParserListener(visitor);

                var lexer = new DefaultLexer(File.OpenText(args[0]));
                var parser = new DefaultParser(lexer, listener, session);

                lexer.GetNextToken();

                while (true)
                {
                    //Console.WriteLine(lexer.CurrentToken);
                    switch (lexer.CurrentToken)
                    {
                        case (int) Token.Eof:
                            //visitor.Dispose();
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