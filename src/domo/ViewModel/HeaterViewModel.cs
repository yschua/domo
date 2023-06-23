using CommunityToolkit.Mvvm.ComponentModel;
using domo.Data;

namespace domo.ViewModel;

public partial class HeaterViewModel : ObservableObject
{
    private Heater _heater;

    public HeaterViewModel(Heater heater)
    {
        _heater = heater;
    }

    [ObservableProperty]
    private HeaterMode _mode;

    partial void OnModeChanged(HeaterMode value)
    {
        _heater.Mode = value;
    }
}
