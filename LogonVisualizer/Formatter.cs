using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace LogonVisualizer
{
    internal sealed class Formatter
    {
        private static readonly string MachineNamePrefix = Environment.MachineName + '\\';

        private readonly Dictionary<SecurityIdentifier, string> formattedSecurityIdentifiers = new Dictionary<SecurityIdentifier, string>();

        public static void FormatTime(StringBuilder builder, DateTime date, string datePrefix = null)
        {
            var today = DateTime.Today;
            var daysAgo = (int)(today - date.Date).TotalDays;

            if (daysAgo >= 0 && daysAgo < 2)
            {
                builder.Append(daysAgo == 0 ? "today" : "yesterday");
            }
            else
            {
                if (datePrefix != null)
                    builder.Append(datePrefix).Append(' ');

                builder.Append(date.ToString(date.Year == today.Year ? "MMM d" : "MMM d, yyyy"));
            }

            builder.Append(" at ");
            builder.Append(date.ToShortTimeString());
        }

        public static void FormatRange(StringBuilder builder, DateTime start, DateTime end)
        {
            var duration = end - start;

            builder.Append("for ");
            builder.Append(FormatDuration(duration));

            if (duration.TotalHours < 1)
            {
                builder.Append(' ');
                FormatTime(builder, start, datePrefix: "on");
            }
            else
            {
                builder.Append(", from ");
                FormatTime(builder, start);
                builder.Append(" to ");
                FormatTime(builder, end);
            }
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 2)
            {
                var days = (int)Math.Round(duration.TotalDays);

                if (duration.TotalDays >= 7)
                {
                    return $"{days / 7:n0} wk {days % 7:n0} days";
                }
                else
                {
                    return $"{days} days";
                }
            }
            if (duration.TotalHours >= 1)
            {
                return $"{duration.Hours} hr {duration.Minutes} min";
            }
            if (duration.TotalMinutes >= 1)
            {
                return $"{duration.TotalMinutes:n0} min";
            }
            if (duration.TotalSeconds >= 1)
            {
                return $"{duration.TotalSeconds:n0} sec";
            }
            if (duration.TotalMilliseconds >= 1)
            {
                return $"{duration.TotalMilliseconds:n0} ms";
            }

            return $"{duration.TotalMilliseconds:0.#######} ms";
        }

        public string FormatUsername(SecurityIdentifier securityIdentifier, string username, string domain)
        {
            if (!formattedSecurityIdentifiers.TryGetValue(securityIdentifier, out var formatted))
            {
                formatted = TryGetUsername(securityIdentifier);

                if (formatted is null)
                {
                    if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(domain))
                    {
                        formatted = domain.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase)
                            ? username
                            : domain + '\\' + username;
                    }
                    else
                    {
                        formatted = securityIdentifier.Value;
                    }
                }

                formattedSecurityIdentifiers.Add(securityIdentifier, formatted);
            }

            return formatted;
        }

        private static string TryGetUsername(SecurityIdentifier securityIdentifier)
        {
            string username;
            try
            {
                username = ((NTAccount)securityIdentifier.Translate(typeof(NTAccount))).Value;
            }
            catch (IdentityNotMappedException)
            {
                return null;
            }

            if (username.StartsWith(MachineNamePrefix, StringComparison.OrdinalIgnoreCase))
                return username.Substring(MachineNamePrefix.Length);

            return username;
        }
    }
}
