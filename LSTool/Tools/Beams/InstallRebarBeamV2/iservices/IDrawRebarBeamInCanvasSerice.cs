using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using System.Windows;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.iservices
{
    public interface IDrawRebarBeamInCanvasSerice
    {
        public void DrawOutLineFukashi(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public void DrawSectionBeamConcrete(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public void DrawSectionBeamStirrup(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<UIElement> DrawSectionBeamMainBar(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
        public List<UIElement> DrawSectionBeamSideBar(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel);
    }
}


