namespace domo.Data;

public class HeaterFactory
{
    public Heater Create()
    {
        var heater = new Heater();
        heater.Mode = HeaterMode.Off;
        heater.Level = HeaterLevel.Low;
        heater.LowLevelSetting = new HeaterSetting
        {
            OnCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.FromMinutes(1),
                FinalDuration = TimeSpan.FromMinutes(1),
                DurationChange = TimeSpan.Zero
            },
            HaltCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.FromMinutes(1),
                FinalDuration = TimeSpan.FromMinutes(1),
                DurationChange = TimeSpan.Zero
            }
        };
        heater.HighLevelSetting = new HeaterSetting
        {
            OnCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.FromMinutes(1),
                FinalDuration = TimeSpan.FromMinutes(1),
                DurationChange = TimeSpan.Zero
            },
            HaltCycleDurations = new HeaterDurations
            {
                InitialDuration = TimeSpan.FromMinutes(1),
                FinalDuration = TimeSpan.FromMinutes(1),
                DurationChange = TimeSpan.Zero
            }
        };
        heater.OverrideDuration = TimeSpan.FromMinutes(1);
        heater.OverrideLevel = HeaterLevel.Low;
        return heater;
    }
}
