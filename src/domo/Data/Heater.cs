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
    public event EventHandler<HeaterMode> HeaterModeChanged;

    public int Id { get; set; }

    public DateTime OverrideStart { get; set; }

    public HeaterSetting CurrentSetting { get; private set; }

    public DateTime CycleStart { get; private set; }

    public int CycleNumber { get; private set; }

    public TimeSpan OnDuration { get; private set; }

    public TimeSpan HaltDuration { get; private set; }


    //public HeaterState CurrentState { get; set; }

    //public bool IsHalted => CurrentState == HeaterState.OverrideHalt ||
    //                        CurrentState == HeaterState.ScheduleHalt;

    //public bool IsOn => CurrentState == HeaterState.OverrideOn ||
    //                    CurrentState == HeaterState.ScheduleOn;


    public HeaterMode PreviousMode { get; set; } = HeaterMode.Off;

    [ObservableProperty]
    public bool _isActivated;

    [ObservableProperty]
    private HeaterMode _mode;

    [ObservableProperty]
    private HeaterLevel _level;

    [ObservableProperty]
    private HeaterSetting _lowLevelSetting;

    [ObservableProperty]
    private HeaterSetting _highLevelSetting;

    [ObservableProperty]
    private TimeSpan? _overrideDuration;

    [ObservableProperty]
    private HeaterLevel _overrideLevel;

    public void Reset()
    {
        Mode = HeaterMode.Off;
        //Deactivate();
    }

    public void Activate()
    {
        IsActivated = true;
    }

    public void Deactivate()
    {
        IsActivated = false;
    }

    public void SetUpPropertyChangedHandler()
    {
        LowLevelSetting.SetUpPropertyChangedHandler();
        HighLevelSetting.SetUpPropertyChangedHandler();

        LowLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(LowLevelSetting));
        HighLevelSetting.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HighLevelSetting));
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
        SetCurrentSetting(value);
    }

    public void SetCurrentSetting(HeaterLevel level)
    {
        CurrentSetting = level switch
        {
            HeaterLevel.Low => LowLevelSetting,
            HeaterLevel.High => HighLevelSetting
        };
    }

    public void StartNextCycle(bool resetCycle)
    {
        CycleStart = DateTime.Now;

        if (resetCycle)
        {
            CycleNumber = 0;
            OnDuration = CurrentSetting.OnCycleDurations.InitialDuration.Value;
            return;
        }

        CycleNumber++;

        if (CycleNumber == 1)
        {
            HaltDuration = CurrentSetting.HaltCycleDurations.InitialDuration.Value;
            return;
        }

        if (IsActivated)
        {
            var durations = CurrentSetting.OnCycleDurations;
            OnDuration -= durations.DurationChange.Value;
            OnDuration = (OnDuration < durations.FinalDuration.Value)
                ? durations.FinalDuration.Value : OnDuration;
        }
        else
        {
            var durations = CurrentSetting.HaltCycleDurations;
            HaltDuration -= durations.DurationChange.Value;
            HaltDuration = (HaltDuration < durations.FinalDuration.Value)
                ? durations.FinalDuration.Value : HaltDuration;
        }
    }
}