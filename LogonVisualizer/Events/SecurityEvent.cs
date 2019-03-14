using System;

namespace LogonVisualizer.Events
{
    public abstract class SecurityEvent
    {
        protected SecurityEvent(DateTime time)
        {
            Time = time;
        }

        public DateTime Time { get; }
    }
}
