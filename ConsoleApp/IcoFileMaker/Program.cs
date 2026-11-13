using IcoFile;
using System.CommandLine;
using System.Globalization;

static int[] GetDefaultIconSizes() => [256, 96, 80, 72, 64, 60, 48, 40, 36, 32, 30, 24, 20, 16];

var inputOption = new Option<string>("--input", "-i")
{
    Required = true,
    Description = "要读取的png图片"
};
var outputOption = new Option<string>("--output", "-o")
{
    Required = false,
    Description = "文件输出位置"
};
var sizesOption = new Option<int[]>("--sizes", "-s")
{
    Required = false,
    Description = "图标包含的尺寸",
    CustomParser = result =>
    {
        // 获取原始输入的第一个 token 的值
        string? inputValue = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;

        if (string.IsNullOrWhiteSpace(inputValue))
        {
            // 如果输入为空，返回默认数组
            return GetDefaultIconSizes();
        }

        try
        {
            // 3. 按逗号分割，并逐个转换为 int
            return [.. inputValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.Parse(s, CultureInfo.InvariantCulture))];
        }
        catch (FormatException ex)
        {
            result.AddError($"无法将 '{inputValue}' 解析为整数数组。请确保格式为以逗号分隔的整数，例如 '1,2,3'。错误详情：{ex.Message}");
            return null;
        }
    }
};

var rootCommand = new RootCommand("将png图片转换为ico文件")
{
    inputOption,
    outputOption,
    sizesOption
};

rootCommand.SetAction(parseResult =>
{
    var input = parseResult.GetValue(inputOption);
    var output = parseResult.GetValue(outputOption);
    var sizes = parseResult.GetValue(sizesOption) ?? GetDefaultIconSizes();

    if (string.IsNullOrEmpty(output))
        output = Path.Combine(AppContext.BaseDirectory, $"Icon {DateTime.Now:yyyy-MM-dd HH-mm-ss}.ico");

    string errorMessage = string.Empty;
    try
    {
        if (!File.Exists(input))
        {
            errorMessage = $"错误：文件不存在 - {input}";
            return 1;
        }

        if (!OperatingSystem.IsWindows())
        {
            errorMessage = "错误：仅支持 Windows 平台";
            return 2;
        }

        var iconList = IcoFileMaker.ResizeToPngBytes(input, sizes);
        IcoFileMaker.CreateIcoFile(sizes, iconList, output);

        return 0;
    }
    catch (Exception ex)
    {
        errorMessage = ex.Message;
        return ex.HResult;
    }
    finally
    {
        if (!string.IsNullOrEmpty(errorMessage))
        {
            var originColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(errorMessage);
            Console.ForegroundColor = originColor;
        }
    }
});

return rootCommand.Parse(args).Invoke();
