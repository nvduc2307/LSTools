using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Grouping;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Grouping
{
    /// <summary>
    /// Gom các thanh thép rời đã tạo thành rebar set bố trí theo Fixed Number.
    /// Chỉ những thanh giống hệt nhau về hình học, cùng chủng loại, cùng host
    /// và nằm cách đều nhau dọc theo normal của thanh mới được gom; phần còn
    /// lại giữ nguyên layout Single nên luồng cũ không bao giờ bị vỡ.
    /// </summary>
    public sealed class RebarLayoutGroupingService
    {
        private const double PointMergeToleranceFt = 1e-8;

        public RebarLayoutGroupingResult Apply(
            Document document,
            IEnumerable<Rebar> rebars,
            RebarGroupingOptions options,
            RebarDiagnosticLog diagnosticLog = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (rebars == null) throw new ArgumentNullException(nameof(rebars));
            options ??= RebarGroupingOptions.Default;

            var candidates = rebars.Where(rebar => rebar != null).ToList();
            var result = new RebarLayoutGroupingResult();
            if (!options.Enabled)
            {
                result.UngroupedBarCount = candidates.Count;
                diagnosticLog?.Record("grouping.skipped", new
                {
                    reason = "disabled",
                    candidateCount = candidates.Count
                });
                return result;
            }

            var descriptors = new List<BarDescriptor>(candidates.Count);
            var ineligibleCount = 0;
            foreach (var rebar in candidates)
            {
                var descriptor = Describe(rebar, options);
                if (descriptor == null) ineligibleCount++;
                else descriptors.Add(descriptor);
            }

            var runs = BuildUniformRuns(descriptors, options);
            var pending = runs
                .Select(run => new PendingGroup(run))
                .ToList();

            if (pending.Count == 0)
            {
                result.UngroupedBarCount = candidates.Count;
                diagnosticLog?.Record("grouping.completed", new
                {
                    candidateCount = candidates.Count,
                    ineligibleCount,
                    groupCount = 0
                });
                return result;
            }

            // Vòng 1: dựng mảng về phía normal rồi đo xem Revit đặt nó ở đâu.
            foreach (var group in pending) ApplyFixedNumber(group, true);
            document.Regenerate();
            Measure(pending, options);

            // Vòng 2: mảng chạy ngược chiều mong muốn thì lật lại.
            var reversed = pending
                .Where(group => !group.Measurement.Succeeded
                    && group.Measurement.SpanIsOpposite)
                .ToList();
            if (reversed.Count > 0)
            {
                foreach (var group in reversed) ApplyFixedNumber(group, false);
                document.Regenerate();
                Measure(reversed, options);
            }

            // Vòng 3: mảng đúng chiều dài nhưng lệch vị trí thì dời cả set về
            // đúng chỗ, thay vì đoán trước Revit neo mảng ở đầu nào.
            var translated = new List<PendingGroup>();
            foreach (var group in pending)
            {
                var measurement = group.Measurement;
                if (measurement.Succeeded || !measurement.SpanMatches) continue;
                var offset = measurement.CorrectionOffset;
                if (offset == null) continue;
                try
                {
                    ElementTransformUtils.MoveElement(
                        document,
                        group.Representative.Rebar.Id,
                        offset);
                    translated.Add(group);
                }
                catch (Exception exception)
                {
                    measurement.Failure = "move: " + exception.Message;
                }
            }
            if (translated.Count > 0)
            {
                document.Regenerate();
                Measure(translated, options);
            }

            var revertedAny = false;
            foreach (var group in pending)
            {
                if (group.Measurement.Succeeded) continue;
                RevertToSingle(group);
                group.Rejected = true;
                revertedAny = true;
            }
            if (revertedAny) document.Regenerate();

            LogMeasurements(pending, diagnosticLog);

            var absorbedIds = new List<ElementId>();
            foreach (var group in pending)
            {
                if (group.Rejected)
                {
                    result.RejectedGroups.Add(
                        $"rebar {group.Representative.Id} x{group.Bars.Count}");
                    continue;
                }

                var summary = new RebarLayoutGroupSummary
                {
                    RepresentativeRebarId = group.Representative.Id,
                    HostId = group.Representative.HostId,
                    BarCount = group.Bars.Count,
                    SpacingMm = Math.Round(
                        (group.ArrayLengthFt / (group.Bars.Count - 1)).FootToMm(),
                        3),
                    ArrayLengthMm = Math.Round(group.ArrayLengthFt.FootToMm(), 3),
                    BarsOnNormalSide = group.BarsOnNormalSide
                };
                for (var index = 1; index < group.Bars.Count; index++)
                {
                    var absorbed = group.Bars[index];
                    summary.AbsorbedRebarIds.Add(absorbed.Id);
                    result.RemovedRebarIds.Add(absorbed.Id);
                    absorbedIds.Add(absorbed.Rebar.Id);
                }
                result.Groups.Add(summary);
            }

            if (absorbedIds.Count > 0) document.Delete(absorbedIds);

            var groupedBarCount = result.Groups.Sum(group => group.BarCount);
            result.UngroupedBarCount = candidates.Count - groupedBarCount;

            diagnosticLog?.Record("grouping.completed", new
            {
                candidateCount = candidates.Count,
                ineligibleCount,
                groupCount = result.Groups.Count,
                groupedBarCount,
                removedBarCount = result.RemovedRebarIds.Count,
                ungroupedBarCount = result.UngroupedBarCount,
                rejectedGroups = result.RejectedGroups,
                groups = result.Groups.Select(group => new
                {
                    representative = group.RepresentativeRebarId,
                    group.HostId,
                    group.BarCount,
                    group.SpacingMm,
                    group.ArrayLengthMm,
                    group.BarsOnNormalSide
                }).ToList()
            });
            return result;
        }

        private static void ApplyFixedNumber(PendingGroup group, bool barsOnNormalSide)
        {
            group.BarsOnNormalSide = barsOnNormalSide;
            group.Representative.Accessor.SetLayoutAsFixedNumber(
                group.Bars.Count,
                group.ArrayLengthFt,
                barsOnNormalSide,
                true,
                true);
        }

        private static void RevertToSingle(PendingGroup group)
        {
            try
            {
                group.Representative.Accessor.SetLayoutAsSingle();
            }
            catch
            {
                // Thanh đại diện đã bị Revit đưa về trạng thái không hợp lệ;
                // không còn gì để hoàn tác, nhóm coi như bị loại.
            }
        }

        private static void Measure(
            IEnumerable<PendingGroup> groups,
            RebarGroupingOptions options)
        {
            foreach (var group in groups)
            {
                group.Measurement = MeasureLayout(group, options);
            }
        }

        /// <summary>
        /// Đo mảng thanh Revit vừa dựng và đối chiếu với vị trí các thanh gốc.
        /// Không giả định Revit neo mảng ở đầu nào; chỉ so vector trải và độ
        /// lệch, để bước sau có thể dời set về đúng chỗ.
        /// </summary>
        private static LayoutMeasurement MeasureLayout(
            PendingGroup group,
            RebarGroupingOptions options)
        {
            var measurement = new LayoutMeasurement
            {
                ExpectedFirst = group.Bars[0].Origin,
                ExpectedLast = group.Bars[group.Bars.Count - 1].Origin,
                ExpectedBarPositions = group.Bars.Count,
                BarsOnNormalSide = group.BarsOnNormalSide
            };
            try
            {
                var rebar = group.Representative.Rebar;
                if (!rebar.IsValidObject)
                {
                    measurement.Failure = "representative is no longer valid";
                    return measurement;
                }

                measurement.ActualBarPositions = rebar.NumberOfBarPositions;
                measurement.ActualArrayLengthFt =
                    group.Representative.Accessor.ArrayLength;
                if (measurement.ActualBarPositions != group.Bars.Count)
                {
                    measurement.Failure = "bar position count mismatch";
                    return measurement;
                }

                measurement.ActualFirst = ReadOrigin(group.Representative, 0);
                measurement.ActualLast = ReadOrigin(
                    group.Representative,
                    group.Bars.Count - 1);
                if (measurement.ActualFirst == null || measurement.ActualLast == null)
                {
                    measurement.Failure = "centerline curves unavailable";
                    return measurement;
                }

                var tolerance = options.ToleranceFt;
                var actualSpan = measurement.ActualLast - measurement.ActualFirst;
                var expectedSpan = measurement.ExpectedLast - measurement.ExpectedFirst;
                measurement.SpanIsForward =
                    (actualSpan - expectedSpan).GetLength() <= tolerance;
                measurement.SpanIsOpposite =
                    (actualSpan + expectedSpan).GetLength() <= tolerance;

                if (measurement.SpanIsForward)
                {
                    measurement.CorrectionOffset =
                        measurement.ExpectedFirst - measurement.ActualFirst;
                }
                else if (measurement.SpanIsOpposite)
                {
                    measurement.CorrectionOffset =
                        measurement.ExpectedLast - measurement.ActualFirst;
                }

                measurement.Succeeded = measurement.SpanMatches
                    && measurement.CorrectionOffset.GetLength() <= tolerance;
                if (!measurement.Succeeded && measurement.Failure == null)
                {
                    measurement.Failure = measurement.SpanMatches
                        ? "array is translated"
                        : "array span does not match the original bars";
                }
                return measurement;
            }
            catch (Exception exception)
            {
                measurement.Failure = exception.Message;
                return measurement;
            }
        }

        /// <summary>
        /// Ghi số liệu đo được của vài nhóm đầu tiên và của mọi nhóm bị loại,
        /// đủ để chẩn đoán mà không làm log phình ra.
        /// </summary>
        private static void LogMeasurements(
            List<PendingGroup> groups,
            RebarDiagnosticLog diagnosticLog)
        {
            if (diagnosticLog == null) return;
            var interesting = groups
                .Where(group => !group.Measurement.Succeeded)
                .Concat(groups.Where(group => group.Measurement.Succeeded))
                .Take(8)
                .ToList();
            foreach (var group in interesting)
            {
                var measurement = group.Measurement;
                diagnosticLog.Record("grouping.verify.detail", new
                {
                    representative = group.Representative.Id,
                    barCount = group.Bars.Count,
                    measurement.Succeeded,
                    measurement.Failure,
                    measurement.BarsOnNormalSide,
                    measurement.ExpectedBarPositions,
                    measurement.ActualBarPositions,
                    plannedArrayLengthMm = Math.Round(
                        group.ArrayLengthFt.FootToMm(),
                        3),
                    actualArrayLengthMm = Math.Round(
                        measurement.ActualArrayLengthFt.FootToMm(),
                        3),
                    measurement.SpanIsForward,
                    measurement.SpanIsOpposite,
                    normal = RebarDiagnosticLog.VectorSnapshot(
                        group.Representative.Normal),
                    expectedFirst = RebarDiagnosticLog.PointSnapshot(
                        measurement.ExpectedFirst),
                    expectedLast = RebarDiagnosticLog.PointSnapshot(
                        measurement.ExpectedLast),
                    actualFirst = RebarDiagnosticLog.PointSnapshot(
                        measurement.ActualFirst),
                    actualLast = RebarDiagnosticLog.PointSnapshot(
                        measurement.ActualLast),
                    correctionOffsetMm = measurement.CorrectionOffset == null
                        ? (double?)null
                        : Math.Round(
                            measurement.CorrectionOffset.GetLength().FootToMm(),
                            3)
                });
            }
        }

        /// <summary>
        /// Đọc gốc của thanh ở vị trí thứ <paramref name="barPositionIndex"/>
        /// trong set. Không dùng tham số barPositionIndex của
        /// GetCenterlineCurves vì có bản Revit bỏ qua nó và luôn trả về hình
        /// học của thanh đầu; thay vào đó lấy hình học thanh đầu rồi áp
        /// transform của vị trí cần đọc.
        /// </summary>
        private static XYZ ReadOrigin(BarDescriptor representative, int barPositionIndex)
        {
            var curves = representative.Rebar.GetCenterlineCurves(
                false,
                false,
                false,
                MultiplanarOption.IncludeAllMultiplanarCurves,
                0);
            var points = FlattenCurves(curves);
            if (points.Count == 0) return null;
            var origin = points[0];
            if (barPositionIndex <= 0) return origin;

            var firstTransform = representative.Accessor.GetBarPositionTransform(0);
            var targetTransform =
                representative.Accessor.GetBarPositionTransform(barPositionIndex);
            if (firstTransform == null || targetTransform == null) return null;

            // Đưa gốc thanh đầu về hệ cục bộ rồi áp transform của vị trí cần
            // đọc. Nếu transform vị trí 0 là đơn vị thì phép này rút gọn thành
            // áp thẳng transform, nên đúng với cả hai quy ước của Revit.
            return targetTransform.OfPoint(firstTransform.Inverse.OfPoint(origin));
        }

        /// <summary>
        /// Chuyển các thanh sang dạng thuần hình học rồi nhờ kernel chia thành
        /// những chuỗi cách đều. Thanh không thuộc chuỗi nào giữ nguyên Single.
        /// </summary>
        private static List<List<BarDescriptor>> BuildUniformRuns(
            List<BarDescriptor> descriptors,
            RebarGroupingOptions options)
        {
            if (descriptors.Count == 0) return new List<List<BarDescriptor>>();

            var samples = descriptors
                .Select((descriptor, index) => new RebarGroupingBar(
                    index,
                    descriptor.BucketKey,
                    ToGroupingPoint(descriptor.Normal),
                    ToGroupingPoint(descriptor.Origin),
                    descriptor.RelativePoints.Select(ToGroupingPoint)))
                .ToList();

            var runs = RebarLayoutGrouping.BuildUniformRuns(
                samples,
                options.ToleranceFt,
                Math.Max(2, options.MinimumBarsPerGroup));

            return runs
                .Select(run => run.Select(index => descriptors[index]).ToList())
                .ToList();
        }

        private static RebarGroupingPoint ToGroupingPoint(XYZ value)
        {
            return new RebarGroupingPoint(value.X, value.Y, value.Z);
        }

        private static BarDescriptor Describe(Rebar rebar, RebarGroupingOptions options)
        {
            try
            {
                if (!rebar.IsValidObject) return null;
                if (!rebar.IsRebarShapeDriven()) return null;

                var accessor = rebar.GetShapeDrivenAccessor();
                if (accessor == null) return null;
                if (rebar.LayoutRule != RebarLayoutRule.Single) return null;

                var normal = accessor.Normal;
                if (normal == null || normal.GetLength() <= options.ToleranceFt) return null;
                normal = normal.Normalize();

                var curves = rebar.GetCenterlineCurves(
                    false,
                    false,
                    false,
                    MultiplanarOption.IncludeAllMultiplanarCurves,
                    0);
                var points = FlattenCurves(curves);
                if (points.Count < 2) return null;

                var origin = points[0];
                var hostId = rebar.GetHostId();
                var hostIdValue = hostId?.Value ?? ElementId.InvalidElementId.Value;
                return new BarDescriptor
                {
                    Rebar = rebar,
                    Accessor = accessor,
                    Id = rebar.Id.Value,
                    HostId = hostIdValue,
                    Normal = normal,
                    Origin = origin,
                    RelativePoints = points
                        .Select(point => point - origin)
                        .ToList(),
                    BucketKey = string.Join(
                        "|",
                        hostIdValue,
                        rebar.GetTypeId().Value,
                        points.Count)
                };
            }
            catch
            {
                // Thanh nào không đọc được hình học thì bỏ qua, giữ nguyên Single.
                return null;
            }
        }

        private static List<XYZ> FlattenCurves(IList<Curve> curves)
        {
            var points = new List<XYZ>();
            if (curves == null) return points;
            foreach (var curve in curves)
            {
                if (curve == null) continue;
                foreach (var point in curve.Tessellate())
                {
                    if (points.Count > 0
                        && points[points.Count - 1].DistanceTo(point) <= PointMergeToleranceFt)
                    {
                        continue;
                    }
                    points.Add(point);
                }
            }
            return points;
        }

        private sealed class BarDescriptor
        {
            public Rebar Rebar { get; set; }
            public RebarShapeDrivenAccessor Accessor { get; set; }
            public long Id { get; set; }
            public long HostId { get; set; }
            public XYZ Normal { get; set; }
            public XYZ Origin { get; set; }
            public IReadOnlyList<XYZ> RelativePoints { get; set; }
            public string BucketKey { get; set; }
        }

        private sealed class PendingGroup
        {
            public PendingGroup(List<BarDescriptor> bars)
            {
                Bars = bars;
                // Các thanh trong một chuỗi nằm thẳng hàng dọc theo normal và
                // đã được sắp tăng dần, nên chiều dài mảng là hiệu hình chiếu
                // của thanh cuối và thanh đầu.
                var axis = bars[0].Normal;
                ArrayLengthFt = bars[bars.Count - 1].Origin.DotProduct(axis)
                    - bars[0].Origin.DotProduct(axis);
            }

            public List<BarDescriptor> Bars { get; }
            public BarDescriptor Representative => Bars[0];
            public double ArrayLengthFt { get; }
            public bool BarsOnNormalSide { get; set; }
            public bool Rejected { get; set; }
            public LayoutMeasurement Measurement { get; set; } = new LayoutMeasurement();
        }

        /// <summary>
        /// Kết quả đối chiếu giữa mảng thanh Revit dựng ra và vị trí các thanh
        /// gốc của nhóm.
        /// </summary>
        private sealed class LayoutMeasurement
        {
            public bool Succeeded { get; set; }
            public string Failure { get; set; }
            public bool BarsOnNormalSide { get; set; }
            public int ExpectedBarPositions { get; set; }
            public int ActualBarPositions { get; set; }
            public double ActualArrayLengthFt { get; set; }
            public XYZ ExpectedFirst { get; set; }
            public XYZ ExpectedLast { get; set; }
            public XYZ ActualFirst { get; set; }
            public XYZ ActualLast { get; set; }

            /// <summary>Vector trải khớp chiều dài, dù cùng hay ngược chiều.</summary>
            public bool SpanIsForward { get; set; }
            public bool SpanIsOpposite { get; set; }
            public bool SpanMatches => SpanIsForward || SpanIsOpposite;

            /// <summary>Quãng cần dời cả set để trùng vị trí các thanh gốc.</summary>
            public XYZ CorrectionOffset { get; set; }
        }
    }
}
