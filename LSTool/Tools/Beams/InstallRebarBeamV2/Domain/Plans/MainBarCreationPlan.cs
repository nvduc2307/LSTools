using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.RevRebars;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Domain.Plans
{
    public sealed class MainBarCreationPlan
    {
        public string StageName { get; }
        public RebarBarTypeCustom BarType { get; }
        public IReadOnlyList<MainBarBeamReal> Geometry { get; }

        public MainBarCreationPlan(
            string stageName,
            RebarBarTypeCustom barType,
            IReadOnlyList<MainBarBeamReal> geometry)
        {
            StageName = stageName ?? throw new ArgumentNullException(nameof(stageName));
            BarType = barType ?? throw new ArgumentNullException(nameof(barType));
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
        }
    }
}
