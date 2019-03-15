using System;
using System.Security.Principal;

namespace LogonVisualizer.Events
{
    /// <summary>
    /// https://docs.microsoft.com/windows/security/threat-protection/auditing/event-4624
    /// </summary>
    public sealed class LogonEvent : SecurityEvent
    {
        public LogonEvent(
            DateTime time,
            ulong logonId,
            ulong linkedLogonId,
            SecurityIdentifier user,
            LogonType logonType,
            bool elevatedToken,
            string workstationName,
            string processName,
            string ipAddress,
            string ipPort)
            : base(time)
        {
            LogonId = logonId;
            LinkedLogonId = linkedLogonId;
            User = user;
            LogonType = logonType;
            ElevatedToken = elevatedToken;
            WorkstationName = workstationName;
            ProcessName = processName;
            IpAddress = ipAddress;
            IpPort = ipPort;
        }

        public ulong LogonId { get; }
        public ulong LinkedLogonId { get; }
        public SecurityIdentifier User { get; }
        public LogonType LogonType { get; }
        public bool ElevatedToken { get; }
        public string WorkstationName { get; }
        public string ProcessName { get; }
        public string IpAddress { get; }
        public string IpPort { get; }
    }
}
