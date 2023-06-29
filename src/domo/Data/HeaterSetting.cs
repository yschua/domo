using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterSetting : ObservableObject
{
    [ObservableProperty]
    private HeaterDurations _onCycleDurations;

    [ObservableProperty]
    private HeaterDurations _haltCycleDurations;

    public void SetUpPropertyChangedHandler()
    {
        OnCycleDurations.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OnCycleDurations));
        HaltCycleDurations.PropertyChanged += (_, _) => OnPropertyChanged(nameof(HaltCycleDurations));
    }
}