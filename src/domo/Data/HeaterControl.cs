using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace domo.Data;

public interface IHeaterControl
{
    void TurnOn();
    void TurnOff();
}

public class HeaterControlOptions
{
    public string PortNameWindows { get; init; }
    public string PortNameLinux { get; init; }
    public bool EmulatedGateway { get; init; }
}

public class HeaterControl : IHeaterControl, IDisposable, IHostedService
{
    private readonly IOptions<HeaterControlOptions> _options;
    private ILogger<HeaterControl> _logger;
    private readonly ISerialPort _serialPort;
    private readonly Heater _heater;
    private readonly System.Timers.Timer _timer;
    private bool _toggleRequested;
    private State _actualState = State.Off;
    private State _targetState = State.Off;
    private State _pendingState = State.Off;
    private DateTime _lastCommandTime;

    public HeaterControl(IOptions<HeaterControlOptions> options, ILogger<HeaterControl> logger,
        ISerialPort serialPort, Heater heater)
    {
        _options = options;
        _logger = logger;
        _serialPort = serialPort;
        _heater = heater;
        _timer = new System.Timers.Timer();
        _timer.Elapsed += ControlLoop;
    }

    public enum Request : byte { Status, Toggle }
    public enum Response : byte { NoChange, ToggleConfirm }
    private enum State { Off, On }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var portName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? _options.Value.PortNameWindows
            : _options.Value.PortNameLinux;
        _serialPort.Open(portName, 115200);
        _timer.Interval = QueryInterval.TotalMilliseconds;
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

    private void ControlLoop(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_timer)
        {
            _serialPort.Write((byte)Request.Status);
            ReadAndProcessResponse();

            if (_targetState != _pendingState && DateTime.Now > _lastCommandTime + SettleDuration)
            {
                _serialPort.Write((byte)Request.Toggle);
                ReadAndProcessResponse();
                _pendingState = _targetState;
            }
        }
    }

    private void ReadAndProcessResponse()
    {
        if (_serialPort.Read() == (byte)Response.ToggleConfirm)
        {
            _actualState = _pendingState;
            _logger.LogInformation($"Heater activation: {_heater.Activated} -> {_actualState == State.On}");
            _heater.Activated = _actualState switch
            {
                State.On => true,
                State.Off => false
            };
        }
    }
}
