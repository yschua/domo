namespace domo.Data;

public class HeaterLevelSetting
{
    public required TimeSpan? InitialDuration { get; set; }

    public required TimeSpan? FinalDuration { get; set; }

    public required TimeSpan? DurationChange { get; set; }
}