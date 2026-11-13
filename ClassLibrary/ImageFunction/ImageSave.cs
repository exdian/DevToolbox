using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;

namespace ImageFunction;

[SupportedOSPlatform("windows")]
public static class ImageSave
{
    public static void SaveAsPng(string inputFile, string outputFile)
    {
        using var originalImage = Image.FromFile(inputFile);
        using var cleanBitmap = new Bitmap(originalImage);

        // 将新 Bitmap 的 DPI 设置回原图的值
        cleanBitmap.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

        cleanBitmap.Save(outputFile, ImageFormat.Png);
    }
}
