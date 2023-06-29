namespace domo.Data;

public class HeaterFactory
{
    public Heater Create()
    {
        var heater = new Heater();
        heater.LowLevelSetting = new HeaterSetting
        {
            OnCycleDuration = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            OffCycleDuration = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        heater.HighLevelSetting = new HeaterSetting
        {
            OnCycleDuration = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            },
            OffCycleDuration = new HeaterDurations
            {
                InitialDuration = TimeSpan.Zero,
                FinalDuration = TimeSpan.Zero,
                DurationChange = TimeSpan.Zero
            }
        };
        return heater;
    }
}
