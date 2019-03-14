using LogonVisualizer.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LogonVisualizer
{
    public static class Program
    {
        private static readonly Formatter Formatter = new Formatter();

        public static void Main()
        {
            var logonsById = new Dictionary<ulong, LogonEvent>();
            var logonRanges = new List<(LogonEvent logon, DateTime logoffTime)>();

            foreach (var ev in SecurityEventReader.ReadAll())
            {
                switch (ev)
                {
                    case StartupEvent _:
                    {
                        foreach (var logon in logonsById.Values.OrderBy(l => l.Time))
                        {
                            logonRanges.Add((logon, ev.Time));
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

            Console.WriteLine("Older:");
            Console.WriteLine();

            foreach (var (logon, logoffTime) in logonRanges.OrderBy(r => r.logon.Time))
            {
                Console.WriteLine(FormatLogonEvent(logon, logoffTime));
            }

            Console.WriteLine();
            Console.WriteLine("Currently logged on:");
            Console.WriteLine();

            foreach (var logon in logonsById.Values.OrderBy(l => l.Time))
            {
                Console.WriteLine(FormatLogonEvent(logon, logoffTime: null));
            }
        }

        private static readonly string ExpectedProcessName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "svchost.exe");

        private static string FormatLogonEvent(LogonEvent logonEvent, DateTime? logoffTime)
        {
            var builder = new StringBuilder();
            builder.Append(Formatter.FormatSid(logonEvent.User));

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
