namespace LineSegmentIntersection;

public static class LineSegmentIntersector
{
    private const double Epsilon = 1e-9;

    /// <summary>
    /// 计算两条线段的交点
    /// </summary>
    /// <param name="a">线段1起点</param>
    /// <param name="b">线段1终点</param>
    /// <param name="c">线段2起点</param>
    /// <param name="d">线段2终点</param>
    /// <returns>
    /// Intersection: 交点坐标（若存在唯一交点）；否则为 null
    /// IsOverlapping: true 表示共线且重叠（有无穷多交点）；false 表示不相交、平行或共线不重叠
    /// </returns>
    public static (Point2D? Intersection, bool IsOverlapping) GetIntersection(
        Point2D a, Point2D b, Point2D c, Point2D d)
    {
        // 方向向量
        var v1 = new Point2D(b.X - a.X, b.Y - a.Y);
        var v2 = new Point2D(d.X - c.X, d.Y - c.Y);
        var ca = new Point2D(c.X - a.X, c.Y - a.Y);

        // 叉积 (Cross Product)
        double denom = Cross(v1, v2);

        // 情况1：平行（包含共线）
        if (Math.Abs(denom) < Epsilon)
        {
            // 检查是否共线：向量 ca 与 v1 的叉积是否为 0
            if (Math.Abs(Cross(ca, v1)) < Epsilon)
            {
                // 共线，检查包围盒 (AABB) 是否重叠
                bool overlapX = !(Math.Max(a.X, b.X) < Math.Min(c.X, d.X) ||
                                  Math.Max(c.X, d.X) < Math.Min(a.X, b.X));
                bool overlapY = !(Math.Max(a.Y, b.Y) < Math.Min(c.Y, d.Y) ||
                                  Math.Max(c.Y, d.Y) < Math.Min(a.Y, b.Y));

                if (overlapX && overlapY)
                {
                    return (null, true); // 重叠，存在无穷多交点
                }
                return (null, false); // 共线但不重叠，或平行不共线
            }
            return (null, false); // 平行不共线
        }

        // 情况2：不平行，计算参数 t 和 s
        double t = Cross(ca, v2) / denom;
        double s = Cross(ca, v1) / denom;

        // 判断交点是否同时落在两条线段上 (t 和 s 必须在 [0, 1] 范围内)
        if (t >= 0 && t <= 1 && s >= 0 && s <= 1)
        {
            double ix = a.X + t * v1.X;
            double iy = a.Y + t * v1.Y;
            return (new Point2D(ix, iy), false);
        }

        return (null, false); // 直线相交，但交点不在线段上
    }

    // 二维叉积辅助函数
    private static double Cross(Point2D p1, Point2D p2)
    {
        return p1.X * p2.Y - p1.Y * p2.X;
    }
}
