using domo.Data;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Xunit;

namespace domo.Tests;

public class HeaterStateMachineTest
{
    private readonly Heater _heater;
    private readonly HeaterStateMachine _machine;

    public HeaterStateMachineTest()
    {
        _heater = new HeaterFactory().Create(TimeSpan.FromMilliseconds(200));
        var options = Options.Create(new HeaterStateMachineOptions
        {
            TickInterval = TimeSpan.FromMilliseconds(50)
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
    public async Task ChangeDurationDuringOverride()
    {
        // shorten duration
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(200);
        _heater.Mode.Should().Be(HeaterMode.Override);
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(100);
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Off);

        // extend duration
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(200);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Override);
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        await Task.Delay(200);
        _heater.Mode.Should().Be(HeaterMode.Override);
    }

    [Fact]
    public async void OverrideCycling()
    {
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(600);
        _heater.OverrideLevel = HeaterLevel.Low;

        _heater.Mode = HeaterMode.Override;

        Debug.WriteLine($"[{DateTime.Now:s.fff}] Check");
        _machine.CurrentState.Should().Be(HeaterState.OverrideOn);
        await Task.Delay(300);
        Debug.WriteLine($"[{DateTime.Now:s.fff}] Check");
        _machine.CurrentState.Should().Be(HeaterState.OverrideHalt);
        await Task.Delay(200);
        Debug.WriteLine($"[{DateTime.Now:s.fff}] Check");
        _machine.CurrentState.Should().Be(HeaterState.OverrideOn);
        await Task.Delay(200);
        Debug.WriteLine($"[{DateTime.Now:s.fff}] Check");
        _machine.CurrentState.Should().Be(HeaterState.Off);
    }

    [Fact]
    public void ChangeHeaterDurationsDuringOverride()
    {

    }

    // test override clears schedule cycle

    // test schedule clears override cycle
}