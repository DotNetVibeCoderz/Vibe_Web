namespace SMSNet.Services;

/// <summary>
/// School-local time (WIB, UTC+7).
/// <para>
/// Every date shown to a user and every "today" comparison goes through here.
/// <c>DateTime.Now</c> would be the server's clock, which is UTC on most hosts —
/// that shifts attendance and payment records into the wrong day for anyone in
/// Indonesia between 00:00 and 07:00 local.
/// </para>
/// </summary>
public static class SchoolClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static DateTimeOffset Now => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateTime LocalNow => Now.DateTime;

    public static DateTime Today => Now.Date;

    public static string TimeZoneLabel => "WIB (UTC+7)";

    private static TimeZoneInfo ResolveZone()
    {
        // The IANA id works on Linux/macOS; the Windows id works on Windows.
        // .NET can usually translate between them, but not on every host, so try both.
        foreach (var id in new[] { "Asia/Jakarta", "SE Asia Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        // WIB has no DST, so a fixed offset is a faithful fallback.
        return TimeZoneInfo.CreateCustomTimeZone("WIB", TimeSpan.FromHours(7), "WIB", "WIB");
    }
}
