namespace domo.Data;

public class HeaterFactory
{
    public Heater Create(TimeSpan? duration = null)
    {
        duration ??= TimeSpan.FromMinutes(10);
        return new Heater
        {
            Mode = HeaterMode.Off,
            Level = HeaterLevel.Low,
            VeryLowLevelSetting = CreateHeaterSetting(duration.Value),
            LowLevelSetting = CreateHeaterSetting(duration.Value),
            HighLevelSetting = CreateHeaterSetting(duration.Value),
            OverrideDuration = duration,
            OverrideLevel = HeaterLevel.Low
        };
    }

    private static HeaterDurations CreateHeaterDurations(TimeSpan duration)
    {
        return new HeaterDurations
        {
            InitialDuration = duration,
            FinalDuration = duration,
            DurationChange = TimeSpan.Zero
        };
    }

    private static HeaterSetting CreateHeaterSetting(TimeSpan duration)
    {
        return new HeaterSetting
        {
            OnCycleDurations = CreateHeaterDurations(duration),
            HaltCycleDurations = CreateHeaterDurations(duration)
        };
    }
}