using Stateless;
using System.Data;
using System.Timers;

namespace domo.Data;

public enum HeaterState
{
    Off,
    OverrideOn,
    OverrideHalt,
    ScheduleIdle,
    ScheduleOn,
    ScheduleHalt,
}

public enum HeaterTrigger
{
    Off,
    Override,
    Schedule,
    Idle,
    On,
    Halt,
}

public class HeaterStateMachine : IDisposable, IHostedService
{
    private readonly StateMachine<HeaterState, HeaterTrigger> _machine;
    private readonly Heater _heater;
    private readonly ILogger _logger;
    private readonly IHeaterControl _heaterControl;
    private readonly System.Timers.Timer _timer = new();
    private readonly object _lock = new();
    private DateTime _cycleStart;
    private int _cycleNumber;
    private TimeSpan _onDuration;
    private TimeSpan _haltDuration;

    public HeaterStateMachine(ILogger<HeaterStateMachine> logger, Heater heater, IHeaterControl heaterControl)
    {
        _machine = new(HeaterState.Off);
        _heater = heater;
        _logger = logger;
        _heaterControl = heaterControl;

        _machine.OnTransitioned(OnTransition);

        _machine.Configure(HeaterState.Off)
            .OnEntry(t => DeactivateHeater())
            .Ignore(HeaterTrigger.Off)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            ;

        _machine.Configure(HeaterState.OverrideOn)
            .OnEntry(t => ActivateHeater())
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Halt, HeaterState.OverrideHalt)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            ;

        _machine.Configure(HeaterState.OverrideHalt)
            .OnEntry(t => DeactivateHeater())
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.On, HeaterState.OverrideOn)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            ;

        _machine.Configure(HeaterState.ScheduleIdle)
            .OnEntry(t => DeactivateHeater())
            .Ignore(HeaterTrigger.Idle)
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            .Permit(HeaterTrigger.On, HeaterState.ScheduleOn)
            ;

        _machine.Configure(HeaterState.ScheduleOn)
            .OnEntry(t => ActivateHeater())
            .Ignore(HeaterTrigger.On)
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            .Permit(HeaterTrigger.Halt, HeaterState.ScheduleHalt)
            .Permit(HeaterTrigger.Idle, HeaterState.ScheduleIdle)
            ;

        _machine.Configure(HeaterState.ScheduleHalt)
            .OnEntry(t => DeactivateHeater())
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            .Permit(HeaterTrigger.On, HeaterState.ScheduleOn)
            .Permit(HeaterTrigger.Idle, HeaterState.ScheduleIdle)
            ;

        _heater.HeaterModeChanged += (_, mode) => ChangeHeaterMode(mode);
        _timer.Elapsed += TimerTick;
    }

    public event EventHandler<HeaterState>? StateChanged;

    public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(1);

    public void Dispose()
    {
        _timer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Interval = TickInterval.TotalMilliseconds;
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Stop();
        return Task.CompletedTask;
    }

    public HeaterState CurrentState => _machine.State;

    private void OnTransition(StateMachine<HeaterState, HeaterTrigger>.Transition transition)
    {
        StateChanged?.Invoke(this, transition.Destination);
        _logger.LogInformation($"Trigger - {transition.Trigger}: {transition.Source} -> {transition.Destination}");
    }

    private void ChangeHeaterMode(HeaterMode mode)
    {
        lock (_lock)
        {
            if (mode == HeaterMode.Override)
            {
                _heater.OverrideStart = DateTime.Now;
                _heater.CurrentLevel = _heater.OverrideLevel;
                UpdateCycleDuration(resetCycle: true, isOnCycle: true);
            }

            _machine.Fire(mode switch
            {
                HeaterMode.Off => HeaterTrigger.Off,
                HeaterMode.Override => HeaterTrigger.Override,
                HeaterMode.Schedule => HeaterTrigger.Schedule,
            });
        }
    }

    private void TimerTick(object? source, ElapsedEventArgs e)
    {
        lock (_lock)
        {
            try
            {
                if (_heater.Mode == HeaterMode.Override)
                {
                    // when override ends
                    if (DateTime.Now > (_heater.OverrideStart + _heater.OverrideDuration))
                    {
                        _heater.Mode = _heater.PreviousMode;
                        _logger.LogInformation("Override ended");
                    }
                }

                if (_heater.Mode == HeaterMode.Schedule)
                {
                    var now = TimeOnly.FromDateTime(DateTime.Now);
                    var events = _heater.Schedule.Events;

                    bool InRange(TimeOnly time) => now >= time && now <= time.Add(TickInterval * 2);
                    var startEvents = events.Where(e => InRange(e.StartTime));

                    if (startEvents.Count() > 0)
                    {
                        _heater.CurrentLevel = startEvents.First().Level;
                        UpdateCycleDuration(resetCycle: true, isOnCycle: true);
                        TimerHeaterOn();
                    }

                    if (events.All(e => now < e.StartTime || now > e.EndTime))
                    {
                        TimerHeaterIdle();
                    }
                }

                if (CurrentState == HeaterState.OverrideHalt || CurrentState == HeaterState.ScheduleHalt)
                {
                    if (DateTime.Now > (_cycleStart + _haltDuration))
                    {
                        TimerHeaterOn();
                        UpdateCycleDuration(resetCycle: false, isOnCycle: true);
                    }
                }
                else if (CurrentState == HeaterState.OverrideOn || CurrentState == HeaterState.ScheduleOn)
                {
                    if (DateTime.Now > (_cycleStart + _onDuration))
                    {
                        TimerHeaterHalt();
                        UpdateCycleDuration(resetCycle: false, isOnCycle: false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }
    }

    private void UpdateCycleDuration(bool resetCycle, bool isOnCycle)
    {
        _cycleStart = DateTime.Now;

        if (resetCycle)
        {
            _cycleNumber = 0;
        }

        if (_cycleNumber < 2)
        {
            if (isOnCycle)
            {
                _onDuration = _heater.CurrentSetting.OnCycleDurations.InitialDuration;
                _logger.LogInformation($"OnDuration Begin -> {_onDuration}");
            }
            else
            {
                _haltDuration = _heater.CurrentSetting.HaltCycleDurations.InitialDuration;
                _logger.LogInformation($"HaltDuration Begin -> {_haltDuration}");
            }
        }
        else
        {
            void UpdateDuration(ref TimeSpan duration, HeaterDurations durationSetting)
            {
                if (duration < durationSetting.FinalDuration)
                {
                    duration = Helper.Min(durationSetting.FinalDuration, duration + durationSetting.DurationChange);
                }
                else if (duration > durationSetting.FinalDuration)
                {
                    duration = Helper.Max(durationSetting.FinalDuration, duration - durationSetting.DurationChange);
                }
            }

            if (isOnCycle)
            {
                var duration = _onDuration;
                UpdateDuration(ref duration, _heater.CurrentSetting.OnCycleDurations);
                _logger.LogInformation($"OnDuration {_onDuration} -> {duration}");
                _onDuration = duration;
            }
            else
            {
                var duration = _haltDuration;
                UpdateDuration(ref duration, _heater.CurrentSetting.HaltCycleDurations);
                _logger.LogInformation($"HaltDuration {_haltDuration} -> {duration}");
                _haltDuration = duration;
            }
        }

        _cycleNumber++;
    }

    private void TimerHeaterOn()
    {
        _machine.Fire(HeaterTrigger.On);
    }

    private void TimerHeaterHalt()
    {
        _machine.Fire(HeaterTrigger.Halt);
    }

    private void TimerHeaterIdle()
    {
        _machine.Fire(HeaterTrigger.Idle);
    }

    private void DeactivateHeater()
    {
        _heaterControl.TurnOff();
    }

    private void ActivateHeater()
    {
        _heaterControl.TurnOn();
    }
}
