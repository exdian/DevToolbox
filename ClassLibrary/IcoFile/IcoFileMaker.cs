using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace IcoFile;

public static class IcoFileMaker
{
    [SupportedOSPlatform("windows")]
    public static IReadOnlyList<byte[]> ResizeToPngBytes(string imagePath, int[] sizes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);
        ArgumentNullException.ThrowIfNull(sizes);

        if (!File.Exists(imagePath))
            throw new FileNotFoundException("图片文件不存在。", imagePath);

        if (sizes.Length == 0)
            return [];

        // 直接加载原图，后续每次缩放都基于它重新创建目标位图
        using var original = Image.FromFile(imagePath);

        var results = new List<byte[]>(sizes.Length);

        foreach (var size in sizes)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(sizes), "尺寸必须大于 0。");

            // 按最长边等比计算目标宽高，保持与 ResizeMode.Max 一致的语义
            var ratio = (double)size / Math.Max(original.Width, original.Height);
            var destWidth = Math.Max(1, (int)Math.Round(original.Width * ratio));
            var destHeight = Math.Max(1, (int)Math.Round(original.Height * ratio));

            // 关键：必须使用 Format32bppArgb，否则透明背景会变成黑色
            using var resized = new Bitmap(destWidth, destHeight, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(resized))
            {
                // 清空为透明背景（新 Bitmap 默认可能是黑色）
                graphics.Clear(Color.Transparent);

                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingMode = CompositingMode.SourceCopy; // 直接覆盖 Alpha，避免混合导致的边缘异常
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                // 消除缩放时边缘可能出现的残影
                using var attributes = new ImageAttributes();
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                graphics.DrawImage(
                    original,
                    new Rectangle(0, 0, destWidth, destHeight),
                    0, 0, original.Width, original.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }

            using var ms = new MemoryStream();
            // PNG 是无损格式，无需 EncoderParameters 设置质量
            resized.Save(ms, ImageFormat.Png);
            results.Add(ms.ToArray());
        }

        return results;
    }

    public static void CreateIcoFile(int[] sizes, IReadOnlyList<byte[]> iconList, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // 流式写入
        using var fileStream = new FileStream(outputPath, FileMode.Create);
        using var writer = new BinaryWriter(fileStream);

        // 文件头，6 字节
        writer.Write((short)0);
        writer.Write((short)1);
        writer.Write((short)iconList.Count);

        // 图标目录，目录中每个条目 16 字节对应一个图标
        var imageOffsets = new List<int>(iconList.Count);
        var currentOffset = 6 + (16 * iconList.Count); // 头大小 + 目录项
        for (int i = 0; i < iconList.Count; i++) // 计算每个图像的偏移量
        {
            imageOffsets.Add(currentOffset);
            currentOffset += iconList[i].Length;
        }
        for (int i = 0; i < iconList.Count; i++) // 写入目录项
        {
            writer.Write((byte)sizes[i]);     // 宽度，256 的尺寸会被截断为 0，刚好符合文件格式
            writer.Write((byte)sizes[i]);     // 高度
            writer.Write((byte)0);            // 颜色数
            writer.Write((byte)0);            // 保留
            writer.Write((short)1);           // 颜色平面
            writer.Write((short)32);          // 每像素位数
            writer.Write(iconList[i].Length); // 数据大小
            writer.Write(imageOffsets[i]);    // 数据偏移
        }

        // 图像数据
        for (int i = 0; i < iconList.Count; i++) // 写入图像数据
            writer.Write(iconList[i]);
    }
}
