namespace domo.Data;

public class HeaterStateMachineOptions
{
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(1);
}
