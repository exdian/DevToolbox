using FileTree;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Text;

var rootPathOption = new Option<string>("--rootPath", "-r")
{
    Required = false,
    Description = "要扫描的目录"
};
var outputOption = new Option<string>("--output", "-o")
{
    Required = false,
    Description = "json文件输出位置"
};
var sinceOption = new Option<int>("--since", "-s")
{
    Required = true,
    Description = "时间窗口（秒）"
};

var fullInfoCommand = new Command("full-info", "输出详细的信息")
{
    rootPathOption,
    outputOption
};
var simpleInfoCommand = new Command("simple-info", "输出仅保留名称信息")
{
    rootPathOption,
    outputOption
};
var recentFullInfoCommand = new Command("recent-full-info", "只输出时间窗口内修改过的详细信息")
{
    rootPathOption,
    outputOption,
    sinceOption
};
var rootCommand = new RootCommand("获取目录结构并输出为json文件")
{
    fullInfoCommand,
    simpleInfoCommand,
    recentFullInfoCommand,
};

rootCommand.Validators.Add(result =>
{
    int usedCount = result.Children
        .OfType<CommandResult>()
        .Count(cr => cr.Command == fullInfoCommand ||
                     cr.Command == simpleInfoCommand ||
                     cr.Command == recentFullInfoCommand);

    if (usedCount != 1)
    {
        result.AddError("必须且只能指定 'full-info'、'simple-info' 或 'recent-full-info' 中的一个命令。");
    }
});

async Task<int> ExecuteCommand(ParseResult parseResult, Func<string, string, Task<int>> action)
{
    var rootPath = parseResult.GetValue(rootPathOption);
    var outputPath = parseResult.GetValue(outputOption);

    if (string.IsNullOrEmpty(rootPath))
        rootPath = AppContext.BaseDirectory;
    if (string.IsNullOrEmpty(outputPath))
        outputPath = Path.Combine(AppContext.BaseDirectory, $"tree {DateTime.Now:yyyy-MM-dd HH-mm-ss}.json");

    try
    {
        Console.OutputEncoding = Encoding.UTF8;
    }
    catch (IOException)
    {
        // 某些重定向场景下无法设置编码，忽略即可
    }

    string errorMessage = string.Empty;
    try
    {
        if (!Directory.Exists(rootPath))
        {
            errorMessage = $"错误：目录不存在 - {rootPath}";
            return 1;
        }

        return await action.Invoke(rootPath, outputPath);
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
}

fullInfoCommand.SetAction(async parseResult =>
{
    return await ExecuteCommand(parseResult, async (rootPath, outputPath) =>
    {
        Console.WriteLine("正在获取目录结构完整信息");
        Console.WriteLine($"扫描目录：{rootPath}");
        Console.WriteLine(new string('-', 60));

        // 输出文件本身要从扫描结果中排除，避免“自包含”
        var scanner = new FullInfoScanner(excludePath: outputPath, sinceUtc: DateTimeOffset.MaxValue);

        var stopwatch = Stopwatch.StartNew();
        var root = scanner.ScanAll(rootPath);
        stopwatch.Stop();

        Console.WriteLine($"目录数量：{scanner.DirectoryCount:N0}");
        Console.WriteLine($"文件数量：{scanner.FileCount:N0}");
        Console.WriteLine($"扫描耗时：{stopwatch.ElapsedMilliseconds:N0} ms");

        await JsonWriter.WriteJsonAsync(outputPath, root);

        var info = new FileInfo(outputPath);
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"JSON 文件：{info.FullName}");
        Console.WriteLine($"文件大小：{info.Length:N0} 字节");

        return 0;
    });
});

simpleInfoCommand.SetAction(async parseResult =>
{
    return await ExecuteCommand(parseResult, async (rootPath, outputPath) =>
    {
        Console.WriteLine("正在获取目录结构简单信息");
        Console.WriteLine($"扫描目录：{rootPath}");
        Console.WriteLine(new string('-', 60));

        var stopwatch = Stopwatch.StartNew();
        var root = SimpleInfoScanner.Scan(rootPath, outputPath);
        stopwatch.Stop();

        Console.WriteLine($"扫描耗时：{stopwatch.ElapsedMilliseconds:N0} ms");

        await JsonWriter.WriteJsonAsync(outputPath, root);

        var info = new FileInfo(outputPath);
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"JSON 文件：{info.FullName}");
        Console.WriteLine($"文件大小：{info.Length:N0} 字节");

        return 0;
    });
});

recentFullInfoCommand.SetAction(async parseResult =>
{
    var since = parseResult.GetValue(sinceOption);

    return await ExecuteCommand(parseResult, async (rootPath, outputPath) =>
    {
        // 以扫描开始的时刻为基准，计算时间窗口起点
        var sinceUtc = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(since);

        Console.WriteLine("正在获取目录结构完整信息");
        Console.WriteLine($"扫描目录：{rootPath}");
        Console.WriteLine($"时间窗口：{sinceUtc:yyyy/MM/dd HH:mm:ss} (UTC) 之后修改的文件");
        Console.WriteLine(new string('-', 60));

        // 输出文件本身要从扫描结果中排除，避免“自包含”
        var scanner = new FullInfoScanner(excludePath: outputPath, sinceUtc: DateTimeOffset.MaxValue);

        var stopwatch = Stopwatch.StartNew();
        var root = scanner.ScanAll(rootPath);
        stopwatch.Stop();

        Console.WriteLine($"目录数量：{scanner.DirectoryCount:N0}");
        Console.WriteLine($"文件数量：{scanner.FileCount:N0}");
        Console.WriteLine($"扫描耗时：{stopwatch.ElapsedMilliseconds:N0} ms");

        await JsonWriter.WriteJsonAsync(outputPath, root);

        var info = new FileInfo(outputPath);
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"JSON 文件：{info.FullName}");
        Console.WriteLine($"文件大小：{info.Length:N0} 字节");

        return 0;
    });
});

return await rootCommand.Parse(args).InvokeAsync();
