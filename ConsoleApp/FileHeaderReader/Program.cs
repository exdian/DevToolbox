using FileHeader;
using System.CommandLine;

var directoryOption = new Option<string>("--directory", "-d")
{
    Required = false,
    Description = "要读取的目录"
};

var rootCommand = new RootCommand("获取目录下所有文件（不包括子目录）的前16字节") { directoryOption };

rootCommand.SetAction(parseResult =>
{
    var directory = parseResult.GetValue(directoryOption);
    if (string.IsNullOrEmpty(directory))
        directory = AppContext.BaseDirectory;

    Console.WriteLine($"正在获取 '{directory}' 目录下所有文件（不包括子目录）的前16字节");
    Console.WriteLine();

    try
    {
        FileHeaderReader.Read(directory, o =>
        {
            foreach (var message in o)
            {
                Console.WriteLine(message);
            }
            Console.WriteLine();
        });

        Console.WriteLine("扫描完成，按任意键退出...");
        Console.ReadKey();
        return 0;
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

return rootCommand.Parse(args).Invoke();
