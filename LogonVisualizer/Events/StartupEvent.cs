using System;

namespace LogonVisualizer.Events
{
    /// <summary>
    /// https://docs.microsoft.com/windows/security/threat-protection/auditing/event-4608
    /// </summary>
    public sealed class StartupEvent : SecurityEvent
    {
        public StartupEvent(DateTime time) : base(time)
        {
        }
    }
}
