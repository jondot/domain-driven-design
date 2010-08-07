namespace DomainEvents.DomainEvent
{
    class Hub
    {
        public DomainEvent<Customer> CustomerChanged = new DomainEvent<Customer>();
        public DomainEvent<Customer> CustomerChanged2 = new DomainEvent<Customer>();
        public Hub()
        {
        }
    }

    internal class Customer
    {
    }
}
