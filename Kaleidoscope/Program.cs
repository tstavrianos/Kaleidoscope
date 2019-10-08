using System;
using System.Collections.Generic;
using System.IO;
using Kaleidoscope.Lexer;
using Kaleidoscope.Parser;

namespace Kaleidoscope
{
    internal static class Program
    {
        private static ILexer _lexer;
        private static IParser _parser;
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
        
        static void HandleDefinition() {
            if (_parser.ParseDefinition() != null) {
                Console.WriteLine("Parsed a function definition");
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleExtern() {
            if (_parser.ParseExtern() != null) {
                Console.WriteLine("Parsed an extern");
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
        
        static void HandleTopLevelExpression() {
            // Evaluate a top-level expression into an anonymous function.
            if (_parser.ParseTopLevelExpr() != null) {
                Console.WriteLine("Parsed a top-level expr");
            } else {
                // Skip token for error recovery.
                _lexer.GetNextToken();
            }
        }
    }
}