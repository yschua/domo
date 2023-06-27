using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public enum HeaterMode
{
    Off,
    Schedule,
    Override,
}

public enum HeaterLevel
{
    Low,
    High,
    Auto,
}

public partial class Heater : ObservableObject
{
    public Heater()
    {
        LowLevelSetting = new(new(10, 10, 0), new(30, 10, 10));
        HighLevelSetting = new(new(20, 10, 0), new(40, 10, 10));
        OverrideDuration = TimeSpan.FromMinutes(60);

        LowLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(LowLevelSetting));
        HighLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HighLevelSetting));
    }

    [ObservableProperty]
    private HeaterMode _mode;

    [ObservableProperty]
    private HeaterLevel _level;

    [ObservableProperty]
    private HeaterLevelSetting _lowLevelSetting;

    [ObservableProperty]
    private HeaterLevelSetting _highLevelSetting;

    [ObservableProperty]
    private TimeSpan? _overrideDuration;

    [ObservableProperty]
    private HeaterLevel _overrideLevel;
}