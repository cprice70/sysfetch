using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sysfetch.Extensions
{
    public static class TimeExtensions
    {
        public static string GetHumanReadableString(this int seconds)
        {
            // Calculate the number of days, hours, minutes, and remaining seconds
            int days = seconds / 86400;
            int hours = (seconds % 86400) / 3600;
            int minutes = (seconds % 3600) / 60;
            seconds %= 60;

            // Create the human-readable string
            string humanReadable = "";
            if (days > 0) humanReadable += $"{days} " + ((days > 1) ? "days ":"day ");
            if (hours > 0) humanReadable += $"{hours} " + ((hours > 1) ? "hours ":"hour ");
            if (minutes > 0) humanReadable += $"{minutes} " + ((minutes > 1) ? "minutes ":"minute ");
            if (seconds > 0) humanReadable += $"{seconds} " + ((seconds > 1) ? "seconds ":"second ");

            return humanReadable.Trim();
        }
    }
}
