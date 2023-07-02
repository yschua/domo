using domo.Data;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Xunit;

namespace domo.Tests;

public class HeaterStateMachineTest : IAsyncLifetime
{
    private readonly Heater _heater;
    private readonly HeaterStateMachine _machine;

    public HeaterStateMachineTest()
    {
        _heater = new HeaterFactory().Create(TimeSpan.FromMilliseconds(200));
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(5000);
        var options = Options.Create(new HeaterStateMachineOptions
        {
            TickInterval = TimeSpan.FromMilliseconds(10)
        });
        _machine = new HeaterStateMachine(options, _heater);
    }

    public async Task InitializeAsync()
    {
        await _machine.StartAsync(CancellationToken.None);
        AssertState(HeaterState.Off);
    }

    public async Task DisposeAsync()
    {
        await _machine.StopAsync(CancellationToken.None);
        _machine.Dispose();
    }

    private void AssertState(HeaterState expectedState, string because = "")
    {
        Debug.WriteLine($"[{DateTime.Now:s.fff}] Assert {expectedState}");
        _machine.CurrentState.Should().Be(expectedState, because);
    }

    private async Task AssertStateTimings(params (int Elapsed, HeaterState ExpectedState)[] expectedTimings)
    {
        await Task.WhenAll(expectedTimings.Select(
            // allow tolerance of 100 ms as the timer is not precise
            t => Task.Delay(t.Elapsed + 100).ContinueWith(
                _ => AssertState(t.ExpectedState, $"{t.ExpectedState} expected after {t.Elapsed} ms")
        )));
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
        AssertState(HeaterState.Off);

        _heater.Mode = HeaterMode.Override;
        AssertState(HeaterState.OverrideOn);

        _heater.Mode = HeaterMode.Schedule;
        AssertState(HeaterState.ScheduleIdle);

        _heater.Mode = HeaterMode.Off;
        AssertState(HeaterState.Off);

        _heater.Mode = HeaterMode.Schedule;
        AssertState(HeaterState.ScheduleIdle);

        _heater.Mode = HeaterMode.Override;
        AssertState(HeaterState.OverrideOn);

        _heater.Mode = HeaterMode.Off;
        AssertState(HeaterState.Off);
    }

    [Theory]
    [InlineData(HeaterMode.Off)]
    [InlineData(HeaterMode.Schedule)]
    public async Task OverrideEndRestoresPreviousState(HeaterMode initialState)
    {
        _heater.Mode = initialState;
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(100);
        _heater.Mode = HeaterMode.Override;
        AssertState(HeaterState.OverrideOn);
        await Task.Delay(150);
        _heater.Mode.Should().Be(initialState);
    }

    [Fact]
    public async Task AdjustOverrideDuration()
    {
        // shorten duration
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Override);
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(100);
        await Task.Delay(200);
        _heater.Mode.Should().Be(HeaterMode.Off);

        // extend duration
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(200);
        _heater.Mode = HeaterMode.Override;
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Override);
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(1000);
        await Task.Delay(100);
        _heater.Mode.Should().Be(HeaterMode.Override);
    }

    [Fact]
    public async Task OverrideCycling()
    {
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(600);
        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       200ms   0ms
         * Halt     200ms   200ms
         * On       200ms   400ms
         * Off      -       600ms
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (200, HeaterState.OverrideHalt),
            (400, HeaterState.OverrideOn),
            (600, HeaterState.Off),
        });
    }

    // TODO extend to test both override and schedule mode cycling?

    [Fact]
    public async Task DynamicCycleDuration_OnCycle()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        var durations = _heater.LowLevelSetting.OnCycleDurations;
        durations.Set_ms(500, 300, 100);
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       500ms   0ms
         * Halt     200ms   500ms
         * On       400ms   700ms
         * Halt     200ms   1100ms
         * On       300ms   1300ms
         * Halt     200ms   1600ms
         * On       300ms   1800ms
         * Halt     200ms   2100ms  <- FinalDuration to 500
         * On       400ms   2300ms
         * Halt     200ms   2700ms
         * On       500ms   2900ms
         * Halt     200ms   3400ms
         * On       500ms   3600ms
         * Halt     200ms   4100ms
         */

        var task = AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (500, HeaterState.OverrideHalt),
            (700, HeaterState.OverrideOn),
            (1100, HeaterState.OverrideHalt),
            (1300, HeaterState.OverrideOn),
            (1600, HeaterState.OverrideHalt),
            (1800, HeaterState.OverrideOn),
            (2100, HeaterState.OverrideHalt),
            (2300, HeaterState.OverrideOn),
            (2700, HeaterState.OverrideHalt),
            (2900, HeaterState.OverrideOn),
            (3400, HeaterState.OverrideHalt),
            (3600, HeaterState.OverrideOn),
            (4100, HeaterState.OverrideHalt),
        });

        await Task.Delay(2200);
        durations.FinalDuration = TimeSpan.FromMilliseconds(500);
        await task;
    }

    [Fact]
    public async Task DynamicCycleDuration_HaltCycle()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        var durations = _heater.LowLevelSetting.HaltCycleDurations;
        durations.Set_ms(500, 300, 100);
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       200ms   0ms
         * Halt     500ms   200ms
         * On       200ms   700ms
         * Halt     400ms   900ms
         * On       200ms   1300ms
         * Halt     300ms   1500ms
         * On       200ms   1800ms
         * Halt     300ms   2000ms  <- FinalDuration to 500
         * On       200ms   2300ms
         * Halt     400ms   2500ms
         * On       200ms   2900ms
         * Halt     500ms   3100ms
         * On       200ms   3600ms
         * Halt     500ms   3800ms
         * On       200ms   4300ms
         */

        var task = AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (200, HeaterState.OverrideHalt),
            (700, HeaterState.OverrideOn),
            (900, HeaterState.OverrideHalt),
            (1300, HeaterState.OverrideOn),
            (1500, HeaterState.OverrideHalt),
            (1800, HeaterState.OverrideOn),
            (2000, HeaterState.OverrideHalt),
            (2300, HeaterState.OverrideOn),
            (2500, HeaterState.OverrideHalt),
            (2900, HeaterState.OverrideOn),
            (3100, HeaterState.OverrideHalt),
            (3600, HeaterState.OverrideOn),
            (3800, HeaterState.OverrideHalt),
            (4300, HeaterState.OverrideOn),
        });

        await Task.Delay(2100);
        durations.FinalDuration = TimeSpan.FromMilliseconds(500);
        await task;
    }

    [Fact]
    public async Task DynamicCycleDuration_BothCycles()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.LowLevelSetting.OnCycleDurations.Set_ms(500, 300, 100);
        _heater.LowLevelSetting.HaltCycleDurations.Set_ms(400, 200, 200);
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       500ms   0ms
         * Halt     400ms   500ms
         * On       400ms   900ms
         * Halt     200ms   1300ms
         * On       300ms   1500ms
         * Halt     200ms   1800ms
         * On       300ms   2000ms
         * Halt     200ms   2300ms
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (500, HeaterState.OverrideHalt),
            (900, HeaterState.OverrideOn),
            (1300, HeaterState.OverrideHalt),
            (1500, HeaterState.OverrideOn),
            (1800, HeaterState.OverrideHalt),
            (2000, HeaterState.OverrideOn),
            (2300, HeaterState.OverrideHalt),
        });
    }

    [Fact]
    public async Task AdjustCycleDurationDuringCycle_InitialDuration()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.LowLevelSetting.OnCycleDurations.Set_ms(200, 200, 0);
        _heater.LowLevelSetting.HaltCycleDurations.Set_ms(1_000_000, 200, 0);
        _heater.Mode = HeaterMode.Override;

        // adjust initial on duration has no effect
        _heater.LowLevelSetting.OnCycleDurations.InitialDuration = TimeSpan.FromHours(1);

        /*
         * Cycle    Durati. Elapsed
         * On       200ms   0ms     <- HaltCycle InitialDuration 200ms
         * Halt     200ms   200ms 
         * On       200ms   400ms
         */

        var task = AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (200, HeaterState.OverrideHalt),
            (400, HeaterState.OverrideOn),
        });

        // adjust initial halt duration during first on cycle
        await Task.Delay(100);
        _heater.LowLevelSetting.HaltCycleDurations.InitialDuration = TimeSpan.FromMilliseconds(200);
        await task;
    }

    [Fact]
    public async Task AdjustCycleDurationDuringCycle_DurationChange()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.LowLevelSetting.OnCycleDurations.Set_ms(500, 200, 0);
        _heater.LowLevelSetting.HaltCycleDurations.Set_ms(200, 200, 0);
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       500ms   0ms
         * Halt     200ms   500ms
         * On       500ms   700ms
         * Halt     200ms   1200ms  <- OnCycle DurationChange 300ms
         * On       200ms   1400ms
         * Halt     200ms   1600ms
         */

        var task = AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (500, HeaterState.OverrideHalt),
            (700, HeaterState.OverrideOn),
            (1200, HeaterState.OverrideHalt),
            (1400, HeaterState.OverrideOn),
            (1600, HeaterState.OverrideHalt),
        });

        // adjust change duration
        await Task.Delay(1300);
        _heater.LowLevelSetting.OnCycleDurations.DurationChange = TimeSpan.FromMilliseconds(300);
        await task;
    }

    [Fact]
    public async Task ZeroDurationChange()
    {
        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.LowLevelSetting.OnCycleDurations.Set_ms(200, 100, 0);
        _heater.LowLevelSetting.HaltCycleDurations.Set_ms(200, 300, 0);
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       200ms   0ms
         * Halt     200ms   200ms
         * On       200ms   400ms
         * Halt     200ms   600ms
         * On       200ms   800ms
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (200, HeaterState.OverrideHalt),
            (400, HeaterState.OverrideOn),
            (600, HeaterState.OverrideHalt),
            (800, HeaterState.OverrideOn),
        });
    }

    [Fact]
    public void InvalidDuration()
    {
        Assert.Throws<ArgumentException>(() => _heater.LowLevelSetting.OnCycleDurations.Set_ms(0, 100, 0));
        Assert.Throws<ArgumentException>(() => _heater.LowLevelSetting.OnCycleDurations.Set_ms(100, 0, 0));
    }

    // changing heater level during override

    // changing final duration after settled

    [Fact]
    public async Task ChangeSettingDuringOverride()
    {
        await Task.CompletedTask;
    }

    // larger final duration than initial duration

    // test override clears schedule cycle

    // test schedule clears override cycle
}