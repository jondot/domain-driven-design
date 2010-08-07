using System;
using System.Collections.Generic;
using DomainEvents.Common;


namespace DomainEvents.DomainEvent
{
    /// <summary>
    /// A Domain Event Hub implementation.
    /// Optionally could hold ThreadStatic, and control order of
    /// invocation, error fallback and logging.
    /// </summary>
    internal class DomainEvent<T>
    {
        private readonly List<WeakDelegate> _handlers = new List<WeakDelegate>();
        
        public void Register(Action<T> handler) 
        {
            PruneGCedRefs();


            if(!_handlers.Exists(WeakDelegateMatcher(handler)))
            {
                _handlers.Add(new WeakAction<T>(handler.Target, handler.Method));
            }
        }

        public void Unregister(Action<T> handler)
        {
            PruneGCedRefs();

            WeakDelegate refAction = _handlers.Find(WeakDelegateMatcher(handler));
            if (refAction != null) _handlers.Remove(refAction);
        }

        private static Predicate<WeakDelegate> WeakDelegateMatcher(Action<T> handler)
        {
            return delegate(WeakDelegate wr) { return wr.Target.IsAlive && wr.GetType() == typeof(WeakAction<T>) && wr.Method == handler.Method; };
        }

        public void Clear()
        {
            _handlers.Clear();
        }

        public void Raise(T arg)
        {
            bool shouldPrune = false;

            foreach (WeakDelegate wd in _handlers)
            {
                if (!wd.IsAlive)
                {
                    shouldPrune = true;
                }
                ((WeakAction<T>)wd).Invoke(arg);
            }
            if (shouldPrune) PruneGCedRefs();
        }
        
        private void PruneGCedRefs()
        {
            _handlers.RemoveAll(delegate(WeakDelegate wr) { return !wr.Target.IsAlive; });
        }
    }

}
