using LogonVisualizer.Events;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Principal;

namespace LogonVisualizer
{
    public static class SecurityEventReader
    {
        public static IReadOnlyList<SecurityEvent> ReadAll()
        {
            var events = new List<SecurityEvent>();

            // To investigate as a separate report:

            // 4625 An account failed to log on.
            // 4648 A logon was attempted using explicit credentials.
            // 4672 Special privileges assigned to new logon.
            // 4675 SIDs were filtered.
            // 4797 Sometimes "An attempt was made to query the existence of a blank password for an account."
            // Logoff events without logon events

            var query = new EventLogQuery(
                "Security",
                PathType.LogName,
                @"*[(((System[EventID=4624] and EventData[Data[@Name='LogonType']!=5]) or System[EventID=4634])
                    and EventData[
                        Data[@Name='TargetUserSid']!='S-1-0-0'
                        and Data[@Name='TargetUserSid']!='S-1-5-7'
                        and Data[@Name='TargetUserSid']!='S-1-5-18'
                        and Data[@Name='TargetDomainName']!='Font Driver Host'
                        and Data[@Name='TargetDomainName']!='Window Manager'])
                    or System[EventID=4647 or EventID=4608]]");

            using (var loginEventPropertySelector = new EventLogPropertySelector(new[]
            {
                "Event/EventData/Data[@Name='TargetLogonId']",
                "Event/EventData/Data[@Name='TargetUserSid']",
                "Event/EventData/Data[@Name='LogonType']",
                "Event/EventData/Data[@Name='ElevatedToken']",
                "Event/EventData/Data[@Name='WorkstationName']",
                "Event/EventData/Data[@Name='ProcessName']",
                "Event/EventData/Data[@Name='IpAddress']",
                "Event/EventData/Data[@Name='IpPort']"
            }))
            using (var logoffEventPropertySelector = new EventLogPropertySelector(new[]
            {
                "Event/EventData/Data[@Name='TargetLogonId']",
                "Event/EventData/Data[@Name='TargetUserSid']"
            }))
            using (var reader = new EventLogReader(query))
            {
                while (reader.ReadEvent() is { } ev)
                {
                    switch (ev.Id)
                    {
                        case 4608:
                            events.Add(new StartupEvent(ev.TimeCreated.Value));
                            break;

                        case 4624:
                            var loginPropertyValues = ((EventLogRecord)ev).GetPropertyValues(loginEventPropertySelector);

                            events.Add(new LogonEvent(
                                ev.TimeCreated.Value,
                                logonId: (ulong)loginPropertyValues[0],
                                user: (SecurityIdentifier)loginPropertyValues[1],
                                logonType: (LogonType)(uint)loginPropertyValues[2],
                                elevatedToken: loginPropertyValues[3] is "%%1842",
                                workstationName: GetXPathString(loginPropertyValues[4]),
                                processName: GetXPathString(loginPropertyValues[5]),
                                ipAddress: GetXPathString(loginPropertyValues[6]),
                                ipPort: GetXPathString(loginPropertyValues[7])));
                            break;

                        case 4634:
                        case 4647:
                            var logoffPropertyValues = ((EventLogRecord)ev).GetPropertyValues(logoffEventPropertySelector);

                            events.Add(new LogoffEvent(
                                ev.TimeCreated.Value,
                                logonId: (ulong)logoffPropertyValues[0],
                                user: (SecurityIdentifier)logoffPropertyValues[1]));
                            break;
                    }
                }
            }

            return events;
        }

        private static string GetXPathString(object value)
        {
            var str = (string)value;
            return str == "-" ? null : str;
        }
    }
}
