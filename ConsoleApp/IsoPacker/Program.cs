using IsoFile;
using System.CommandLine;

var inputOption = new Option<string[]>("--input", "-i")
{
    AllowMultipleArgumentsPerToken = true,
    Required = true,
    Description = "要打包的文件和文件夹"
};
var outputOption = new Option<string>("--output", "-o")
{
    Required = false,
    Description = "iso文件输出位置"
};

var rootCommand = new RootCommand("将文件和文件夹打包为iso文件")
{
    inputOption,
    outputOption,
};

rootCommand.SetAction(parseResult =>
{
    var input = parseResult.GetValue(inputOption) ?? [];
    var output = parseResult.GetValue(outputOption);

    if (string.IsNullOrEmpty(output))
        output = Path.Combine(AppContext.BaseDirectory, $"IsoFile {DateTime.Now:yyyy-MM-dd HH-mm-ss}.iso");

    string errorMessage = string.Empty;
    try
    {
        if (!OperatingSystem.IsWindows())
        {
            errorMessage = "错误：仅支持 Windows 平台";
            return 2;
        }

        Console.WriteLine("正在打包...");
        IsoBuilder.BuildIso(input, output);
        Console.WriteLine("打包完成");
        Console.WriteLine($"ISO 文件已保存到：\n{Path.GetFullPath(output)}");

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
