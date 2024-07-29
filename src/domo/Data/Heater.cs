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
    VeryLow,
}

public partial class Heater : ObservableObject
{
    public event EventHandler<HeaterMode>? HeaterModeChanged;

    public int Id { get; set; }

    public DateTime OverrideStart { get; set; }

    public HeaterLevel CurrentLevel { get; set; }

    public HeaterMode PreviousMode { get; set; } = HeaterMode.Off;

    public HeaterSetting CurrentSetting => CurrentLevel switch
    {
        HeaterLevel.VeryLow => VeryLowLevelSetting,
        HeaterLevel.Low => LowLevelSetting,
        HeaterLevel.High => HighLevelSetting
    };

    [ObservableProperty]
    private HeaterSchedule _schedule = new();

    [ObservableProperty]
    public bool _activated;

    [ObservableProperty]
    private HeaterMode _mode;

    [ObservableProperty]
    private HeaterLevel _level;

    [ObservableProperty]
    private HeaterSetting _veryLowLevelSetting = new();

    [ObservableProperty]
    private HeaterSetting _lowLevelSetting = new();

    [ObservableProperty]
    private HeaterSetting _highLevelSetting = new();

    [ObservableProperty]
    private TimeSpan? _overrideDuration;

    [ObservableProperty]
    private HeaterLevel _overrideLevel;

    public void Reset()
    {
        Mode = HeaterMode.Off;
        Activated = false;
    }

    public void SetUpPropertyChangedHandler()
    {
        VeryLowLevelSetting.SetUpPropertyChangedHandler();
        LowLevelSetting.SetUpPropertyChangedHandler();
        HighLevelSetting.SetUpPropertyChangedHandler();
        Schedule.SetUpPropertyChangedHandler();

        VeryLowLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(VeryLowLevelSetting));
        LowLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(LowLevelSetting));
        HighLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HighLevelSetting));
        Schedule.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Schedule));
    }

    public void RegisterUpdateHandler(Action handler)
    {
        PropertyChanged += (_, _) => handler();
    }

    partial void OnModeChanged(HeaterMode oldValue, HeaterMode newValue)
    {
        PreviousMode = oldValue;
        HeaterModeChanged?.Invoke(this, newValue);
    }

    partial void OnOverrideLevelChanged(HeaterLevel value)
    {
        CurrentLevel = value;
    }
}