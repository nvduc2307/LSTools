using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.Utils.Compares;
using RIMT.Utils.BoundingBoxs;
using RIMT.Utils.Entities;
using RIMT.Utils.Geometries;
using RIMT.Utils.Revit;
using RIMT.Utils.RevRebars;

namespace RIMT.Utils.RevitElements
{
    public sealed class RevBeamHole
    {
        public XYZ Origin { get; set; }
        public double RadiusMm { get; set; }
        public Element Opening { get; set; }

        public static List<RevBeamHole> GetRevBeamHole(FamilyInstance beam)
        {
            var results = new List<RevBeamHole>();
            try
            {
                var solid = beam.GetSingleSolid();
                if (solid == null) return results;
                foreach (var cylindricalFace in solid.Faces.Cast<Face>().OfType<CylindricalFace>())
                {
                    foreach (var arc in cylindricalFace
                                 .GetEdgesAsCurveLoops()
                                 .SelectMany(curveLoop => curveLoop)
                                 .OfType<Arc>())
                    {
                        if (arc.Radius <= 50.MmToFoot()) continue;
                        if (results.Any(hole => hole.Origin.IsAlmostEqualTo(arc.Center)
                                                && Math.Abs(hole.RadiusMm - arc.Radius.FootToMm()) < 1))
                            continue;
                        results.Add(new RevBeamHole
                        {
                            Origin = arc.Center,
                            RadiusMm = arc.Radius.FootToMm()
                        });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to inspect openings for beam {beam?.Id.Value}.", ex);
            }
            return results;
        }

        public static List<Rebar> DeleteMainStirrup(
            Document document,
            FamilyInstance beam,
            BoxElement beamBox,
            List<Rebar> stirrups,
            double beamThicknessMm,
            double bottomElevationMm,
            double topElevationMm,
            int numberRebarAdd,
            SchemaInfo schemaInfo,
            out List<Rebar> rebarDeletes)
        {
            rebarDeletes = new List<Rebar>();
            var results = new List<Rebar>();
            const double spacingMm = 50;
            try
            {
                var holes = GetRevBeamHole(beam);
                if (!holes.Any()) return results;

                var transform = beam.GetTransform();
                var xAxis = transform.OfVector(XYZ.BasisX);
                var yAxis = transform.OfVector(XYZ.BasisY);
                var zAxis = transform.OfVector(XYZ.BasisZ);
                if (beamBox?.LineBox == null)
                    throw new InvalidOperationException(
                        $"Cached beam geometry is unavailable for beam {beam.Id.Value}.");
                var midpoint = beamBox.LineBox.Midpoint();
                var elevationFace = new FaceCustom(yAxis, midpoint);
                var planFace = new FaceCustom(zAxis, midpoint);
                var validStirrups = stirrups
                    .Where(rebar => rebar != null && rebar.IsValidObject)
                    .ToList();
                var targetCandidates = validStirrups
                    .Where(rebar => !rebar.RebarNormal().IsPerpendicular(xAxis))
                    .ToList();
                var sourceGroups = validStirrups
                    .GroupBy(rebar => rebar, new CompareRebarFoLowFace(elevationFace, planFace))
                    .Select(group => group.ToList())
                    .OrderByDescending(group => group.Count)
                    .ToList();
                var sourceGroup = sourceGroups.FirstOrDefault();
                if (sourceGroup == null || sourceGroup.Count == 0)
                    throw new InvalidOperationException(
                        "A source stirrup group could not be resolved for opening reinforcement.");
                var correctedLengths = sourceGroup.ToDictionary(
                    rebar => rebar.Id.Value,
                    rebar => rebar.GetRebaLengthRealFromData());
                sourceGroup = sourceGroup
                    .OrderBy(rebar => correctedLengths[rebar.Id.Value])
                    .ToList();
                var sourceRay = sourceGroup.Last().GetCurvesOrgin().FirstOrDefault()?.Midpoint()
                    ?? throw new InvalidOperationException(
                        "A source stirrup ray could not be resolved for opening reinforcement.");
                var sourceMetadataById = sourceGroup.ToDictionary(
                    rebar => rebar.Id.Value,
                    rebar => SchemaInfo.ReadAll(schemaInfo.SchemaBase, schemaInfo.SchemaField, rebar)?.Value
                        ?? throw new InvalidOperationException(
                            $"Required metadata is missing on source stirrup {rebar.Id.Value}."));
                var deleteIds = new HashSet<long>();

                foreach (var hole in holes)
                {
                    var installPoint = hole.Origin.RayPointToFace(yAxis, elevationFace);
                    var radius = hole.RadiusMm.MmToFoot()
                                 + (numberRebarAdd + 0.5) * spacingMm.MmToFoot();
                    var lowerHeight = Math.Abs(bottomElevationMm.MmToFoot() - installPoint.Z)
                                      + spacingMm.MmToFoot();
                    var upperHeight = Math.Abs(topElevationMm.MmToFoot() - installPoint.Z)
                                      + spacingMm.MmToFoot();
                    var bounds = new BoundingBoxXYZ
                    {
                        Min = installPoint - xAxis * radius - yAxis * beamThicknessMm.MmToFoot() - zAxis * lowerHeight,
                        Max = installPoint + xAxis * radius + yAxis * beamThicknessMm.MmToFoot() + zAxis * upperHeight
                    };
                    var interferenceSolid = bounds.SolidFromBoundingbox();
                    var targets = targetCandidates
                        .Where(rebar =>
                        {
                            var centerlines = rebar.GetLinesOrigin();
                            if (centerlines.Count == 0)
                                throw new InvalidOperationException(
                                    $"Centerline geometry is unavailable for stirrup {rebar.Id.Value}.");
                            return centerlines.Any(curve =>
                                interferenceSolid.IntersectWithCurve(
                                    curve,
                                    new SolidCurveIntersectionOptions()).SegmentCount > 0);
                        })
                        .ToList();
                    if (!targets.Any()) continue;

                    var barType = document.GetElement(targets[0].GetTypeId()) as RebarBarType;
                    var diameter = barType.GetRebarDiameter();
                    var installPoints = new List<XYZ>();
                    for (var index = 0; index < numberRebarAdd; index++)
                    {
                        var offset = diameter / 2 + hole.RadiusMm.MmToFoot()
                            + spacingMm.MmToFoot() * (index + 1);
                        installPoints.Add(installPoint + xAxis * offset);
                        installPoints.Add(installPoint - xAxis * offset);
                    }

                    foreach (var point in installPoints)
                    {
                        var targetFace = new FaceCustom(xAxis, point);
                        var copyDirection = sourceRay.RayPointToFace(xAxis, targetFace) - sourceRay;
                        foreach (var sourceRebar in sourceGroup)
                        {
                            var copiedId = ElementTransformUtils.CopyElement(document, sourceRebar.Id, copyDirection).FirstOrDefault();
                            if (copiedId == null || document.GetElement(copiedId) is not Rebar copiedRebar)
                                throw new InvalidOperationException(
                                    $"Failed to copy source stirrup {sourceRebar.Id.Value} around an opening.");
                            CopyParameterValue(
                                sourceRebar,
                                copiedRebar,
                                LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE);
                            var copiedInfo = JsonConvert.DeserializeObject<BeamRebarInfo>(
                                    sourceMetadataById[sourceRebar.Id.Value])
                                ?? throw new InvalidOperationException(
                                    $"Metadata on source stirrup {sourceRebar.Id.Value} is invalid.");
                            copiedInfo.Id = copiedRebar.Id.Value;
                            copiedInfo.UniqueId = copiedRebar.UniqueId;
                            copiedInfo.Name = copiedRebar.Name;
                            var schemaValue = new SchemaField
                            {
                                Name = schemaInfo.SchemaField.Name,
                                Value = JsonConvert.SerializeObject(copiedInfo)
                            };
                            SchemaInfo.Write(schemaInfo.SchemaBase, copiedRebar, schemaValue);
                            results.Add(copiedRebar);
                        }
                    }
                    foreach (var target in targets)
                    {
                        if (deleteIds.Add(target.Id.Value))
                            rebarDeletes.Add(target);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create opening-support stirrups for beam {beam?.Id.Value}.", ex);
            }
            return results;
        }

        private static void CopyParameterValue(Element source, Element target, string parameterName)
        {
            var sourceParameter = source.LookupParameter(parameterName)
                ?? throw new InvalidOperationException(
                    $"Source parameter '{parameterName}' is missing on element {source.Id.Value}.");
            var targetParameter = target.LookupParameter(parameterName)
                ?? throw new InvalidOperationException(
                    $"Target parameter '{parameterName}' is missing on element {target.Id.Value}.");
            if (targetParameter.IsReadOnly)
                throw new InvalidOperationException(
                    $"Target parameter '{parameterName}' is read-only on element {target.Id.Value}.");
            if (targetParameter.StorageType != sourceParameter.StorageType)
                throw new InvalidOperationException(
                    $"Parameter '{parameterName}' has incompatible storage types while copying opening reinforcement.");
            bool success;
            switch (sourceParameter.StorageType)
            {
                case StorageType.Double:
                    success = targetParameter.Set(sourceParameter.AsDouble());
                    break;
                case StorageType.Integer:
                    success = targetParameter.Set(sourceParameter.AsInteger());
                    break;
                case StorageType.String:
                    success = targetParameter.Set(sourceParameter.AsString());
                    break;
                case StorageType.ElementId:
                    success = targetParameter.Set(sourceParameter.AsElementId());
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Parameter '{parameterName}' has unsupported storage type {sourceParameter.StorageType}.");
            }
            if (!success)
                throw new InvalidOperationException(
                    $"Revit rejected copied parameter '{parameterName}' on element {target.Id.Value}.");
        }
    }
}
