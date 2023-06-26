using CommunityToolkit.Mvvm.ComponentModel;

namespace domo.Data;

public partial class HeaterLevelSetting : ObservableObject
{
    public HeaterLevelSetting(HeaterDurationSetting onCycleDuration, HeaterDurationSetting offCycleDuration)
    {
        OnCycleDuration = onCycleDuration;
        OffCycleDuration = offCycleDuration;

        OnCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OnCycleDuration));
        OffCycleDuration.PropertyChanged += (_, _) => OnPropertyChanged(nameof(OffCycleDuration));
    }

    [ObservableProperty]
    private HeaterDurationSetting _onCycleDuration;

    [ObservableProperty]
    private HeaterDurationSetting _offCycleDuration;
}