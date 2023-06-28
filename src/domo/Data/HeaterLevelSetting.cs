using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterLevelSetting : ObservableObject
{
    [ObservableProperty]
    private HeaterDurationSetting _onCycleDuration;

    [ObservableProperty]
    private HeaterDurationSetting _offCycleDuration;

    public void SetUpPropertyChangedHandler()
    {
        OnCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OnCycleDuration));
        OffCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OffCycleDuration));
    }
}