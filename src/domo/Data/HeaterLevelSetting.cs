using System.Diagnostics.CodeAnalysis;

namespace domo.Data;

public record HeaterLevelSetting(
    HeaterDurationSetting OnCycleDuration,
    HeaterDurationSetting OffCycleDuration
);

public class HeaterDurationSetting
{
    [SetsRequiredMembers]
    public HeaterDurationSetting(int initial, int final, int change)
    {
        InitialDuration = TimeSpan.FromMinutes(initial);
        FinalDuration = TimeSpan.FromMinutes(final);
        DurationChange = TimeSpan.FromMinutes(change);
    }

    public required TimeSpan? InitialDuration { get; set; }

    public required TimeSpan? FinalDuration { get; set; }

    public required TimeSpan? DurationChange { get; set; }
}