using Microsoft.Extensions.Options;
using Stateless;
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

public interface IHeaterControl
{
    bool IsActive { get; }
    void Activate();
    void Deactivate();
}

public class HeaterStateMachine : IDisposable
{
    private readonly StateMachine<HeaterState, HeaterTrigger> _machine;
    private readonly Heater _heater;
    private Action? _nextCycleTrigger;
    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new object();

    public HeaterStateMachine(IOptions<HeaterStateMachineOptions> options, Heater heater)
    {
        _machine = new(HeaterState.Off);
        _heater = heater;

        // _machine.OnTransitioned(); TODO logging

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
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public HeaterState CurrentState => _machine.State;

    private void ChangeHeaterMode(HeaterMode mode)
    {
        lock (_lock)
        {
            // user change invalidates current timers?
            _machine.Fire(mode switch
            {
                HeaterMode.Off => HeaterTrigger.Off,
                HeaterMode.Override => HeaterTrigger.Override,
                HeaterMode.Schedule => HeaterTrigger.Schedule,
            });

            if (mode == HeaterMode.Override)
            {
                _heater.OverrideStart = DateTime.Now;
            }
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
        lock (_lock)
        {
            if (_heater.Mode == HeaterMode.Override)
            {
                // when override ends
                if (DateTime.Now > (_heater.OverrideStart + _heater.OverrideDuration))
                {
                    _heater.Mode = _heater.PreviousMode;

                    //_nextCycleTrigger = null;
                    //UserChangedHeaterMode(HeaterMode.Off);
                    // need to restore previous mode
                }
            }


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
    }

    private void ActivateHeater(Action? nextCycleTrigger = null)
    {
        //_heater.CurrentCycleStart = DateTime.Now;
        //_nextCycleTrigger = nextCycleTrigger;
    }
}
