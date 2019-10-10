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
        private readonly Session _session;

        public DefaultParser(ILexer scanner, IParserListener baseListener, Session session)
        {
            this._scanner = scanner;
            this._baseListener = new BaseParserListener(baseListener);
            this._session = session;
        }
        public void HandleDefinition()
        {
            //Console.WriteLine($"HandleDefinition: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"HandleExtern: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"HandleTopLevelExpression: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"ParseIdentifierExpr: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"ParseNumberExpr: {this._scanner.CurrentToken}");
            var result = new Expr.Number(this._scanner.LastNumber);
            this._scanner.GetNextToken();
            return result;
        }

        // parenexpr ::= '(' expression ')'
        private Expr ParseParenExpr()
        {
            //Console.WriteLine($"ParseParenExpr: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"ParsePrimary: {this._scanner.CurrentToken}");
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
                case (int)Token.Var:
                    return this.ParseVar();
                default:
                    Console.WriteLine($"unknown token when expecting an expression {this._scanner.CurrentToken}, {(char)this._scanner.CurrentToken}");
                    return null;
            }
        }

        private Expr ParseBinOpRhs(int exprPrec, Expr lhs)
        {
            //Console.WriteLine($"ParseBinOpRhs: {this._scanner.CurrentToken}");
            while (true)
            {
                var tokPrec = this._session.GetTokPrecedence(this._scanner.CurrentToken);
                if (tokPrec < exprPrec)
                {
                    return lhs;
                }

                var binOp = this._scanner.CurrentToken;
                this._scanner.GetNextToken();  // eat binop

                var rhs = this.ParseUnary();
                if (rhs == null)
                {
                    return null;
                }

                var nextPrec = this._session.GetTokPrecedence(this._scanner.CurrentToken);
                if (tokPrec < nextPrec)
                {
                    rhs = this.ParseBinOpRhs(tokPrec + 1, rhs);
                    if (rhs == null)
                    {
                        return null;
                    }
                }

                lhs = new Expr.Binary((char)binOp, lhs, rhs);
            }
        }

        // expression
        //   ::= primary binoprhs
        //
        private Expr ParseExpression()
        {
            //Console.WriteLine($"ParseExpression: {this._scanner.CurrentToken}");
            var lhs = this.ParseUnary();
            return lhs == null ? null : this.ParseBinOpRhs(0, lhs);
        }

        // prototype
        //   ::= id '(' id* ')'
        private Expr.Prototype ParsePrototype()
        {
            //Console.WriteLine($"ParsePrototype: {this._scanner.CurrentToken}");
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
                case (int)Token.Unary:
                    this._scanner.GetNextToken();
                    if (!this._scanner.CurrentToken.IsAscii())
                    {
                        Console.WriteLine("Expected unary operator");
                        return null;
                    }

                    fnName = $"unary{(char)this._scanner.CurrentToken}";
                    kind = 1;
                    this._scanner.GetNextToken();
                    break;
                case (int)Token.Binary:
                    this._scanner.GetNextToken();
                    if (!this._scanner.CurrentToken.IsAscii())
                    {
                        Console.WriteLine("Expected unary operator");
                        return null;
                    }
                    fnName = $"binary{(char)this._scanner.CurrentToken}";
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
        private Expr.Function ParseDefinition()
        {
            //Console.WriteLine($"ParseDefinition: {this._scanner.CurrentToken}");
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
        private Expr.Function ParseTopLevelExpr()
        {
            //Console.WriteLine($"ParseTopLevelExpr: {this._scanner.CurrentToken}");
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
        private Expr.Prototype ParseExtern()
        {
            //Console.WriteLine($"ParseExtern: {this._scanner.CurrentToken}");
            this._scanner.GetNextToken();  // eat extern.
            return this.ParsePrototype();
        }

        private Expr.If ParseIf()
        {
            //Console.WriteLine($"ParseIf: {this._scanner.CurrentToken}");
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
            //Console.WriteLine($"ParseFor: {this._scanner.CurrentToken}");
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

        private Expr ParseUnary()
        {
            //Console.WriteLine($"ParseUnary: {this._scanner.CurrentToken}");
            if(!this._scanner.CurrentToken.IsAscii() || this._scanner.CurrentToken == '(' || this._scanner.CurrentToken == ',')
                return this.ParsePrimary();

            var op = this._scanner.CurrentToken;
            this._scanner.GetNextToken();
            var operand = this.ParseUnary();
            return operand != null ? new Expr.Unary((char)op, operand) : null;
        }

        private Expr.Var ParseVar()
        {
            this._scanner.GetNextToken();

            var varNames = new List<Expr._VarName>();
            if (this._scanner.CurrentToken != (int)Token.Identifier)
            {
                Console.WriteLine("expected identifier after var");
                return null;
            }

            while (true)
            {
                var name = this._scanner.LastIdentifier;
                this._scanner.GetNextToken();

                Expr init = null;
                if (this._scanner.CurrentToken == '=')
                {
                    this._scanner.GetNextToken();
                    init = this.ParseExpression();
                    if (init == null) return null;
                }
                
                varNames.Add(new Expr._VarName(name, init));

                if (this._scanner.CurrentToken != ',') break;
                this._scanner.GetNextToken();

                if (this._scanner.CurrentToken == (int) Token.Identifier) continue;
                Console.WriteLine("expected identifier list after var");
                return null;
            }

            if (this._scanner.CurrentToken != (int) Token.In)
            {
                Console.WriteLine("expected 'in' keyword after 'var'");
                return null;
            }

            this._scanner.GetNextToken();

            var body = this.ParseExpression();
            return body == null ? null : new Expr.Var(varNames.ToArray(), body);
        }
    }
}