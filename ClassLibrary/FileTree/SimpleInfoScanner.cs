namespace FileTree;

public static class SimpleInfoScanner
{
    /// <summary>
    /// 递归扫描目录，构建树。excludePath 用于跳过输出文件自身。
    /// </summary>
    public static SimpleInfoNode Scan(string path, string? excludePath)
    {
        var name = Path.GetFileName(path);
        // 盘符根目录（如 "C:\"）的 GetFileName 返回空，用完整路径兜底
        if (string.IsNullOrEmpty(name))
        {
            name = path;
        }

        var node = new SimpleInfoNode { Name = name };

        try
        {
            foreach (var dir in Directory.EnumerateDirectories(path))
            {
                // 跳过符号链接 / 目录联接，避免无限递归
                if (IsReparsePoint(dir))
                {
                    node.Dirs?.Add(new SimpleInfoNode { Name = Path.GetFileName(dir) });
                    continue;
                }

                (node.Dirs ??= []).Add(Scan(dir, excludePath));
            }

            foreach (var file in Directory.EnumerateFiles(path))
            {
                if (excludePath is not null && PathsEqual(file, excludePath))
                {
                    continue;
                }

                (node.Files ??= []).Add(new SimpleInfoNode { Name = Path.GetFileName(file) });
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // 权限不足或目录被删除，忽略剩余内容
        }

        return node;
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
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
