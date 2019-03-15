using LogonVisualizer.Events;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LogonVisualizer
{
    public static class Program
    {
        public static void Main()
        {
            var ranges = MergeLinkedLogons(GetLogonRanges());

            var today = DateTime.Today;

            foreach (var group in ranges
                .Where(r => r.logoffTime != null)
                .GroupBy(r => Formatter.GetDateGrouping(r.logoffTime.Value, today, DateTimeFormatInfo.CurrentInfo))
                .OrderBy(g => g.First().logoffTime.Value))
            {
                Console.WriteLine($"Ending {group.Key}:");
                Console.WriteLine();

                foreach (var (logon, logoffTime) in group
                    .OrderBy(r => r.logon.Time))
                {
                    Console.WriteLine(FormatLogonEvent(logon, logoffTime));
                }

                Console.WriteLine();
            }

            Console.WriteLine("Currently logged on:");
            Console.WriteLine();

            foreach (var (logon, _) in ranges
                .Where(r => r.logoffTime is null)
                .OrderBy(r => r.logon.Time))
            {
                Console.WriteLine(FormatLogonEvent(logon, logoffTime: null));
            }
        }

        private static IReadOnlyList<(LogonEvent logon, DateTime? logoffTime)> GetLogonRanges()
        {
            var logonsById = new Dictionary<ulong, LogonEvent>();
            var logonRanges = new List<(LogonEvent logon, DateTime? logoffTime)>();

            foreach (var ev in SecurityEventReader.ReadAll())
            {
                switch (ev)
                {
                    case StartupEvent _:
                    {
                        foreach (var logon in logonsById.Values.OrderBy(l => l.Time))
                        {
                            logonRanges.Add((logon, logoffTime: ev.Time));
                        }
                        logonsById.Clear();
                        break;
                    }
                    case LogonEvent logon:
                    {
                        logonsById.Add(logon.LogonId, logon);
                        break;
                    }
                    case LogoffEvent logoff when logonsById.TryGetValue(logoff.LogonId, out var logon):
                    {
                        logonRanges.Add((logon, logoff.Time));
                        logonsById.Remove(logoff.LogonId);
                        break;
                    }
                }
            }

            foreach (var logon in logonsById.Values)
            {
                logonRanges.Add((logon, logoffTime: null));
            }

            return logonRanges;
        }

        private static IReadOnlyList<(LogonEvent logon, DateTime? logoffTime)> MergeLinkedLogons(IReadOnlyList<(LogonEvent logon, DateTime? logoffTime)> logonRanges)
        {
            var rangesByLogonId = logonRanges.ToDictionary(r => r.logon.LogonId);
            var skip = new HashSet<ulong>();

            var merged = new List<(LogonEvent logon, DateTime? logoffTime)>();

            foreach (var range in logonRanges)
            {
                if (skip.Contains(range.logon.LogonId)) continue;

                if (range.logon.LinkedLogonId != 0
                    && rangesByLogonId.TryGetValue(range.logon.LinkedLogonId, out var linkedRange))
                {
                    if (linkedRange.logon.LinkedLogonId != range.logon.LogonId)
                    {
                        throw new NotImplementedException("Expected logins to be linked in pairs.");
                    }

                    skip.Add(range.logon.LinkedLogonId);

                    merged.Add((
                        range.logon.Time < linkedRange.logon.Time
                            ? range.logon
                            : linkedRange.logon,
                        range.logoffTime is null
                        || (linkedRange.logoffTime != null && range.logoffTime.Value < linkedRange.logoffTime.Value)
                            ? range.logoffTime
                            : linkedRange.logoffTime));
                }
                else
                {
                    merged.Add(range);
                }
            }

            return merged;
        }

        private static readonly Formatter Formatter = new Formatter();
        private static readonly string ExpectedProcessName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");

        private static string FormatLogonEvent(LogonEvent logonEvent, DateTime? logoffTime)
        {
            var builder = new StringBuilder(" - ");
            builder.Append(Formatter.FormatUsername(logonEvent.User, logonEvent.UserName, logonEvent.DomainName));

            switch (logonEvent.LogonType)
            {
                case LogonType.Interactive:
                    if (ShouldWriteProcessName(logonEvent))
                        builder.Append(" (").Append(logonEvent.ProcessName).Append(')');
                    break;

                case LogonType.Network:
                    builder.Append(" (from ").Append(logonEvent.WorkstationName);

                    if (ShouldWriteProcessName(logonEvent))
                        builder.Append(", ").Append(logonEvent.ProcessName);

                    builder.Append(')');
                    break;

                default:
                    builder.Append(" (").Append(
                        logonEvent.LogonType == LogonType.Unlock ? "unlock" :
                        Enum.IsDefined(typeof(LogonType), logonEvent.LogonType) ? logonEvent.LogonType.ToString() :
                        "unknown logon type " + logonEvent.LogonType);

                    if (ShouldWriteProcessName(logonEvent))
                        builder.Append(", ").Append(logonEvent.ProcessName);

                    builder.Append(')');
                    break;
            }

            if (logoffTime is null)
            {
                builder.Append(" since ");
                Formatter.FormatTime(builder, logonEvent.Time);
            }
            else
            {
                builder.Append(' ');
                Formatter.FormatRange(builder, logonEvent.Time, logoffTime.Value);
            }

            return builder.ToString();
        }

        private static bool ShouldWriteProcessName(LogonEvent logonEvent)
        {
            return !string.IsNullOrWhiteSpace(logonEvent.ProcessName)
                && !ExpectedProcessName.Equals(logonEvent.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
