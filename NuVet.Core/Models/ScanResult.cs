using Semver;

namespace NuVet.Core.Models;

/// <summary>
/// Results of a vulnerability scan
/// </summary>
public class ScanResult
{
    public required string SolutionPath { get; init; }
    public required DateTime ScanDate { get; init; }
    public required List<VulnerablePackage> VulnerablePackages { get; init; }
    public required List<ProjectInfo> ScannedProjects { get; init; }
    public required ScanSummary Summary { get; init; }
    public TimeSpan ScanDuration { get; init; }
    public string? ScanVersion { get; init; }
    
    /// <summary>
    /// Gets vulnerable packages by severity level
    /// </summary>
    public IEnumerable<VulnerablePackage> GetVulnerablePackagesBySeverity(VulnerabilitySeverity severity)
    {
        return VulnerablePackages.Where(vp => vp.Vulnerabilities.Any(v => v.Severity == severity));
    }
    
    /// <summary>
    /// Gets the highest severity level found in the scan
    /// </summary>
    public VulnerabilitySeverity GetHighestSeverity()
    {
        if (!VulnerablePackages.Any()) return VulnerabilitySeverity.Unknown;
        
        return VulnerablePackages
            .SelectMany(vp => vp.Vulnerabilities)
            .Max(v => v.Severity);
    }
    
    /// <summary>
    /// Checks if the scan found any vulnerabilities
    /// </summary>
    public bool HasVulnerabilities => VulnerablePackages.Any();
    
    /// <summary>
    /// Checks if the scan found any critical vulnerabilities
    /// </summary>
    public bool HasCriticalVulnerabilities => VulnerablePackages.Any(vp => 
        vp.Vulnerabilities.Any(v => v.Severity == VulnerabilitySeverity.Critical));
}

/// <summary>
/// Represents a package that has one or more vulnerabilities
/// </summary>
public class VulnerablePackage
{
    public required PackageReference Package { get; init; }
    public required List<Vulnerability> Vulnerabilities { get; init; }
    public required List<string> AffectedProjects { get; init; }
    
    /// <summary>
    /// Gets the highest severity vulnerability for this package
    /// </summary>
    public VulnerabilitySeverity HighestSeverity => 
        Vulnerabilities.Any() ? Vulnerabilities.Max(v => v.Severity) : VulnerabilitySeverity.Unknown;
    
    /// <summary>
    /// Gets suggested update versions that would fix all vulnerabilities
    /// </summary>
    public List<SemVersion> GetSuggestedUpdateVersions()
    {
        var allPatchedVersions = Vulnerabilities
            .SelectMany(v => v.PatchedVersions)
            .Distinct()
            .OrderBy(v => v)
            .ToList();
            
        // Find versions that fix all vulnerabilities
        var suggestedVersions = new List<SemVersion>();
        
        foreach (var version in allPatchedVersions)
        {
            if (Vulnerabilities.All(vuln => vuln.PatchedVersions.Any(pv => SemVersion.ComparePrecedence(pv, version) <= 0)))
            {
                suggestedVersions.Add(version);
            }
        }
        
        return suggestedVersions;
    }
}

/// <summary>
/// Summary statistics for a vulnerability scan
/// </summary>
public class ScanSummary
{
    public required int TotalProjects { get; init; }
    public required int TotalPackages { get; init; }
    public required int VulnerablePackages { get; init; }
    public required int CriticalVulnerabilities { get; init; }
    public required int HighVulnerabilities { get; init; }
    public required int ModerateVulnerabilities { get; init; }
    public required int LowVulnerabilities { get; init; }
    public required int UnknownVulnerabilities { get; init; }
    
    public int TotalVulnerabilities => CriticalVulnerabilities + HighVulnerabilities + 
                                      ModerateVulnerabilities + LowVulnerabilities + UnknownVulnerabilities;
}