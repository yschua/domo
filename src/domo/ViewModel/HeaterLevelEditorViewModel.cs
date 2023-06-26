using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using domo.Data;

namespace domo.ViewModel;

public partial class HeaterLevelEditorViewModel : ObservableObject
{
    private readonly Heater _heater;

    public HeaterLevelEditorViewModel(Heater heater)
    {
        _heater = heater;
        OnSelectedLevelChanged(SelectedLevel);
    }

    [ObservableProperty]
    private HeaterLevel _selectedLevel;

    [ObservableProperty]
    private HeaterDurationSetting _onCycleDuration;

    [ObservableProperty]
    private HeaterDurationSetting _offCycleDuration;

    partial void OnSelectedLevelChanged(HeaterLevel value)
    {
        var selectedLevelSetting = value switch
        {
            HeaterLevel.Low => _heater.LowLevelSetting,
            HeaterLevel.High => _heater.HighLevelSetting
        };

        OnCycleDuration = selectedLevelSetting.OnCycleDuration;
        OffCycleDuration = selectedLevelSetting.OffCycleDuration;
    }
}
