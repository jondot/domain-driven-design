using System;
using System.Reflection;

namespace DomainEvents.Common
{
    class WeakDelegate
    {
        public WeakReference Target;
        public MethodInfo Method;

        public WeakDelegate(object target, MethodInfo method)
        {
            Target = new WeakReference(target);
            Method = method;
        }
        public bool IsAlive { get { return Target.IsAlive || Method.IsStatic; } }
    }
}