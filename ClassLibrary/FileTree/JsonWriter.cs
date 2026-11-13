using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileTree;

public static class JsonWriter
{
    public static async Task WriteJsonAsync(string outputPath, FullInfoNode root)
    {
        await using var stream = GetStream(outputPath);

        // 源生成器版本，零反射、AOT / Trim 友好
        await JsonSerializer.SerializeAsync(stream, root, FullInfoJsonContext.Default.FullInfoNode);
    }

    public static async Task WriteJsonAsync(string outputPath, SimpleInfoNode root)
    {
        await using var stream = GetStream(outputPath);

        // 源生成器版本，零反射、AOT / Trim 友好
        await JsonSerializer.SerializeAsync(stream, root, SimpleInfoJsonContext.Default.SimpleInfoNode);
    }

    private static FileStream GetStream(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1 << 16,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }
}

public enum FileSystemNodeKind
{
    Directory,
    File,
}

/// <summary>文件系统中的一个节点（目录或文件）。</summary>
public sealed class FullInfoNode
{
    public required string Name { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<FileSystemNodeKind>))]
    public required FileSystemNodeKind Kind { get; init; }

    /// <summary>文件扩展名（含点号），仅文件有值。</summary>
    public string? Extension { get; init; }

    /// <summary>文件字节大小，仅文件有值。</summary>
    public long? Size { get; init; }

    public DateTimeOffset? LastWriteTimeUtc { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSymbolicLink { get; init; }

    public List<string>? Attributes { get; init; }

    /// <summary>子节点。目录为数组（可能为空），文件为 null 且不输出。</summary>
    public List<FullInfoNode>? Children { get; init; }
}

/// <summary>
/// System.Text.Json 源生成上下文：编译期生成序列化代码，
/// 无需反射，兼容 NativeAOT / 裁剪，启动更快。
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NewLine = "\n")]
[JsonSerializable(typeof(FullInfoNode))]
internal sealed partial class FullInfoJsonContext : JsonSerializerContext;

/// <summary>树节点：一个名字，加子文件夹和文件。</summary>
public sealed class SimpleInfoNode
{
    public required string Name { get; init; }

    /// <summary>子文件夹，无则为 null 且不输出。</summary>
    public List<SimpleInfoNode>? Dirs { get; set; }

    /// <summary>文件（仅名字），无则为 null 且不输出。</summary>
    public List<SimpleInfoNode>? Files { get; set; }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SimpleInfoNode))]
internal sealed partial class SimpleInfoJsonContext : JsonSerializerContext;
