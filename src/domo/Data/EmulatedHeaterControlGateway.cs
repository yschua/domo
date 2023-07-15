namespace domo.Data;

public class EmulatedHeaterControlGateway : ISerialPort, IHostedService
{
    private ILogger _logger;
    private readonly System.Timers.Timer _timer;
    private readonly AutoResetEvent _readEvent = new(false);
    private readonly AutoResetEvent _writeEvent = new(true);
    private byte _writeData;
    private byte _readData;
    private bool _toggleRequested;
    private bool _toggleConfirmed;
    private DateTime _lastConfirmTime;

    public EmulatedHeaterControlGateway(ILogger logger)
    {
        _logger = logger;
        _timer = new System.Timers.Timer(TimeSpan.FromMilliseconds(10));
        _timer.Elapsed += GatewayLoop;
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

    public void Open(string portName, int baudRate)
    {
    }

    public void Close()
    {
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    public byte Read()
    {
        _readEvent.WaitOne();
        _logger.LogDebug($"Read: {_readData}");
        return _readData;
    }

    public void Write(byte data)
    {
        _writeData = data;
        _logger.LogDebug($"Write: {_writeData}");
        _writeEvent.Set();
    }

    public TimeSpan ConfirmInterval { get; init; } = TimeSpan.FromMilliseconds(100);

    // emulates HeaterControlGateway.ino loop()
    private void GatewayLoop(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_writeEvent.WaitOne(1))
        {
            if (_writeData == (byte)HeaterControl.Request.Toggle)
            {
                _toggleRequested = !_toggleRequested;
            }

            if (_toggleConfirmed)
            {
                _readData = (byte)HeaterControl.Response.ToggleConfirm;
                _toggleConfirmed = false;
            }
            else
            {
                _readData = (byte)HeaterControl.Response.NoChange;
            }

            _readEvent.Set();
        }

        if (DateTime.Now > _lastConfirmTime + ConfirmInterval)
        {
            if (_toggleRequested)
            {
                _toggleConfirmed = true;
                _toggleRequested = false;
                _lastConfirmTime = DateTime.Now;
            }
        }
    }
}