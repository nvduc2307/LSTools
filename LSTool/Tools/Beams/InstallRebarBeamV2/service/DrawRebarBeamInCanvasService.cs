using HcBimUtils;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using RevitApp.Utils.RevElements.RevRebars;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using RIMT.Utils.canvass;
using RIMT.Utils.RevRebars;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LSTool.Tools.Beams.InstallRebarBeamV2.models.SecondaryStirrups;
using Canvas = System.Windows.Controls.Canvas;
using Point = System.Windows.Point;
using Ellipse = System.Windows.Shapes.Ellipse;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.service;
using Brushes = System.Windows.Media.Brushes;
using LineSegment = System.Windows.Media.LineSegment;
using Size = System.Windows.Size;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.service
{
    public partial class DrawRebarBeamInCanvasSerice : IDrawRebarBeamInCanvasSerice
    {
        private ISubInstallRebarBeamInModelService _subInstallRebarBeamInModelService;
        public DrawRebarBeamInCanvasSerice(ISubInstallRebarBeamInModelService subInstallRebarBeamInModelService)
        {
            _subInstallRebarBeamInModelService = subInstallRebarBeamInModelService;
        }
        public void DrawOutLineFukashi(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
            var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
            var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;

            _drawSectionBeamFukashi(rebarBeam, canvasStart, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamFukashi(rebarBeam, canvasMid, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamFukashi(rebarBeam, canvasEnd, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

            var canvasStartStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            var canvasMidStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            var canvasEndStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;

            _drawSectionBeamFukashi(rebarBeam, canvasStartStirrup, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamFukashi(rebarBeam, canvasMidStirrup, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamFukashi(rebarBeam, canvasEndStirrup, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
        }
        public void DrawSectionBeamConcrete(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
            var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
            var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;
            _drawSectionBeamConcrete(rebarBeam, canvasStart);
            _drawSectionBeamConcrete(rebarBeam, canvasMid);
            _drawSectionBeamConcrete(rebarBeam, canvasEnd);

            var canvasStartStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            var canvasMidStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            var canvasEndStirrup = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
            _drawSectionBeamConcrete(rebarBeam, canvasStartStirrup);
            _drawSectionBeamConcrete(rebarBeam, canvasMidStirrup);
            _drawSectionBeamConcrete(rebarBeam, canvasEndStirrup);
        }

        public void DrawSectionBeamStirrup(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
            var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
            var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasStart, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasMid, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasEnd, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

            var canvasStirrupStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            var canvasStirrupMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            var canvasStirrupEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasStirrupStart, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasStirrupMid, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
            _drawSectionBeamStirrup(rebarBeam, installRebarBeamV2ViewModel.ElementInstances.CoverMm, canvasStirrupEnd, installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
        }

        public List<UIElement> DrawSectionBeamMainBar(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            try
            {
                var results = new List<UIElement>();
                var uiRebarMainTop = _drawSectionBeamMainBarTop(rebarBeam, installRebarBeamV2ViewModel);
                var uiRebarMainBot = _drawSectionBeamMainBarBot(rebarBeam, installRebarBeamV2ViewModel);
                results.AddRange(uiRebarMainTop);
                results.AddRange(uiRebarMainBot);
                return results;
            }
            catch (Exception)
            {
            }
            return new List<UIElement>();
        }

        public List<UIElement> DrawSectionBeamSideBar(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var results = new List<UIElement>();
            {
                try
                {
                    var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
                    var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
                    var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;

                    foreach (var item in installRebarBeamV2ViewModel.ElementInstances.SideBarUIElement)
                    {
                        try
                        {
                            canvasStart.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            canvasMid.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            canvasEnd.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    var uiElement1 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasStart,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionStart,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                    var uiElement2 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasMid,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionMid,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                    var uiElement3 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasEnd,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionEnd,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                    results.AddRange(uiElement1);
                    results.AddRange(uiElement2);
                    results.AddRange(uiElement3);
                }
                catch (Exception)
                {
                }
            }

            {
                try
                {
                    var canvasStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
                    var canvasMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
                    var canvasEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;

                    foreach (var item in installRebarBeamV2ViewModel.ElementInstances.SideBarUIElementStirrup)
                    {
                        try
                        {
                            canvasStart.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            canvasMid.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }

                        try
                        {
                            canvasEnd.Parent.Children.Remove(item);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    var uiElement1 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasStart,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionStart,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                    var uiElement2 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasMid,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionMid,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                    var uiElement3 = _drawSectionBeamSideBar(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                        canvasEnd,
                        RebarBeamMainBarLevelType.RebarBot,
                        RebarBeamSectionType.SectionEnd,
                        RebarBeamMainBarGroupType.GroupLevel1,
                        installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                    installRebarBeamV2ViewModel.ElementInstances.SideBarUIElementStirrup.AddRange(uiElement1);
                    installRebarBeamV2ViewModel.ElementInstances.SideBarUIElementStirrup.AddRange(uiElement2);
                    installRebarBeamV2ViewModel.ElementInstances.SideBarUIElementStirrup.AddRange(uiElement3);
                }
                catch (Exception)
                {
                }
            }
            return results;
        }


    }
}


