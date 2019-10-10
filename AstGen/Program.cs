using System.Collections.Generic;
using System.IO;
using CodeGen;
using CodeGen.Building;

namespace AstGen
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            BuildExpressions();
            BuildStatements();
        }

        private static string Title(this string v)
        {
            return char.ToUpper(v[0]) + v.Substring(1);
        }

        private static void Build(IEnumerable<string> types, string baseType)
        {
            var b = new Base();
            var ns = new Namespace("Kaleidoscope.Ast");
            b.Comment = "ReSharper disable ArrangeStaticMemberQualifier\nReSharper disable RedundantCast\nReSharper disable RedundantNameQualifier";
            b.Namespaces.Add(ns);

            var baseClass = new Class(baseType) {Modifiers = Modifiers.Abstract | Modifiers.Public};

            var intf1 = new Interface("IVisitor<out T>") {Modifiers = Modifiers.Public};
            var intf2 = new Interface("IVisitor") {Modifiers = Modifiers.Public};

            foreach (var type in types)
            {
                var split = type.Split(":");
                var typeName = split[0].Trim();
                
               
                intf1.Members.Add(new IntMethod("T", "Visit") {Parameters = $"{typeName} {baseType.ToLower()}"});
                intf2.Members.Add(new IntMethod("void", "Visit") {Parameters = $"{typeName} {baseType.ToLower()}"});
                
                var nc = new Class(typeName);
                nc.Extends.Add(baseType);
                nc.Modifiers = Modifiers.Public | Modifiers.Sealed;
                var con = new Method(null, typeName)
                {
                    Modifiers = Modifiers.Public, Parameters = split[1].Trim(), Body = new Builder()
                };

                var parameters = split[1].Split(",");
                foreach (var p in parameters)
                {
                    var pType = p.Trim().Split(" ")[0];
                    var pName = p.Trim().Split(" ")[1];
                    con.Body.AppendLine($"this.{pName.Title()} = {pName};");
                    nc.Members.Add(new Field(pType, pName.Title()) {Modifiers = Modifiers.ReadOnly | Modifiers.Public});
                }

                var over1 = new Method("T", "Accept<T>")
                {
                    Parameters = "IVisitor<T> visitor",
                    Modifiers = Modifiers.Override | Modifiers.Public,
                    Body = new Builder()
                };
                over1.Body.AppendLine("return visitor.Visit(this);");
                nc.Members.Add(over1);
                var over2 = new Method("void", "Accept")
                {
                    Parameters = "IVisitor visitor",
                    Modifiers = Modifiers.Override | Modifiers.Public,
                    Body = new Builder()
                };
                over2.Body.AppendLine("visitor.Visit(this);");
                nc.Members.Add(over2);
                nc.Members.Add(con);
                
                baseClass.Types.Add(nc);
            }
            
            baseClass.Types.Add(intf1);
            baseClass.Types.Add(intf2);
            var mv1 = new Method("T", "Accept<T>")
            {
                Modifiers = Modifiers.Virtual | Modifiers.Public,
                Parameters = "IVisitor<T> visitor",
                Body = new Builder()
            };
            mv1.Body.AppendLine("throw new NotImplementedException();");
            baseClass.Members.Add(mv1);
            var mv2 = new Method("void", "Accept")
            {
                Modifiers = Modifiers.Virtual | Modifiers.Public,
                Parameters = "IVisitor visitor",
                Body = new Builder()
            };
            mv2.Body.AppendLine("throw new NotImplementedException();");
            baseClass.Members.Add(mv2);
            
            ns.Types.Add(baseClass);
            b.Using.Add("System");
            
            File.WriteAllText($"{baseType}.cs", b.BuildCode(false));
        }

        private static void BuildExpressions()
        {
            var types = new [] {
                "Number    : double value",
                "Variable  : string name",
                "Unary     : char opcode, Expr operand",
                "Binary    : char op, Expr lhs, Expr rhs",
                "Call      : string callee, Expr[] arguments",
                "If        : Expr cond, Expr thenExpr, Expr elseExpr",
                "For       : string varName, Expr start, Expr end, Expr step, Expr body",
                "_VarName   : string name, Expr expression",
                "Var       : _VarName[] varNames, Expr body",
                "Prototype : string name, string[] arguments, bool isOperator, int precedence",
                "Function  : Prototype proto, Expr body"
            };

            Build(types, "Expr");
        }

        private static void BuildStatements()
        {
            /*var types = new [] {
                "Prototype : string name, string[] arguments",
                "Function  : Prototype proto, Expr body"
            };
            
            Build(types, "Stmt");*/
        }
    }
}