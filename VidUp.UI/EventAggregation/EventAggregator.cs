using System;
using System.Collections.Generic;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class EventAggregator
    {
        private static readonly EventAggregator instance = new EventAggregator();

        private object locker = new object();
        private List<(Type eventType, Delegate method)> eventRegister = new List<(Type eventType, Delegate method)>();

        static EventAggregator()
        {
        }

        private EventAggregator()
        {
        }

        public static EventAggregator Instance
        {
            get
            {
                return EventAggregator.instance;
            }
        
        }
        public Subscription Subscribe<T>(Action<T> action)
        {
            if (action != null)
            {
                lock (locker)
                {
                    this.eventRegister.Add((typeof(T), action));
                    return new Subscription(() =>
                        {
                            this.eventRegister.Remove((typeof(T), action));
                        }
                    );
                }
            }

            return null;
        }
        public void Publish<T>(T data)
        {
            List<(Type eventType, Delegate method)> eventRegister = null;
            lock (locker)
            {
                eventRegister= new List<(Type eventType, Delegate method)>(this.eventRegister);
            }

            foreach ((Type eventType, Delegate method) tEvent in eventRegister)
            {
                if (tEvent.eventType == typeof(T))
                {
                    ((Action<T>)tEvent.method)(data);
                }
            }
        }
    }
}
