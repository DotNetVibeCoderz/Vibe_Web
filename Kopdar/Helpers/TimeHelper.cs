using System;

namespace Kopdar.Helpers;

public static class TimeHelper
{
    public static string ToFriendlyTime(DateTime dt)
    {
        var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
        double delta = Math.Abs(ts.TotalSeconds);

        if (delta < 60) return ts.Seconds + " seconds ago";
        if (delta < 3600) return ts.Minutes + " minutes ago";
        if (delta < 86400) return ts.Hours + " hours ago";
        if (delta < 2592000) return ts.Days + " days ago";
        
        return dt.ToString("dd MMM yyyy HH:mm");
    }
}
