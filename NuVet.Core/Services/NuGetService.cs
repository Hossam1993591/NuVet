using System.Text.Json;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuVet.Core.Models;
using NuVet.Core.Services;
using Semver;

namespace NuVet.Core.Services.Implementation;

/// <summary>
/// Implementation of INuGetService using NuGet.Protocol
/// </summary>
public class NuGetService : INuGetService
{
    private readonly ILogger<NuGetService> _logger;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceCacheContext _cacheContext;

    public NuGetService(ILogger<NuGetService> logger)
    {
        _logger = logger;
        
        // Use nuget.org as the default source
        var packageSource = new NuGet.Configuration.PackageSource("https://api.nuget.org/v3/index.json");
        var providers = Repository.Provider.GetCoreV3();
        _sourceRepository = new SourceRepository(packageSource, providers.Select(p => p.Value));
        _cacheContext = new SourceCacheContext();
    }

    public async Task<PackageMetadata?> GetPackageMetadataAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting metadata for package {PackageId}", packageId);
            
            var metadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>(cancellationToken);
            var packages = await metadataResource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: false,
                _cacheContext,
                NullLogger.Instance,
                cancellationToken);

            var packageList = packages.ToList();
            if (!packageList.Any())
            {
                _logger.LogWarning("No metadata found for package {PackageId}", packageId);
                return null;
            }

            var latestPackage = packageList.OrderByDescending(p => p.Identity.Version).First();
            var latestStable = packageList
                .Where(p => !p.Identity.Version.IsPrerelease)
                .OrderByDescending(p => p.Identity.Version)
                .FirstOrDefault();

            return new PackageMetadata
            {
                Id = latestPackage.Identity.Id,
                Title = latestPackage.Title ?? latestPackage.Identity.Id,
                Description = latestPackage.Description ?? string.Empty,
                Authors = latestPackage.Authors?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>(),
                Tags = latestPackage.Tags?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>(),
                ProjectUrl = latestPackage.ProjectUrl ?? new Uri("https://nuget.org"),
                LicenseUrl = latestPackage.LicenseUrl ?? new Uri("https://nuget.org"),
                Published = latestPackage.Published?.DateTime ?? DateTime.MinValue,
                DownloadCount = latestPackage.DownloadCount ?? 0,
                IsPrerelease = latestPackage.Identity.Version.IsPrerelease,
                LatestVersion = SemVersion.Parse(latestPackage.Identity.Version.ToString(), SemVersionStyles.Strict),
                LatestStableVersion = latestStable != null ? SemVersion.Parse(latestStable.Identity.Version.ToString(), SemVersionStyles.Strict) : null,
                Versions = packageList.Select(p => new PackageVersion
                {
                    Version = SemVersion.Parse(p.Identity.Version.ToString(), SemVersionStyles.Strict),
                    Published = p.Published?.DateTime ?? DateTime.MinValue,
                    DownloadCount = p.DownloadCount ?? 0,
                    IsPrerelease = p.Identity.Version.IsPrerelease,
                    ReleaseNotes = null // ReleaseNotes not available in search metadata
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metadata for package {PackageId}", packageId);
            return null;
        }
    }

    public async Task<List<string>> GetPackageVersionsAsync(string packageId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting versions for package {PackageId}", packageId);
            
            var findPackageByIdResource = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
            var versions = await findPackageByIdResource.GetAllVersionsAsync(
                packageId,
                _cacheContext,
                NullLogger.Instance,
                cancellationToken);

            return versions.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting versions for package {PackageId}", packageId);
            return new List<string>();
        }
    }

    public async Task<List<PackageSearchResult>> SearchPackagesAsync(
        string searchQuery,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching packages with query '{SearchQuery}'", searchQuery);
            
            var searchResource = await _sourceRepository.GetResourceAsync<PackageSearchResource>(cancellationToken);
            var searchFilter = new SearchFilter(includePrerelease: false);
            
            var results = await searchResource.SearchAsync(
                searchQuery,
                searchFilter,
                skip,
                take,
                NullLogger.Instance,
                cancellationToken);

            return results.Select(result => new PackageSearchResult
            {
                Id = result.Identity.Id,
                Title = result.Title,
                Description = result.Description,
                Authors = result.Authors?.Split(',').Select(a => a.Trim()).ToList() ?? new List<string>(),
                Tags = result.Tags?.Split(',').Select(t => t.Trim()).ToList() ?? new List<string>(),
                LatestVersion = SemVersion.Parse(result.Identity.Version.ToString(), SemVersionStyles.Strict),
                TotalDownloads = result.DownloadCount ?? 0,
                IsVerified = false // NuGet.Protocol doesn't expose verified status in search results
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching packages with query '{SearchQuery}'", searchQuery);
            return new List<PackageSearchResult>();
        }
    }

    public async Task<List<PackageDependency>> GetPackageDependenciesAsync(
        string packageId,
        string version,
        string? targetFramework = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting dependencies for package {PackageId} version {Version}", packageId, version);
            
            var dependencyInfoResource = await _sourceRepository.GetResourceAsync<DependencyInfoResource>(cancellationToken);
            var packageIdentity = new NuGet.Packaging.Core.PackageIdentity(packageId, NuGet.Versioning.NuGetVersion.Parse(version));
            
            var dependencyInfo = await dependencyInfoResource.ResolvePackage(
                packageIdentity,
                NuGet.Frameworks.NuGetFramework.ParseFolder(targetFramework ?? "net8.0"),
                _cacheContext,
                NullLogger.Instance,
                cancellationToken);

            if (dependencyInfo == null)
            {
                return new List<PackageDependency>();
            }

            return dependencyInfo.Dependencies.Select(dep => new PackageDependency
            {
                Id = dep.Id,
                VersionRange = dep.VersionRange.ToString(),
                TargetFramework = targetFramework
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dependencies for package {PackageId} version {Version}", packageId, version);
            return new List<PackageDependency>();
        }
    }

    public async Task<bool> PackageVersionExistsAsync(string packageId, string version, CancellationToken cancellationToken = default)
    {
        try
        {
            var versions = await GetPackageVersionsAsync(packageId, cancellationToken);
            return versions.Contains(version, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if package {PackageId} version {Version} exists", packageId, version);
            return false;
        }
    }

    public void Dispose()
    {
        _cacheContext?.Dispose();
    }
}