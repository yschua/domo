using Divergic.Logging.Xunit;
using domo.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        _gateway = new(output.BuildLoggerFor<EmulatedHeaterControlGateway>())
        { 
            ConfirmInterval = TimeSpan.Zero
        };
        var options = Options.Create(new HeaterControlOptions());
        _heaterControl = new(options, Logger, _gateway, _heater)
        {
            QueryInterval = TimeSpan.FromMilliseconds(40),
            SettleDuration = TimeSpan.FromMilliseconds(200),
        };

        _heater.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Heater.Activated))
            {
                Logger.LogDebug($"Heater activated: {_heater.Activated}");
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

    private void AssertActivated(bool activated, int expectedToggleCount)
    {
        Logger.LogDebug($"Assert Activated: {activated}");
        _heater.Activated.Should().Be(activated);
        _activatedCount.Should().Be(expectedToggleCount);
        _activatedCount = 0;
    }

    [Fact]
    public async Task TurnOnOff()
    {
        _heaterControl.TurnOn();
        AssertActivated(false, 0);
        await Task.Delay(300);
        AssertActivated(true, 1);
        _heaterControl.TurnOff();
        AssertActivated(true, 0);
        await Task.Delay(300);
        AssertActivated(false, 1);
    }

    [Fact]
    public async Task ActivateOnlyAfterSettling()
    {
        _heaterControl.TurnOn();
        await Task.Delay(100);
        _heaterControl.TurnOff();
        await Task.Delay(100);
        _heaterControl.TurnOn();
        await Task.Delay(300);
        AssertActivated(true, 1);
    }

    [Fact]
    public async Task NoActivateWhenNoChange()
    {
        _heaterControl.TurnOn();
        await Task.Delay(100);
        _heaterControl.TurnOff();
        await Task.Delay(100);
        _heaterControl.TurnOn();
        await Task.Delay(100);
        _heaterControl.TurnOff();
        await Task.Delay(300);
        AssertActivated(false, 0);
    }
}