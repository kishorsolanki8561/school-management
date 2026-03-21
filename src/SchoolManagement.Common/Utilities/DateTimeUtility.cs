namespace SchoolManagement.Common.Utilities;

public static class DateTimeUtility
{
    public static DateTime UtcNow => DateTime.UtcNow;
    public static DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);

    public static bool IsExpired(DateTime expiryUtc) => DateTime.UtcNow >= expiryUtc;

    public static DateTime AddMinutes(int minutes) => DateTime.UtcNow.AddMinutes(minutes);
    public static DateTime AddDays(int days) => DateTime.UtcNow.AddDays(days);
}
