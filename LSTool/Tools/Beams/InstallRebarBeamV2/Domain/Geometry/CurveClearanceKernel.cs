using System;
using System.Collections.Generic;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry
{
    /// <summary>
    /// Điểm ba chiều thuần số học, tách khỏi Revit API để phần tính khoảng hở
    /// giữa hai thanh thép kiểm thử được độc lập.
    /// </summary>
    public readonly struct ClearancePoint
    {
        public ClearancePoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public ClearancePoint Subtract(ClearancePoint other)
            => new ClearancePoint(X - other.X, Y - other.Y, Z - other.Z);

        public ClearancePoint Add(ClearancePoint other)
            => new ClearancePoint(X + other.X, Y + other.Y, Z + other.Z);

        public ClearancePoint Scale(double factor)
            => new ClearancePoint(X * factor, Y * factor, Z * factor);

        public double Dot(ClearancePoint other)
            => X * other.X + Y * other.Y + Z * other.Z;

        public double Length => Math.Sqrt(Dot(this));

        public double DistanceTo(ClearancePoint other) => Subtract(other).Length;
    }

    /// <summary>
    /// Tính khoảng hở nhỏ nhất giữa hai đường thép đã được chia nhỏ thành
    /// chuỗi đoạn thẳng.
    ///
    /// Đây là đầu vào của một kiểm tra an toàn: giá trị trả về được phép nhỏ
    /// hơn khoảng cách thật, nhưng tuyệt đối không được lớn hơn. Báo lớn hơn
    /// thật nghĩa là để lọt hai thanh thép chạm nhau.
    /// </summary>
    public static class CurveClearanceKernel
    {
        private const double Epsilon = 1e-12;

        /// <summary>
        /// Khoảng cách nhỏ nhất giữa hai đoạn thẳng có giới hạn. Chính xác
        /// tuyệt đối, không phụ thuộc solver của Revit nên không chết khi hai
        /// đoạn song song.
        /// </summary>
        public static double SegmentDistance(
            ClearancePoint firstStart,
            ClearancePoint firstEnd,
            ClearancePoint secondStart,
            ClearancePoint secondEnd)
        {
            var firstDirection = firstEnd.Subtract(firstStart);
            var secondDirection = secondEnd.Subtract(secondStart);
            var offset = firstStart.Subtract(secondStart);
            var firstLengthSquared = firstDirection.Dot(firstDirection);
            var crossProjection = firstDirection.Dot(secondDirection);
            var secondLengthSquared = secondDirection.Dot(secondDirection);
            var firstOffset = firstDirection.Dot(offset);
            var secondOffset = secondDirection.Dot(offset);
            var denominator =
                firstLengthSquared * secondLengthSquared
                - crossProjection * crossProjection;
            var firstNumerator = denominator;
            var secondNumerator = denominator;
            var firstDenominator = denominator;
            var secondDenominator = denominator;

            if (denominator < Epsilon)
            {
                // Hai đoạn song song hoặc gần song song. Ghim tham số của đoạn
                // thứ nhất rồi chiếu sang đoạn thứ hai; đây chính là trường hợp
                // làm solver của Revit ném lỗi vô số nghiệm.
                firstNumerator = 0.0;
                firstDenominator = 1.0;
                secondNumerator = secondOffset;
                secondDenominator = secondLengthSquared;
            }
            else
            {
                firstNumerator =
                    crossProjection * secondOffset
                    - secondLengthSquared * firstOffset;
                secondNumerator =
                    firstLengthSquared * secondOffset
                    - crossProjection * firstOffset;
                if (firstNumerator < 0.0)
                {
                    firstNumerator = 0.0;
                    secondNumerator = secondOffset;
                    secondDenominator = secondLengthSquared;
                }
                else if (firstNumerator > firstDenominator)
                {
                    firstNumerator = firstDenominator;
                    secondNumerator = secondOffset + crossProjection;
                    secondDenominator = secondLengthSquared;
                }
            }

            if (secondNumerator < 0.0)
            {
                secondNumerator = 0.0;
                if (-firstOffset < 0.0)
                {
                    firstNumerator = 0.0;
                }
                else if (-firstOffset > firstLengthSquared)
                {
                    firstNumerator = firstDenominator;
                }
                else
                {
                    firstNumerator = -firstOffset;
                    firstDenominator = firstLengthSquared;
                }
            }
            else if (secondNumerator > secondDenominator)
            {
                secondNumerator = secondDenominator;
                var adjustedFirstOffset = -firstOffset + crossProjection;
                if (adjustedFirstOffset < 0.0)
                {
                    firstNumerator = 0.0;
                }
                else if (adjustedFirstOffset > firstLengthSquared)
                {
                    firstNumerator = firstDenominator;
                }
                else
                {
                    firstNumerator = adjustedFirstOffset;
                    firstDenominator = firstLengthSquared;
                }
            }

            var firstParameter = Math.Abs(firstNumerator) < Epsilon
                ? 0.0
                : firstNumerator / firstDenominator;
            var secondParameter = Math.Abs(secondNumerator) < Epsilon
                ? 0.0
                : secondNumerator / secondDenominator;
            var closestOffset = offset
                .Add(firstDirection.Scale(firstParameter))
                .Subtract(secondDirection.Scale(secondParameter));
            return closestOffset.Length;
        }

        /// <summary>
        /// Khoảng cách nhỏ nhất giữa hai chuỗi đoạn thẳng.
        /// </summary>
        public static double PolylineDistance(
            IReadOnlyList<ClearancePoint> first,
            IReadOnlyList<ClearancePoint> second)
        {
            ValidatePolyline(first, nameof(first));
            ValidatePolyline(second, nameof(second));

            var minimum = double.MaxValue;
            for (var i = 0; i < first.Count - 1; i++)
            {
                for (var j = 0; j < second.Count - 1; j++)
                {
                    var distance = SegmentDistance(
                        first[i],
                        first[i + 1],
                        second[j],
                        second[j + 1]);
                    if (distance < minimum) minimum = distance;
                    if (minimum <= 0.0) return 0.0;
                }
            }
            return minimum;
        }

        /// <summary>
        /// Độ phồng lớn nhất của cung tròn so với dây cung của nó.
        ///
        /// Dây cung nằm phía lõm, nên cung thật có thể gần đường bên kia hơn
        /// chuỗi đoạn thẳng đúng bằng giá trị này. Trừ nó đi thì kết quả chắc
        /// chắn không bao giờ lớn hơn khoảng cách thật.
        /// </summary>
        public static double SagittaBound(double radius, double chordLength)
        {
            if (!IsFinite(radius) || !IsFinite(chordLength))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (radius <= 0.0 || chordLength <= 0.0) return 0.0;

            var halfChord = chordLength / 2.0;
            if (halfChord >= radius) return radius;
            return radius - Math.Sqrt(radius * radius - halfChord * halfChord);
        }

        /// <summary>Dây cung dài nhất trong một chuỗi đoạn thẳng.</summary>
        public static double MaximumChordLength(IReadOnlyList<ClearancePoint> polyline)
        {
            ValidatePolyline(polyline, nameof(polyline));
            var maximum = 0.0;
            for (var i = 0; i < polyline.Count - 1; i++)
            {
                var chord = polyline[i].DistanceTo(polyline[i + 1]);
                if (chord > maximum) maximum = chord;
            }
            return maximum;
        }

        /// <summary>
        /// Khoảng hở giữa hai đường cong, đã trừ cận sai số của phép chia nhỏ
        /// nên chắc chắn không lớn hơn khoảng cách thật. Truyền bán kính 0 cho
        /// đường thẳng, vì đường thẳng không có sai số chia nhỏ.
        /// </summary>
        public static double ConservativeDistance(
            IReadOnlyList<ClearancePoint> first,
            double firstRadius,
            IReadOnlyList<ClearancePoint> second,
            double secondRadius)
        {
            var distance = PolylineDistance(first, second);
            var margin =
                SagittaBound(firstRadius, MaximumChordLength(first))
                + SagittaBound(secondRadius, MaximumChordLength(second));
            var result = distance - margin;
            return result > 0.0 ? result : 0.0;
        }

        private static void ValidatePolyline(
            IReadOnlyList<ClearancePoint> polyline,
            string name)
        {
            if (polyline == null) throw new ArgumentNullException(name);
            if (polyline.Count < 2)
                throw new ArgumentException(
                    "A polyline needs at least two points.", name);
            foreach (var point in polyline)
            {
                if (!IsFinite(point.X) || !IsFinite(point.Y) || !IsFinite(point.Z))
                    throw new ArgumentException(
                        "A polyline point is not finite.", name);
            }
        }

        private static bool IsFinite(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
