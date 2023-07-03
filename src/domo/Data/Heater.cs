using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

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

    public HeaterMode PreviousMode { get; set; } = HeaterMode.Off;

    [ObservableProperty]
    private HeaterSchedule _schedule = new();

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
        IsActivated = false;
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
        Schedule.SetUpPropertyChangedHandler();

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
            OnDuration = CurrentSetting.OnCycleDurations.InitialDuration;
            return;
        }

        CycleNumber++;

        if (CycleNumber == 1)
        {
            HaltDuration = CurrentSetting.HaltCycleDurations.InitialDuration;
            return;
        }

        void UpdateDuration(ref TimeSpan duration, HeaterDurations durationSetting)
        {
            if (duration < durationSetting.FinalDuration)
            {
                duration = Helper.Min(durationSetting.FinalDuration, duration + durationSetting.DurationChange);
            }
            else if (duration > durationSetting.FinalDuration)
            {
                duration = Helper.Max(durationSetting.FinalDuration, duration - durationSetting.DurationChange);
            }
        }

        if (IsActivated)
        {
            var duration = OnDuration;
            UpdateDuration(ref duration, CurrentSetting.OnCycleDurations);
            // TODO put logging on setter instead
            Debug.WriteLine($"[{DateTime.Now:s.fff}] OnDuration: {OnDuration} -> {duration}");
            OnDuration = duration;
        }
        else
        {
            var duration = HaltDuration;
            UpdateDuration(ref duration, CurrentSetting.HaltCycleDurations);
            Debug.WriteLine($"[{DateTime.Now:s.fff}] HaltDuration: {HaltDuration} -> {duration}");
            HaltDuration = duration;
        }
    }
}