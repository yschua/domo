using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterSetting : ObservableObject
{
    [ObservableProperty]
    private HeaterDurations _onCycleDuration;

    [ObservableProperty]
    private HeaterDurations _offCycleDuration;

    public void SetUpPropertyChangedHandler()
    {
        OnCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OnCycleDuration));
        OffCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OffCycleDuration));
    }
}