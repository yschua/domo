using domo.Data;
using LiteDB;
using MudBlazor.Services;
using Serilog;

var AppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "domo");
Directory.CreateDirectory(AppDir);

const string LogOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

LoggerConfiguration CreateDefaultLoggerConfiguration()
{
    return new LoggerConfiguration()
        .WriteTo.File(
            path: Path.Combine(AppDir, "logs", ".log"),
            outputTemplate: LogOutputTemplate,
            rollingInterval: RollingInterval.Day,
            shared: true);
}

try
{
    Log.Logger = CreateDefaultLoggerConfiguration().CreateLogger();

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddMudServices();

    builder.Services.AddSingleton<LogViewer>();
    builder.Services.AddSingleton<HeaterDatabaseService>();
    builder.Services.AddOptions<HeaterStateMachineOptions>();
    builder.Services.AddHostedService<HeaterStateMachine>();
    builder.Services.AddHostedService<HeaterControl>();
    builder.Services.AddSingleton<IHeaterControl>(p => p.GetRequiredService<HeaterControl>());
    builder.Services.AddSingleton<HeaterFactory>();
    builder.Services.AddSingleton<Heater>(p => p.GetRequiredService<HeaterDatabaseService>().Heater);
    builder.Services.AddSingleton<LiteDatabase>(_ => new LiteDatabase(Path.Combine(AppDir, "domo.db")));

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