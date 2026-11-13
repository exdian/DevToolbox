namespace FileTree;

/// <summary>
/// 文件系统扫描器。使用枚举选项 <see cref="EnumerationOptions.IgnoreInaccessible"/> 跳过无权限目录，
/// 并识别重解析点（符号链接 / 目录联接）以防止无限递归。
/// 仅收集 <c>sinceUtc</c> 之后被修改过的文件；不含匹配文件的目录会被剪掉。
/// </summary>
public sealed class FullInfoScanner(string? excludePath, DateTimeOffset sinceUtc)
{
    private static readonly EnumerationOptions s_EnumerationOptions = new()
    {
        RecurseSubdirectories = false,
        IgnoreInaccessible = true,
        AttributesToSkip = 0,               // 隐藏 / 系统文件也要包含
        ReturnSpecialDirectories = false,
        MatchType = MatchType.Simple,
    };

    private readonly string? _excludePath = excludePath;
    private readonly DateTimeOffset _sinceUtc = sinceUtc;

    public int FileCount { get; private set; }

    public int DirectoryCount { get; private set; }

    public FullInfoNode ScanAll(string rootPath)
    {
        var rootDir = new DirectoryInfo(rootPath);
        var node = ScanAllDirectory(rootDir);
        if (node is not null)
        {
            return node;
        }

        // 即使没有任何匹配项，也输出根节点，保证 JSON 结构完整
        DirectoryCount++;
        return CreateDirectoryNode(rootDir, children: [], isSymbolicLink: false);
    }

    public FullInfoNode ScanRecent(string rootPath)
    {
        var rootDir = new DirectoryInfo(rootPath);
        var node = ScanRecentDirectory(rootDir);
        if (node is not null)
        {
            return node;
        }

        // 即使没有任何匹配项，也输出根节点，保证 JSON 结构完整
        DirectoryCount++;
        return CreateDirectoryNode(rootDir, children: [], isSymbolicLink: false);
    }

    private FullInfoNode ScanAllDirectory(DirectoryInfo dir)
    {
        DirectoryCount++;

        var children = new List<FullInfoNode>();

        try
        {
            foreach (var entry in dir.EnumerateFileSystemInfos("*", s_EnumerationOptions))
            {
                switch (entry)
                {
                    case FileInfo file:
                        if (_excludePath is not null && PathsEqual(file.FullName, _excludePath))
                        {
                            continue;
                        }

                        FileCount++;
                        children.Add(CreateFileNode(file));
                        break;

                    // 符号链接 / 联接点：记录下来但不再向下递归
                    case DirectoryInfo link when IsReparsePoint(link):
                        DirectoryCount++;
                        children.Add(CreateDirectoryNode(link, children: null, isSymbolicLink: true));
                        break;

                    case DirectoryInfo sub:
                        children.Add(ScanAllDirectory(sub));
                        break;
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // 目录在枚举过程中被删除或权限不足：忽略剩余内容
        }

        children.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        return CreateDirectoryNode(dir, children, isSymbolicLink: false);
    }

    /// <summary>
    /// 递归扫描目录。若目录（含所有子目录）中不存在匹配文件，则返回 <c>null</c>，
    /// 由调用方决定是否丢弃该目录。
    /// </summary>
    private FullInfoNode? ScanRecentDirectory(DirectoryInfo dir)
    {
        var children = new List<FullInfoNode>();

        try
        {
            foreach (var entry in dir.EnumerateFileSystemInfos("*", s_EnumerationOptions))
            {
                switch (entry)
                {
                    case FileInfo file:
                        {
                            if (_excludePath is not null && PathsEqual(file.FullName, _excludePath))
                            {
                                continue;
                            }

                            var lastWrite = TryGetLastWriteTime(file);
                            if (lastWrite is null || lastWrite < _sinceUtc)
                            {
                                continue;
                            }

                            FileCount++;
                            children.Add(CreateFileNode(file, lastWrite));
                            break;
                        }

                    // 符号链接 / 联接点：记录下来但不再向下递归
                    case DirectoryInfo link when IsReparsePoint(link):
                        {
                            var linkTime = TryGetLastWriteTime(link);
                            if (linkTime is null || linkTime < _sinceUtc)
                            {
                                continue;
                            }

                            DirectoryCount++;
                            children.Add(CreateDirectoryNode(link, children: null, isSymbolicLink: true));
                            break;
                        }

                    case DirectoryInfo sub:
                        {
                            var childNode = ScanRecentDirectory(sub);
                            if (childNode is not null)
                            {
                                children.Add(childNode);
                            }

                            break;
                        }
                }
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // 目录在枚举过程中被删除或权限不足：忽略剩余内容
        }

        if (children.Count == 0)
        {
            // 该目录下没有匹配的文件，也不需要出现在结果中
            return null;
        }

        children.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));
        DirectoryCount++;

        return CreateDirectoryNode(dir, children, isSymbolicLink: false);
    }

    private static FullInfoNode CreateFileNode(FileInfo file) => new()
    {
        Name = file.Name,
        Kind = FileSystemNodeKind.File,
        Extension = file.Extension.Length > 0 ? file.Extension : null,
        Size = TryGetSize(file),
        LastWriteTimeUtc = TryGetLastWriteTime(file),
        Attributes = DescribeAttributes(file),
    };

    private static FullInfoNode CreateFileNode(FileInfo file, DateTimeOffset? lastWrite) => new()
    {
        Name = file.Name,
        Kind = FileSystemNodeKind.File,
        Extension = file.Extension.Length > 0 ? file.Extension : null,
        Size = TryGetSize(file),
        LastWriteTimeUtc = lastWrite,
        Attributes = DescribeAttributes(file),
    };

    private static FullInfoNode CreateDirectoryNode(DirectoryInfo dir, List<FullInfoNode>? children, bool isSymbolicLink)
    {
        return new FullInfoNode()
        {
            // 根目录（如 "C:\"）的 Name 可能为空，退回使用完整路径
            Name = string.IsNullOrEmpty(dir.Name) ? dir.FullName : dir.Name,
            Kind = FileSystemNodeKind.Directory,
            LastWriteTimeUtc = TryGetLastWriteTime(dir),
            IsSymbolicLink = isSymbolicLink,
            Attributes = DescribeAttributes(dir),
            Children = children,
        };
    }

    private static long? TryGetSize(FileInfo file)
    {
        try
        {
            return file.Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static DateTimeOffset? TryGetLastWriteTime(FileSystemInfo info)
    {
        try
        {
            return info.LastWriteTimeUtc;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsReparsePoint(FileSystemInfo info)
    {
        try
        {
            return (info.Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static List<string>? DescribeAttributes(FileSystemInfo info)
    {
        try
        {
            var attributes = info.Attributes;
            if (attributes == 0)
            {
                return null;
            }

            List<string> flags = [];
            foreach (var value in Enum.GetValues<FileAttributes>())
            {
                // Normal 值为 0，HasFlag 恒为 true，需排除
                if (value is FileAttributes.Normal or 0)
                {
                    continue;
                }

                if (attributes.HasFlag(value))
                {
                    flags.Add(value.ToString());
                }
            }

            return flags.Count > 0 ? flags : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.TrimEndingDirectorySeparator(left),
            Path.TrimEndingDirectorySeparator(right),
            comparison);
    }
}
