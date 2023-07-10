using Divergic.Logging.Xunit;
using domo.Data;
using Moq;
using Xunit.Abstractions;

namespace domo.Tests;

public class HeaterControlTest : LoggingTestsBase<HeaterControl>
{
    private readonly IHeaterControl _heaterControl;
    private readonly Heater _heater;
    private readonly Mock<ISerialPort> _serialPortMock = new();
    
    public HeaterControlTest(ITestOutputHelper output) : base(output, TestLoggingConfig.Current)
    {
        _heater = new HeaterFactory().Create();
        _heaterControl = new HeaterControl(Logger, _serialPortMock.Object, _heater);
    }
}
