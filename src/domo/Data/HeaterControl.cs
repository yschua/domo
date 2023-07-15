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
    private State _actualState;
    private State _targetState;
    private State _pendingState;

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

                if (_targetState != _pendingState)
                {
                    _serialPort.Write((byte)Request.Toggle);
                    _pendingState = _targetState;
                    if (_serialPort.Read() != (byte)Response.NoChange)
                    {
                        throw new InvalidOperationException("");
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

    public TimeSpan QueryInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    public void TurnOn()
    {
        lock (_timer)
        {
            _logger.LogDebug($"{nameof(TurnOn)}");
            _targetState = State.On;
        }
    }

    public void TurnOff()
    {
        lock (_timer)
        {
            _logger.LogDebug($"{nameof(TurnOff)}");
            _targetState = State.Off;
        }
    }
}
