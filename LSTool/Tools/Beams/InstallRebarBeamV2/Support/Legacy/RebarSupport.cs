using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using Newtonsoft.Json;
using RIMT.Utils.Geometries;
using System.Globalization;
using System.IO;
using System.Reflection;
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
        private sealed class RebarLengthRule
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CheckSpecial { get; set; }
            public int BendCount { get; set; }
            public int HookCount { get; set; }
            public List<int> Angles { get; set; }
            public Dictionary<string, int> Diameters { get; set; }
        }

        private sealed class RebarLengthInfo
        {
            public int BendCount { get; set; }
            public int HookCount { get; set; }
            public int CheckSpecial { get; set; }
            public List<int> Angles { get; set; }
        }

        private static readonly Lazy<IReadOnlyList<RebarLengthRule>> RebarLengthRules =
            new(LoadRebarLengthRules);

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
            if (rebar == null || !rebar.IsValidObject)
                throw new InvalidOperationException("Cannot calculate the length of an invalid rebar.");

            try
            {
                var baseLengthMm = CalculateBaseLengthMm(rebar);
                var diameterKey = GetDiameterKey(rebar);
                var shapeParameterName = LSTool.Properties.Langs.RebarScheduleParameter
                    .SCHEDULE_REBAR_GEOMETRI_SHAPE;
                var storedShapeName = rebar.LookupParameter(shapeParameterName)?.AsString();
                var rule = string.IsNullOrWhiteSpace(storedShapeName)
                    ? null
                    : RebarLengthRules.Value.FirstOrDefault(candidate =>
                        candidate.Name.Equals(storedShapeName, StringComparison.Ordinal));

                if (rule == null)
                {
                    var info = AnalyzeRebarLength(rebar);
                    rule = RebarLengthRules.Value.FirstOrDefault(candidate =>
                        candidate.BendCount == info.BendCount
                        && candidate.HookCount == info.HookCount
                        && candidate.CheckSpecial == info.CheckSpecial
                        && ((candidate.Angles ?? new List<int>()).SequenceEqual(info.Angles)
                            || (candidate.Angles ?? new List<int>()).AsEnumerable().Reverse()
                                .SequenceEqual(info.Angles)));
                }

                var correctionMm = 0;
                if (rule?.Diameters != null
                    && rule.Diameters.TryGetValue(diameterKey, out var correction))
                {
                    correctionMm = correction;
                    LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy.RebarSharedParameterSupport
                        .SetRequiredStringParameter(rebar, shapeParameterName, rule.Name);
                }
                else
                {
                    LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy.RebarSharedParameterSupport
                        .SetRequiredStringParameter(rebar, shapeParameterName, "Unknown");
                }

                return RoundUpToTen(Math.Round(baseLengthMm + correctionMm));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to calculate corrected length for rebar {rebar.Id.Value}.", ex);
            }
        }

        private static double CalculateBaseLengthMm(Rebar rebar)
        {
            var shapeId = rebar.GetShapeId();
            if (shapeId == ElementId.InvalidElementId
                || rebar.Document.GetElement(shapeId) is not RebarShape shape)
            {
                var curves = rebar.GetCenterlineCurves(
                    true,
                    true,
                    true,
                    MultiplanarOption.IncludeAllMultiplanarCurves,
                    0);
                if (curves.Count == 0)
                    throw new InvalidOperationException("Free-form rebar has no centerline curves.");
                var bendCount = Math.Max(0, curves.Count - 1);
                var diameter = (rebar.Document.GetElement(rebar.GetTypeId()) as RebarBarType)
                    ?.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble() ?? 0;
                return (curves.Sum(curve => curve.Length) + diameter * bendCount).FootToMm();
            }

            var segments = shape.GetSegmentParamNames();
            if (segments.Count == 0)
            {
                var length = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH)
                    ?? throw new InvalidOperationException("Built-in rebar length is unavailable.");
                return length.AsDouble().FootToMm();
            }

            var result = 0d;
            foreach (var segment in segments)
            {
                var parameter = rebar.LookupParameter(segment)
                    ?? throw new InvalidOperationException(
                        $"Shape segment parameter '{segment}' is unavailable.");
                var segmentLength = FormatLengthLikeRevit(rebar, parameter.AsDouble());
                if (segmentLength > 0) result += segmentLength;
            }
            return result;
        }

        private static double FormatLengthLikeRevit(Rebar rebar, double lengthInFeet)
        {
            var formatOptions = rebar.Document.GetUnits().GetFormatOptions(SpecTypeId.Length);
            var accuracy = formatOptions?.Accuracy ?? 1d;
            var lengthInMillimeters = UnitUtils.ConvertFromInternalUnits(
                lengthInFeet,
                UnitTypeId.Millimeters);
            return Math.Round(lengthInMillimeters / accuracy) * accuracy;
        }

        private static RebarLengthInfo AnalyzeRebarLength(Rebar rebar)
        {
            var lines = rebar.GetCenterlineCurves(
                    false,
                    false,
                    false,
                    MultiplanarOption.IncludeOnlyPlanarCurves,
                    0)
                .OfType<Line>()
                .ToList();
            if (lines.Count == 0)
                throw new InvalidOperationException("Rebar has no straight centerline segments.");

            var startHookId = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_START_TYPE)
                ?.AsElementId() ?? ElementId.InvalidElementId;
            var endHookId = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_HOOK_END_TYPE)
                ?.AsElementId() ?? ElementId.InvalidElementId;
            var hasStartHook = startHookId != ElementId.InvalidElementId;
            var hasEndHook = endHookId != ElementId.InvalidElementId;
            var hookCount = (hasStartHook ? 1 : 0) + (hasEndHook ? 1 : 0);
            var barType = rebar.Document.GetElement(rebar.GetTypeId()) as RebarBarType
                ?? throw new InvalidOperationException("Rebar bar type could not be resolved.");
            var diameter = barType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble() ?? 0;

            var special = 0;
            if (hookCount == 1 && lines.Count > 1)
            {
                special = hasStartHook
                    ? CheckSpecialValue(lines[1], lines[0], diameter)
                    : CheckSpecialValue(lines[lines.Count - 2], lines[lines.Count - 1], diameter);
            }

            var angles = new List<int>();
            for (var index = 1; index < lines.Count; index++)
            {
                var angleDegrees = lines[index - 1].Direction
                    .AngleTo(lines[index].Direction) * 180d / Math.PI;
                angles.Add(Math.Abs(angleDegrees - 90d) < 1e-9 ? 1 : 0);
            }
            if (hasStartHook && angles.Count > 0) angles.RemoveAt(0);
            if (hasEndHook && angles.Count > 0) angles.RemoveAt(angles.Count - 1);

            return new RebarLengthInfo
            {
                BendCount = lines.Count - 1,
                HookCount = hookCount,
                CheckSpecial = special,
                Angles = angles
            };
        }

        private static int CheckSpecialValue(Line first, Line second, double diameter)
        {
            var dotProduct = Math.Max(-1d, Math.Min(1d,
                first.Direction.Normalize().DotProduct(second.Direction.Normalize())));
            var angleDegrees = Math.Acos(dotProduct) * 180d / Math.PI;
            if (Math.Abs(angleDegrees - 180d) < 0.01
                && Math.Abs(first.Length - 8d * diameter) < 0.01) return 3;
            if (Math.Abs(angleDegrees - 135d) < 0.01
                && Math.Abs(first.Length - 6d * diameter) < 0.01) return 2;
            if (Math.Abs(angleDegrees - 90d) < 0.01
                && Math.Abs(first.Length - 4d * diameter) < 0.01) return 1;
            return 0;
        }

        private static string GetDiameterKey(Rebar rebar)
        {
            var barType = rebar.Document.GetElement(rebar.GetTypeId()) as RebarBarType
                ?? throw new InvalidOperationException("Rebar bar type could not be resolved.");
            var diameter = barType.get_Parameter(BuiltInParameter.REBAR_MODEL_BAR_DIAMETER)
                ?.AsDouble() ?? 0;
            if (diameter <= 0)
                diameter = barType.get_Parameter(BuiltInParameter.REBAR_BAR_DIAMETER)?.AsDouble() ?? 0;
            if (diameter <= 0)
                throw new InvalidOperationException("Rebar diameter could not be resolved.");
            return "D" + Math.Round(diameter.FootToMm())
                .ToString(CultureInfo.InvariantCulture);
        }

        private static IReadOnlyList<RebarLengthRule> LoadRebarLengthRules()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("The add-in directory could not be resolved.");
            var path = Path.Combine(
                assemblyDirectory,
                "Resources",
                "Data",
                "DataRebarLenght.json");
            if (!File.Exists(path))
                throw new FileNotFoundException("The corrected rebar-length data file was not found.", path);
            return JsonConvert.DeserializeObject<List<RebarLengthRule>>(File.ReadAllText(path))
                ?? throw new InvalidOperationException("The corrected rebar-length data file is invalid.");
        }

        private static double RoundUpToTen(double value)
            => Math.Ceiling(value / 10d) * 10d;

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
                    if (shapeId == null || rebar.Document.GetElement(shapeId) is not RebarShape shape)
                        throw new InvalidOperationException(
                            $"The shape for rebar {rebar.Id.Value} could not be resolved.");
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
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to initialize segment metadata for rebar {rebar.Id.Value}.", ex);
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
            var parameter = rebar.LookupParameter(parameterName)
                ?? throw new InvalidOperationException(
                    $"Required segment parameter '{parameterName}' is missing on rebar {rebar.Id.Value}.");
            if (parameter.IsReadOnly)
                throw new InvalidOperationException(
                    $"Required segment parameter '{parameterName}' is read-only on rebar {rebar.Id.Value}.");
            bool success;
            switch (parameter.StorageType)
            {
                case StorageType.Integer:
                    success = parameter.Set(value ? 1 : 0);
                    break;
                case StorageType.Double:
                    success = parameter.Set(value ? 1d : 0d);
                    break;
                case StorageType.String:
                    success = parameter.Set(value ? "1" : "0");
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Segment parameter '{parameterName}' has unsupported storage type {parameter.StorageType}.");
            }
            if (!success)
                throw new InvalidOperationException(
                    $"Revit rejected segment parameter '{parameterName}' on rebar {rebar.Id.Value}.");
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
