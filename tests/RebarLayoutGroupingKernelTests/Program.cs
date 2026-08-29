using System;
using System.Collections.Generic;
using System.Linq;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping;

internal static class Program
{
    private const double Tolerance = 0.001;
    private const int DefaultMinimumBarsPerGroup = 2;
    private const string DefaultBucket = "host1|type1|3";

    private static int Main()
    {
        var tests = new Action[]
        {
            ReturnsNoRunsForNoBars,
            KeepsASingleBarUngrouped,
            GroupsEvenlySpacedIdenticalBars,
            OrdersTheRunAlongTheNormal,
            DropsTheRemainderBarThatBreaksTheSpacing,
            SplitsAlternatingShapesIntoTwoRunsAtDoubleSpacing,
            SeparatesDifferentBuckets,
            RejectsBarsOffsetPerpendicularToTheNormal,
            SeparatesDifferentShapes,
            SeparatesDifferentPointCounts,
            HonorsMinimumBarsPerGroup,
            EmitsTwoRunsSeparatedByAGap,
            IgnoresBarsSharingTheSamePosition,
            NormalizesTheNormalVector,
            GroupsWhenSpacingDriftIsWithinTolerance,
            SplitsWhenSpacingDriftExceedsTolerance,
            SeparatesBarsWithOppositeNormals,
            RejectsDuplicateOriginalIndex,
            RejectsAnInvalidTolerance,
            RejectsAMinimumBelowTwo,
            RejectsADegenerateNormal,
            RejectsABarWithTooFewPoints,
            RejectsANullBar
        };

        foreach (var test in tests)
        {
            test();
        }

        Console.WriteLine(
            $"Passed {tests.Length} rebar layout grouping tests.");
        return 0;
    }

    private static void ReturnsNoRunsForNoBars()
    {
        AssertRuns(BuildRuns());
    }

    private static void KeepsASingleBarUngrouped()
    {
        AssertRuns(BuildRuns(Bar(0, 0)));
    }

    private static void GroupsEvenlySpacedIdenticalBars()
    {
        AssertRuns(
            BuildRuns(Bar(0, 0), Bar(1, 1), Bar(2, 2)),
            new[] { 0, 1, 2 });
    }

    private static void OrdersTheRunAlongTheNormal()
    {
        // Đầu vào lộn xộn nhưng chuỗi phải xếp tăng dần theo normal, vì thanh
        // đầu chuỗi sẽ là thanh đại diện mang cả rebar set.
        AssertRuns(
            BuildRuns(Bar(0, 2), Bar(1, 0), Bar(2, 1)),
            new[] { 1, 2, 0 });
    }

    private static void DropsTheRemainderBarThatBreaksTheSpacing()
    {
        // Thanh bù dư cuối đoạn đai nằm sát thanh trước nó, không thuộc mảng.
        AssertRuns(
            BuildRuns(Bar(0, 0), Bar(1, 1), Bar(2, 2), Bar(3, 2.5)),
            new[] { 0, 1, 2 });
    }

    private static void SplitsAlternatingShapesIntoTwoRunsAtDoubleSpacing()
    {
        // Đai lật móc xen kẽ: thanh chẵn và thanh lẻ khác hình học nên tách
        // thành hai nhóm, mỗi nhóm bước gấp đôi.
        AssertRuns(
            BuildRuns(
                Bar(0, 0),
                Bar(1, 1, shape: 1),
                Bar(2, 2),
                Bar(3, 3, shape: 1),
                Bar(4, 4),
                Bar(5, 5, shape: 1)),
            new[] { 0, 2, 4 },
            new[] { 1, 3, 5 });
    }

    private static void SeparatesDifferentBuckets()
    {
        AssertRuns(
            BuildRuns(
                Bar(0, 0),
                Bar(1, 1, bucket: "host2|type1|3"),
                Bar(2, 2, bucket: "host1|type9|3")));
    }

    private static void RejectsBarsOffsetPerpendicularToTheNormal()
    {
        AssertRuns(
            BuildRuns(
                Bar(0, 0),
                Bar(1, 1, lateral: 0.5),
                Bar(2, 2, lateral: 1.0)));
    }

    private static void SeparatesDifferentShapes()
    {
        AssertRuns(BuildRuns(Bar(0, 0), Bar(1, 1, shape: 1)));
    }

    private static void SeparatesDifferentPointCounts()
    {
        var shortBar = new RebarGroupingBar(
            1,
            DefaultBucket,
            P(1, 0, 0),
            P(1, 0, 0),
            new[] { P(0, 0, 0), P(0, 1, 0) });
        AssertRuns(BuildRuns(Bar(0, 0), shortBar));
    }

    private static void HonorsMinimumBarsPerGroup()
    {
        AssertRuns(
            RebarLayoutGrouping.BuildUniformRuns(
                new[] { Bar(0, 0), Bar(1, 1) },
                Tolerance,
                3));
    }

    private static void EmitsTwoRunsSeparatedByAGap()
    {
        AssertRuns(
            BuildRuns(
                Bar(0, 0), Bar(1, 1), Bar(2, 2),
                Bar(3, 5), Bar(4, 6), Bar(5, 7)),
            new[] { 0, 1, 2 },
            new[] { 3, 4, 5 });
    }

    private static void IgnoresBarsSharingTheSamePosition()
    {
        AssertRuns(
            BuildRuns(Bar(0, 0), Bar(1, 0), Bar(2, 1)),
            new[] { 1, 2 });
    }

    private static void NormalizesTheNormalVector()
    {
        AssertRuns(
            BuildRuns(
                Bar(0, 0, normal: P(0, 0, 3), axis: Axis.Z),
                Bar(1, 1, normal: P(0, 0, 3), axis: Axis.Z),
                Bar(2, 2, normal: P(0, 0, 3), axis: Axis.Z)),
            new[] { 0, 1, 2 });
    }

    private static void GroupsWhenSpacingDriftIsWithinTolerance()
    {
        AssertRuns(
            BuildRuns(Bar(0, 0), Bar(1, 1), Bar(2, 2 + Tolerance * 0.5)),
            new[] { 0, 1, 2 });
    }

    private static void SplitsWhenSpacingDriftExceedsTolerance()
    {
        AssertRuns(
            BuildRuns(Bar(0, 0), Bar(1, 1), Bar(2, 2 + Tolerance * 3)),
            new[] { 0, 1 });
    }

    private static void SeparatesBarsWithOppositeNormals()
    {
        // Normal ngược chiều nghĩa là mặt phẳng thanh bị lật, Revit không thể
        // trải chúng chung một set với thanh 0. Chuỗi còn lại xếp theo chính
        // normal của nó, nên thanh 2 mới là thanh đại diện.
        AssertRuns(
            BuildRuns(
                Bar(0, 0),
                Bar(1, 1, normal: P(-1, 0, 0)),
                Bar(2, 2, normal: P(-1, 0, 0))),
            new[] { 2, 1 });
    }

    private static void RejectsDuplicateOriginalIndex()
    {
        AssertThrows<InvalidOperationException>(
            () => BuildRuns(Bar(0, 0), Bar(0, 1)));
    }

    private static void RejectsAnInvalidTolerance()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => RebarLayoutGrouping.BuildUniformRuns(
                new[] { Bar(0, 0) },
                0.0,
                DefaultMinimumBarsPerGroup));
        AssertThrows<ArgumentOutOfRangeException>(
            () => RebarLayoutGrouping.BuildUniformRuns(
                new[] { Bar(0, 0) },
                double.NaN,
                DefaultMinimumBarsPerGroup));
    }

    private static void RejectsAMinimumBelowTwo()
    {
        AssertThrows<ArgumentOutOfRangeException>(
            () => RebarLayoutGrouping.BuildUniformRuns(
                new[] { Bar(0, 0) },
                Tolerance,
                1));
    }

    private static void RejectsADegenerateNormal()
    {
        AssertThrows<InvalidOperationException>(
            () => BuildRuns(Bar(0, 0, normal: P(0, 0, 0))));
        AssertThrows<InvalidOperationException>(
            () => BuildRuns(Bar(0, 0, normal: P(double.NaN, 0, 0))));
    }

    private static void RejectsABarWithTooFewPoints()
    {
        var degenerate = new RebarGroupingBar(
            0,
            DefaultBucket,
            P(1, 0, 0),
            P(0, 0, 0),
            new[] { P(0, 0, 0) });
        AssertThrows<InvalidOperationException>(() => BuildRuns(degenerate));
    }

    private static void RejectsANullBar()
    {
        AssertThrows<InvalidOperationException>(
            () => BuildRuns(Bar(0, 0), null!));
    }

    private enum Axis
    {
        X,
        Z
    }

    private static List<List<int>> BuildRuns(params RebarGroupingBar[] bars)
    {
        return RebarLayoutGrouping.BuildUniformRuns(
            bars,
            Tolerance,
            DefaultMinimumBarsPerGroup);
    }

    /// <summary>
    /// Dựng một thanh nằm cách gốc <paramref name="along"/> dọc theo trục mảng.
    /// </summary>
    private static RebarGroupingBar Bar(
        int originalIndex,
        double along,
        string bucket = DefaultBucket,
        double lateral = 0.0,
        int shape = 0,
        RebarGroupingPoint? normal = null,
        Axis axis = Axis.X)
    {
        var relativePoints = shape == 0
            ? new[] { P(0, 0, 0), P(0, 1, 0), P(0, 1, 1) }
            : new[] { P(0, 0, 0), P(0, -1, 0), P(0, -1, 1) };
        var origin = axis == Axis.X
            ? P(along, lateral, 0)
            : P(lateral, 0, along);
        return new RebarGroupingBar(
            originalIndex,
            bucket,
            normal ?? P(1, 0, 0),
            origin,
            relativePoints);
    }

    private static RebarGroupingPoint P(double x, double y, double z)
    {
        return new RebarGroupingPoint(x, y, z);
    }

    private static void AssertRuns(
        IReadOnlyList<List<int>> actual,
        params int[][] expected)
    {
        var actualValues = actual
            .Select(run => string.Join(",", run))
            .ToArray();
        var expectedValues = expected
            .Select(run => string.Join(",", run))
            .ToArray();
        if (!actualValues.SequenceEqual(expectedValues))
            throw new InvalidOperationException(
                $"Expected [{string.Join(" | ", expectedValues)}], "
                + $"actual [{string.Join(" | ", actualValues)}].");
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

        throw new InvalidOperationException(
            $"Expected {typeof(TException).Name}.");
    }
}
