using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterDurations : ObservableObject
{
    [ObservableProperty]
    private TimeSpan _initialDuration;

    [ObservableProperty]
    private TimeSpan _finalDuration;

    [ObservableProperty]
    private TimeSpan _durationChange;

    public void Set(TimeSpan initialDuration, TimeSpan finalDuration, TimeSpan durationChange)
    {
        InitialDuration = initialDuration;
        FinalDuration = finalDuration;
        DurationChange = durationChange;
    }

    public void Set_min(int initialDuration, int finalDuration, int durationChange)
    {
        Set(TimeSpan.FromMinutes(initialDuration), TimeSpan.FromMinutes(finalDuration),
            TimeSpan.FromMinutes(durationChange));
    }

    public void Set_ms(int initialDuration, int finalDuration, int durationChange)
    {
        Set(TimeSpan.FromMilliseconds(initialDuration), TimeSpan.FromMilliseconds(finalDuration),
            TimeSpan.FromMilliseconds(durationChange));
    }

    public void SetSingle(TimeSpan duration)
    {
        Set(duration, duration, TimeSpan.FromDays(1));
    }

    partial void OnInitialDurationChanging(TimeSpan value)
    {
        if (value == TimeSpan.Zero)
        {
            throw new ArgumentException($"{nameof(InitialDuration)} cannot be zero");
        }
    }

    partial void OnFinalDurationChanging(TimeSpan value)
    {
        if (value == TimeSpan.Zero)
        {
            throw new ArgumentException($"{nameof(FinalDuration)} cannot be zero");
        }
    }
}