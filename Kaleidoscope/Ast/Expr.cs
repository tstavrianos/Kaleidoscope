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
            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.Visit(this);
            }
            public override void Accept(IVisitor visitor)
            {
                visitor.Visit(this);
            }
            public Prototype(string name, string[] arguments)
            {
                this.Name = name;
                this.Arguments = arguments;
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
        public interface IVisitor<out T>
        {
            T Visit (Binary expr);
            T Visit (Call expr);
            T Visit (Number expr);
            T Visit (Variable expr);
            T Visit (Prototype expr);
            T Visit (Function expr);
        }
        public interface IVisitor
        {
            void Visit (Binary expr);
            void Visit (Call expr);
            void Visit (Number expr);
            void Visit (Variable expr);
            void Visit (Prototype expr);
            void Visit (Function expr);
        }
    }
}
