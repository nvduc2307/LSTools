using PointCanvas = System.Windows.Point;
using VectorCanvas = System.Windows.Vector;

namespace LSTool.Cores.canvas
{
    public static class GeometryInCanvas
    {
        public static List<PointCanvas> OffsetOut(List<PointCanvas> polygon, double offsetValue)
        {
            var qty = polygon.Count;
            var result = new List<PointCanvas>();

            if (qty <= 1) return polygon;

            // Trường hợp 1: Đường thẳng (chỉ có 2 điểm)
            if (qty == 2)
            {
                double dx = polygon[1].X - polygon[0].X;
                double dy = polygon[1].Y - polygon[0].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);

                // Tính pháp tuyến bên trái (-y, x)
                double nx = -dy / len;
                double ny = dx / len;

                result.Add(new PointCanvas(polygon[0].X + nx * offsetValue, polygon[0].Y + ny * offsetValue));
                result.Add(new PointCanvas(polygon[1].X + nx * offsetValue, polygon[1].Y + ny * offsetValue));

                return result;
            }

            // Trường hợp 2: Đa giác khép kín (từ 3 điểm trở lên)
            for (int i = 0; i < qty; i++)
            {
                // Lấy 3 điểm liền kề: Điểm trước, Điểm hiện tại, Điểm tiếp theo
                // Sử dụng modulo để nối điểm cuối vòng lại điểm đầu
                var pPrev = polygon[(i - 1 + qty) % qty];
                var pCurr = polygon[i];
                var pNext = polygon[(i + 1) % qty];

                // 1. Vector cạnh đi vào đỉnh hiện tại (pPrev -> pCurr)
                double v1x = pCurr.X - pPrev.X;
                double v1y = pCurr.Y - pPrev.Y;
                double len1 = Math.Sqrt(v1x * v1x + v1y * v1y);
                v1x /= len1; v1y /= len1;

                // 2. Vector cạnh đi ra khỏi đỉnh hiện tại (pCurr -> pNext)
                double v2x = pNext.X - pCurr.X;
                double v2y = pNext.Y - pCurr.Y;
                double len2 = Math.Sqrt(v2x * v2x + v2y * v2y);
                v2x /= len2; v2y /= len2;

                // 3. Tính Vector pháp tuyến (Normal) của 2 cạnh
                // Giả định trục Y của Canvas hướng xuống, pháp tuyến trái là (-y, x)
                double n1x = -v1y; double n1y = v1x;
                double n2x = -v2y; double n2y = v2x;

                // 4. Tính tích chéo (Cross Product) để kiểm tra độ uốn khúc
                double cross = v1x * v2y - v1y * v2x;

                if (Math.Abs(cross) < 1e-6)
                {
                    // Góc cua cực nhỏ (gần như 3 điểm thẳng hàng)
                    // Ta chỉ cần đẩy điểm hiện tại ra theo phương pháp tuyến
                    result.Add(new PointCanvas(
                        pCurr.X + n1x * offsetValue,
                        pCurr.Y + n1y * offsetValue));
                }
                else
                {
                    // 5. Tìm giao điểm của 2 đường thẳng đã được dịch chuyển (Offset Lines)
                    // Khoảng cách chênh lệch giữa 2 pháp tuyến
                    double dx = (n2x - n1x) * offsetValue;
                    double dy = (n2y - n1y) * offsetValue;

                    // Sử dụng định lý Cramer để giải hệ phương trình tìm khoảng t
                    double t = (dx * v2y - dy * v2x) / cross;

                    // 6. Tính tọa độ đỉnh mới sau khi offset
                    double newX = pCurr.X + n1x * offsetValue + t * v1x;
                    double newY = pCurr.Y + n1y * offsetValue + t * v1y;

                    result.Add(new PointCanvas(newX, newY));
                }
            }

            return result;
        }
        public static PointCanvas Mid(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(0.5*(p1.X + p2.X), 0.5 * (p1.Y + p2.Y)); 
        }
        public static double VtDistance(this PointCanvas p)
        {
            return Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }
        public static PointCanvas Vt(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(p2.X - p1.X, p2.Y - p1.Y);
        }
        public static PointCanvas VtNormal(this PointCanvas p)
        {
            var d = p.VtDistance();
            return new PointCanvas(p.X / d, p.Y / d);
        }
        public static PointCanvas GetVector(this PointCanvas p1, PointCanvas p2)
        {
            return new PointCanvas(p2.X - p1.X, p2.Y - p1.Y);
        }
        public static PointCanvas Rotate(this PointCanvas p, PointCanvas c, double angle)
        {
            var x = (p.X - c.X) * Math.Cos(angle) - (p.Y - c.Y) * Math.Sin(angle) + c.X;
            var y = (p.X - c.X) * Math.Sin(angle) + (p.Y - c.Y) * Math.Cos(angle) + c.Y;
            return new PointCanvas(x, y);
        }
        public static PointCanvas RotateVector(this PointCanvas p, PointCanvas c, double angle)
        {
            var pOri = new PointCanvas();
            var p0 = pOri.Rotate(c, angle);
            var p1 = p.Rotate(c, angle);
            var vt = new PointCanvas(p1.X - p0.X, p1.Y - p0.Y);

            return vt.VtNormal();
        }
        public static PointCanvas Translate(this PointCanvas p, PointCanvas vt)
        {
            return new PointCanvas(p.X + vt.X, p.Y + vt.Y);
        }
        public static PointCanvas RotateAndTranslate(this PointCanvas p, PointCanvas c, double angle, PointCanvas vt)
        {
            var pn = p.Rotate(c, angle);
            return pn.Translate(vt);
        }
    }
}
