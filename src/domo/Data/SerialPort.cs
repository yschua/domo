namespace domo.Data;

public interface ISerialPort : IDisposable
{
    void Open(string portName, int baudRate);
    void Close();
    byte Read();
    void Write(byte data);
}

public class SerialPort : ISerialPort
{
    private readonly System.IO.Ports.SerialPort _port = new();
    private readonly ILogger<SerialPort> _logger;

    public SerialPort(ILogger<SerialPort> logger)
    {
        _logger = logger;
    }

    public void Open(string portName, int baudRate)
    {
        _port.PortName = portName;
        _port.BaudRate = baudRate;
        _port.ReadTimeout = 1000;
        _port.WriteTimeout = 1000;
        _port.Open();
    }

    public void Close()
    {
        _port.Close();
    }

    public void Dispose()
    {
        _port.Dispose();
    }

    public byte Read()
    {
        var data = (byte)_port.ReadByte();
        _logger.LogDebug($"Read: {data}");
        return data;
    }

    public void Write(byte data)
    {
        _logger.LogDebug($"Write: {data}");
        _port.Write(new[] { data }, 0, 1);
    }
}
