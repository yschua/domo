﻿using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterDurations : ObservableObject
{
    [ObservableProperty]
    private TimeSpan? _initialDuration;

    [ObservableProperty]
    private TimeSpan? _finalDuration;

    [ObservableProperty]
    private TimeSpan? _durationChange;

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
}