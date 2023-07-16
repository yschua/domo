namespace domo.Data;

public class EmulatedHeaterControlGateway : ISerialPort, IHostedService
{
    private ILogger _logger;
    private readonly System.Timers.Timer _timer;
    private readonly AutoResetEvent _dataReadyToSendEvent = new(false);
    private readonly AutoResetEvent _dataReceivedEvent = new(false);
    private byte _dataReceived;
    private byte _dataToSend;
    private bool _toggleRequested;
    private bool _toggleConfirmed;
    private DateTime _lastConfirmTime;

    public EmulatedHeaterControlGateway(ILogger<EmulatedHeaterControlGateway> logger)
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
        _dataReadyToSendEvent.WaitOne();
        _logger.LogDebug($"Gateway sent: {_dataToSend}");
        return _dataToSend;
    }

    public void Write(byte data)
    {
        _dataReceived = data;
        _logger.LogDebug($"Gateway received: {_dataReceived}");
        _dataReceivedEvent.Set();
    }

    public TimeSpan ConfirmInterval { get; init; } = TimeSpan.FromSeconds(60);

    // emulates HeaterControlGateway.ino loop()
    private void GatewayLoop(object? sender, System.Timers.ElapsedEventArgs e)
    {
        lock (_timer)
        {
            var dataAvailable = _dataReceivedEvent.WaitOne(1);
            _logger.LogDebug($"Gateway tick, dataAvailable: {dataAvailable}");

            if (dataAvailable)
            {
                if (_dataReceived == (byte)HeaterControl.Request.Toggle)
                {
                    _logger.LogInformation("Gateway toggle requested");
                    _toggleRequested = !_toggleRequested;
                }

                if (_toggleConfirmed)
                {
                    _dataToSend = (byte)HeaterControl.Response.ToggleConfirm;
                    _toggleConfirmed = false;
                }
                else
                {
                    _dataToSend = (byte)HeaterControl.Response.NoChange;
                }

                _dataReadyToSendEvent.Set();
            }

            if (DateTime.Now > _lastConfirmTime + ConfirmInterval)
            {
                if (_toggleRequested)
                {
                    _logger.LogInformation("Gateway toggle confirmed");
                    _toggleConfirmed = true;
                    _toggleRequested = false;
                }

                _lastConfirmTime = DateTime.Now;
            }
        }
    }
}