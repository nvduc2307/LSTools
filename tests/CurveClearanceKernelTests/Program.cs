using System;
using System.Collections.Generic;
using System.Linq;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Geometry;

internal static class Program
{
    private const double Tolerance = 1e-9;

    private static int Main()
    {
        var tests = new Action[]
        {
            ParallelSegmentsReturnTheirSeparation,
            CollinearOverlappingSegmentsTouch,
            CrossingSegmentsTouch,
            SkewSegmentsUseTheCommonPerpendicular,
            EndpointIsTheClosestPointWhenSegmentsFallShort,
            DegenerateSegmentBehavesLikeAPoint,
            PolylineTakesTheSmallestSegmentPair,
            PolylineRejectsTooFewPoints,
            PolylineRejectsNonFinitePoints,
            SagittaIsZeroForAStraightRun,
            SagittaMatchesTheClosedForm,
            SagittaCapsAtTheRadius,
            MaximumChordLengthPicksTheLongestStep,
            ConservativeDistanceSubtractsBothMargins,
            ConservativeDistanceNeverGoesNegative,
            ArcParallelToALineNeverOverstatesClearance,
            TangentArcAndLineReportTouching,
            SampledArcStaysWithinOneHundredthMillimetre
        };

        foreach (var test in tests) test();

        Console.WriteLine($"Passed {tests.Length} curve clearance tests.");
        return 0;
    }

    // ---- SegmentDistance -------------------------------------------------

    private static void ParallelSegmentsReturnTheirSeparation()
    {
        AssertClose(
            5.0,
            CurveClearanceKernel.SegmentDistance(
                P(0, 0, 0), P(10, 0, 0),
                P(0, 5, 0), P(10, 5, 0)));
    }

    private static void CollinearOverlappingSegmentsTouch()
    {
        AssertClose(
            0.0,
            CurveClearanceKernel.SegmentDistance(
                P(0, 0, 0), P(10, 0, 0),
                P(5, 0, 0), P(15, 0, 0)));
    }

    private static void CrossingSegmentsTouch()
    {
        AssertClose(
            0.0,
            CurveClearanceKernel.SegmentDistance(
                P(-5, 0, 0), P(5, 0, 0),
                P(0, -5, 0), P(0, 5, 0)));
    }

    private static void SkewSegmentsUseTheCommonPerpendicular()
    {
        // Trục X ở z=0 và trục Y ở z=3: chéo nhau, cách đúng 3.
        AssertClose(
            3.0,
            CurveClearanceKernel.SegmentDistance(
                P(-5, 0, 0), P(5, 0, 0),
                P(0, -5, 3), P(0, 5, 3)));
    }

    private static void EndpointIsTheClosestPointWhenSegmentsFallShort()
    {
        // Hai đoạn cùng trục X, cách nhau một quãng: đầu mút mới là điểm gần nhất.
        AssertClose(
            4.0,
            CurveClearanceKernel.SegmentDistance(
                P(0, 0, 0), P(10, 0, 0),
                P(14, 0, 0), P(20, 0, 0)));
    }

    private static void DegenerateSegmentBehavesLikeAPoint()
    {
        AssertClose(
            7.0,
            CurveClearanceKernel.SegmentDistance(
                P(0, 0, 0), P(0, 0, 0),
                P(0, 7, 0), P(10, 7, 0)));
    }

    // ---- PolylineDistance ------------------------------------------------

    private static void PolylineTakesTheSmallestSegmentPair()
    {
        // Hai đoạn cuối cùng nằm trên đường x=10, hở đúng 2 giữa y=10 và y=12.
        // Mọi cặp đoạn khác đều xa hơn.
        var first = new[] { P(0, 0, 0), P(10, 0, 0), P(10, 10, 0) };
        var second = new[] { P(0, 20, 0), P(10, 20, 0), P(10, 12, 0) };
        AssertClose(2.0, CurveClearanceKernel.PolylineDistance(first, second));
    }

    private static void PolylineRejectsTooFewPoints()
    {
        AssertThrows<ArgumentException>(() =>
            CurveClearanceKernel.PolylineDistance(
                new[] { P(0, 0, 0) },
                new[] { P(0, 1, 0), P(1, 1, 0) }));
    }

    private static void PolylineRejectsNonFinitePoints()
    {
        AssertThrows<ArgumentException>(() =>
            CurveClearanceKernel.PolylineDistance(
                new[] { P(0, 0, 0), P(double.NaN, 0, 0) },
                new[] { P(0, 1, 0), P(1, 1, 0) }));
    }

    // ---- SagittaBound ----------------------------------------------------

    private static void SagittaIsZeroForAStraightRun()
    {
        AssertClose(0.0, CurveClearanceKernel.SagittaBound(0.0, 1.0));
    }

    private static void SagittaMatchesTheClosedForm()
    {
        // R = 15, dây cung = 1  ->  15 - sqrt(225 - 0.25)
        AssertClose(
            15.0 - Math.Sqrt(225.0 - 0.25),
            CurveClearanceKernel.SagittaBound(15.0, 1.0));
    }

    private static void SagittaCapsAtTheRadius()
    {
        // Dây cung dài hơn đường kính là vô nghĩa; chặn trên là chính bán kính.
        AssertClose(5.0, CurveClearanceKernel.SagittaBound(5.0, 50.0));
    }

    private static void MaximumChordLengthPicksTheLongestStep()
    {
        AssertClose(
            4.0,
            CurveClearanceKernel.MaximumChordLength(
                new[] { P(0, 0, 0), P(1, 0, 0), P(5, 0, 0), P(7, 0, 0) }));
    }

    // ---- ConservativeDistance --------------------------------------------

    private static void ConservativeDistanceSubtractsBothMargins()
    {
        var first = new[] { P(0, 0, 0), P(1, 0, 0) };
        var second = new[] { P(0, 10, 0), P(1, 10, 0) };
        var expected = 10.0
            - CurveClearanceKernel.SagittaBound(15.0, 1.0)
            - CurveClearanceKernel.SagittaBound(20.0, 1.0);
        AssertClose(
            expected,
            CurveClearanceKernel.ConservativeDistance(first, 15.0, second, 20.0));
    }

    private static void ConservativeDistanceNeverGoesNegative()
    {
        var first = new[] { P(0, 0, 0), P(10, 0, 0) };
        var second = new[] { P(0, 0.001, 0), P(10, 0.001, 0) };
        var result = CurveClearanceKernel.ConservativeDistance(
            first, 5.0, second, 5.0);
        if (result < 0.0)
            throw new InvalidOperationException($"Expected >= 0, got {result}.");
    }

    // ---- Tính chất an toàn quan trọng nhất -------------------------------

    private static void ArcParallelToALineNeverOverstatesClearance()
    {
        // Đây chính là cặp làm solver của Revit chết: cung uốn R15 nằm song
        // song ngay cạnh một thanh thẳng. Duyệt nhiều khoảng hở và nhiều độ
        // mịn, kết quả phải LUÔN nhỏ hơn hoặc bằng khoảng cách thật.
        foreach (var gap in new[] { 0.5, 1.0, 6.0, 30.0 })
        {
            foreach (var segments in new[] { 8, 16, 32, 64 })
            {
                var radius = 15.0;
                var arc = SampleArc(radius, Math.PI / 2, segments);
                // Đường thẳng nằm ngoài cung, cách tâm radius + gap.
                var line = new[]
                {
                    P(radius + gap, -50, 0),
                    P(radius + gap, 50, 0)
                };
                var actual = CurveClearanceKernel.ConservativeDistance(
                    arc, radius, line, 0.0);
                if (actual > gap + Tolerance)
                {
                    throw new InvalidOperationException(
                        $"Overstated clearance: gap {gap}, segments {segments}, "
                        + $"reported {actual}.");
                }
            }
        }
    }

    private static void TangentArcAndLineReportTouching()
    {
        var radius = 15.0;
        var arc = SampleArc(radius, Math.PI / 2, 64);
        var line = new[] { P(radius, -50, 0), P(radius, 50, 0) };
        var actual = CurveClearanceKernel.ConservativeDistance(
            arc, radius, line, 0.0);
        if (actual > 1e-6)
            throw new InvalidOperationException(
                $"Tangent pair should read as touching, got {actual}.");
    }

    private static void SampledArcStaysWithinOneHundredthMillimetre()
    {
        // Đơn vị ở đây là mm cho dễ đọc. 64 đoạn trên cung R15 phải cho sai số
        // dưới 0.01mm, tức nhỏ hơn nhiều so với dung sai 0.12mm của bài kiểm
        // tra khoảng hở.
        const double radius = 15.0;
        const double gap = 10.0;
        var arc = SampleArc(radius, Math.PI / 2, 64);
        var line = new[] { P(radius + gap, -50, 0), P(radius + gap, 50, 0) };
        var actual = CurveClearanceKernel.ConservativeDistance(
            arc, radius, line, 0.0);
        var error = gap - actual;
        if (error < 0.0 || error > 0.01)
            throw new InvalidOperationException(
                $"Expected an understatement below 0.01, got {error}.");
    }

    // ---- helpers ---------------------------------------------------------

    /// <summary>Cung tròn tâm gốc, nằm trong mặt phẳng XY, bắt đầu từ trục X.</summary>
    private static List<ClearancePoint> SampleArc(
        double radius,
        double sweep,
        int segments)
    {
        return Enumerable.Range(0, segments + 1)
            .Select(index =>
            {
                var angle = sweep * index / segments;
                return P(radius * Math.Cos(angle), radius * Math.Sin(angle), 0);
            })
            .ToList();
    }

    private static ClearancePoint P(double x, double y, double z)
        => new ClearancePoint(x, y, z);

    private static void AssertClose(double expected, double actual)
    {
        if (Math.Abs(expected - actual) > 1e-6)
            throw new InvalidOperationException(
                $"Expected {expected}, got {actual}.");
    }

    private static void AssertThrows<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
