using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using HcBimUtils.RebarUtils;
using RIMT.Utils.Compares;
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
            catch
            {
                return new List<RevBeamHole>();
            }
            return results;
        }

        public static List<Rebar> DeleteMainStirrup(
            Document document,
            FamilyInstance beam,
            List<Rebar> stirrups,
            double beamThicknessMm,
            double beamHeightMm,
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
                var beamBox = new RevElement(beam);
                var midpoint = beamBox.BoxElement.LineBox.Midpoint();
                var elevationFace = new FaceCustom(yAxis, midpoint);
                var planFace = new FaceCustom(zAxis, midpoint);

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
                    var targets = stirrups
                        .Where(rebar => rebar != null && rebar.IsValidObject)
                        .Where(rebar => !rebar.RebarNormal().IsPerpendicular(xAxis))
                        .Where(rebar => rebar.GetLinesOrigin().Any(curve =>
                            interferenceSolid.IntersectWithCurve(curve, new SolidCurveIntersectionOptions()).SegmentCount > 0))
                        .ToList();
                    if (!targets.Any()) continue;

                    var groups = stirrups
                        .Where(rebar => rebar != null && rebar.IsValidObject)
                        .GroupBy(rebar => rebar, new CompareRebarFoLowFace(elevationFace, planFace))
                        .Select(group => group.ToList())
                        .OrderByDescending(group => group.Count)
                        .ToList();
                    var sourceGroup = groups.FirstOrDefault()?.OrderBy(rebar => rebar.GetRebaLengthRealFromData()).ToList();
                    var sourceRay = sourceGroup?.LastOrDefault()?.GetCurvesOrgin().FirstOrDefault()?.Midpoint();
                    if (sourceGroup == null || sourceRay == null) continue;

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
                            var schemaValue = SchemaInfo.ReadAll(schemaInfo.SchemaBase, schemaInfo.SchemaField, sourceRebar);
                            var copiedId = ElementTransformUtils.CopyElement(document, sourceRebar.Id, copyDirection).FirstOrDefault();
                            if (copiedId == null || document.GetElement(copiedId) is not Rebar copiedRebar) continue;
                            CopyParameterValue(
                                sourceRebar,
                                copiedRebar,
                                LSTool.Properties.RTParams.RT_PARAMS_REBAR_TYPE);
                            if (schemaValue != null)
                                SchemaInfo.Write(schemaInfo.SchemaBase, copiedRebar, schemaValue);
                            results.Add(copiedRebar);
                        }
                    }
                    rebarDeletes.AddRange(targets);
                }
            }
            catch
            {
                return results;
            }
            return results;
        }

        private static void CopyParameterValue(Element source, Element target, string parameterName)
        {
            var sourceParameter = source.LookupParameter(parameterName);
            var targetParameter = target.LookupParameter(parameterName);
            if (sourceParameter == null || targetParameter == null || targetParameter.IsReadOnly) return;
            switch (sourceParameter.StorageType)
            {
                case StorageType.Double:
                    targetParameter.Set(sourceParameter.AsDouble());
                    break;
                case StorageType.Integer:
                    targetParameter.Set(sourceParameter.AsInteger());
                    break;
                case StorageType.String:
                    targetParameter.Set(sourceParameter.AsString());
                    break;
                case StorageType.ElementId:
                    targetParameter.Set(sourceParameter.AsElementId());
                    break;
            }
        }
    }
}
