using System;
using System.ComponentModel;
using System.Reflection;

namespace DomainEvents.Common
{
    class WeakAction<T> : WeakDelegate
    {
        public WeakAction(object target, MethodInfo method): base(target, method){}
        public void Invoke(T param)
        {
            Delegate d = Delegate.CreateDelegate(typeof(Action<T>), Target.Target, Method);

            try
            {
                ISynchronizeInvoke synchronizer = Target as ISynchronizeInvoke;
                if (synchronizer != null)
                {
                    //Requires thread affinity
                    if (synchronizer.InvokeRequired)
                    {
                        synchronizer.Invoke(d, new object[] {param });
                        return;
                    }
                }
                if (d != null) d.DynamicInvoke(param);
            }
            catch (Exception e)
            {
                //TODO: domain logging.
            }
        }
    }
}