using System;
using System.Collections.Generic;
using System.Reflection;
using Kaleidoscope.Ast;

namespace Kaleidoscope.Parser
{
    internal sealed class BaseParserListener
    {
        private static readonly Type ParserListenerType = typeof(IParserListener);

        private readonly Stack<string> _descentStack = new Stack<string>();

        private readonly Stack<AstContext> _ascentStack = new Stack<AstContext>();

        private readonly IParserListener _listener;

        public BaseParserListener(IParserListener listener)
        {
            this._listener = listener;
        }

        public void EnterRule(string ruleName)
        {
            this._descentStack.Push(ruleName);
        }

        public void ExitRule(Expr argument)
        {
            var ruleName = this._descentStack.Pop();
            this._ascentStack.Push(new AstContext(ParserListenerType.GetMethod("Exit" + ruleName), this._listener,
                argument));
            this._ascentStack.Push(new AstContext(ParserListenerType.GetMethod("Enter" + ruleName), this._listener,
                argument));
        }

        public void Listen()
        {
            if (this._listener == null) return;
            while (this._ascentStack.Count != 0)
            {
                var context = this._ascentStack.Pop();
                context.MethodInfo.Invoke(context.Instance, new object[] {context.Argument});
            }
        }

        private sealed class AstContext
        {
            public AstContext(MethodInfo methodInfo, object instance, Expr argument)
            {
                this.MethodInfo = methodInfo;
                this.Instance = instance;
                this.Argument = argument;
            }

            public MethodInfo MethodInfo { get; }

            public Expr Argument { get; }

            public object Instance { get; }
        }
    }
}