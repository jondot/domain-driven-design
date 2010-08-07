using System;
using System.Threading;
using DomainEvents.Events;
using DomainEvents.Hub;
using NUnit.Framework;

namespace DomainEventsDemo
{
    class AutoResetEventLatch
    {
        private int _fired;
        private DateTime _fireTime;

        public int FireCount
        {
            get
            {
                int fired = _fired;
                _fired = 0;
                return fired;
            }
        }
        public DateTime FireTime
        {
            get { return _fireTime;  }
        }

        public AutoResetEventLatch()
        {
            _fired = 0;
        }

        public void Set()
        {
            _fired++;
            _fireTime = DateTime.Now;
        }
    }

    [TestFixture]
    public class DomainEventsTest
    {
        private AutoResetEventLatch _eventLatch;
        private DomainEventHub _domainEventsHub;
        private static AutoResetEventLatch _staticLatch;

        [SetUp]
        public void SetUp()
        {
            _eventLatch = new AutoResetEventLatch();
            _domainEventsHub = new DomainEventHub();
            _staticLatch = new AutoResetEventLatch();
        }

        [Test]
        public void it_should_fire_event_when_registered()
        {
            _domainEventsHub.Register<WeatherChangedEvent>(delegate{ _eventLatch.Set();});
            _domainEventsHub.Raise(new WeatherChangedEvent());
            Assert.AreEqual(1, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_not_fire_event_when_registered_for_different_event()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Raise(new WeatherChangedEvent());
            Assert.AreEqual(0, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_fire_twice_when_two_different_events_raised()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Register<WeatherChangedEvent>(delegate { _eventLatch.Set(); });

            _domainEventsHub.Raise(new CustomerChangedEvent());
            _domainEventsHub.Raise(new WeatherChangedEvent());

            Assert.AreEqual(2, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_fire_twice_when_two_events_raised_and_registering_sparsly()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Raise(new CustomerChangedEvent());
            _domainEventsHub.Register<WeatherChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Raise(new WeatherChangedEvent());

            Assert.AreEqual(2, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_fire_twice_when_raised_twice()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Raise(new CustomerChangedEvent());
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(2, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_not_fire_when_no_handlers()
        {
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(0, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_clear_handlers()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Clear();
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(0, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_fire_for_all_handlers_of_event()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });
           
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(2, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_fire_ordered_by_order_of_registration()
        {
            AutoResetEventLatch secondaryLatch = new AutoResetEventLatch();

            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); Thread.Sleep(100); });
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { secondaryLatch.Set(); });

            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.Greater(secondaryLatch.FireTime, _eventLatch.FireTime);
        }

        [Test]
        public void it_should_be_able_to_absorb_badly_acting_handlers()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { throw new Exception(); });
            _domainEventsHub.Register<CustomerChangedEvent>(delegate { _eventLatch.Set(); });

            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(1, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_remove_a_specific_handler()
        {
            _domainEventsHub.Register<WeatherChangedEvent>(delegate{});
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);

            _domainEventsHub.Unregister<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(0, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_not_register_twice_same_handler()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Raise(new CustomerChangedEvent());
             
            Assert.AreEqual(1, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_remove_handler_when_registering_twice()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Unregister<CustomerChangedEvent>(HandleStuff);
            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(0, _eventLatch.FireCount);
        }

        [Test]
        public void it_should_collect_dead_references_when_GC_collects()
        {
            new DeadObject(_domainEventsHub, _eventLatch);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(0, _eventLatch.FireCount);
        }
        class DeadObject
        {
            public DeadObject(DomainEventHub ev, AutoResetEventLatch latch)
            {
                ev.Register<CustomerChangedEvent>(delegate{latch.Set();});
            }
        }
        [Test]
        public void it_should_not_collect_presistent_references_when_GC_collects()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(HandleStuff);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(1, _eventLatch.FireCount);
        }
        [Test]
        public void it_should_fire_event_when_delegate_is_static()
        {
            _domainEventsHub.Register<CustomerChangedEvent>(ForeverLivingDelegate);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            _domainEventsHub.Raise(new CustomerChangedEvent());

            Assert.AreEqual(1, _staticLatch.FireCount);
        }

        private static void ForeverLivingDelegate(CustomerChangedEvent customerChangedEvent)
        {
            _staticLatch.Set();
        }

        private void HandleStuff(CustomerChangedEvent customerChangedEvent)
        {
            _eventLatch.Set();
        }
    }
}
