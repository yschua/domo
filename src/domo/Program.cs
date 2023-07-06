using domo.Data;
using LiteDB;
using MudBlazor.Services;
using Serilog;

var appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "domo");
Directory.CreateDirectory(appDir);

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(Path.Combine(appDir, "logs", ".log"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    builder.Services.AddMudServices();

    builder.Services.AddSingleton<HeaterDatabaseService>();
    builder.Services.AddOptions<HeaterStateMachineOptions>();
    builder.Services.AddHostedService<HeaterStateMachine>();
    builder.Services.AddSingleton<HeaterFactory>();
    builder.Services.AddSingleton<Heater>(p => p.GetRequiredService<HeaterDatabaseService>().Heater);
    builder.Services.AddSingleton<LiteDatabase>(_ => new LiteDatabase(Path.Combine(appDir, "domo.db")));

    builder.Host.UseSerilog();

    var app = builder.Build();

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