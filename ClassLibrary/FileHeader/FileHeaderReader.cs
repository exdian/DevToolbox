using System.Text;

namespace FileHeader;

public static class FileHeaderReader
{
    public static void Read(string directory, Action<string[]> onOutput)
    {
        // 获取该目录下所有文件（不包括子目录）
        string[] files = Directory.GetFiles(directory);

        foreach (string filePath in files)
        {
            try
            {
                byte[] header = new byte[16];
                int bytesRead;

                // 读取文件前 16 字节
                using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytesRead = fs.Read(header, 0, header.Length);
                }

                // 构建十六进制字符串和 ASCII 字符串
                StringBuilder hexBuilder = new();
                StringBuilder asciiBuilder = new();

                for (int i = 0; i < bytesRead; i++)
                {
                    hexBuilder.Append(header[i].ToString("X2") + " ");

                    // 可打印 ASCII 字符范围 32~126
                    if (header[i] >= 32 && header[i] <= 126)
                        asciiBuilder.Append((char)header[i]);
                    else
                        asciiBuilder.Append('.');
                }

                // 输出结果
                onOutput.Invoke(
                    [$"文件: {Path.GetFileName(filePath)}",
                    $"Hex : {hexBuilder.ToString().TrimEnd()}",
                    $"ASCII: {asciiBuilder}"]);
            }
            catch (Exception ex)
            {
                // 如果文件无法访问，输出错误信息并继续处理下一个文件
                onOutput.Invoke([$"文件: {Path.GetFileName(filePath)} - 错误: {ex.Message}"]);
            }
        }
    }
}
