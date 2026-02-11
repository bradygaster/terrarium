// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Globalization;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Manages the local cache of creature DLL assemblies. Stores organism
/// assemblies in an obfuscated directory, provides assembly resolution via
/// <see cref="AssemblyLoadContext"/>, and enforces blacklisting by replacing
/// assemblies with zero-length files.
/// </summary>
public sealed class PrivateAssemblyCache : IDisposable
{
    private static readonly string _versionDirectoryPreamble;

    private readonly ILogger<PrivateAssemblyCache> _logger;
    private readonly Dictionary<string, string> _loadedAssemblies = new();
    private readonly OrganismAssemblyLoadContext _loadContext;

    private string _dataFile;
    private string _dataPath;
    private bool _hookAssemblyResolve;
    private long _pacSize;
    private bool _trackLastRun = true;
    private bool _disposed;

    static PrivateAssemblyCache()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
        _versionDirectoryPreamble = $"{version.Major}.{version.Minor}.{version.Build}";
    }

    /// <summary>
    /// Creates a new private assembly cache.
    /// </summary>
    public PrivateAssemblyCache(
        string dataPath,
        string dataFile,
        ILogger<PrivateAssemblyCache> logger,
        bool hookAssemblyResolve = true,
        bool trackLastRun = true)
    {
        _logger = logger;
        _trackLastRun = trackLastRun;
        _dataPath = string.IsNullOrEmpty(dataPath) ? Path.GetFullPath(".") : Path.GetFullPath(dataPath);
        _dataFile = Path.GetFileName(dataFile);
        _hookAssemblyResolve = hookAssemblyResolve;

        if (!Directory.Exists(BaseAssemblyDirectory))
        {
            Directory.CreateDirectory(BaseAssemblyDirectory);
        }

        _loadContext = new OrganismAssemblyLoadContext(AssemblyDirectory, _logger);

        if (hookAssemblyResolve)
        {
            HookAssemblyResolve();
        }
    }

    /// <summary>Event raised when assemblies in the PAC change.</summary>
    public event EventHandler? PacAssembliesChanged;

    /// <summary>Path to the assembly cache directory.</summary>
    public string AssemblyDirectory => BaseAssemblyDirectory;

    private string BaseAssemblyDirectory => GetBaseAssemblyDirectory(_dataPath, _dataFile);

    /// <summary>Approximate size in bytes of all loaded assemblies.</summary>
    public long PacSize => _pacSize;

    /// <summary>Number of organisms loaded from the PAC.</summary>
    public int PacOrganismCount => _loadedAssemblies.Count;

    /// <summary>The versioned directory preamble for cache paths.</summary>
    public static string VersionedDirectoryPreamble => _versionDirectoryPreamble;

    /// <summary>
    /// Tracks which organism was running when Terrarium last shut down,
    /// enabling post-crash blacklisting.
    /// </summary>
    public string LastRun
    {
        get
        {
            var filePath = Path.Combine(AssemblyDirectory, "data.dat");
            try
            {
                if (!File.Exists(filePath)) return "";
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var reader = new BinaryReader(stream);
                return stream.Length == 0 ? "" : reader.ReadString();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read LastRun file.");
                return "";
            }
        }
        set
        {
            var filePath = Path.Combine(AssemblyDirectory, "data.dat");
            if (_trackLastRun || value.Length == 0)
            {
                try
                {
                    using var stream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                    using var writer = new BinaryWriter(stream);
                    writer.Write(value);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to write LastRun file.");
                }
            }
        }
    }

    /// <summary>Hooks assembly resolution for the custom load context.</summary>
    public void HookAssemblyResolve()
    {
        if (_hookAssemblyResolve)
        {
            _logger.LogDebug("Assembly resolve hooked via AssemblyLoadContext.");
        }
    }

    /// <summary>Unhooks assembly resolve and releases resources.</summary>
    public void Close()
    {
        _logger.LogDebug("PrivateAssemblyCache closed.");
    }

    /// <summary>Generates a file name for the given assembly full name.</summary>
    public string GetFileName(string fullName)
    {
        return Path.Combine(AssemblyDirectory, GetAssemblyShortName(fullName) + ".dll");
    }

    /// <summary>Extracts the short name from an assembly full name.</summary>
    public static string GetAssemblyShortName(string fullName)
    {
        var chunks = fullName.Split(',');
        return chunks[0].ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>Extracts the version string from an assembly full name.</summary>
    public static string GetAssemblyVersion(string fullName)
    {
        var chunks = fullName.Split(',');
        var versionPieces = chunks[1].Split('=');
        return versionPieces[1];
    }

    /// <summary>Computes the base assembly directory from a path and data file name.</summary>
    public static string GetBaseAssemblyDirectory(string dataPath, string dataFile)
    {
        var dataFilePart = dataFile[..dataFile.LastIndexOf('.')];
        return Path.Combine(dataPath, dataFilePart + "_Assemblies");
    }

    /// <summary>
    /// Blacklists assemblies by replacing them with zero-length files,
    /// preventing them from being loaded or replaced with fresh copies.
    /// </summary>
    public void BlacklistAssemblies(string[]? assemblies)
    {
        if (assemblies == null) return;

        foreach (var assembly in assemblies)
        {
            var fileName = GetFileName(assembly);
            try
            {
                var fullPath = Path.GetFullPath(fileName);
                if (!fullPath.StartsWith(Path.GetFullPath(AssemblyDirectory), StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Blocked blacklist attempt outside assembly directory: {Path}", fullPath);
                    continue;
                }

                using var stream = File.Create(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error blacklisting organism assembly: {Assembly}", assembly);
            }
        }
    }

    /// <summary>
    /// Loads an organism assembly via <see cref="AssemblyLoadContext"/>.
    /// </summary>
    public Assembly LoadOrganismAssembly(string fullName)
    {
        if (!_loadedAssemblies.ContainsKey(fullName))
        {
            _loadedAssemblies[fullName] = fullName;
            var fileInfo = new FileInfo(GetFileName(fullName));
            if (fileInfo.Exists)
            {
                _pacSize += fileInfo.Length;
            }
        }

        var assemblyPath = GetFileName(fullName);
        return _loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
    }

    /// <summary>Checks whether the assembly exists in the PAC.</summary>
    public bool Exists(string fullName)
    {
        var fileName = GetFileName(fullName);
        var fullPath = Path.GetFullPath(fileName);

        if (!fullPath.StartsWith(Path.GetFullPath(AssemblyDirectory), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return File.Exists(fullPath);
    }

    /// <summary>Saves raw assembly bytes to the PAC.</summary>
    public async Task SaveOrganismBytesAsync(byte[] bytes, string fullName, CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var fileName = GetFileName(fullName);
        if (File.Exists(fileName)) return;

        ValidatePathSecurity(fileName);

        try
        {
            await using var targetStream = File.Create(fileName);
            await targetStream.WriteAsync(bytes, cancellationToken);
        }
        catch
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            throw;
        }

        OnPacAssembliesChanged();
    }

    /// <summary>Saves raw assembly bytes to the PAC (synchronous).</summary>
    public void SaveOrganismBytes(byte[] bytes, string fullName)
    {
        EnsureDirectoryExists();

        var fileName = GetFileName(fullName);
        if (File.Exists(fileName)) return;

        ValidatePathSecurity(fileName);

        try
        {
            using var targetStream = File.Create(fileName);
            targetStream.Write(bytes, 0, bytes.Length);
        }
        catch
        {
            if (File.Exists(fileName)) File.Delete(fileName);
            throw;
        }

        OnPacAssembliesChanged();
    }

    /// <summary>
    /// Saves an assembly from a file path, optionally with symbols.
    /// Validates the assembly before saving.
    /// </summary>
    public async Task SaveOrganismAssemblyAsync(
        string assemblyPath,
        string fullName,
        string? symbolPath = null,
        AssemblyValidator? validator = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDirectoryExists();

        var fileName = GetFileName(fullName);
        if (File.Exists(fileName)) return;

        if (validator != null)
        {
            var result = validator.Validate(assemblyPath);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    $"Assembly failed validation: {string.Join("; ", result.Reasons)}");
            }
        }

        ValidatePathSecurity(fileName);

        await using (var sourceStream = File.OpenRead(assemblyPath))
        {
            try
            {
                await using var targetStream = File.Create(fileName);
                await sourceStream.CopyToAsync(targetStream, cancellationToken);
            }
            catch
            {
                if (File.Exists(fileName)) File.Delete(fileName);
                throw;
            }
        }

        if (!string.IsNullOrEmpty(symbolPath) && File.Exists(symbolPath))
        {
            var symName = Path.ChangeExtension(fileName, ".pdb");
            try
            {
                await using var symSource = File.OpenRead(symbolPath);
                await using var symTarget = File.Create(symName);
                await symSource.CopyToAsync(symTarget, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to copy symbols for {Assembly}.", fullName);
            }
        }

        OnPacAssembliesChanged();
    }

    /// <summary>Returns info about all valid assemblies in the cache.</summary>
    public OrganismAssemblyInfo[] GetAssemblies()
    {
        if (!Directory.Exists(AssemblyDirectory))
        {
            return [];
        }

        var infoList = new List<OrganismAssemblyInfo>();
        foreach (var fileName in Directory.GetFiles(AssemblyDirectory, "*.dll"))
        {
            var info = new FileInfo(fileName);
            if (info.Length <= 0) continue;

            try
            {
                var assembly = _loadContext.LoadFromAssemblyPath(Path.GetFullPath(fileName));
                infoList.Add(new OrganismAssemblyInfo(assembly.FullName!, GetAssemblyShortName(assembly.FullName!)));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load assembly info for {File}.", fileName);
            }
        }

        return infoList.ToArray();
    }

    /// <summary>Returns assembly short names of all blacklisted (zero-length) assemblies.</summary>
    public string[] GetBlacklistedAssemblies()
    {
        var blacklisted = new List<string>();

        if (!Directory.Exists(AssemblyDirectory)) return [];

        foreach (var fileName in Directory.GetFiles(AssemblyDirectory, "*.dll"))
        {
            var info = new FileInfo(fileName);
            if (info.Length == 0)
            {
                blacklisted.Add(Path.GetFileNameWithoutExtension(fileName));
            }
        }

        return blacklisted.ToArray();
    }

    /// <summary>Creates a temp file path that can't be guessed.</summary>
    public static string GetSafeTempFileName()
    {
        return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
    }

    // --- Helpers ---

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(AssemblyDirectory))
        {
            Directory.CreateDirectory(AssemblyDirectory);
        }
    }

    private void ValidatePathSecurity(string fileName)
    {
        var fullPath = Path.GetFullPath(fileName);
        if (!fullPath.StartsWith(Path.GetFullPath(AssemblyDirectory), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Path traversal detected: '{fullPath}' is outside the assembly directory.");
        }
    }

    private void OnPacAssembliesChanged()
    {
        PacAssembliesChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- Inner types ---

    /// <summary>
    /// Custom <see cref="AssemblyLoadContext"/> for isolating organism assemblies.
    /// </summary>
    private sealed class OrganismAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly string _assemblyDirectory;
        private readonly ILogger _logger;

        public OrganismAssemblyLoadContext(string assemblyDirectory, ILogger logger)
            : base("OrganismContext", isCollectible: true)
        {
            _assemblyDirectory = assemblyDirectory;
            _logger = logger;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var shortName = assemblyName.Name?.ToLower(CultureInfo.InvariantCulture);
            if (shortName == null) return null;

            var candidatePath = Path.Combine(_assemblyDirectory, shortName + ".dll");
            if (File.Exists(candidatePath))
            {
                var info = new FileInfo(candidatePath);
                if (info.Length > 0)
                {
                    _logger.LogDebug("Loading organism assembly: {Path}", candidatePath);
                    return LoadFromAssemblyPath(Path.GetFullPath(candidatePath));
                }
            }

            return null;
        }
    }
}

/// <summary>
/// Stores assembly metadata for display.
/// </summary>
public sealed class OrganismAssemblyInfo
{
    public OrganismAssemblyInfo(string fullName, string shortName)
    {
        FullName = fullName;
        ShortName = shortName;
    }

    public string FullName { get; }
    public string ShortName { get; }
}
