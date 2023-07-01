namespace domo.Data;

public class HeaterFactory
{
    public Heater Create(TimeSpan? defaultDuration = null)
    {
        defaultDuration ??= TimeSpan.FromMinutes(10);
        return new Heater
        {
            Mode = HeaterMode.Off,
            Level = HeaterLevel.Low,
            LowLevelSetting = new HeaterSetting
            {
                OnCycleDurations = new HeaterDurations
                {
                    InitialDuration = defaultDuration,
                    FinalDuration = defaultDuration,
                    DurationChange = TimeSpan.Zero
                },
                HaltCycleDurations = new HeaterDurations
                {
                    InitialDuration = defaultDuration,
                    FinalDuration = defaultDuration,
                    DurationChange = TimeSpan.Zero
                }
            },
            HighLevelSetting = new HeaterSetting
            {
                OnCycleDurations = new HeaterDurations
                {
                    InitialDuration = defaultDuration,
                    FinalDuration = defaultDuration,
                    DurationChange = TimeSpan.Zero
                },
                HaltCycleDurations = new HeaterDurations
                {
                    InitialDuration = defaultDuration,
                    FinalDuration = defaultDuration,
                    DurationChange = TimeSpan.Zero
                }
            },
            OverrideDuration = defaultDuration,
            OverrideLevel = HeaterLevel.Low
        };
    }
}
