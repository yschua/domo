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
    public HeaterMode Mode { get; set; }

    public HeaterLevel Level { get; set; }

    public HeaterLevelSetting LowLevelSetting { get; init; }
    
    public HeaterLevelSetting HighLevelSetting { get; init; }
}