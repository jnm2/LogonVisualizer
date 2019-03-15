using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Principal;
using System.Text;

namespace LogonVisualizer
{
    internal sealed class Formatter
    {
        private static readonly string MachineNamePrefix = Environment.MachineName + '\\';

        private readonly DateTime today;
        private readonly CultureInfo cultureInfo;
        private readonly Dictionary<SecurityIdentifier, string> formattedSecurityIdentifiers = new Dictionary<SecurityIdentifier, string>();

        public Formatter(DateTime today, CultureInfo cultureInfo)
        {
            this.today = today.Date;
            this.cultureInfo = cultureInfo ?? throw new ArgumentNullException(nameof(cultureInfo));
        }

        public void FormatTime(StringBuilder builder, DateTime date, string datePrefix = null)
        {
            var daysAgo = (int)(today - date.Date).TotalDays;

            if (daysAgo >= 0 && daysAgo < 2)
            {
                builder.Append(daysAgo == 0 ? "today" : "yesterday");
            }
            else
            {
                if (datePrefix != null)
                    builder.Append(datePrefix).Append(' ');

                builder.Append(date.ToString(date.Year == today.Year ? "MMM d" : "MMM d, yyyy", cultureInfo));
            }

            builder.Append(" at ");
            builder.Append(date.ToString("t", cultureInfo));
        }

        public void FormatRange(StringBuilder builder, DateTime start, DateTime end)
        {
            var duration = end - start;

            builder.Append("for ");
            FormatDuration(builder, duration);

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

        public void FormatDuration(StringBuilder builder, TimeSpan duration)
        {
            if (duration.TotalDays >= 2)
            {
                var days = (int)Math.Round(duration.TotalDays);
                if (days >= 7)
                {
                    builder.Append((days / 7).ToString(cultureInfo));
                    builder.Append(" wk");
                }

                builder.Append((days % 7).ToString(cultureInfo));
                builder.Append(" days");
            }
            else if (duration.TotalHours >= 1)
            {
                builder.Append(((int)Math.Floor(duration.TotalHours)).ToString(cultureInfo));
                builder.Append(" hr ");
                builder.Append(duration.Minutes.ToString(cultureInfo));
                builder.Append(" min");
            }
            else if (duration.TotalMinutes >= 1)
            {
                builder.Append(duration.TotalMinutes.ToString("0", cultureInfo));
                builder.Append(" min");
            }
            else if (duration.TotalSeconds >= 1)
            {
                builder.Append(duration.TotalSeconds.ToString("0", cultureInfo));
                builder.Append(" sec");
            }
            else
            {
                builder.Append(duration.TotalMilliseconds.ToString(
                    duration.TotalMilliseconds >= 1 ? "0" : "0.#######",
                    cultureInfo));

                builder.Append(" ms");
            }
        }

        public string GetDateGrouping(DateTime date)
        {
            var daysAgo = (int)(today.Date - date.Date).TotalDays;

            if (daysAgo < 0) throw new NotImplementedException();
            if (daysAgo == 0) return "today";
            if (daysAgo == 1) return "yesterday";

            var startOfWeek = today.StartOfWeek(cultureInfo.DateTimeFormat);
            if (date >= startOfWeek) return "earlier this week";
            if (date >= startOfWeek.AddDays(-7)) return "last week";

            if (date.Year == today.Year)
            {
                if (date.Month == today.Month) return "earlier this month";
                if (date.Month == today.Month - 1) return "last month";

                return "earlier in " + date.ToString("yyyy", cultureInfo);
            }

            return date.ToString("yyyy", cultureInfo);
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
