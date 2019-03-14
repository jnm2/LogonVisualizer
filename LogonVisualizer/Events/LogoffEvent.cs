using System;
using System.Security.Principal;

namespace LogonVisualizer.Events
{
    /// <summary>
    /// https://docs.microsoft.com/windows/security/threat-protection/auditing/event-4634
    /// </summary>
    public sealed class LogoffEvent : SecurityEvent
    {
        public LogoffEvent(
            DateTime time,
            ulong logonId,
            SecurityIdentifier user)
            : base(time)
        {
            LogonId = logonId;
            User = user;
        }

        public ulong LogonId { get; }
        public SecurityIdentifier User { get; }
    }
}
