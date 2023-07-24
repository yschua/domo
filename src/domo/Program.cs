using domo.Data;
using LiteDB;
using MudBlazor.Services;
using Serilog;
using System.Runtime.InteropServices;

var AppDataDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData");
var AppDir = Path.Combine(AppDataDir, "domo");
Directory.CreateDirectory(AppDir);

const string LogOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

LoggerConfiguration CreateDefaultLoggerConfiguration()
{
    return new LoggerConfiguration()
        .MinimumLevel.Information()
        .WriteTo.File(
            path: Path.Combine(AppDir, "logs", ".log"),
            outputTemplate: LogOutputTemplate,
            rollingInterval: RollingInterval.Day,
            shared: true);
}

void AddHostedApiService<TInterface, TService>(IServiceCollection services)
    where TInterface : class
    where TService : class, IHostedService, TInterface
{
    services.AddSingleton<TInterface, TService>();
    services.AddSingleton<IHostedService>(p => (TService)p.GetRequiredService<TInterface>());
}

try
{
    Log.Logger = CreateDefaultLoggerConfiguration().CreateLogger();

    AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    {
        var ex = (Exception)e.ExceptionObject;
        Log.Error(ex.ToString());
        Log.Error($"Runtime terminating: {e.IsTerminating}");
    };

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddMudServices();

    var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
    var heaterControlOptions = configuration.GetSection(nameof(HeaterControlOptions));
    builder.Services.Configure<HeaterControlOptions>(heaterControlOptions);
    if (heaterControlOptions.GetValue<bool>(nameof(HeaterControlOptions.EmulatedGateway)))
    {
        AddHostedApiService<ISerialPort, EmulatedHeaterControlGateway>(builder.Services);
    }
    else
    {
        builder.Services.AddSingleton<ISerialPort, SerialPort>();
    }
    AddHostedApiService<IHeaterControl, HeaterControl>(builder.Services);
    builder.Services.AddHostedService<HeaterStateMachine>();

    builder.Services.AddSingleton<LogViewer>();
    builder.Services.AddSingleton<LiteDatabase>(_ => new LiteDatabase(Path.Combine(AppDir, "domo.db")));
    builder.Services.AddSingleton<HeaterDatabaseService>();
    builder.Services.AddSingleton<HeaterFactory>();
    builder.Services.AddSingleton<Heater>(p => p.GetRequiredService<HeaterDatabaseService>().Heater);

    builder.Services.AddSingleton<Serilog.ILogger>(p =>
        CreateDefaultLoggerConfiguration()
            .WriteTo.LogViewerSink(p.GetRequiredService<LogViewer>(), LogOutputTemplate)
            .CreateLogger());
    builder.Host.UseSerilog();

    var app = builder.Build();

    Log.Logger = app.Services.GetRequiredService<Serilog.ILogger>();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.MapBlazorHub();
    app.MapFallbackToPage("/_Host");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}