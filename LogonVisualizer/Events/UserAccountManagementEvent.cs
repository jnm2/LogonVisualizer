using System;

namespace LogonVisualizer.Events
{
    /// <summary>
    /// https://docs.microsoft.com/windows/security/threat-protection/auditing/event-4798
    /// </summary>
    public sealed class UserAccountManagementEvent : SecurityEvent
    {
        public UserAccountManagementEvent(
            DateTime time,
            string callerProcessName)
            : base(time)
        {
            CallerProcessName = callerProcessName;
        }

        public string CallerProcessName { get; }
    }
}
