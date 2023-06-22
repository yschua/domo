namespace domo.Data;

public enum HeaterMode
{
    Off,
    Schedule,
    Override,
}

public class Heater
{
    public HeaterMode Mode { get; set; }
}