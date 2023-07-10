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

    public HeaterControl(ILogger<HeaterControl> logger,
        ISerialPort serialPort, Heater heater)
    {
        _logger = logger;
        _serialPort = serialPort;
        _heater = heater;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _serialPort.Open("COM5", 115200);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _serialPort.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _serialPort.Dispose();
    }

    public void TurnOn()
    {
        _heater.IsActivated = true;
    }

    public void TurnOff()
    {
        _heater.IsActivated = false;
    }
}
