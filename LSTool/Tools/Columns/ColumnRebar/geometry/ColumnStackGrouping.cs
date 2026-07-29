using System;
using System.Collections.Generic;
using System.Linq;

namespace LSTool.Tools.Columns.ColumnRebar.geometry
{
    public static class ColumnStackGrouping
    {
        public static List<List<T>> Group<T>(
            IReadOnlyList<T> items,
            Func<T, double> getX,
            Func<T, double> getY,
            Func<T, double> getElevation,
            double planTolerance)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (getX == null) throw new ArgumentNullException(nameof(getX));
            if (getY == null) throw new ArgumentNullException(nameof(getY));
            if (getElevation == null) throw new ArgumentNullException(nameof(getElevation));
            if (planTolerance < 0) throw new ArgumentOutOfRangeException(nameof(planTolerance));

            var components = new List<List<int>>();
            var visited = new bool[items.Count];
            var toleranceSquared = planTolerance * planTolerance;

            for (var startIndex = 0; startIndex < items.Count; startIndex++)
            {
                if (visited[startIndex]) continue;

                var component = new List<int>();
                var pending = new Queue<int>();
                pending.Enqueue(startIndex);
                visited[startIndex] = true;

                while (pending.Count > 0)
                {
                    var currentIndex = pending.Dequeue();
                    component.Add(currentIndex);

                    for (var candidateIndex = 0; candidateIndex < items.Count; candidateIndex++)
                    {
                        if (visited[candidateIndex]) continue;

                        var deltaX = getX(items[currentIndex]) - getX(items[candidateIndex]);
                        var deltaY = getY(items[currentIndex]) - getY(items[candidateIndex]);
                        var planDistanceSquared = deltaX * deltaX + deltaY * deltaY;
                        if (planDistanceSquared > toleranceSquared) continue;

                        visited[candidateIndex] = true;
                        pending.Enqueue(candidateIndex);
                    }
                }

                component.Sort((left, right) =>
                {
                    var elevationComparison = getElevation(items[left])
                        .CompareTo(getElevation(items[right]));
                    return elevationComparison != 0
                        ? elevationComparison
                        : left.CompareTo(right);
                });
                components.Add(component);
            }

            components.Sort((left, right) => left.Min().CompareTo(right.Min()));
            return components
                .Select(component => component.Select(index => items[index]).ToList())
                .ToList();
        }
    }
}
