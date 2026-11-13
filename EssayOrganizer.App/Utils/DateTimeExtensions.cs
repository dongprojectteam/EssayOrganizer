namespace EssayOrganizer.App.Utils;

public static class DateTimeExtensions
{
    public static string ToMonthKey(this DateTime utc)
    {
        return utc.ToLocalTime().ToString("yyyy-MM");
    }
}

