using Microsoft.Extensions.Options;
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

public class HeaterStateMachineOptions
{
    public TimeSpan TickInterval { get; init; } = TimeSpan.FromSeconds(1);
}

public class HeaterStateMachine : IDisposable, IHostedService
{
    private readonly StateMachine<HeaterState, HeaterTrigger> _machine;
    private readonly Heater _heater;
    private readonly HeaterStateMachineOptions _options;
    private readonly ILogger _logger;
    private readonly IHeaterControl _heaterControl;
    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new();

    public HeaterStateMachine(IOptions<HeaterStateMachineOptions> options,
        ILogger<HeaterStateMachine> logger, Heater heater, IHeaterControl heaterControl)
    {
        _machine = new(HeaterState.Off);
        _heater = heater;
        _options = options.Value;
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

        _timer = new System.Timers.Timer(_options.TickInterval);
        _timer.Elapsed += TimerTick;
    }

    public event EventHandler<HeaterState>? StateChanged;

    public void Dispose()
    {
        _timer.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
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
                _heater.StartNextCycle(resetCycle: true);
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
            if (_heater.Mode == HeaterMode.Override)
            {
                // when override ends
                if (DateTime.Now > (_heater.OverrideStart + _heater.OverrideDuration))
                {
                    _heater.Mode = _heater.PreviousMode;
                }
            }
            
            if (_heater.Mode == HeaterMode.Schedule)
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                var events = _heater.Schedule.Events;

                bool InRange(TimeOnly time) => now >= time && now <= time.Add(_options.TickInterval * 2);
                var startEvents = events.Where(e => InRange(e.StartTime));

                if (startEvents.Count() > 0)
                {
                    _heater.CurrentLevel = startEvents.First().Level;
                    _heater.StartNextCycle(resetCycle: true);
                    TimerHeaterOn();
                }

                if (events.All(e => now < e.StartTime || now > e.EndTime))
                {
                    TimerHeaterIdle();
                }
            }

            TimeSpan? cycleDuration = CurrentState switch
            {
                HeaterState.OverrideHalt => _heater.HaltDuration,
                HeaterState.ScheduleHalt => _heater.HaltDuration,
                HeaterState.OverrideOn => _heater.OnDuration,
                HeaterState.ScheduleOn => _heater.OnDuration,
                _ => null
            };

            if (cycleDuration != null)
            {
                // when cycle ends
                if (DateTime.Now > (_heater.CycleStart + cycleDuration))
                {
                    switch (CurrentState)
                    {
                        case HeaterState.OverrideHalt:
                        case HeaterState.ScheduleHalt:
                            TimerHeaterOn();
                            break;
                        case HeaterState.OverrideOn:
                        case HeaterState.ScheduleOn:
                            TimerHeaterHalt();
                            break;
                        default:
                            throw new InvalidOperationException(
                                $"Cycle duration end in invalid state: {CurrentState}");
                    }

                    _heater.StartNextCycle(resetCycle: false);
                }
            }
        }
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
