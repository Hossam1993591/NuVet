using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuVet.CLI.Commands;
using NuVet.Core.Services;
using NuVet.Core.Services.Implementation;
using System.CommandLine;

// Build the host with dependency injection
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register core services
        services.AddHttpClient();
        services.AddScoped<INuGetService, NuGetService>();
        services.AddScoped<IVulnerabilityService, VulnerabilityService>();
        services.AddScoped<IDependencyAnalyzer, DependencyAnalyzer>();
        services.AddScoped<IVulnerabilityScanner, VulnerabilityScanner>();
        services.AddScoped<IPackageUpdater, PackageUpdater>();
        
        // Register CLI commands
        services.AddTransient<ScanCommand>();
        services.AddTransient<UpdateCommand>();
        services.AddTransient<AnalyzeCommand>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

// Create the root command
var rootCommand = new RootCommand("NuVet - NuGet Vulnerability Scanner & Auto-Updater")
{
    Name = "nuvet"
};

// Get services from DI container
using var scope = host.Services.CreateScope();
var scanCommand = scope.ServiceProvider.GetRequiredService<ScanCommand>();
var updateCommand = scope.ServiceProvider.GetRequiredService<UpdateCommand>();
var analyzeCommand = scope.ServiceProvider.GetRequiredService<AnalyzeCommand>();

// Add commands to root
rootCommand.AddCommand(scanCommand.Create());
rootCommand.AddCommand(updateCommand.Create());
rootCommand.AddCommand(analyzeCommand.Create());

// Execute the command
return await rootCommand.InvokeAsync(args);
