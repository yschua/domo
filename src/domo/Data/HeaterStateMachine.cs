using Microsoft.Extensions.Options;
using Stateless;
using System.Diagnostics;
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
    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new object();

    public HeaterStateMachine(IOptions<HeaterStateMachineOptions> options, Heater heater)
    {
        _machine = new(HeaterState.Off);
        _heater = heater;

        _machine.OnTransitioned(OnTransition);

        _machine.Configure(HeaterState.Off)
            .OnEntry(t => DeactivateHeater())
            .Ignore(HeaterTrigger.Off)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            ;

        _machine.Configure(HeaterState.OverrideOn)
            .OnEntry(t => ActivateHeater(TimerHeaterHalt))
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Halt, HeaterState.OverrideHalt)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            ;

        _machine.Configure(HeaterState.OverrideHalt)
            .OnEntry(t => DeactivateHeater(TimerHeaterOn))
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.On, HeaterState.OverrideOn)
            .Permit(HeaterTrigger.Schedule, HeaterState.ScheduleIdle)
            ;

        _machine.Configure(HeaterState.ScheduleIdle)
            .OnEntry(t => DeactivateHeater())
            .Permit(HeaterTrigger.Off, HeaterState.Off)
            .Permit(HeaterTrigger.Override, HeaterState.OverrideOn)
            ;

        _heater.HeaterModeChanged += (_, mode) => ChangeHeaterMode(mode);

        //_heater.HeaterModeChanged += (_, mode) =>
        //{
        //    if (mode == HeaterMode.Off)
        //    {
        //        SetModeOff();
        //    }
        //    else if (mode == HeaterMode.Override)
        //    {
        //        SetModeOverride();
        //    }
        //};

        _timer = new System.Timers.Timer(options.Value.TickInterval);
        _timer.Elapsed += TimerTick;
    }

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
        Debug.WriteLine($"[{DateTime.Now:s.fff}] {transition.Trigger}: {transition.Source} -> {transition.Destination}");
    }

    private void ChangeHeaterMode(HeaterMode mode)
    {
        lock (_lock)
        {
            // user change invalidates current timers?


            if (mode == HeaterMode.Override)
            {
                _heater.OverrideStart = DateTime.Now;
                _heater.SetCurrentSetting(_heater.OverrideLevel);
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

    //private void SetModeOff()
    //{
    //    _machine.Fire(HeaterTrigger.Off);
    //}

    //private void SetModeOverride()
    //{
    //    _machine.Fire(HeaterTrigger.Override);
    //    _heater.OverrideStart = DateTime.Now;
    //}

    private void TimerTick(object? source, ElapsedEventArgs e)
    {
        Debug.WriteLine($"[{DateTime.Now:s.fff}] Tick");

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

            //if (_nextCycleTrigger != null)
            //{
            //    TimeSpan cycleDuration;
            //    if (_heater.IsHalted)
            //    {
            //        cycleDuration = _h
            //    }

            //}


            // when schedule ends

            // when cycle ends
        }
    }

    //public void TimerHeaterOff()
    //{
    //    _machine.Fire(HeaterTrigger.Off);
    //}

    public void TimerHeaterOn()
    {
        _machine.Fire(HeaterTrigger.On);
    }

    public void TimerHeaterHalt()
    {
        _machine.Fire(HeaterTrigger.Halt);
    }

    public void TimerHeaterIdle()
    {
        _machine.Fire(HeaterTrigger.Idle);
    }

    private void DeactivateHeater(Action? nextCycleTrigger = null)
    {
        _heater.Deactivate();
    }

    private void ActivateHeater(Action? nextCycleTrigger = null)
    {
        _heater.Activate();
    }
}
