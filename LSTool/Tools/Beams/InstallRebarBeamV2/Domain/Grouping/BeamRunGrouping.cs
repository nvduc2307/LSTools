using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping
{
    public sealed class BeamRunGroupingMember
    {
        public int OriginalIndex { get; }
        public double DirectionX { get; }
        public double DirectionY { get; }
        public IReadOnlyList<long> JoinedColumnIds { get; }

        public BeamRunGroupingMember(
            int originalIndex,
            double directionX,
            double directionY,
            IEnumerable<long> joinedColumnIds)
        {
            OriginalIndex = originalIndex;
            DirectionX = directionX;
            DirectionY = directionY;
            JoinedColumnIds = (joinedColumnIds
                    ?? throw new ArgumentNullException(nameof(joinedColumnIds)))
                .Distinct()
                .ToList();
        }
    }

    public static class BeamRunGrouping
    {
        private const double NumericTolerance = 1e-9;

        public static List<List<int>> Group(
            IReadOnlyList<BeamRunGroupingMember> members,
            double maximumDirectionAngleDegrees)
        {
            if (members == null)
                throw new ArgumentNullException(nameof(members));
            if (!IsFinite(maximumDirectionAngleDegrees)
                || maximumDirectionAngleDegrees < 0.0
                || maximumDirectionAngleDegrees >= 90.0)
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDirectionAngleDegrees));

            ValidateMembers(members);
            if (members.Count == 0)
                return new List<List<int>>();

            var adjacency = BuildAdjacency(
                members,
                maximumDirectionAngleDegrees);
            return FindComponents(members, adjacency);
        }

        private static void ValidateMembers(
            IReadOnlyList<BeamRunGroupingMember> members)
        {
            var originalIndices = new HashSet<int>();
            foreach (var member in members)
            {
                if (member == null)
                    throw new InvalidOperationException(
                        "The beam grouping input contains a null member.");
                if (!originalIndices.Add(member.OriginalIndex))
                    throw new InvalidOperationException(
                        $"Duplicate original beam index {member.OriginalIndex}.");
                if (!IsFinite(member.DirectionX)
                    || !IsFinite(member.DirectionY))
                    throw new InvalidOperationException(
                        $"Beam {member.OriginalIndex} has an invalid plan direction.");

                var planLengthSquared =
                    member.DirectionX * member.DirectionX
                    + member.DirectionY * member.DirectionY;
                if (planLengthSquared <= NumericTolerance * NumericTolerance)
                    throw new InvalidOperationException(
                        $"Beam {member.OriginalIndex} has no plan direction.");
            }
        }

        private static List<int>[] BuildAdjacency(
            IReadOnlyList<BeamRunGroupingMember> members,
            double maximumDirectionAngleDegrees)
        {
            var adjacency = Enumerable.Range(0, members.Count)
                .Select(_ => new List<int>())
                .ToArray();

            for (var firstIndex = 0;
                 firstIndex < members.Count;
                 firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < members.Count;
                     secondIndex++)
                {
                    if (!AreConnected(
                            members[firstIndex],
                            members[secondIndex],
                            maximumDirectionAngleDegrees))
                        continue;

                    adjacency[firstIndex].Add(secondIndex);
                    adjacency[secondIndex].Add(firstIndex);
                }
            }

            return adjacency;
        }

        private static bool AreConnected(
            BeamRunGroupingMember first,
            BeamRunGroupingMember second,
            double maximumDirectionAngleDegrees)
        {
            if (!HaveCommonJoinedColumn(first, second))
                return false;

            NormalizePlanDirection(
                first,
                out var firstDirectionX,
                out var firstDirectionY);
            NormalizePlanDirection(
                second,
                out var secondDirectionX,
                out var secondDirectionY);

            var absoluteDot = Math.Abs(
                firstDirectionX * secondDirectionX
                + firstDirectionY * secondDirectionY);
            var minimumDot = Math.Cos(
                maximumDirectionAngleDegrees * Math.PI / 180.0);
            return absoluteDot + NumericTolerance >= minimumDot;
        }

        private static bool HaveCommonJoinedColumn(
            BeamRunGroupingMember first,
            BeamRunGroupingMember second)
        {
            if (first.JoinedColumnIds.Count == 0
                || second.JoinedColumnIds.Count == 0)
                return false;

            var secondColumnIds =
                new HashSet<long>(second.JoinedColumnIds);
            return first.JoinedColumnIds.Any(secondColumnIds.Contains);
        }

        private static void NormalizePlanDirection(
            BeamRunGroupingMember member,
            out double directionX,
            out double directionY)
        {
            var length = Math.Sqrt(
                member.DirectionX * member.DirectionX
                + member.DirectionY * member.DirectionY);
            directionX = member.DirectionX / length;
            directionY = member.DirectionY / length;
        }

        private static List<List<int>> FindComponents(
            IReadOnlyList<BeamRunGroupingMember> members,
            IReadOnlyList<List<int>> adjacency)
        {
            var visited = new bool[members.Count];
            var result = new List<List<int>>();

            for (var startIndex = 0;
                 startIndex < members.Count;
                 startIndex++)
            {
                if (visited[startIndex])
                    continue;

                var memberIndices = new List<int>();
                var pending = new Queue<int>();
                pending.Enqueue(startIndex);
                visited[startIndex] = true;

                while (pending.Count > 0)
                {
                    var currentIndex = pending.Dequeue();
                    memberIndices.Add(currentIndex);
                    foreach (var adjacentIndex in adjacency[currentIndex])
                    {
                        if (visited[adjacentIndex])
                            continue;
                        visited[adjacentIndex] = true;
                        pending.Enqueue(adjacentIndex);
                    }
                }

                result.Add(memberIndices
                    .Select(index => members[index].OriginalIndex)
                    .OrderBy(index => index)
                    .ToList());
            }

            return result
                .OrderBy(group => group.Min())
                .ToList();
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
