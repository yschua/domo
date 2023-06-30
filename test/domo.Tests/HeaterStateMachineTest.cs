using domo.Data;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace domo.Tests;

public class HeaterStateMachineTest
{
    private readonly Heater _heater;
    private readonly HeaterStateMachine _machine;

    public HeaterStateMachineTest()
    {
        _heater = new HeaterFactory().Create();
        var options = Options.Create(new HeaterStateMachineOptions
        {
            TickInterval = TimeSpan.FromMilliseconds(100)
        });
        _machine = new HeaterStateMachine(options, _heater);
        _machine.CurrentState.Should().Be(HeaterState.Off);
    }

    [Fact]
    public void SetSameMode()
    {
        _heater.Mode = HeaterMode.Off;
        _heater.Mode = HeaterMode.Off;

        _heater.Mode = HeaterMode.Override;
        _heater.Mode = HeaterMode.Override;

        _heater.Mode = HeaterMode.Schedule;
        _heater.Mode = HeaterMode.Schedule;
    }

    [Fact]
    public void AllModesTransition()
    {
        _heater.Mode = HeaterMode.Off;
        _machine.CurrentState.Should().Be(HeaterState.Off);

        _heater.Mode = HeaterMode.Override;
        _machine.CurrentState.Should().Be(HeaterState.OverrideOn);

        _heater.Mode = HeaterMode.Schedule;
        _machine.CurrentState.Should().Be(HeaterState.ScheduleIdle);

        _heater.Mode = HeaterMode.Off;
        _machine.CurrentState.Should().Be(HeaterState.Off);

        _heater.Mode = HeaterMode.Schedule;
        _machine.CurrentState.Should().Be(HeaterState.ScheduleIdle);

        _heater.Mode = HeaterMode.Override;
        _machine.CurrentState.Should().Be(HeaterState.OverrideOn);

        _heater.Mode = HeaterMode.Off;
        _machine.CurrentState.Should().Be(HeaterState.Off);
    }

    [Theory]
    [InlineData(HeaterMode.Off)]
    [InlineData(HeaterMode.Schedule)]
    public async Task OverrideEndRestoresPreviousState(HeaterMode initialState)
    {
        _heater.Mode = initialState;
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(100);
        _heater.Mode = HeaterMode.Override;
        _machine.CurrentState.Should().Be(HeaterState.OverrideOn);
        await Task.Delay(200);
        _heater.Mode.Should().Be(initialState);
    }

    [Fact]
    public async Task ChangeOverrideDurationDuringOverride()
    {
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(200);
        _heater.Mode = HeaterMode.Override;
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(100);
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Off);

        _heater.OverrideDuration = TimeSpan.FromMilliseconds(200);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(100);
        _heater.Mode = HeaterMode.Override;
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        await Task.Delay(200);
        _heater.Mode = HeaterMode.Override;
    }

    [Fact]
    public void OverrideOnHaltCycling()
    {

    }

    [Fact]
    public void ChangeHeaterDurationsDuringOverride()
    {

    }
}