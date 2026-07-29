using Autodesk.Revit.DB;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application.Selection
{
    internal static class BeamSelectionRunGrouping
    {
        private const double MaximumDirectionAngleDegrees = 1.0;

        internal static List<List<Element>> Group(
            IReadOnlyList<Element> selectedBeams)
        {
            if (selectedBeams == null)
                throw new ArgumentNullException(nameof(selectedBeams));

            var beams = selectedBeams
                .Where(beam => beam != null)
                .GroupBy(beam => beam.Id.Value)
                .Select(group => group.First())
                .ToList();
            if (beams.Count == 0)
                throw new InvalidOperationException(
                    "Select at least one structural framing beam.");

            var document = beams[0].Document;
            var groupingMembers = beams
                .Select((beam, index) =>
                    CreateGroupingMember(document, beam, index))
                .ToList();
            var groups = BeamRunGrouping.Group(
                groupingMembers,
                MaximumDirectionAngleDegrees);

            return groups
                .Select(group => group
                    .Select(index => beams[index])
                    .ToList())
                .ToList();
        }

        private static BeamRunGroupingMember CreateGroupingMember(
            Document document,
            Element beam,
            int originalIndex)
        {
            if (beam is not FamilyInstance
                || beam.Category?.Id.Value
                != (long)BuiltInCategory.OST_StructuralFraming)
                throw new InvalidOperationException(
                    $"Element {beam?.Id.Value} is not a structural framing "
                    + "family instance.");

            var line =
                (beam.Location as LocationCurve)?.Curve as Line;
            if (line == null)
                throw new InvalidOperationException(
                    $"Beam {beam.Id.Value} is not straight and cannot be "
                    + "grouped into a beam run.");

            var direction = line.Direction;
            var joinedColumnIds = JoinGeometryUtils
                .GetJoinedElements(document, beam)
                .Where(joinedId =>
                    document.GetElement(joinedId)?.Category?.Id.Value
                    == (long)BuiltInCategory.OST_StructuralColumns)
                .Select(joinedId => joinedId.Value)
                .ToList();

            return new BeamRunGroupingMember(
                originalIndex,
                direction.X,
                direction.Y,
                joinedColumnIds);
        }
    }
}
