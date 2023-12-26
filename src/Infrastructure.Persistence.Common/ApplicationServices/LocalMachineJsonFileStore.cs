#if TESTINGONLY
using System.Diagnostics.CodeAnalysis;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Polly;

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to individual files of JSON on the local hard drive.
///     The files are located in named folders under the <see cref="rootPath" />
/// </summary>
[ExcludeFromCodeCoverage]
public partial class LocalMachineJsonFileStore
{
    private const string PathSettingName = "ApplicationServices:Persistence:LocalMachineJsonFileStore:RootPath";
    private readonly string _rootPath;

    public static LocalMachineJsonFileStore Create(ISettings settings,
        IQueueStoreNotificationHandler? handler = default)
    {
        var configPath = settings.GetString(PathSettingName);
        var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var path = Path.GetFullPath(Path.Combine(basePath, configPath));
        VerifyRootPath(path);

        return new LocalMachineJsonFileStore(path, handler);
    }

    private LocalMachineJsonFileStore(string rootPath, IQueueStoreNotificationHandler? handler = default)
    {
        rootPath.ThrowIfNotValuedParameter(nameof(rootPath));
        if (rootPath.IsInvalidParameter(ValidateRootPath, nameof(rootPath),
                $"Root path '{rootPath}' is not a valid path", out var error))
        {
            throw new InvalidOperationException(error.Message);
        }

        _rootPath = rootPath;

        if (handler.Exists())
        {
            FireMessageQueueUpdated += (_, args) =>
            {
                handler.HandleMessagesQueueUpdated(args.QueueName, args.MessageCount);
            };
            NotifyAllQueuedMessages();
        }
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

    private static Dictionary<string, HydrationProperties> QueryPrimaryEntities(
        FileContainer container, PersistedEntityMetadata metadata)
    {
        var primaryEntities = container.GetEntityIds()
            .ToDictionary(id => id, id => GetEntityFromFile(container, id, metadata));

        return primaryEntities;
    }

    private static HydrationProperties GetEntityFromFile(FileContainer container, string id,
        PersistedEntityMetadata metadata)
    {
        try
        {
            var containerEntityProperties = container.Read(id);
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
        private const string FileExtension = "json";
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

        public void AddBinary(string name, string contentType, Stream stream)
        {
            var data = stream.ReadFully();
            Write(name, new Dictionary<string, Optional<string>>
            {
                { nameof(BinaryFile.ContentType), contentType },
                { nameof(BinaryFile.Data), Convert.ToBase64String(data) }
            });
        }

        public IEnumerable<FileContainer> Containers()
        {
            var containers = GetDirectories(_dirPath).ToList();
            if (!containers.Any())
            {
                return Enumerable.Empty<FileContainer>();
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
            var filename = GetFullFilePathFromId(entityId);
            return File.Exists(filename);
        }

        public Optional<BinaryFile> GetBinary(string name)
        {
            var properties = Read(name);
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

        public void Overwrite(string entityId, IReadOnlyDictionary<string, Optional<string>> properties)
        {
            if (Exists(entityId))
            {
                Write(entityId, properties);
            }
        }

        /// <summary>
        ///     Reads the properties from the JSON data within the file on disk
        /// </summary>
        public IReadOnlyDictionary<string, Optional<string>> Read(string entityId)
        {
            if (Exists(entityId))
            {
                var filename = GetFullFilePathFromId(entityId);
                var content = File.ReadAllText(filename);
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
                var filename = GetFullFilePathFromId(entityId);
                File.Delete(filename);
            }
        }

        /// <summary>
        ///     Writes the specified <see cref="properties" /> to JSON to a file on the disk
        /// </summary>
        public void Write(string entityId, IReadOnlyDictionary<string, Optional<string>> properties)
        {
            var retryPolicy = Policy.Handle<IOException>()
                .WaitAndRetry(3, _ => TimeSpan.FromMilliseconds(300));

            var filename = GetFullFilePathFromId(entityId);
            retryPolicy.Execute(SaveFile);
            return;

            void SaveFile()
            {
                using var file = File.CreateText(filename);
                var json = properties
                    .Where(pair => pair.Value.HasValue)
                    .ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrDefault)
                    .ToJson();
                file.Write(json);
                file.Flush();
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

        private string GetFullFilePathFromId(string entityId)
        {
            var filename = $"{CleanFileName(entityId)}.{FileExtension}";
            return Path.Combine(_dirPath, filename);
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
        if (!propertyValue.HasValue
            || propertyValue.Value == LocalMachineJsonFileStore.NullToken)
        {
            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(bool) || targetPropertyType == typeof(bool?)
                                               || targetPropertyType == typeof(Optional<bool>))
        {
            return bool.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(DateTime) || targetPropertyType == typeof(DateTime?)
                                                   || targetPropertyType == typeof(Optional<DateTime>))
        {
            return propertyValue.Value.FromIso8601();
        }

        if (targetPropertyType == typeof(DateTimeOffset) || targetPropertyType == typeof(DateTimeOffset?)
                                                         || targetPropertyType == typeof(Optional<DateTimeOffset>))
        {
            return DateTimeOffset.ParseExact(propertyValue.Value, "O", null).ToUniversalTime();
        }

        if (targetPropertyType == typeof(Guid) || targetPropertyType == typeof(Guid?)
                                               || targetPropertyType == typeof(Optional<Guid>))
        {
            return Guid.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(double) || targetPropertyType == typeof(double?)
                                                 || targetPropertyType == typeof(Optional<double>))
        {
            return double.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(long) || targetPropertyType == typeof(long?)
                                               || targetPropertyType == typeof(Optional<long>))
        {
            return long.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(int) || targetPropertyType == typeof(int?)
                                              || targetPropertyType == typeof(Optional<int>))
        {
            return int.Parse(propertyValue.Value);
        }

        if (targetPropertyType == typeof(byte[]) || targetPropertyType == typeof(Optional<byte[]>))
        {
            return Convert.FromBase64String(propertyValue.Value);
        }

        if (targetPropertyType.IsEnum || targetPropertyType.IsOptionalEnum())
        {
            return Enum.Parse(targetPropertyType, propertyValue.Value, true);
        }

        if (targetPropertyType.IsNullableEnum())
        {
            return propertyValue.HasValue
                ? targetPropertyType.ParseNullable(propertyValue.Value)
                : Optional<object>.None;
        }

        if (Optional.IsOptionalType(targetPropertyType))
        {
            return propertyValue.Value;
        }

        if (targetPropertyType.IsComplexStorageType())
        {
            return propertyValue.ComplexTypeFromContainerProperty(targetPropertyType);
        }

        return propertyValue.Value;
    }
}
#endif