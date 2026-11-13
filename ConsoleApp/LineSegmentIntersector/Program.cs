using LineSegmentIntersection;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

static Point2D PointParser(ArgumentResult result)
{
    string? inputValue = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;

    try
    {
        if (string.IsNullOrWhiteSpace(inputValue))
            throw new Exception("无输入");

        var pointString = inputValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pointString.Length != 2)
            throw new Exception("请确保格式为以逗号分隔的二维坐标，例如 '1,2' 或 '1.2,3.4'。");

        var pointValue = pointString.Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        return new Point2D(pointValue[0], pointValue[1]);
    }
    catch (Exception ex)
    {
        result.AddError($"坐标无效: {ex.Message}");
        return default;
    }
}

var p1Option = new Option<Point2D>("--line1-start", "-p1", "-a")
{
    Required = true,
    Description = "线段1起点",
    CustomParser = PointParser
};
var p2Option = new Option<Point2D>("--line1-end", "-p2", "-b")
{
    Required = true,
    Description = "线段1终点",
    CustomParser = PointParser
};
var p3Option = new Option<Point2D>("--line2-start", "-p3", "-c")
{
    Required = true,
    Description = "线段2起点",
    CustomParser = PointParser
};
var p4Option = new Option<Point2D>("--line2-end", "-p4", "-d")
{
    Required = true,
    Description = "线段2终点",
    CustomParser = PointParser
};

var rootCommand = new RootCommand("计算两条线段的交点")
{
    p1Option,
    p2Option,
    p3Option,
    p4Option,
};

rootCommand.SetAction(parseResult =>
{
    var p1 = parseResult.GetValue(p1Option);
    var p2 = parseResult.GetValue(p2Option);
    var p3 = parseResult.GetValue(p3Option);
    var p4 = parseResult.GetValue(p4Option);

    var (result, overlap) = LineSegmentIntersector.GetIntersection(p1, p2, p3, p4);
    Console.WriteLine($"相交: {result} (重叠标志: {overlap})");

    return 0;
});

return rootCommand.Parse(args).Invoke();
