using Microsoft.Extensions.Logging;
using NuVet.Core.Services;
using Spectre.Console;
using System.CommandLine;

namespace NuVet.CLI.Commands;

/// <summary>
/// Command to analyze project dependencies
/// </summary>
public class AnalyzeCommand
{
    private readonly IDependencyAnalyzer _analyzer;
    private readonly ILogger<AnalyzeCommand> _logger;

    public AnalyzeCommand(IDependencyAnalyzer analyzer, ILogger<AnalyzeCommand> logger)
    {
        _analyzer = analyzer;
        _logger = logger;
    }

    public Command Create()
    {
        var pathArgument = new Argument<string>("path", "Path to solution, project, or directory to analyze");
        var outputOption = new Option<string?>("--output", "Output file path for dependency graph (JSON format)");
        var showTransitiveOption = new Option<bool>("--show-transitive", () => false, "Show transitive dependencies");
        var treeFormatOption = new Option<bool>("--tree", "Display dependencies in tree format");
        var verboseOption = new Option<bool>("--verbose", "Enable verbose logging");

        var command = new Command("analyze", "Analyze project dependencies")
        {
            pathArgument,
            outputOption,
            showTransitiveOption,
            treeFormatOption,
            verboseOption
        };

        command.SetHandler(async (path, output, showTransitive, treeFormat, verbose) =>
        {
            await ExecuteAnalyzeAsync(path, output, showTransitive, treeFormat, verbose);
        }, pathArgument, outputOption, showTransitiveOption, treeFormatOption, verboseOption);

        return command;
    }

    private async Task ExecuteAnalyzeAsync(string path, string? outputPath, bool showTransitive, bool treeFormat, bool verbose)
    {
        try
        {
            AnsiConsole.Write(new Rule("[bold green]NuVet - Dependency Analyzer[/]"));
            AnsiConsole.WriteLine();

            if (!Path.Exists(path))
            {
                AnsiConsole.WriteLine($"[red]Error:[/] Path '{path}' does not exist.");
                Environment.Exit(1);
                return;
            }

            AnsiConsole.WriteLine($"[blue]Analyzing:[/] {path}");
            AnsiConsole.WriteLine();

            var dependencyGraph = await AnsiConsole.Progress()
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("[green]Analyzing dependencies...[/]");
                    task.MaxValue = 100;
                    
                    var graph = await _analyzer.AnalyzeAsync(path);
                    task.Value = 100;
                    
                    return graph;
                });

            // Summary
            AnsiConsole.Write(new Rule("[bold yellow]Analysis Summary[/]"));
            AnsiConsole.WriteLine();

            var summaryTable = new Table();
            summaryTable.AddColumn("Metric");
            summaryTable.AddColumn("Count");
            
            summaryTable.AddRow("Projects", dependencyGraph.Projects.Count.ToString());
            summaryTable.AddRow("Total Packages", dependencyGraph.AllPackages.Count.ToString());
            summaryTable.AddRow("Unique Packages", dependencyGraph.GetUniquePackages().Count().ToString());
            summaryTable.AddRow("Direct Dependencies", dependencyGraph.AllPackages.Count(p => p.IsDirectDependency).ToString());
            summaryTable.AddRow("Transitive Dependencies", dependencyGraph.AllPackages.Count(p => !p.IsDirectDependency).ToString());

            AnsiConsole.Write(summaryTable);
            AnsiConsole.WriteLine();

            // Projects
            AnsiConsole.Write(new Rule("[bold blue]Projects[/]"));
            AnsiConsole.WriteLine();

            var projectsTable = new Table();
            projectsTable.AddColumn("Project");
            projectsTable.AddColumn("Type");
            projectsTable.AddColumn("Target Framework");
            projectsTable.AddColumn("Package Count");

            foreach (var project in dependencyGraph.Projects.OrderBy(p => p.Name))
            {
                var packageCount = dependencyGraph.GetDirectDependencies(project.Path).Count();
                
                projectsTable.AddRow(
                    project.Name,
                    project.Type.ToString(),
                    project.TargetFramework,
                    packageCount.ToString()
                );
            }

            AnsiConsole.Write(projectsTable);
            AnsiConsole.WriteLine();

            if (treeFormat)
            {
                // Display as tree
                AnsiConsole.Write(new Rule("[bold cyan]Dependency Tree[/]"));
                AnsiConsole.WriteLine();

                foreach (var project in dependencyGraph.Projects.OrderBy(p => p.Name))
                {
                    var tree = new Tree($"[bold]{project.Name}[/] [dim]({project.Type})[/]");
                    
                    var directDeps = dependencyGraph.GetDirectDependencies(project.Path).ToList();
                    foreach (var dep in directDeps.OrderBy(d => d.Id))
                    {
                        var depNode = tree.AddNode($"[blue]{dep.Id}[/] [dim]{dep.Version}[/]");
                        
                        if (showTransitive && dep.Dependencies.Any())
                        {
                            AddTransitiveDependencies(depNode, dep, new HashSet<string>());
                        }
                    }
                    
                    AnsiConsole.Write(tree);
                    AnsiConsole.WriteLine();
                }
            }
            else
            {
                // Display as table
                AnsiConsole.Write(new Rule("[bold cyan]Package Dependencies[/]"));
                AnsiConsole.WriteLine();

                var packagesTable = new Table();
                packagesTable.AddColumn("Package");
                packagesTable.AddColumn("Version");
                packagesTable.AddColumn("Type");
                packagesTable.AddColumn("Used By");

                var packages = showTransitive 
                    ? dependencyGraph.GetUniquePackages() 
                    : dependencyGraph.GetUniquePackages().Where(p => p.IsDirectDependency);

                foreach (var package in packages.OrderBy(p => p.Id))
                {
                    var projectsUsingPackage = dependencyGraph.AllPackages
                        .Where(p => p.Id == package.Id && p.Version.Equals(package.Version))
                        .Select(p => Path.GetFileNameWithoutExtension(p.ProjectPath))
                        .Distinct()
                        .ToList();

                    packagesTable.AddRow(
                        package.Id,
                        package.Version.ToString(),
                        package.IsDirectDependency ? "Direct" : "Transitive",
                        string.Join(", ", projectsUsingPackage.Take(3)) + (projectsUsingPackage.Count > 3 ? "..." : "")
                    );
                }

                AnsiConsole.Write(packagesTable);
            }

            // Save to file if requested
            if (!string.IsNullOrEmpty(outputPath))
            {
                var jsonContent = System.Text.Json.JsonSerializer.Serialize(dependencyGraph, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(outputPath, jsonContent);
                AnsiConsole.WriteLine();
                AnsiConsole.WriteLine($"[blue]Dependency graph saved to:[/] {outputPath}");
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during analysis execution");
            AnsiConsole.WriteLine($"[red]Error:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void AddTransitiveDependencies(TreeNode parentNode, Core.Models.PackageReference package, HashSet<string> visited)
    {
        var key = $"{package.Id}_{package.Version}";
        if (visited.Contains(key)) return;
        
        visited.Add(key);
        
        foreach (var dep in package.Dependencies.Take(5)) // Limit to avoid too much output
        {
            var depNode = parentNode.AddNode($"[dim]{dep.Id} {dep.Version}[/]");
            
            if (dep.Dependencies.Any())
            {
                AddTransitiveDependencies(depNode, dep, visited);
            }
        }
        
        if (package.Dependencies.Count > 5)
        {
            parentNode.AddNode($"[dim]... and {package.Dependencies.Count - 5} more[/]");
        }
    }
}