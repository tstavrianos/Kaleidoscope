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
                case (int)Token.If:
                    return this.ParseIf();
                case (int)Token.For:
                    return this.ParseFor();
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
            var kind = 0;
            var precedence = 30;
            var fnName = string.Empty;

            switch (this._scanner.CurrentToken)
            {
                case (int)Token.Identifier:
                    kind = 0;
                    fnName = this._scanner.LastIdentifier;
                    this._scanner.GetNextToken();
                    break;
                case (int)Token.Binary:
                    this._scanner.GetNextToken();
                    fnName = "binary" + (char)this._scanner.CurrentToken;
                    kind = 2;
                    this._scanner.GetNextToken();

                    if (this._scanner.CurrentToken == (int) Token.Number)
                    {
                        if (this._scanner.LastNumber < 1 || this._scanner.LastNumber > 100)
                        {
                            Console.WriteLine("Invalid precedence: must be 1..100");
                            return null;
                        }

                        precedence = (int)this._scanner.LastNumber;
                        this._scanner.GetNextToken();
                    }
                    break;
                default:
                    Console.WriteLine("Expected function name in prototype");
                    return null;
            }
            
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

            if (kind == 0 || kind == argNames.Count)
                return new Expr.Prototype(fnName, argNames.ToArray(), kind != 0, precedence);
            Console.WriteLine("Invalid number of operands for operator");
            return null;

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
            var proto = new Expr.Prototype("__anon_expr", new string[0], false, 0);
            return new Expr.Function(proto, e);
        }

        /// external ::= 'extern' prototype
        public Expr.Prototype ParseExtern()
        {
            this._scanner.GetNextToken();  // eat extern.
            return this.ParsePrototype();
        }

        private Expr.If ParseIf()
        {
            this._scanner.GetNextToken();

            var cond = this.ParseExpression();
            if (cond == null) return null;

            if (this._scanner.CurrentToken != (int) Token.Then)
            {
                Console.WriteLine("expected then");
                return null;
            }
            this._scanner.GetNextToken();

            var then = this.ParseExpression();
            if (then == null) return null;
            
            if (this._scanner.CurrentToken != (int) Token.Else)
            {
                Console.WriteLine("expected else");
                return null;
            }
            this._scanner.GetNextToken();

            var elseExpr = this.ParseExpression();
            return elseExpr == null ? null : new Expr.If(cond, then, elseExpr);
        }

        private Expr.For ParseFor()
        {
            this._scanner.GetNextToken();

            if (this._scanner.CurrentToken != (int) Token.Identifier)
            {
                Console.WriteLine("expected identifier after for");
                return null;
            }

            var idName = this._scanner.LastIdentifier;
            this._scanner.GetNextToken();

            if (this._scanner.CurrentToken != '=')
            {
                Console.WriteLine("expected '=' after for");
                return null;
            }
            this._scanner.GetNextToken();

            var start = this.ParseExpression();
            if (start == null) return null;
            if (this._scanner.CurrentToken != ',')
            {
                Console.WriteLine("expected ',' after for start value");
                return null;
            }
            this._scanner.GetNextToken();
            
            var end = this.ParseExpression();
            if (end == null) return null;

            Expr step = null;
            if (this._scanner.CurrentToken == ',')
            {
                this._scanner.GetNextToken();
                step = this.ParseExpression();
                if (step == null) return null;
            }

            if (this._scanner.CurrentToken != (int) Token.In)
            {
                Console.WriteLine("expected 'in' after for");
                return null;
            }
            this._scanner.GetNextToken();

            var body = this.ParseExpression();
            return body == null ? null : new Expr.For(idName, start, end, step, body);
        }
    }
}