using Divergic.Logging.Xunit;
using domo.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace domo.Tests;

public class HeaterStateMachineTest : LoggingTestsBase<HeaterStateMachine>, IAsyncLifetime
{
    private readonly Heater _heater;
    private readonly HeaterStateMachine _machine;

    public HeaterStateMachineTest(ITestOutputHelper output) : base(output, TestLoggingConfig.Current)
    {
        _heater = new HeaterFactory().Create(TimeSpan.FromMilliseconds(200));
        _heater.OverrideDuration = TimeSpan.FromMilliseconds(5000);
        var heaterControlMock = new Mock<IHeaterControl>();
        _machine = new HeaterStateMachine(Logger, _heater, heaterControlMock.Object)
        {
            TickInterval = TimeSpan.FromMilliseconds(10),
            StartMode = HeaterMode.Off,
        };
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
        Logger.LogDebug($"Assert {expectedState}");
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
        await Task.Delay(200);
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

    [Fact]
    public async Task ChangeLevelDuringOverride()
    {
        _heater.LowLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromMilliseconds(200));
        _heater.LowLevelSetting.HaltCycleDurations.SetSingle(TimeSpan.FromMilliseconds(400));

        _heater.HighLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromMilliseconds(400));
        _heater.HighLevelSetting.HaltCycleDurations.SetSingle(TimeSpan.FromMilliseconds(200));

        _heater.OverrideLevel = HeaterLevel.Low;
        _heater.Mode = HeaterMode.Override;

        /*
         * Cycle    Durati. Elapsed
         * On       200ms   0ms     <- HeaterMode to High
         * Halt     200ms   200ms
         * On       400ms   400ms   <- HeaterMode to Low
         * Halt     400ms   800ms
         * On       200ms   1200ms
         */

        var task = Task.WhenAll(
            Task.Delay(100).ContinueWith(_ => _heater.OverrideLevel = HeaterLevel.High),
            Task.Delay(500).ContinueWith(_ => _heater.OverrideLevel = HeaterLevel.Low)
        );

        await AssertStateTimings(new[]
        {
            (0, HeaterState.OverrideOn),
            (200, HeaterState.OverrideHalt),
            (400, HeaterState.OverrideOn),
            (800, HeaterState.OverrideHalt),
            (1200, HeaterState.OverrideOn),
        });

        await task;
    }

    [Fact]
    public async Task ScheduleCycling()
    {
        _heater.Mode = HeaterMode.Schedule;

        _heater.LowLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromMilliseconds(400));
        _heater.LowLevelSetting.HaltCycleDurations.SetSingle(TimeSpan.FromMilliseconds(300));

        _heater.HighLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromMilliseconds(300));
        _heater.HighLevelSetting.HaltCycleDurations.SetSingle(TimeSpan.FromMilliseconds(400));

        var now = DateTime.Now;

         _heater.Schedule.AddEvent(new(
            now + TimeSpan.FromMilliseconds(1300),
            now + TimeSpan.FromMilliseconds(1800),
            HeaterLevel.High));

        _heater.Schedule.AddEvent(new(
            now + TimeSpan.FromMilliseconds(200),
            now + TimeSpan.FromMilliseconds(1100),
            HeaterLevel.Low));

        /*
         * Cycle    Durati. Elapsed
         * Idle     -       0ms
         * On       400ms   200ms   <- Event 2 start
         * Halt     300ms   600ms
         * On       400ms   900ms
         * Idle     -       1100ms  <- Event 2 end
         * On       300ms   1300ms  <- Event 1 start
         * Halt     400ms   1600ms  <- Event 1 end
         * Idle     -       1800ms
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.ScheduleIdle),
            (200, HeaterState.ScheduleOn),
            (600, HeaterState.ScheduleHalt),
            (900, HeaterState.ScheduleOn),
            (1100, HeaterState.ScheduleIdle),
            (1300, HeaterState.ScheduleOn),
            (1600, HeaterState.ScheduleHalt),
            (1800, HeaterState.ScheduleIdle),
        });
    }

    [Fact]
    public async Task ScheduleCycleCanStartsMidway()
    {
        _heater.LowLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromSeconds(1));
        _heater.Mode = HeaterMode.Schedule;
        int stateChangedCount = 0;
        _machine.StateChanged += (_, state) => stateChangedCount++;
        var now = DateTime.Now;
        _heater.Schedule.AddEvent(new(
            now - TimeSpan.FromMilliseconds(100),
            now + TimeSpan.FromMilliseconds(300),
            HeaterLevel.Low));
        await Task.Delay(100);
        stateChangedCount.Should().Be(1);
        AssertState(HeaterState.ScheduleOn);
    }

    [Fact]
    public async Task ScheduleCycleStartsOnScheduleModeChange()
    {
        _heater.LowLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromSeconds(1));
        _heater.Mode = HeaterMode.Off;
        int stateChangedCount = 0;
        _machine.StateChanged += (_, state) => stateChangedCount++;
        var now = DateTime.Now;
        _heater.Schedule.AddEvent(new(
            now - TimeSpan.FromMilliseconds(100),
            now + TimeSpan.FromMilliseconds(1000),
            HeaterLevel.Low));
        await Task.Delay(300);
        stateChangedCount.Should().Be(0);
        _heater.Mode = HeaterMode.Schedule;
        await Task.Delay(300);
        stateChangedCount.Should().Be(2);
        AssertState(HeaterState.ScheduleOn);
        await Task.Delay(1000);
        stateChangedCount.Should().Be(3);
        AssertState(HeaterState.ScheduleIdle);
    }

    [Fact]
    public async Task RemoveScheduleStopsCycle()
    {
        _heater.Mode = HeaterMode.Schedule;
        var now = DateTime.Now;
        _heater.Schedule.AddEvent(new(
            now + TimeSpan.FromMilliseconds(200),
            now + TimeSpan.FromMilliseconds(700),
            HeaterLevel.Low));

        var task = Task.Delay(500).ContinueWith(_ => _heater.Schedule.Events.RemoveAt(0));

        /*
         * Cycle    Durati. Elapsed
         * Idle     -       0ms
         * On       200ms   200ms
         * Halt     200ms   400ms   
         * Idle     -       500ms   <- Delete event
         * On
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.ScheduleIdle),
            (200, HeaterState.ScheduleOn),
            (400, HeaterState.ScheduleHalt),
            (500, HeaterState.ScheduleIdle),
        });

        await task;
    }

    [Fact]
    public async Task OverrideAndRestoreSchedule()
    {
         _heater.Mode = HeaterMode.Schedule;
        _heater.LowLevelSetting.OnCycleDurations.SetSingle(TimeSpan.FromMilliseconds(400));

        var now = DateTime.Now;
        _heater.Schedule.AddEvent(new(
            now + TimeSpan.FromMilliseconds(200),
            now + TimeSpan.FromMilliseconds(800),
            HeaterLevel.Low));

        _heater.Schedule.AddEvent(new(
            now + TimeSpan.FromMilliseconds(1000),
            now + TimeSpan.FromMilliseconds(1500),
            HeaterLevel.Low));

        var task = Task.Delay(500).ContinueWith(_ =>
        {
            _heater.Mode = HeaterMode.Override;
            _heater.OverrideDuration = TimeSpan.FromMilliseconds(300);
        });

        /*
         * Cycle    Durati. Elapsed
         * Idle     -       0ms
         * On       400ms   200ms
         * On       400ms   500ms   <- Override start
         * Idle     -       800ms   <- Override restore
         * On       400ms   1000ms
         */

        await AssertStateTimings(new[]
        {
            (0, HeaterState.ScheduleIdle),
            (200, HeaterState.ScheduleOn),
            (500, HeaterState.OverrideOn),
            (800, HeaterState.ScheduleIdle),
            (1000, HeaterState.ScheduleOn),
        });

        await task;
    }
}