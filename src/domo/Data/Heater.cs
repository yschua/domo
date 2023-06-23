namespace domo.Data;

public enum HeaterMode
{
    Off,
    Schedule,
    Override,
}

public class Heater
{
    private HeaterMode _mode;
    public HeaterMode Mode 
    {
        get => _mode;
        set 
        {
            if (_mode != value)
            {
                _mode = value;
                Changed?.Invoke();
            }
        }
    }

    public event Action? Changed;
}