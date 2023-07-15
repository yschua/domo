using Divergic.Logging.Xunit;
using domo.Data;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace domo.Tests;

public class HeaterControlTest : LoggingTestsBase<HeaterControl>, IAsyncLifetime
{
    private readonly HeaterControl _heaterControl;
    private readonly EmulatedHeaterControlGateway _gateway;
    private readonly Heater _heater;
    private int _activatedCount = 0;
    
    public HeaterControlTest(ITestOutputHelper output) : base(output, TestLoggingConfig.Current)
    {
        _heater = new HeaterFactory().Create();
        _gateway = new(Logger) { ConfirmInterval = TimeSpan.FromMilliseconds(100) };
        _heaterControl = new(Logger, _gateway, _heater) { QueryInterval = TimeSpan.FromMilliseconds(25) };

        _heater.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Heater.Activated))
            {
                _activatedCount++;
            }
        };
    }

    public async Task DisposeAsync()
    {
        await _heaterControl.StopAsync(default);
        await _gateway.StopAsync(default);
        _heaterControl.Dispose();
        _gateway.Dispose();
    }

    public async Task InitializeAsync()
    {
        await _gateway.StartAsync(default);
        await _heaterControl.StartAsync(default);
    }

    [Fact]
    public async Task TurnOnOff()
    {
        _heaterControl.TurnOn();
        _heater.Activated.Should().BeFalse();
        await Task.Delay(200);
        _heater.Activated.Should().BeTrue();
        _activatedCount.Should().Be(1);
        _heaterControl.TurnOff();
        _heater.Activated.Should().BeTrue();
        await Task.Delay(200);
        _heater.Activated.Should().BeFalse();
        _activatedCount.Should().Be(2);
    }

    //  (actual = 0, target = 0)
    // turn on case
    // send toggle request - intention to toggle
    //  (actual = 0, target = 1)
    // respond no change confirm
    //  (actual = 0, target = 1)
    // radio toggle
    // send status request
    //  (actual = 0, target = 1)
    // respond toggle confirm
    //  (actual = 1, target = 1)
    // send status request
    // respond no change confirm

    //  (actual = 1, target = 1)
    // turn off case
    // send toggle request - intention to toggle
    //  (actual = 1, target = 0)
    // respond no change confirm
    //  (actual = 1, target = 0)
    // radio toggle
    // send status request
    //  (actual = 1, target = 0)
    // respond toggle confirm
    //  (actual = 0, target = 0)
    // send status request
    // respond no change confirm

    // idle case
    // send status request
    // respond no change confirm

    //  (actual = 0, target = 0)
    // edge case 1
    // send toggle request - intention to toggle
    //  (actual = 0, target = 1)
    // respond no change confirm
    //  (actual = 0, target = 1)
    // radio toggle
    // send toggle request - intention to cancel
    //  (actual = 0, target = 0)
    // respond toggle confirm
    //  (actual = 1, target = 0)

    //  (actual = 1, target = 1)
    // edge case 2
    // send toggle request - intention to cancel
    //  (actual = 1, target = 0)
    // respond no change confirm
    //  (actual = 1, target = 0)
    // radio toggle
    // send toggle request - intention to toggle
    //  (actual = 1, target = 1)
    // respond toggle confirm
    //  (actual = 0, target = 1)
}
