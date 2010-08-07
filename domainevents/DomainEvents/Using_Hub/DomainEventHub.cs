using System;
using System.Collections.Generic;
using DomainEvents.Common;

namespace DomainEvents.Hub
{
    /// <summary>
    /// A Domain Event Hub implementation.
    /// Optionally could hold ThreadStatic, and control order of
    /// invocation, error fallback and logging.
    /// </summary>
    internal class DomainEventHub : IDomainEventHub
    {
        private readonly Dictionary<Type, List<WeakDelegate>> _handlerDict = new Dictionary<Type, List<WeakDelegate>>();
        
        public void Register<T>(Action<T> handler)
        {
            PruneGCedRefs();

            Type t = typeof (T);
            if(!_handlerDict.ContainsKey(t))
            {
                _handlerDict.Add(t, new List<WeakDelegate>());
            }

            List<WeakDelegate> handlers = _handlerDict[t];
            if(!handlers.Exists(WeakDelegateMatcher(handler)))
            {
                handlers.Add(new WeakAction<T>(handler.Target, handler.Method));
            }
        }

        public void Unregister<T>(Action<T> handler)
        {
            PruneGCedRefs();

            Type t = typeof(T);
            if(!_handlerDict.ContainsKey(t))
                return;

            List<WeakDelegate> handlers = _handlerDict[t];

            WeakDelegate refAction = handlers.Find(WeakDelegateMatcher(handler));
            if (refAction != null) handlers.Remove(refAction);
        }

        private static Predicate<WeakDelegate> WeakDelegateMatcher<T>(Action<T> handler)
        {
            return delegate(WeakDelegate wr) { return wr.Target.IsAlive && wr.GetType() == typeof(WeakAction<T>) && wr.Method == handler.Method; };
        }

        public void Clear()
        {
            _handlerDict.Clear();
        }

        public void Raise<T>(T arg)
        {
            bool shouldPrune = false;
            Type t = typeof(T);
            if (!_handlerDict.ContainsKey(t))
                return;

            foreach (WeakDelegate wd in _handlerDict[t])
            {
                if (!wd.IsAlive)
                {
                    shouldPrune = true;
                }
                if (wd is WeakAction<T>)
                {
                    ((WeakAction<T>)wd).Invoke(arg);
                }
            }
            if (shouldPrune) PruneGCedRefs();
        }
        
        private void PruneGCedRefs()
        {
            foreach (List<WeakDelegate> weakDelegates in _handlerDict.Values)
            {
                weakDelegates.RemoveAll(delegate(WeakDelegate wr) { return !wr.Target.IsAlive; });
            }
            
        }
    }
}
