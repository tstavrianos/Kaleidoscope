// ReSharper disable ArrangeStaticMemberQualifier
// ReSharper disable RedundantCast
// ReSharper disable RedundantNameQualifier
using System;

namespace Kaleidoscope.Ast
{
    public abstract class Expr
    {
        public virtual T Accept<T>(IVisitor<T> visitor)
        {
            throw new NotImplementedException();
        }
        public virtual void Accept(IVisitor visitor)
        {
            throw new NotImplementedException();
        }
        public sealed class Binary : Expr 
        {
            public readonly char Op;
            public readonly Expr Lhs;
            public readonly Expr Rhs;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Binary(char op, Expr lhs, Expr rhs)
            {
                this.Op = op;
                this.Lhs = lhs;
                this.Rhs = rhs;
            }
        }
        public sealed class Call : Expr 
        {
            public readonly string Callee;
            public readonly Expr[] Arguments;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Call(string callee, Expr[] arguments)
            {
                this.Callee = callee;
                this.Arguments = arguments;
            }
        }
        public sealed class Number : Expr 
        {
            public readonly double Value;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Number(double value)
            {
                this.Value = value;
            }
        }
        public sealed class Variable : Expr 
        {
            public readonly string Name;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Variable(string name)
            {
                this.Name = name;
            }
        }
        public sealed class Prototype : Expr 
        {
            public readonly string Name;
            public readonly string[] Arguments;
            public readonly bool IsOperator;
            public readonly int Precedence;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Prototype(string name, string[] arguments, bool isOperator, int precedence)
            {
                this.Name = name;
                this.Arguments = arguments;
                this.IsOperator = isOperator;
                this.Precedence = precedence;
            }
        }
        public sealed class Function : Expr 
        {
            public readonly Prototype Proto;
            public readonly Expr Body;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Function(Prototype proto, Expr body)
            {
                this.Proto = proto;
                this.Body = body;
            }
        }
        public sealed class If : Expr 
        {
            public readonly Expr Cond;
            public readonly Expr ThenExpr;
            public readonly Expr ElseExpr;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public If(Expr cond, Expr thenExpr, Expr elseExpr)
            {
                this.Cond = cond;
                this.ThenExpr = thenExpr;
                this.ElseExpr = elseExpr;
            }
        }
        public sealed class For : Expr 
        {
            public readonly string VarName;
            public readonly Expr Start;
            public readonly Expr End;
            public readonly Expr Step;
            public readonly Expr Body;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public For(string varName, Expr start, Expr end, Expr step, Expr body)
            {
                this.VarName = varName;
                this.Start = start;
                this.End = end;
                this.Step = step;
                this.Body = body;
            }
        }
        public sealed class Unary : Expr 
        {
            public readonly char Opcode;
            public readonly Expr Operand;
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Unary(char opcode, Expr operand)
            {
                this.Opcode = opcode;
                this.Operand = operand;
            }
        }
        public interface IVisitor<out T>
        {
            T Visit (Binary expr);
            T Visit (Call expr);
            T Visit (Number expr);
            T Visit (Variable expr);
            T Visit (Prototype expr);
            T Visit (Function expr);
            T Visit (If expr);
            T Visit (For expr);
            T Visit (Unary expr);
        }
        public interface IVisitor
        {
            void Visit (Binary expr);
            void Visit (Call expr);
            void Visit (Number expr);
            void Visit (Variable expr);
            void Visit (Prototype expr);
            void Visit (Function expr);
            void Visit (If expr);
            void Visit (For expr);
            void Visit (Unary expr);
        }
    }
}
