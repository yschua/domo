namespace domo.Data;

public class HeaterFactory
{
    public Heater Create()
    {
        var heater = new Heater();
        heater.LowLevelSetting = new HeaterSetting
        {
            OnCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            HaltCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        heater.HighLevelSetting = new HeaterSetting
        {
            OnCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            HaltCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        heater.OverrideDuration = TimeSpan.Zero;
        return heater;
    }
}
