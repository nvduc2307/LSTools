using System;
using System.Collections.Generic;
using System.Linq;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping;

internal static class Program
{
    private const double MaximumDirectionAngleDegrees = 1.0;

    private static int Main()
    {
        var tests = new Action[]
        {
            ReturnsNoGroupsForNoBeams,
            KeepsOneBeamIndependent,
            GroupsParallelBeamsJoinedToTheSameColumn,
            GroupsReverseDrawnBeams,
            GroupsNearlyAntiParallelBeamsWithinTolerance,
            SeparatesPerpendicularBeamsAtTheSameColumn,
            SeparatesParallelBeamsWithoutACommonColumn,
            GroupsATransitiveRunAcrossTwoColumns,
            SeparatesMixedRunsAndASingleton,
            SeparatesTwoDirectionsAtTheSameColumn,
            GroupsThreeParallelBeamsAtTheSameColumn,
            IgnoresDuplicateJoinedColumnIds,
            NormalizesDirectionVectors,
            HonorsTheDirectionTolerance,
            IncludesTheExactDirectionToleranceBoundary,
            GroupsTransitiveDirectionToleranceAcrossColumns,
            SortsMembersByTheirOriginalSelectionIndex,
            RejectsAnInvalidDirection
        };

        foreach (var test in tests)
        {
            test();
        }

        Console.WriteLine(
            $"Passed {tests.Length} beam run grouping tests.");
        return 0;
    }

    private static void ReturnsNoGroupsForNoBeams()
    {
        AssertGroups(Group());
    }

    private static void KeepsOneBeamIndependent()
    {
        AssertGroups(
            Group(Member(0, 1, 0)),
            new[] { 0 });
    }

    private static void GroupsParallelBeamsJoinedToTheSameColumn()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 1, 0, 100)),
            new[] { 0, 1 });
    }

    private static void GroupsReverseDrawnBeams()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, -3, 0, 100)),
            new[] { 0, 1 });
    }

    private static void GroupsNearlyAntiParallelBeamsWithinTolerance()
    {
        var angle = 179.1 * Math.PI / 180.0;

        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(
                    1,
                    Math.Cos(angle),
                    Math.Sin(angle),
                    100)),
            new[] { 0, 1 });
    }

    private static void SeparatesPerpendicularBeamsAtTheSameColumn()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 0, 1, 100)),
            new[] { 0 },
            new[] { 1 });
    }

    private static void SeparatesParallelBeamsWithoutACommonColumn()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 1, 0, 200)),
            new[] { 0 },
            new[] { 1 });
    }

    private static void GroupsATransitiveRunAcrossTwoColumns()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 1, 0, 100, 200),
                Member(2, 1, 0, 200)),
            new[] { 0, 1, 2 });
    }

    private static void SeparatesMixedRunsAndASingleton()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 1, 0, 300),
                Member(2, 1, 0, 100),
                Member(3, 1, 0, 300),
                Member(4, 1, 0)),
            new[] { 0, 2 },
            new[] { 1, 3 },
            new[] { 4 });
    }

    private static void SeparatesTwoDirectionsAtTheSameColumn()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, -1, 0, 100),
                Member(2, 0, 1, 100),
                Member(3, 0, -1, 100)),
            new[] { 0, 1 },
            new[] { 2, 3 });
    }

    private static void GroupsThreeParallelBeamsAtTheSameColumn()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(1, 2, 0, 100),
                Member(2, -4, 0, 100)),
            new[] { 0, 1, 2 });
    }

    private static void IgnoresDuplicateJoinedColumnIds()
    {
        AssertGroups(
            Group(
                Member(0, 1, 0, 100, 100),
                Member(1, 1, 0, 100)),
            new[] { 0, 1 });
    }

    private static void NormalizesDirectionVectors()
    {
        AssertGroups(
            Group(
                Member(0, 10, 0, 100),
                Member(1, 0.01, 0, 100)),
            new[] { 0, 1 });
    }

    private static void HonorsTheDirectionTolerance()
    {
        var insideAngle = 0.9 * Math.PI / 180.0;
        var outsideAngle = 2.1 * Math.PI / 180.0;

        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(
                    1,
                    Math.Cos(insideAngle),
                    Math.Sin(insideAngle),
                    100),
                Member(
                    2,
                    Math.Cos(outsideAngle),
                    Math.Sin(outsideAngle),
                    100)),
            new[] { 0, 1 },
            new[] { 2 });
    }

    private static void IncludesTheExactDirectionToleranceBoundary()
    {
        var boundaryAngle =
            MaximumDirectionAngleDegrees * Math.PI / 180.0;
        var outsideAngle =
            (MaximumDirectionAngleDegrees + 0.01) * Math.PI / 180.0;

        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(
                    1,
                    Math.Cos(boundaryAngle),
                    Math.Sin(boundaryAngle),
                    100)),
            new[] { 0, 1 });

        AssertGroups(
            Group(
                Member(0, 1, 0, 200),
                Member(
                    1,
                    Math.Cos(outsideAngle),
                    Math.Sin(outsideAngle),
                    200)),
            new[] { 0 },
            new[] { 1 });
    }

    private static void GroupsTransitiveDirectionToleranceAcrossColumns()
    {
        var firstTurn = 0.9 * Math.PI / 180.0;
        var secondTurn = 1.8 * Math.PI / 180.0;

        AssertGroups(
            Group(
                Member(0, 1, 0, 100),
                Member(
                    1,
                    Math.Cos(firstTurn),
                    Math.Sin(firstTurn),
                    100,
                    200),
                Member(
                    2,
                    Math.Cos(secondTurn),
                    Math.Sin(secondTurn),
                    200)),
            new[] { 0, 1, 2 });
    }

    private static void SortsMembersByTheirOriginalSelectionIndex()
    {
        AssertGroups(
            Group(
                Member(2, 1, 0, 100),
                Member(0, 1, 0, 100),
                Member(1, 1, 0, 100)),
            new[] { 0, 1, 2 });
    }

    private static void RejectsAnInvalidDirection()
    {
        AssertThrows<InvalidOperationException>(
            () => Group(Member(0, 0, 0, 100)));
        AssertThrows<InvalidOperationException>(
            () => Group(Member(0, double.NaN, 1, 100)));
        AssertThrows<InvalidOperationException>(
            () => Group(Member(0, double.PositiveInfinity, 1, 100)));
    }

    private static List<List<int>> Group(
        params BeamRunGroupingMember[] members)
    {
        return BeamRunGrouping.Group(
            members,
            MaximumDirectionAngleDegrees);
    }

    private static BeamRunGroupingMember Member(
        int originalIndex,
        double directionX,
        double directionY,
        params long[] joinedColumnIds)
    {
        return new BeamRunGroupingMember(
            originalIndex,
            directionX,
            directionY,
            joinedColumnIds);
    }

    private static void AssertGroups(
        IReadOnlyList<List<int>> actual,
        params int[][] expected)
    {
        var actualValues = actual
            .Select(group => string.Join(",", group))
            .ToArray();
        var expectedValues = expected
            .Select(group => string.Join(",", group))
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
