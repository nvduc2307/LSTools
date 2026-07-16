using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using RIMT.Utils.Geometries;
using View = Autodesk.Revit.DB.View;

namespace RIMT.Utils.RevRebars
{
    public sealed class RebarBarTypeCustom
    {
        public RebarBarType RebarBarType { get; set; }
        public string Name { get; set; }
        public string NameStyle { get; set; }
        public double ModelBarDiameter { get; set; }
        public double BarDiameter { get; set; }
        public double BarDiameterReal { get; set; }
        public double StandardBendDiameter { get; set; }
        public double StandardHookBendDiameter { get; set; }
        public double StirrupOrTieBendDiameter { get; set; }
        public double MaximumBendRadius { get; set; }

        public RebarBarTypeCustom()
        {
        }

        public RebarBarTypeCustom(RebarBarType rebarBarType)
        {
            RebarBarType = rebarBarType;
            Name = rebarBarType?.Name;
            NameStyle = rebarBarType?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsString()
                ?? rebarBarType?.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME)?.AsValueString()
                ?? rebarBarType?.Name;
            BarDiameter = ReadDouble(rebarBarType, BuiltInParameter.REBAR_BAR_DIAMETER);
            ModelBarDiameter = ReadDouble(rebarBarType, BuiltInParameter.REBAR_MODEL_BAR_DIAMETER);
            if (ModelBarDiameter <= 0) ModelBarDiameter = BarDiameter;
            BarDiameterReal = BarDiameter;
            StandardBendDiameter = ReadDouble(rebarBarType, BuiltInParameter.REBAR_STANDARD_BEND_DIAMETER);
            StandardHookBendDiameter = ReadDouble(rebarBarType, BuiltInParameter.REBAR_STANDARD_HOOK_BEND_DIAMETER);
            StirrupOrTieBendDiameter = ReadDouble(rebarBarType, BuiltInParameter.REBAR_BAR_STIRRUP_BEND_DIAMETER);
            MaximumBendRadius = ReadDouble(rebarBarType, BuiltInParameter.REBAR_BAR_MAXIMUM_BEND_RADIUS);
        }

        private static double ReadDouble(RebarBarType type, BuiltInParameter parameter)
            => type?.get_Parameter(parameter)?.AsDouble() ?? 0;
    }

    public static class RevRebarUtils
    {
        public static List<Curve> GetLinesOrigin(this Rebar rebar)
        {
            var results = new List<Curve>();
            try
            {
                var curves = rebar
                    .GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0)
                    .Where(curve => curve is Line)
                    .ToList();
                if (curves.Count <= 1) return curves;

                var startPoint = curves[0].GetEndPoint(0);
                var lastPoint = curves[curves.Count - 1].GetEndPoint(1);
                for (var index = 0; index < curves.Count; index++)
                {
                    if (index == curves.Count - 1)
                    {
                        results.Add(Line.CreateBound(startPoint, lastPoint));
                        continue;
                    }

                    var nextIndex = index + 1;
                    var firstDirection = curves[index].Direction();
                    var crossDirection = firstDirection.CrossProduct(curves[nextIndex].Direction());
                    var planeNormal = firstDirection.CrossProduct(crossDirection);
                    var face = new FaceCustom(planeNormal, curves[index].Midpoint());
                    var endPoint = curves[nextIndex]
                        .Midpoint()
                        .RayPointToFace(curves[nextIndex].Direction(), face);
                    results.Add(Line.CreateBound(startPoint, endPoint));
                    startPoint = endPoint;
                }
            }
            catch
            {
                return results;
            }
            return results;
        }

        public static List<Curve> GetCurvesOrgin(this Rebar rebar)
        {
            try
            {
                return rebar
                    .GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0)
                    .ToList();
            }
            catch
            {
                return new List<Curve>();
            }
        }

        public static double GetRebaLengthRealFromData(this Rebar rebar)
        {
            try
            {
                return rebar.GetCurvesOrgin().Sum(curve => curve.Length).FootToMm();
            }
            catch
            {
                return 0;
            }
        }

        public static void InitSegment(this List<Rebar> rebars)
        {
            if (rebars == null || rebars.Count == 0) return;
            var segmentParameters = new[]
            {
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_A,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_B,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_C,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_D,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_E,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_F,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_G,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_H,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_J,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_K,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_R,
                LSTool.Properties.Langs.RebarScheduleParameter.SEGMENT_O
            };

            foreach (var rebar in rebars.Where(rebar => rebar != null && rebar.IsValidObject))
            {
                try
                {
                    var shapeId = rebar.get_Parameter(BuiltInParameter.REBAR_SHAPE)?.AsElementId();
                    if (shapeId == null || rebar.Document.GetElement(shapeId) is not RebarShape shape) continue;
                    var segments = shape.GetSegmentParamNames();
                    var hasUnknownSegment = segments.Any(segment =>
                        !segmentParameters.Any(parameter => SegmentParameterMatches(parameter, segment)));

                    foreach (var parameterName in segmentParameters)
                    {
                        var isUsed = hasUnknownSegment
                                     || segments.Any(segment => SegmentParameterMatches(parameterName, segment));
                        SetSegmentFlag(rebar, parameterName, isUsed);
                    }
                }
                catch
                {
                    // Missing shared parameters must not roll back rebar creation.
                }
            }
        }

        private static bool SegmentParameterMatches(string parameterName, string segmentName)
        {
            if (string.IsNullOrWhiteSpace(parameterName) || string.IsNullOrWhiteSpace(segmentName)) return false;
            var suffix = parameterName.Split('_').LastOrDefault() ?? parameterName;
            return suffix.IndexOf(segmentName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void SetSegmentFlag(Rebar rebar, string parameterName, bool value)
        {
            var parameter = rebar.LookupParameter(parameterName);
            if (parameter == null || parameter.IsReadOnly) return;
            switch (parameter.StorageType)
            {
                case StorageType.Integer:
                    parameter.Set(value ? 1 : 0);
                    break;
                case StorageType.Double:
                    parameter.Set(value ? 1d : 0d);
                    break;
                case StorageType.String:
                    parameter.Set(value ? "1" : "0");
                    break;
            }
        }

        public static void SetSolidRebar3DView(this Rebar rebar, View view)
        {
            if (rebar == null || view is not View3D view3D) return;
            var setSolidMethod = rebar.GetType().GetMethod(
                "SetSolidInView",
                new[] { typeof(View3D), typeof(bool) });
            setSolidMethod?.Invoke(rebar, new object[] { view3D, true });
            rebar.SetUnobscuredInView(view3D, true);
        }
    }

    public static class RebarShapeUtils
    {
        public static List<string> GetSegmentParamNames(this RebarShape rebarShape)
        {
            var results = new List<string>();
            if (rebarShape?.GetRebarShapeDefinition() is not RebarShapeDefinitionBySegments definition)
                return results;

            for (var segmentIndex = 0; segmentIndex < definition.NumberOfSegments; segmentIndex++)
            {
                foreach (var constraint in definition.GetSegment(segmentIndex).GetConstraints())
                {
                    if (constraint is not RebarShapeConstraintSegmentLength lengthConstraint) continue;
                    var parameterId = lengthConstraint.GetParamId();
                    if (parameterId == ElementId.InvalidElementId) continue;
                    var parameter = rebarShape.Parameters
                        .Cast<Parameter>()
                        .FirstOrDefault(candidate => candidate.Id.ToString() == parameterId.ToString());
                    if (parameter != null && !results.Contains(parameter.Definition.Name))
                        results.Add(parameter.Definition.Name);
                }
            }
            return results;
        }
    }

    public static class RebarCreationCompat
    {
        public static Rebar CreateFromCurves(
            Document document,
            RebarStyle style,
            RebarBarType barType,
            Element host,
            XYZ normal,
            IList<Curve> curves,
            bool useExistingShapeIfPossible,
            bool createNewShape)
        {
#if R26
            using (var terminations = new BarTerminationsData(document))
            {
                return Rebar.CreateFromCurves(
                    document,
                    style,
                    barType,
                    host,
                    normal,
                    curves,
                    terminations,
                    useExistingShapeIfPossible,
                    createNewShape);
            }
#else
            return Rebar.CreateFromCurves(
                document,
                style,
                barType,
                null,
                null,
                host,
                normal,
                curves,
                RebarHookOrientation.Left,
                RebarHookOrientation.Left,
                useExistingShapeIfPossible,
                createNewShape);
#endif
        }

        public static Rebar CreateFromCurvesAndShape(
            Document document,
            RebarShape shape,
            RebarBarType barType,
            Element host,
            XYZ normal,
            IList<Curve> curves)
        {
#if R26
            using (var terminations = new BarTerminationsData(document))
            {
                return Rebar.CreateFromCurvesAndShape(
                    document,
                    shape,
                    barType,
                    host,
                    normal,
                    curves,
                    terminations);
            }
#else
            return Rebar.CreateFromCurvesAndShape(
                document,
                shape,
                barType,
                null,
                null,
                host,
                normal,
                curves,
                RebarHookOrientation.Left,
                RebarHookOrientation.Left);
#endif
        }
    }
}

namespace RIMT.Utils.Revit
{
    public static class RebarUtils
    {
        public static double GetRebarDiameter(this RebarBarType rebarBarType)
        {
            if (rebarBarType == null) return 0;
            var diameter = rebarBarType
                .get_Parameter(BuiltInParameter.REBAR_MODEL_BAR_DIAMETER)?.AsDouble() ?? 0;
            return diameter > 0
                ? diameter
                : rebarBarType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble() ?? 0;
        }
    }
}

namespace RevitApp.Utils.RevElements.RevRebars
{
    using RIMT.Utils.RevRebars;

    public static class RebarBarTypeCustomUtils
    {
        public static RebarBarTypeCustom GetRebarBarTypeCustom(
            string name,
            List<RebarBarTypeCustom> rebarBarTypeCustoms)
            => rebarBarTypeCustoms?.FirstOrDefault(type => type.NameStyle == name);
    }
}
