using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public enum HeaterMode
{
    Off,
    Schedule,
    Override,
}

public partial class Heater : ObservableObject
{
    [ObservableProperty]
    private HeaterMode _mode;
}