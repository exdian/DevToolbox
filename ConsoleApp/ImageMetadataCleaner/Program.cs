using ImageFunction;
using System.CommandLine;

var inputOption = new Option<string>("--input", "-i")
{
    Required = true,
    Description = "要读取的图片"
};
var outputOption = new Option<string>("--output", "-o")
{
    Required = false,
    Description = "png图片输出位置"
};

var rootCommand = new RootCommand("重新保存图片为无元数据的png")
{
    inputOption,
    outputOption,
};

rootCommand.SetAction(parseResult =>
{
    var input = parseResult.GetValue(inputOption);
    var output = parseResult.GetValue(outputOption);

    string errorMessage = string.Empty;
    try
    {
        if (!OperatingSystem.IsWindows())
        {
            errorMessage = "错误：仅支持 Windows 平台";
            return 2;
        }

        if (!File.Exists(input))
        {
            errorMessage = $"错误：文件不存在 - {input}";
            return 1;
        }

        if (string.IsNullOrEmpty(output))
        {
            string dir = string.IsNullOrEmpty(input)
                ? AppContext.BaseDirectory
                : Path.GetDirectoryName(Path.GetFullPath(input)) ?? AppContext.BaseDirectory;

            output = Path.Combine(dir, $"Image {DateTime.Now:yyyy-MM-dd HH-mm-ss}.png");
        }

        ImageSave.SaveAsPng(input, output);

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
