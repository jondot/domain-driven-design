using System;

namespace DomainEvents.Hub
{
    /// <summary>
    /// Domain Events.
    /// </summary>
    public interface IDomainEventHub
    {
        /// <summary>
        /// Registers the specified handler.
        /// </summary>
        /// <typeparam name="T">The domain event type</typeparam>
        /// <param name="handler">The handler.</param>
        void Register<T>(Action<T> handler);
        /// <summary>
        /// Removes the specified handler.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">The handler.</param>
        void Unregister<T>(Action<T> handler);
        /// <summary>
        /// Clears all handlers.
        /// </summary>
        void Clear();
    }
}