using CSharpCode;
using System.CommandLine;

var rootPathOption = new Option<string>("--root-path", "-r")
{
    Required = true,
    Description = "输入目录",
};
var markerFileNameOption = new Option<string>("--marker-file-name", "-m")
{
    Required = false,
    Description = "如果指定了标记文件，将检查其是否存在",
};
var outputPathOption = new Option<string>("--output-path", "-o")
{
    Required = false,
    Description = "输出目录",
};

var rootCommand = new RootCommand("提取C#代码中的所有公共类型和公共成员声明")
{
    rootPathOption,
    markerFileNameOption,
    outputPathOption,
};

rootCommand.SetAction(parseResult =>
{
    var rootPath = parseResult.GetValue(rootPathOption);
    var markerFileName = parseResult.GetValue(markerFileNameOption);
    var outputPath = parseResult.GetValue(outputPathOption);

    if (string.IsNullOrEmpty(outputPath))
        outputPath = Path.Combine(AppContext.BaseDirectory, $"Api {DateTime.Now:yyyy-MM-dd HH-mm-ss}");

    try
    {
        var count = PublicApiExtractor.ExtractFromPath(rootPath!, markerFileName, outputPath, Console.WriteLine);
        return count > 0 ? 0 : 1;
    }
    catch (Exception ex)
    {
        var originColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(ex.Message);
        Console.ForegroundColor = originColor;
        return ex.HResult;
    }
});

//args = "-r D:\\Code\\DiantyCore -m DiantyCore.slnx -o DiantyCore"
//    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
return rootCommand.Parse(args).Invoke();
