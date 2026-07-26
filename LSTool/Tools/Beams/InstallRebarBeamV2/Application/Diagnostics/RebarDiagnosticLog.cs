using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using HcBimUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using Newtonsoft.Json;
using RIMT.Utils.Paths;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Application.Diagnostics
{
    /// <summary>
    /// Append-only diagnostic trace for one InstallRebarBeamV2 execution.
    /// Logging failures after startup must never alter the Revit transaction.
    /// </summary>
    public sealed class RebarDiagnosticLog : IDisposable
    {
        private const string DifferentSectionGeometryRevision =
            "20260726.9-bentz-bends-in-beams";
        private readonly object _syncRoot = new();
        private readonly StreamWriter _writer;
        private readonly JsonSerializerSettings _serializerSettings = new()
        {
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Include,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        private bool _disposed;

        private RebarDiagnosticLog(string filePath, StreamWriter writer)
        {
            FilePath = filePath;
            RunId = Path.GetFileNameWithoutExtension(filePath);
            _writer = writer;
        }

        public string FilePath { get; }
        public string RunId { get; }

        public static RebarDiagnosticLog Start(InstallRebarBeamV2ViewModel viewModel)
        {
            if (viewModel == null) throw new ArgumentNullException(nameof(viewModel));

            var document = viewModel.ElementInstances?.Beam?.Element?.Document
                ?? throw new InvalidOperationException("The active Revit document is unavailable for diagnostic logging.");
            var logDirectory = Path.Combine(
                PathUtils.AppDataRimT,
                "Logs",
                "InstallRebarBeamV2");
            Directory.CreateDirectory(logDirectory);

            var documentName = SanitizeFileName(document.Title);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var filePath = Path.Combine(logDirectory, $"{timestamp}_{documentName}.jsonl");
            var writer = new StreamWriter(filePath, false, new UTF8Encoding(false))
            {
                AutoFlush = true
            };
            var result = new RebarDiagnosticLog(filePath, writer);
            var assembly = typeof(RebarDiagnosticLog).Assembly;
            var assemblyLocation = assembly.Location;

            File.WriteAllText(
                Path.Combine(logDirectory, "latest.txt"),
                filePath,
                new UTF8Encoding(false));

            result.Record("run.started", new
            {
                documentTitle = document.Title,
                documentPath = document.PathName,
                assemblyLocation,
                assemblyVersion = assembly.GetName().Version?.ToString(),
                assemblyLastWriteTimeUtc =
                    string.IsNullOrWhiteSpace(assemblyLocation)
                    || !File.Exists(assemblyLocation)
                        ? (DateTime?)null
                        : File.GetLastWriteTimeUtc(assemblyLocation),
                differentSectionGeometryRevision =
                    DifferentSectionGeometryRevision,
                selectedElementId = viewModel.OBJ?.Id.Value,
                selectedElementName = viewModel.OBJ?.Name,
                rebarBeamCount = viewModel.ElementInstances?.RebarBeams?.Count ?? 0,
                physicalMemberCount = viewModel.ElementInstances?.Beam?.ElementSubs?.Count ?? 0,
                axisX = VectorSnapshot(viewModel.ElementInstances?.Beam?.BoxElement?.VTX),
                axisY = VectorSnapshot(viewModel.ElementInstances?.Beam?.BoxElement?.VTY),
                axisZ = VectorSnapshot(viewModel.ElementInstances?.Beam?.BoxElement?.VTZ),
                spans = viewModel.ElementInstances?.RebarBeams?.Select(SideBarInputSnapshot).ToList()
            });
            return result;
        }

        public void Record(string eventName, object data = null)
        {
            if (_disposed) return;
            try
            {
                var envelope = new
                {
                    timestamp = DateTimeOffset.Now,
                    runId = RunId,
                    eventName,
                    data
                };
                var line = JsonConvert.SerializeObject(
                    envelope,
                    Formatting.None,
                    _serializerSettings);
                lock (_syncRoot)
                {
                    if (!_disposed) _writer.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"InstallRebarBeamV2 diagnostic write failed: {ex}");
            }
        }

        public void RecordException(string stage, Exception exception)
        {
            Record("run.exception", new
            {
                stage,
                exceptions = FlattenException(exception)
            });
        }

        public void RecordRebar(
            string eventName,
            Rebar rebar,
            long? sourceBeamId = null,
            ElementId intendedHostId = null,
            string group = null)
        {
            if (rebar == null)
            {
                Record(eventName, new
                {
                    sourceBeamId,
                    intendedHostId = intendedHostId?.Value,
                    group,
                    isNull = true
                });
                return;
            }

            try
            {
                Record(eventName, new
                {
                    rebarId = rebar.Id.Value,
                    rebar.UniqueId,
                    rebar.Name,
                    rebar.IsValidObject,
                    hostId = rebar.GetHostId()?.Value,
                    sourceBeamId,
                    intendedHostId = intendedHostId?.Value,
                    group,
                    curves = CurveSnapshots(rebar)
                });
            }
            catch (Exception ex)
            {
                Record(eventName, new
                {
                    sourceBeamId,
                    intendedHostId = intendedHostId?.Value,
                    group,
                    snapshotError = ex.ToString()
                });
            }
        }

        public static object PointSnapshot(XYZ point)
        {
            if (point == null) return null;
            return new
            {
                xMm = Math.Round(point.X.FootToMm(), 3),
                yMm = Math.Round(point.Y.FootToMm(), 3),
                zMm = Math.Round(point.Z.FootToMm(), 3)
            };
        }

        public static object VectorSnapshot(XYZ vector)
        {
            if (vector == null) return null;
            return new
            {
                x = Math.Round(vector.X, 6),
                y = Math.Round(vector.Y, 6),
                z = Math.Round(vector.Z, 6)
            };
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed) return;
                try
                {
                    var envelope = new
                    {
                        timestamp = DateTimeOffset.Now,
                        runId = RunId,
                        eventName = "log.closed",
                        data = new { filePath = FilePath }
                    };
                    _writer.WriteLine(JsonConvert.SerializeObject(
                        envelope,
                        Formatting.None,
                        _serializerSettings));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"InstallRebarBeamV2 diagnostic close failed: {ex}");
                }
                finally
                {
                    _disposed = true;
                    _writer.Dispose();
                }
            }
        }

        private static object SideBarInputSnapshot(models.RebarBeam span)
        {
            return new
            {
                beamId = span.BeamId,
                span.Name,
                span.BeamWidthMm,
                span.BeamHeightMm,
                start = SectionSideBarSnapshot(span.RebarBeamSectionStart),
                mid = SectionSideBarSnapshot(span.RebarBeamSectionMid),
                end = SectionSideBarSnapshot(span.RebarBeamSectionEnd)
            };
        }

        private static object SectionSideBarSnapshot(models.RebarBeamSection section)
        {
            return new
            {
                quantitySide = section?.RebarBeamSideBar?.QuantitySide,
                diameter = section?.RebarBeamSideBar?.Diameter,
                stirrupDiameter = section?.RebarBeamStirrup?.Diameter
            };
        }

        private static List<object> CurveSnapshots(Rebar rebar)
        {
            return rebar
                .GetCenterlineCurves(
                    false,
                    false,
                    false,
                    MultiplanarOption.IncludeAllMultiplanarCurves,
                    0)
                .Select((curve, index) => (object)new
                {
                    index,
                    curveType = curve.GetType().Name,
                    lengthMm = Math.Round(curve.Length.FootToMm(), 3),
                    start = PointSnapshot(curve.GetEndPoint(0)),
                    end = PointSnapshot(curve.GetEndPoint(1))
                })
                .ToList();
        }

        private static List<object> FlattenException(Exception exception)
        {
            var result = new List<object>();
            for (var current = exception; current != null; current = current.InnerException)
            {
                result.Add(new
                {
                    type = current.GetType().FullName,
                    current.Message,
                    current.StackTrace
                });
            }
            return result;
        }

        private static string SanitizeFileName(string value)
        {
            var source = string.IsNullOrWhiteSpace(value) ? "Untitled" : value;
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
                source = source.Replace(invalidCharacter, '_');
            return source;
        }
    }
}
