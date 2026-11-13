namespace LineSegmentIntersection;

public struct Point2D(double x, double y)
{
    public double X = x;
    public double Y = y;

    public override readonly string ToString() => $"({X:F4}, {Y:F4})";
}
