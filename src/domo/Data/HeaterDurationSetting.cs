using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterDurationSetting : ObservableObject
{
    public HeaterDurationSetting(int initial, int final, int change)
    {
        InitialDuration = TimeSpan.FromMinutes(initial);
        FinalDuration = TimeSpan.FromMinutes(final);
        DurationChange = TimeSpan.FromMinutes(change);
    }

    [ObservableProperty]
    private TimeSpan? _initialDuration;

    [ObservableProperty]
    private TimeSpan? _finalDuration;

    [ObservableProperty]
    private TimeSpan? _durationChange;
}