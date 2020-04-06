using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ModestTree;

namespace CreativeZone.Utils
{
    static class DelegateCreator
    {
        private static readonly Func<Type[], Type> MakeNewCustomDelegate = (Func<Type[], Type>)Delegate.CreateDelegate(typeof(Func<Type[], Type>), typeof(Expression).Assembly
            .GetType("System.Linq.Expressions.Compiler.DelegateHelpers")
            .GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static));

        public static Type NewDelegateType(MethodInfo targetMethod, bool unboundInstanceCall = false)
        {
            var args = targetMethod.GetParameters().Select(x => x.ParameterType);

            if(unboundInstanceCall && targetMethod.IsStatic == false)
            {
                args = new[] {targetMethod.DeclaringType}.Concat(args);
            }

            return NewDelegateType
            (
                targetMethod.ReturnType,
                args.ToArray()
            );
        }

        public static Type NewDelegateType(Type returnType, params Type[] parameters)
        {
            var args = new Type[parameters.Length + 1];
            parameters.CopyTo(args, 0);
            args[args.Length - 1] = returnType;
            return MakeNewCustomDelegate(args);
        }
    }
}
