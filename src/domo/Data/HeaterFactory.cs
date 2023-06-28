namespace domo.Data;

public class HeaterFactory
{
    public Heater Create()
    {
        var heater = new Heater();
        heater.LowLevelSetting = new HeaterLevelSetting
        {
            OnCycleDuration = new HeaterDurationSetting
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            OffCycleDuration = new HeaterDurationSetting
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        heater.HighLevelSetting = new HeaterLevelSetting
        {
            OnCycleDuration = new HeaterDurationSetting
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            OffCycleDuration = new HeaterDurationSetting
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        return heater;
    }
}
