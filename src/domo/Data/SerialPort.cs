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
    public void Open(string portName, int baudRate)
    {
        throw new NotImplementedException();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public byte Read()
    {
        throw new NotImplementedException();
    }

    public void Write(byte data)
    {
        throw new NotImplementedException();
    }
}
