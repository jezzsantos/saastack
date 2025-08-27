#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Polly;

namespace Infrastructure.External.Persistence.TestingOnly.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to individual files of JSON on the local hard drive.
///     The files are located in named folders under <see cref="_rootPath" />,
///     on Windows that is: %LOCALAPPDATA%\saastack\{containerName},
///     on macOS that is: /Users/username/.local/share/saastack/{containerName}
///     Note: Should NEVER be used in production systems, but useful in testing distributed systems, across multiple
///     processes.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed partial class LocalMachineJsonFileStore : IDisposable
{
    public const string DefaultPrefix = "LocalMachineJsonFileStore";
    public const string PathSettingFormatName = "ApplicationServices:Persistence:{0}:RootPath";
    private readonly FileSystemWatcher? _fileSystemWatcher;
    private readonly string _rootPath;

    public static LocalMachineJsonFileStore Create(IConfigurationSettings settings,
        IMessageMonitor? handler = default)
    {
        return Create(settings, DefaultPrefix, handler);
    }

    public static LocalMachineJsonFileStore Create(IConfigurationSettings settings,
        string prefix, IMessageMonitor? handler = default)
    {
        var configPath = settings.GetString(PathSettingFormatName.Format(prefix));
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.GetFullPath(Path.Combine(basePath, configPath));
        VerifyRootPath(path);

        return new LocalMachineJsonFileStore(path, handler);
    }

    private LocalMachineJsonFileStore(string rootPath, IMessageMonitor? monitor = default)
    {
        rootPath.ThrowIfNotValuedParameter(nameof(rootPath));
        if (rootPath.IsInvalidParameter(ValidateRootPath, nameof(rootPath),
                $"Root path '{rootPath}' is not a valid path", out var error))
        {
            throw new InvalidOperationException(error.Message);
        }

        _rootPath = rootPath;

        if (monitor.Exists())
        {
            _fileSystemWatcher = new FileSystemWatcher(_rootPath, $"*.{FileContainer.FileExtension}")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _fileSystemWatcher.Created += FileSystemWatcherOnCreated;
            FireQueueMessage += (_, args) => { monitor.NotifyQueueMessagesChanged(args.QueueName, args.MessageCount); };
            NotifyPendingQueuedMessages();
            FireTopicMessage += (_, args) => { monitor.NotifyTopicMessagesChanged(args.QueueName, args.MessageCount); };
            NotifyPendingBusTopicMessages();
        }
    }

    ~LocalMachineJsonFileStore()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fileSystemWatcher?.Dispose();
        }
    }

    private void FileSystemWatcherOnCreated(object sender, FileSystemEventArgs e)
    {
        if (!e.ChangeType.HasFlag(WatcherChangeTypes.Created))
        {
            return;
        }

        if (TryNotifyQueuedMessage(e.FullPath))
        {
            return;
        }

        if (TryNotifyBusTopicMessage(e.FullPath))
        {
            return;
        }

        // ReSharper disable once RedundantJumpStatement
        return;
    }

    private static void VerifyRootPath(string path)
    {
        if (!Directory.Exists(path))
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    path.Format(), ex);
            }
        }
    }

    private FileContainer EnsureContainer(string containerName)
    {
        return new FileContainer(_rootPath, containerName);
    }

    private static bool ValidateRootPath(string path)
    {
        if (!path.HasValue())
        {
            return false;
        }

        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var info = new FileInfo(path);
            if ((info.Attributes & FileAttributes.ReadOnly) != 0)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<Dictionary<string, HydrationProperties>> QueryPrimaryEntitiesAsync(
        FileContainer container, PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        var ids = container.GetEntityIds();
        var primaryEntities = new Dictionary<string, HydrationProperties>();
        foreach (var id in ids)
        {
            var properties = await GetEntityFromFileAsync(container, id, metadata, cancellationToken);
            primaryEntities.Add(id, properties);
        }

        return primaryEntities;
    }

    private static async Task<HydrationProperties> GetEntityFromFileAsync(FileContainer container, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        try
        {
            var containerEntityProperties = await container.ReadAsync(id, cancellationToken);
            if (containerEntityProperties.HasNone())
            {
                return new HydrationProperties();
            }

            return containerEntityProperties.FromFileProperties(metadata);
        }
        catch (Exception)
        {
            return new HydrationProperties();
        }
    }

    private sealed class FileContainer
    {
        internal const string FileExtension = "json";

        private static readonly Encoding DefaultSystemTextEncoding = new UTF8Encoding(false, true);
        private readonly string _dirPath;
        private readonly string _rootPath;

        public FileContainer(string rootPath, string containerName)
        {
            _rootPath = rootPath;
            _dirPath =
                CleanDirectoryPath(Path.Combine(Environment.ExpandEnvironmentVariables(rootPath), containerName));
            if (!Directory.Exists(_dirPath))
            {
                Directory.CreateDirectory(_dirPath);
            }
        }

        public long Count => GetFilesExcludingIgnored(_dirPath).Count();

        public string Name => new DirectoryInfo(_dirPath).Name;

        public async Task AddBinaryAsync(string name, string contentType, Stream stream,
            CancellationToken cancellationToken)
        {
            var data = await stream.ReadFullyAsync(cancellationToken);
            await WriteAsync(name, new Dictionary<string, Optional<string>>
            {
                { nameof(BinaryFile.ContentType), contentType },
                { nameof(BinaryFile.Data), Convert.ToBase64String(data) }
            }, cancellationToken);
        }

        public IEnumerable<FileContainer> Containers()
        {
            var containers = GetDirectories(_dirPath).ToList();
            if (!containers.Any())
            {
                return [];
            }

            return containers
                .Select(container => new FileContainer(_rootPath, container));
        }

        public void Erase()
        {
            var dir = new DirectoryInfo(_dirPath);
            if (dir.Exists)
            {
                Try.Safely(() => dir.Delete(true));
            }
        }

        public bool Exists(string entityId)
        {
            var filename = GetFullFilePathFromName(entityId);
            return File.Exists(filename);
        }

        public async Task<Optional<BinaryFile>> GetBinaryAsync(string name, CancellationToken cancellationToken)
        {
            var properties = await ReadAsync(name, cancellationToken);
            if (properties.HasNone())
            {
                return Optional<BinaryFile>.None;
            }

            return new BinaryFile
            {
                ContentType = properties[nameof(BinaryFile.ContentType)],
                Data = Convert.FromBase64String(properties[nameof(BinaryFile.Data)])
            };
        }

        public IEnumerable<string> GetEntityIds()
        {
            return GetFilesExcludingIgnored(_dirPath)
                .Select(GetIdFromFullFilePath)
                .ToList();
        }

        public bool IsEmpty()
        {
            return Count == 0;
        }

        public bool IsPath(string fullPath)
        {
            return Path.GetFullPath(_dirPath).WithoutTrailingSlash()
                .EqualsIgnoreCase(Path.GetFullPath(fullPath).WithoutTrailingSlash());
        }

        public bool NotExists(string entityId)
        {
            return !Exists(entityId);
        }

        public async Task OverwriteAsync(string entityId, IReadOnlyDictionary<string, Optional<string>> properties,
            CancellationToken cancellationToken)
        {
            if (Exists(entityId))
            {
                await WriteAsync(entityId, properties, cancellationToken);
            }
        }

        /// <summary>
        ///     Reads the properties from the JSON data within the file on disk
        /// </summary>
        public async Task<IReadOnlyDictionary<string, Optional<string>>> ReadAsync(string entityId,
            CancellationToken cancellationToken)
        {
            if (Exists(entityId))
            {
                var filename = GetFullFilePathFromName(entityId);
                var content = await File.ReadAllTextAsync(filename, cancellationToken);
                return (content.FromJson<Dictionary<string, string>>()
                        ?? new Dictionary<string, string>())
                    .ToDictionary(pair => pair.Key, pair => pair.Value.ToOptional());
            }

            return new Dictionary<string, Optional<string>>();
        }

        public void Remove(string entityId)
        {
            if (Exists(entityId))
            {
                var filename = GetFullFilePathFromName(entityId);
                File.Delete(filename);
            }
        }

        /// <summary>
        ///     Writes the specified <see cref="properties" /> to JSON to a file on the disk.
        ///     This version of the method will overwrite the file if it already exists.
        /// </summary>
        public async Task WriteAsync(string filename, IReadOnlyDictionary<string, Optional<string>> properties,
            CancellationToken cancellationToken)
        {
            var retryPolicy = Policy.Handle<IOException>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(300));

            var fullFilename = GetFullFilePathFromName(filename);
            await retryPolicy.ExecuteAsync(SaveFileAsync);
            return;

            async Task SaveFileAsync()
            {
                await using var file = File.CreateText(fullFilename);
                var json = properties
                    .Where(pair => pair.Value.HasValue)
                    .ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrDefault)
                    .ToJson();
                await file.WriteAsync(json);
                await file.FlushAsync(cancellationToken);
            }
        }

        /// <summary>
        ///     Writes the specified <see cref="properties" /> to JSON to a file on the disk.
        ///     This version of the method will return an error if the file already exists.
        /// </summary>
        public async Task<Result<Error>> WriteExclusiveAsync(string filename,
            IReadOnlyDictionary<string, Optional<string>> properties,
            CancellationToken cancellationToken)
        {
            var retryPolicy = Policy.Handle<IOException>()
                .WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(300));

            var fullFilename = GetFullFilePathFromName(filename);
            return await retryPolicy.ExecuteAsync(CreateFileExclusiveAsync);

            async Task<Result<Error>> CreateFileExclusiveAsync()
            {
                var json = properties
                    .Where(pair => pair.Value.HasValue)
                    .ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrDefault)
                    .ToJson()!;

                var content = DefaultSystemTextEncoding.GetBytes(json);

                FileStream? file = null;
                try
                {
                    // Try to open the file for writing, but fail if it already exists, or if someone else is writing
                    // or reading it at the same time (essentially locking the file).
                    file = File.Open(fullFilename, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                }
                catch (IOException)
                {
                    if (file.Exists())
                    {
                        file.Close();
                    }

                    return Error.EntityExists();
                }

                try
                {
                    await file.WriteAsync(content, cancellationToken);
                    await file.FlushAsync(cancellationToken);

                    return Result.Ok;
                }
                finally
                {
                    file.Close();
                }
            }
        }

        private static string CleanDirectoryPath(string path)
        {
            return string.Join("", path.Split(Path.GetInvalidPathChars()));
        }

        private static string CleanFileName(string fileName)
        {
            return string.Join("", fileName.Split(Path.GetInvalidFileNameChars()));
        }

        private string GetFullFilePathFromName(string filename)
        {
            var fullFilename = $"{CleanFileName(filename)}.{FileExtension}";
            return Path.Combine(_dirPath, fullFilename);
        }

        private static string GetIdFromFullFilePath(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private static IEnumerable<string> GetFilesExcludingIgnored(string dirPath)
        {
            return Directory.GetFiles(dirPath)
                .Where(filePath => !ShouldIgnoreFile(Path.GetFileName(filePath)));
        }

        private static IEnumerable<string> GetDirectories(string dirPath)
        {
            return Directory.GetDirectories(dirPath);
        }

        private static bool ShouldIgnoreFile(string fileName)
        {
            // Causes issues on Mac OS - https://en.wikipedia.org/wiki/.DS_Store
            return fileName == ".DS_Store";
        }
    }
}

internal static class LocalMachineFileStoreExtensions
{
    public static HydrationProperties FromFileProperties(
        this IReadOnlyDictionary<string, Optional<string>> containerProperties, PersistedEntityMetadata metadata)
    {
        var properties = containerProperties
            .Where(pair => metadata.HasType(pair.Key) && pair.Value.HasValue)
            .ToDictionary(pair => pair.Key,
                pair => pair.Value.FromFileProperty(metadata.GetPropertyType(pair.Key)));

        ApplyMappings(metadata, containerProperties, properties);

        return new HydrationProperties(properties);
    }

    public static IReadOnlyDictionary<string, Optional<string>> ToFileProperties(this CommandEntity entity)
    {
        var containerProperties = entity.Properties
            .ToDictionary<KeyValuePair<string, Optional<object>>, string, Optional<string>>(pair => pair.Key,
                pair => ToFileProperty(pair.Value));
        containerProperties[nameof(CommandEntity.LastPersistedAtUtc)] = DateTime.UtcNow.ToIso8601();

        return containerProperties;
    }

    private static void ApplyMappings(PersistedEntityMetadata metadata,
        IReadOnlyDictionary<string, Optional<string>> containerProperties,
        Dictionary<string, Optional<object>>? properties)
    {
        if (properties.NotExists())
        {
            return;
        }

        var mappings = metadata.GetReadMappingsOverride();
        if (mappings.HasAny())
        {
            var containerPropertiesDictionary = containerProperties
                .ToDictionary(pair => pair.Key, pair => pair.Value.HasValue
                    ? pair.Value.Value
                    : (object?)null);
            foreach (var mapping in mappings)
            {
                var mapResult = Try.Safely(() => mapping.Value(containerPropertiesDictionary));
                if (mapResult.Exists())
                {
                    if (!properties.TryAdd(mapping.Key, mapResult))
                    {
                        properties[mapping.Key] = mapResult;
                    }
                }
            }
        }
    }

    private static Optional<string> ToFileProperty(this Optional<object> propertyValue)
    {
        if (!propertyValue.HasValue)
        {
            return LocalMachineJsonFileStore.NullToken;
        }

        var value = propertyValue.Value;
        switch (value)
        {
            case DateTime dateTime:
                if (!dateTime.HasValue())
                {
                    dateTime = DateTime.MinValue;
                }

                return dateTime.ToIso8601();

            case DateTimeOffset dateTimeOffset:
                if (dateTimeOffset == DateTimeOffset.MinValue)
                {
                    dateTimeOffset = DateTimeOffset.MinValue.ToUniversalTime();
                }

                return dateTimeOffset.ToIso8601();

            case Guid guid:
                return guid.ToString("D");

            case byte[] bytes:
                return Convert.ToBase64String(bytes);

            case string text:
                return text;

            case null:
                return LocalMachineJsonFileStore.NullToken;

            default:
                return propertyValue.ComplexTypeToContainerProperty();
        }
    }

    private static Optional<object> FromFileProperty(this Optional<string> propertyValue, Type targetPropertyType)
    {
        if (!propertyValue.HasValue)
        {
            return Optional<object>.None;
        }

        if (propertyValue.Value == LocalMachineJsonFileStore.NullToken)
        {
            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(string)
            || targetPropertyType == typeof(Optional<string>)
            || targetPropertyType == typeof(Optional<string?>))
        {
            return propertyValue.Value;
        }

        if (targetPropertyType == typeof(bool) || targetPropertyType == typeof(bool?)
                                               || targetPropertyType == typeof(Optional<bool>)
                                               || targetPropertyType == typeof(Optional<bool?>))
        {
            return bool.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(DateTime) || targetPropertyType == typeof(DateTime?)
                                                   || targetPropertyType == typeof(Optional<DateTime>)
                                                   || targetPropertyType == typeof(Optional<DateTime?>))
        {
            return propertyValue.Value.FromIso8601();
        }

        if (targetPropertyType == typeof(DateTimeOffset) || targetPropertyType == typeof(DateTimeOffset?)
                                                         || targetPropertyType == typeof(Optional<DateTimeOffset>)
                                                         || targetPropertyType == typeof(Optional<DateTimeOffset?>))
        {
            return DateTimeOffset.ParseExact(propertyValue.Value, "O", null).ToUniversalTime();
        }

        if (targetPropertyType == typeof(Guid) || targetPropertyType == typeof(Guid?)
                                               || targetPropertyType == typeof(Optional<Guid>)
                                               || targetPropertyType == typeof(Optional<Guid?>))
        {
            return Guid.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(decimal) || targetPropertyType == typeof(decimal?)
                                                  || targetPropertyType == typeof(Optional<decimal>)
                                                  || targetPropertyType == typeof(Optional<decimal?>))
        {
            return decimal.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(double) || targetPropertyType == typeof(double?)
                                                 || targetPropertyType == typeof(Optional<double>)
                                                 || targetPropertyType == typeof(Optional<double?>))
        {
            return double.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(long) || targetPropertyType == typeof(long?)
                                               || targetPropertyType == typeof(Optional<long>)
                                               || targetPropertyType == typeof(Optional<long?>))
        {
            return long.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(int) || targetPropertyType == typeof(int?)
                                              || targetPropertyType == typeof(Optional<int>)
                                              || targetPropertyType == typeof(Optional<int?>))
        {
            return int.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(byte[]) || targetPropertyType == typeof(Optional<byte[]>))
        {
            return Convert.FromBase64String(propertyValue.Value);
        }

        if (targetPropertyType.IsEnum)
        {
            return Enum.Parse(targetPropertyType, propertyValue.Value, true);
        }

        if (targetPropertyType.IsOptionalEnum())
        {
            var underlyingType = targetPropertyType.GetGenericArguments().First();
            return Enum.Parse(underlyingType, propertyValue.Value, true);
        }

        if (targetPropertyType.IsNullableEnum())
        {
            return propertyValue.HasValue
                ? targetPropertyType.ParseNullable(propertyValue.Value)
                : Optional<object>.None;
        }

        if (targetPropertyType.IsComplexStorageType())
        {
            return propertyValue.ComplexTypeFromContainerProperty(targetPropertyType);
        }

        if (typeof(IDehydratableValueObject).IsAssignableFrom(targetPropertyType))
        {
            return propertyValue.Value;
        }

        return propertyValue.Value;
    }
}
#endif