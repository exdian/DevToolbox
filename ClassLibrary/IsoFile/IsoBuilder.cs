using DiscUtils.Iso9660;

namespace IsoFile;

public static class IsoBuilder
{
    public static void BuildIso(IReadOnlyList<string> sourcePaths, string isoPath)
    {
        var builder = new CDBuilder
        {
            UseJoliet = true,               // 启用 Joliet，支持长文件名和 Unicode
            //VolumeIdentifier = "MY_ISO"     // 卷标，可按需修改
        };

        // 用于避免重复添加同名文件
        var addedEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in sourcePaths)
        {
            if (File.Exists(path))
            {
                AddFileToIso(builder, path, Path.GetFileName(path), addedEntries);
            }
            else if (Directory.Exists(path))
            {
                AddDirectoryToIso(builder, path, Path.GetFileName(path), addedEntries);
            }
        }

        builder.Build(isoPath);
    }

    private static void AddFileToIso(CDBuilder builder, string filePath, string isoEntryPath, HashSet<string> addedEntries)
    {
        isoEntryPath = isoEntryPath.Replace('/', '\\');
        if (!addedEntries.Add(isoEntryPath))
            return; // 已存在同名条目，跳过

        builder.AddFile(isoEntryPath, filePath);
    }

    private static void AddDirectoryToIso(CDBuilder builder, string sourceDir, string isoRootName, HashSet<string> addedEntries)
    {
        // 递归获取所有文件
        string[] allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);

        foreach (string file in allFiles)
        {
            // 计算该文件相对于 sourceDir 的相对路径
            string relative = Path.GetRelativePath(sourceDir, file);

            // ISO 内路径：isoRootName\relative\filename
            string isoEntryPath = Path.Combine(isoRootName, relative).Replace('/', '\\');

            if (addedEntries.Add(isoEntryPath))
            {
                builder.AddFile(isoEntryPath, file);
            }
        }

        // 即使文件夹为空，也确保文件夹本身被创建
        if (allFiles.Length == 0)
        {
            builder.AddDirectory(isoRootName);
        }
    }
}
