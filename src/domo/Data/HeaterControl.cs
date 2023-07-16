namespace domo.Data;

public interface IHeaterControl
{
    void TurnOn();
    void TurnOff();
}

public class HeaterControl : IHeaterControl, IDisposable, IHostedService
{
    private ILogger<HeaterControl> _logger;
    private readonly ISerialPort _serialPort;
    private readonly Heater _heater;
    private readonly System.Timers.Timer _timer;
    private bool _toggleRequested;
    private State _actualState = State.Off;
    private State _targetState = State.Off;
    private State _pendingState = State.Off;
    private DateTime _lastCommandTime;

    public HeaterControl(ILogger<HeaterControl> logger,
        ISerialPort serialPort, Heater heater)
    {
        _logger = logger;
        _serialPort = serialPort;
        _heater = heater;
        _timer = new System.Timers.Timer();
    }

    public enum Request : byte { Status, Toggle }
    public enum Response : byte { NoChange, ToggleConfirm }
    private enum State { Off, On }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _serialPort.Open("COM5", 115200);
        _timer.Interval = QueryInterval.TotalMilliseconds;
        _timer.Elapsed += (_, _) =>
        {
            lock (_timer)
            {
                _serialPort.Write((byte)Request.Status);

                if (_serialPort.Read() == (byte)Response.ToggleConfirm)
                {
                    _actualState = _pendingState;
                    _heater.Activated = _actualState switch
                    {
                        State.On => true,
                        State.Off => false
                    };
                }

                if (_targetState != _pendingState && DateTime.Now > _lastCommandTime + SettleDuration)
                {
                    _serialPort.Write((byte)Request.Toggle);
                    _pendingState = _targetState;
                    if (_serialPort.Read() != (byte)Response.NoChange)
                    {
                        throw new InvalidOperationException(
                            "Unxpected response immediately after toggle request");
                    }
                }
            }
        };
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Stop();
        _serialPort.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer.Dispose();
        _serialPort.Dispose();
    }

    public TimeSpan QueryInterval { get; init; } = TimeSpan.FromSeconds(1);

    public TimeSpan SettleDuration { get; init; } = TimeSpan.FromSeconds(20);

    public void TurnOn()
    {
        lock (_timer)
        {
            _logger.LogDebug($"{nameof(TurnOn)}");
            _targetState = State.On;
            _lastCommandTime = DateTime.Now;
        }
    }

    public void TurnOff()
    {
        lock (_timer)
        {
            _logger.LogDebug($"{nameof(TurnOff)}");
            _targetState = State.Off;
            _lastCommandTime = DateTime.Now;
        }
    }
}
