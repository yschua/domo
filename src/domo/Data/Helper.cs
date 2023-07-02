namespace domo.Data;

public static class Helper
{
    public static TimeSpan Max(TimeSpan val1, TimeSpan val2) => (val1 > val2) ? val1 : val2;

    public static TimeSpan Min(TimeSpan val1, TimeSpan val2) => (val1 < val2) ? val1 : val2;
}
