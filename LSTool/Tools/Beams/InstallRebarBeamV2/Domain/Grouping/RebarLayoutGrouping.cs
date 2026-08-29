using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping
{
    /// <summary>
    /// Điểm ba chiều thuần số học, tách khỏi Revit API để phần chia nhóm thép
    /// kiểm thử được độc lập.
    /// </summary>
    public readonly struct RebarGroupingPoint
    {
        public RebarGroupingPoint(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public double DistanceTo(RebarGroupingPoint other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            var dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public double Dot(RebarGroupingPoint other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public RebarGroupingPoint Subtract(RebarGroupingPoint other)
        {
            return new RebarGroupingPoint(X - other.X, Y - other.Y, Z - other.Z);
        }

        public RebarGroupingPoint Scale(double factor)
        {
            return new RebarGroupingPoint(X * factor, Y * factor, Z * factor);
        }

        public double Length => Math.Sqrt(Dot(this));
    }

    /// <summary>
    /// Một thanh thép ứng viên: hình học đã quy về gốc thanh, cùng normal của
    /// mặt phẳng thanh - đây là trục mà rebar set sẽ trải theo.
    /// </summary>
    public sealed class RebarGroupingBar
    {
        public RebarGroupingBar(
            int originalIndex,
            string bucketKey,
            RebarGroupingPoint normal,
            RebarGroupingPoint origin,
            IEnumerable<RebarGroupingPoint> relativePoints)
        {
            if (relativePoints == null)
                throw new ArgumentNullException(nameof(relativePoints));

            OriginalIndex = originalIndex;
            BucketKey = bucketKey
                ?? throw new ArgumentNullException(nameof(bucketKey));
            Normal = normal;
            Origin = origin;
            RelativePoints = relativePoints.ToList();
        }

        public int OriginalIndex { get; }
        public string BucketKey { get; }
        public RebarGroupingPoint Normal { get; }
        public RebarGroupingPoint Origin { get; }
        public IReadOnlyList<RebarGroupingPoint> RelativePoints { get; }
    }

    /// <summary>
    /// Chia các thanh thép rời thành những chuỗi có thể chuyển sang một rebar
    /// set bố trí Fixed Number: cùng bucket, hình học trùng khít sau khi tịnh
    /// tiến, tịnh tiến dọc theo normal, và cách đều nhau.
    /// </summary>
    public static class RebarLayoutGrouping
    {
        private const double NumericTolerance = 1e-12;
        private const double ParallelDotTolerance = 1e-6;

        /// <summary>
        /// Trả về các chuỗi thanh gom được, mỗi chuỗi là danh sách
        /// OriginalIndex đã sắp xếp dọc theo normal. Thanh không gom được
        /// không xuất hiện trong kết quả và phải giữ nguyên layout Single.
        /// </summary>
        public static List<List<int>> BuildUniformRuns(
            IReadOnlyList<RebarGroupingBar> bars,
            double tolerance,
            int minimumBarsPerGroup)
        {
            if (bars == null)
                throw new ArgumentNullException(nameof(bars));
            if (!IsFinite(tolerance) || tolerance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(tolerance));
            if (minimumBarsPerGroup < 2)
                throw new ArgumentOutOfRangeException(nameof(minimumBarsPerGroup));

            ValidateBars(bars);
            if (bars.Count == 0) return new List<List<int>>();

            var runs = new List<List<int>>();
            foreach (var bucket in bars.GroupBy(bar => bar.BucketKey, StringComparer.Ordinal))
            {
                var members = bucket.ToList();
                foreach (var cluster in BuildEquivalentClusters(members, tolerance))
                {
                    if (cluster.Count < minimumBarsPerGroup) continue;
                    runs.AddRange(SplitIntoUniformRuns(
                        cluster,
                        tolerance,
                        minimumBarsPerGroup));
                }
            }
            return runs;
        }

        private static void ValidateBars(IReadOnlyList<RebarGroupingBar> bars)
        {
            var seen = new HashSet<int>();
            foreach (var bar in bars)
            {
                if (bar == null)
                    throw new InvalidOperationException(
                        "The rebar grouping input contains a null bar.");
                if (!seen.Add(bar.OriginalIndex))
                    throw new InvalidOperationException(
                        $"Duplicate original rebar index {bar.OriginalIndex}.");
                if (bar.RelativePoints.Count < 2)
                    throw new InvalidOperationException(
                        $"Rebar {bar.OriginalIndex} needs at least two centerline points.");
                if (!IsFinite(bar.Normal.X) || !IsFinite(bar.Normal.Y) || !IsFinite(bar.Normal.Z))
                    throw new InvalidOperationException(
                        $"Rebar {bar.OriginalIndex} has an invalid normal.");
                if (bar.Normal.Length <= NumericTolerance)
                    throw new InvalidOperationException(
                        $"Rebar {bar.OriginalIndex} has a degenerate normal.");
            }
        }

        /// <summary>
        /// Gom các thanh có hình học trùng khít sau khi tịnh tiến, với điều
        /// kiện hướng tịnh tiến song song normal của thanh neo.
        /// </summary>
        private static List<List<RebarGroupingBar>> BuildEquivalentClusters(
            List<RebarGroupingBar> bucket,
            double tolerance)
        {
            var clusters = new List<List<RebarGroupingBar>>();
            foreach (var bar in bucket)
            {
                var matched = false;
                foreach (var cluster in clusters)
                {
                    var anchor = cluster[0];
                    if (!HasSameShape(anchor, bar, tolerance)) continue;
                    if (!IsAlignedWithNormal(anchor, bar, tolerance)) continue;
                    cluster.Add(bar);
                    matched = true;
                    break;
                }
                if (!matched) clusters.Add(new List<RebarGroupingBar> { bar });
            }
            return clusters;
        }

        /// <summary>
        /// Cắt một cụm thanh tương đương thành các chuỗi cách đều liên tiếp dài
        /// nhất có thể. Thanh phá nhịp (ví dụ thanh bù dư cuối đoạn) chỉ đơn
        /// giản là rơi ra ngoài mọi chuỗi.
        /// </summary>
        private static List<List<int>> SplitIntoUniformRuns(
            List<RebarGroupingBar> cluster,
            double tolerance,
            int minimumBarsPerGroup)
        {
            var axis = Normalize(cluster[0].Normal);
            var ordered = cluster
                .Select(bar => new
                {
                    Bar = bar,
                    Projection = bar.Origin.Dot(axis)
                })
                .OrderBy(entry => entry.Projection)
                .ThenBy(entry => entry.Bar.OriginalIndex)
                .ToList();

            var runs = new List<List<int>>();
            var index = 0;
            while (index < ordered.Count - 1)
            {
                var step = ordered[index + 1].Projection - ordered[index].Projection;
                if (step <= tolerance)
                {
                    // Hai thanh chồng nhau, không thể coi là một mảng đều.
                    index++;
                    continue;
                }

                var last = index + 1;
                while (last + 1 < ordered.Count)
                {
                    var nextStep = ordered[last + 1].Projection - ordered[last].Projection;
                    if (Math.Abs(nextStep - step) > tolerance) break;
                    last++;
                }

                var length = last - index + 1;
                if (length >= minimumBarsPerGroup)
                {
                    runs.Add(ordered
                        .GetRange(index, length)
                        .Select(entry => entry.Bar.OriginalIndex)
                        .ToList());
                    index = last + 1;
                }
                else
                {
                    index++;
                }
            }
            return runs;
        }

        private static bool HasSameShape(
            RebarGroupingBar left,
            RebarGroupingBar right,
            double tolerance)
        {
            if (left.RelativePoints.Count != right.RelativePoints.Count) return false;
            var leftNormal = Normalize(left.Normal);
            var rightNormal = Normalize(right.Normal);
            if (leftNormal.Dot(rightNormal) < 1.0 - ParallelDotTolerance) return false;
            for (var index = 0; index < left.RelativePoints.Count; index++)
            {
                if (left.RelativePoints[index].DistanceTo(right.RelativePoints[index])
                    > tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsAlignedWithNormal(
            RebarGroupingBar anchor,
            RebarGroupingBar candidate,
            double tolerance)
        {
            var axis = Normalize(anchor.Normal);
            var delta = candidate.Origin.Subtract(anchor.Origin);
            var alongAxis = axis.Scale(delta.Dot(axis));
            return delta.Subtract(alongAxis).Length <= tolerance;
        }

        private static RebarGroupingPoint Normalize(RebarGroupingPoint value)
        {
            var length = value.Length;
            return length <= NumericTolerance ? value : value.Scale(1.0 / length);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
