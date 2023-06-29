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
}