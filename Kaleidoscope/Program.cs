using System;
using System.IO;
using System.Runtime.CompilerServices;
using Kaleidoscope.Compiler;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;
using Llvm.NET;
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
                var machine = new TargetMachine( Triple.HostTriple);
                var visitor = new CodeGenVisitor(false, session, machine);
                var listener = new CodeGenParserListener(visitor);
                var lexer = new DefaultLexer(File.OpenText(args[0]));
                var parser = new DefaultParser(lexer, listener, session);
                
                MainLoop(lexer, parser);
                
                machine.EmitToFile(visitor.Module, "output.o", CodeGenFileType.ObjectFile);
                if( !visitor.Module.WriteToTextFile( "output.ll", out string msg ) )
                {
                    Console.Error.WriteLine( msg );
                    return;
                }
                machine.EmitToFile( visitor.Module, "output.s", CodeGenFileType.AssemblySource );
            }
        }

        private static void MainLoop(ILexer lexer, IParser parser)
        {
            lexer.GetNextToken();

            while (true)
            {
                switch (lexer.CurrentToken)
                {
                    case (int) Token.Eof:
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