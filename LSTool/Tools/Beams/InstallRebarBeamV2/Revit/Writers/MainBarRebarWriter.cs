using Autodesk.Revit.DB.Structure;
using LSTool.Tools.Beams.InstallRebarBeamV2.Application;
using LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans;
using RIMT.Utils.RevPoints;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Revit.Writers
{
    public sealed class MainBarRebarWriter
    {
        public List<Rebar> Create(
            MainBarCreationPlan plan,
            RebarExecutionContext context)
        {
            try
            {
                var result = new List<Rebar>(plan.Geometry.Count);
                foreach (var geometry in plan.Geometry)
                {
                    var curves = geometry.MainPoints.PointsToCurves();
                    var rebar = RebarCreationCompat.CreateFromCurves(
                        context.Document,
                        RebarStyle.Standard,
                        plan.BarType.RebarBarType,
                        context.TemporaryHost,
                        -context.YAxis,
                        curves,
                        true,
                        true);
                    RevRebarUtils.SetSolidRebar3DView(rebar, context.Document.ActiveView);
                    result.Add(rebar);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create {plan.StageName} bars.", ex);
            }
        }
    }
}
