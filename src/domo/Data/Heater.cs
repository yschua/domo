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

public class Heater
{
    public Heater()
    {
        LowLevelSetting = new HeaterLevelSetting(
            new HeaterDurationSetting(10, 10, 0),
            new HeaterDurationSetting(30, 10, 10)
        );

        HighLevelSetting = new HeaterLevelSetting(
            new HeaterDurationSetting(10, 10, 0),
            new HeaterDurationSetting(30, 10, 10)
        );
    }

    public HeaterMode Mode { get; set; }

    public HeaterLevel Level { get; set; }

    public HeaterLevelSetting LowLevelSetting { get; init; }
    
    public HeaterLevelSetting HighLevelSetting { get; init; }
}