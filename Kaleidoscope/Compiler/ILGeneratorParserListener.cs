using System;
using Kaleidoscope.Ast;
using Kaleidoscope.Parser;

namespace Kaleidoscope.Compiler
{
    public sealed class IlGeneratorParserListener: IParserListener
    {
        private readonly IlGeneratorVisitor _visitor;

        public IlGeneratorParserListener(IlGeneratorVisitor visitor)
        {
            this._visitor = visitor;
        }
        
        public void EnterHandleDefinition(Expr.Function data)
        {
        }

        public void ExitHandleDefinition(Expr.Function data)
        {
            //Console.WriteLine($"ExitHandleDefinition {data.Proto.Name}");
            this._visitor.Visit(data);
        }

        public void EnterHandleExtern(Expr.Prototype data)
        {
        }

        public void ExitHandleExtern(Expr.Prototype data)
        {
            //Console.WriteLine($"ExitHandleExtern {data.Name}");
            this._visitor.NextIsExtern = true;
            this._visitor.Visit(data);
        }

        public void EnterHandleTopLevelExpression(Expr.Function data)
        {
        }

        private delegate double Anon();
        public void ExitHandleTopLevelExpression(Expr.Function data)
        {
            //Console.WriteLine($"ExitHandleTopLevelExpression {data.Proto.Name}");
            this._visitor.Visit(data);

            var method = this._visitor.Methods[data.Proto.Name];
            Console.WriteLine($"Evaluated to {method.methodInfo.Invoke(null, null)}");
        }
    }
}