using System;
using System.Collections.Generic;
using System.Linq;
using LSTool.Tools.Columns.ColumnRebar.geometry;

internal sealed class ColumnPoint
{
    internal ColumnPoint(string name, double x, double y, double elevation)
    {
        Name = name;
        X = x;
        Y = y;
        Elevation = elevation;
    }

    internal string Name { get; }
    internal double X { get; }
    internal double Y { get; }
    internal double Elevation { get; }
}

internal static class Program
{
    private const double Tolerance = 300;

    private static int Main()
    {
        var tests = new Action[]
        {
            GroupsOneColumn,
            SeparatesThreeColumnsOnTheSameLevel,
            SortsOneVerticalStackByElevation,
            SeparatesAndSortsMixedStacks,
            KeepsAnEccentricMultiStoryStackConnected,
            SeparatesColumnsBeyondTolerance
        };

        foreach (var test in tests)
        {
            test();
        }

        Console.WriteLine($"Passed {tests.Length} column stack grouping tests.");
        return 0;
    }

    private static void GroupsOneColumn()
    {
        var groups = Group(new ColumnPoint("A", 0, 0, 0));
        AssertGroups(groups, new[] { "A" });
    }

    private static void SeparatesThreeColumnsOnTheSameLevel()
    {
        var groups = Group(
            new ColumnPoint("A", 0, 0, 0),
            new ColumnPoint("B", 4000, 0, 0),
            new ColumnPoint("C", 8000, 0, 0));

        AssertGroups(groups, new[] { "A" }, new[] { "B" }, new[] { "C" });
    }

    private static void SortsOneVerticalStackByElevation()
    {
        var groups = Group(
            new ColumnPoint("Top", 0, 0, 6000),
            new ColumnPoint("Bottom", 0, 0, 0),
            new ColumnPoint("Middle", 0, 0, 3000));

        AssertGroups(groups, new[] { "Bottom", "Middle", "Top" });
    }

    private static void SeparatesAndSortsMixedStacks()
    {
        var groups = Group(
            new ColumnPoint("A2", 0, 0, 3000),
            new ColumnPoint("B1", 5000, 0, 0),
            new ColumnPoint("A1", 0, 0, 0),
            new ColumnPoint("B2", 5000, 0, 3000));

        AssertGroups(groups, new[] { "A1", "A2" }, new[] { "B1", "B2" });
    }

    private static void KeepsAnEccentricMultiStoryStackConnected()
    {
        var groups = Group(
            new ColumnPoint("L1", 0, 0, 0),
            new ColumnPoint("L2", 200, 0, 3000),
            new ColumnPoint("L3", 400, 0, 6000));

        AssertGroups(groups, new[] { "L1", "L2", "L3" });
    }

    private static void SeparatesColumnsBeyondTolerance()
    {
        var groups = Group(
            new ColumnPoint("A", 0, 0, 0),
            new ColumnPoint("B", Tolerance + 0.01, 0, 3000));

        AssertGroups(groups, new[] { "A" }, new[] { "B" });
    }

    private static List<List<ColumnPoint>> Group(params ColumnPoint[] columns)
    {
        return ColumnStackGrouping.Group(
            columns,
            column => column.X,
            column => column.Y,
            column => column.Elevation,
            Tolerance);
    }

    private static void AssertGroups(
        IReadOnlyList<List<ColumnPoint>> actual,
        params string[][] expected)
    {
        var actualNames = actual
            .Select(group => string.Join(",", group.Select(column => column.Name)))
            .ToArray();
        var expectedNames = expected.Select(group => string.Join(",", group)).ToArray();

        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"Expected [{string.Join(" | ", expectedNames)}], " +
                $"actual [{string.Join(" | ", actualNames)}].");
        }
    }
}
