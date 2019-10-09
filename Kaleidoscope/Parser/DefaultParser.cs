using System;
using System.Collections.Generic;
using Kaleidoscope.Ast;
using Kaleidoscope.Lexer;

namespace Kaleidoscope.Parser
{
    public sealed class DefaultParser: IParser
    {
        private readonly ILexer _scanner;
        private readonly BaseParserListener _baseListener;

        public DefaultParser(ILexer scanner, IParserListener baseListener)
        {
            this._scanner = scanner;
            this._baseListener = new BaseParserListener(baseListener);
        }
        public void HandleDefinition()
        {
            this._baseListener.EnterRule("HandleDefinition");

            var functionAst = this.ParseDefinition();

            this._baseListener.ExitRule(functionAst);

            if (functionAst != null)
            {
                this._baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                this._scanner.GetNextToken();
            }
        }

        public void HandleExtern()
        {
            this._baseListener.EnterRule("HandleExtern");

            var prototypeAst = this.ParseExtern();

            this._baseListener.ExitRule(prototypeAst);

            if (prototypeAst != null)
            {
                this._baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                this._scanner.GetNextToken();
            }
        }

        public void HandleTopLevelExpression()
        {
            // Evaluate a top-level expression into an anonymous function.
            this._baseListener.EnterRule("HandleTopLevelExpression");

            var functionAst = this.ParseTopLevelExpr();

            this._baseListener.ExitRule(functionAst);

            if (functionAst != null)
            {
                this._baseListener.Listen();
            }
            else
            {
                // Skip token for error recovery.
                this._scanner.GetNextToken();
            }
        }


        // identifierexpr
        //   ::= identifier
        //   ::= identifier '(' expression* ')'
        private Expr ParseIdentifierExpr()
        {
            var idName = this._scanner.LastIdentifier;
            
            this._scanner.GetNextToken();  // eat identifier.

            if (this._scanner.CurrentToken != '(') // Simple variable ref.
            {
                return new Expr.Variable(idName);
            }

            // Call.
            this._scanner.GetNextToken();  // eat (
            var args = new List<Expr>();

            if (this._scanner.CurrentToken != ')')
            {
                while (true)
                {
                    var arg = this.ParseExpression();
                    if (arg == null)
                    {
                        return null;
                    }

                    args.Add(arg);

                    if (this._scanner.CurrentToken == ')')
                    {
                        break;
                    }

                    if (this._scanner.CurrentToken != ',')
                    {
                        Console.WriteLine("Expected ')' or ',' in argument list");
                        return null;
                    }
                    
                    this._scanner.GetNextToken();
                }
            }
            
            // Eat the ')'.
            this._scanner.GetNextToken();

            return new Expr.Call(idName, args.ToArray());
        }

        // numberexpr ::= number
        private Expr ParseNumberExpr()
        {
            var result = new Expr.Number(this._scanner.LastNumber);
            this._scanner.GetNextToken();
            return result;
        }

        // parenexpr ::= '(' expression ')'
        private Expr ParseParenExpr()
        {
            this._scanner.GetNextToken();  // eat (.
            var v = this.ParseExpression();
            if (v == null)
            {
                return null;
            }

            if (this._scanner.CurrentToken != ')')
            {
                Console.WriteLine("expected ')'");
                return null;
            }

            this._scanner.GetNextToken(); // eat ).

            return v;
        }

        // primary
        //   ::= identifierexpr
        //   ::= numberexpr
        //   ::= parenexpr
        private Expr ParsePrimary()
        {
            switch (this._scanner.CurrentToken)
            {
                case (int)Token.Identifier:
                    return this.ParseIdentifierExpr();
                case (int)Token.Number:
                    return this.ParseNumberExpr();
                case '(':
                    return this.ParseParenExpr();
                default:
                    Console.WriteLine("unknown token when expecting an expression");
                    return null;
            }
        }

        // binoprhs
        //   ::= ('+' primary)*
        private Expr ParseBinOpRhs(int exprPrec, Expr lhs)
        {
            // If this is a binop, find its precedence.
            while (true)
            {
                var tokPrec = this._scanner.GetTokPrecedence();

                // If this is a binop that binds at least as tightly as the current binop,
                // consume it, otherwise we are done.
                if (tokPrec < exprPrec)
                {
                    return lhs;
                }

                // Okay, we know this is a binop.
                var binOp = this._scanner.CurrentToken;
                this._scanner.GetNextToken();  // eat binop

                // Parse the primary expression after the binary operator.
                var rhs = this.ParsePrimary();
                if (rhs == null)
                {
                    return null;
                }

                // If BinOp binds less tightly with RHS than the operator after RHS, let
                // the pending operator take RHS as its LHS.
                var nextPrec = this._scanner.GetTokPrecedence();
                if (tokPrec < nextPrec)
                {
                    rhs = this.ParseBinOpRhs(tokPrec + 1, rhs);
                    if (rhs == null)
                    {
                        return null;
                    }
                }

                // Merge LHS/RHS.
                lhs = new Expr.Binary((char)binOp, lhs, rhs);
            }
        }

        // expression
        //   ::= primary binoprhs
        //
        private Expr ParseExpression()
        {
            var lhs = this.ParsePrimary();
            return lhs == null ? null : this.ParseBinOpRhs(0, lhs);
        }

        // prototype
        //   ::= id '(' id* ')'
        private Expr.Prototype ParsePrototype()
        {
            if (this._scanner.CurrentToken != (int)Token.Identifier)
            {
                Console.WriteLine("Expected function name in prototype");
                return null;
            }

            var fnName = this._scanner.LastIdentifier;

            this._scanner.GetNextToken();

            if (this._scanner.CurrentToken != '(')
            {
                Console.WriteLine("Expected '(' in prototype");
                return null;
            }

            var argNames = new List<string>();
            while (this._scanner.GetNextToken() == (int)Token.Identifier)
            {
                argNames.Add(this._scanner.LastIdentifier);
            }

            if (this._scanner.CurrentToken != ')')
            {
                Console.WriteLine("Expected ')' in prototype");
                return null;
            }

            this._scanner.GetNextToken(); // eat ')'.

            return new Expr.Prototype(fnName, argNames.ToArray());
        }

        // definition ::= 'def' prototype expression
        public Expr.Function ParseDefinition()
        {
            this._scanner.GetNextToken(); // eat def.
            var proto = this.ParsePrototype();

            if (proto == null)
            {
                return null;
            }

            var body = this.ParseExpression();
            return body == null ? null : new Expr.Function(proto, body);
        }

        /// toplevelexpr ::= expression
        public Expr.Function ParseTopLevelExpr()
        {
            var e = this.ParseExpression();
            if (e == null)
            {
                return null;
            }

            // Make an anonymous proto.
            var proto = new Expr.Prototype("__anon_expr", new string[0]);
            return new Expr.Function(proto, e);
        }

        /// external ::= 'extern' prototype
        public Expr.Prototype ParseExtern()
        {
            this._scanner.GetNextToken();  // eat extern.
            return this.ParsePrototype();
        }
    }
}