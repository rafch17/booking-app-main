using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace booking.infrastructure.Persistence;

public class DbInitializerHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;

    public DbInitializerHostedService(IServiceProvider sp, IHostEnvironment env, IConfiguration config)
    {
        _sp = sp;
        _env = env;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var infraRoot = Path.Combine(_env.ContentRootPath, "..", "booking.infrastructure");
        var seedDemoData = _env.IsDevelopment();
        var demoUserPassword = _config["Seed:DemoUserPassword"];

        await DatabaseBootstrapper.InitAsync(db, infraRoot, seedDemoData, demoUserPassword, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}