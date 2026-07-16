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
    public class DrawRebarBeamInCanvasSerice : IDrawRebarBeamInCanvasSerice
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

        private List<UIElement> _drawSectionBeamMainBarTop(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {

            var results = new List<UIElement>();
            {
                var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
                var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
                var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;

                foreach (var item in installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElement)
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

                var uiElement1 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement2 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement3 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

                var uiElement4 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement5 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement6 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

                var uiElement7 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement8 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement9 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                results.AddRange(uiElement1);
                results.AddRange(uiElement2);
                results.AddRange(uiElement3);
                results.AddRange(uiElement4);
                results.AddRange(uiElement5);
                results.AddRange(uiElement6);
                results.AddRange(uiElement7);
                results.AddRange(uiElement8);
                results.AddRange(uiElement9);
            }

            {
                var canvasStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
                var canvasMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
                var canvasEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;

                foreach (var item in installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup)
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

                // xóa thép đai nếu có sự thay đổi số lượng
                RefreshStirrupVertical(installRebarBeamV2ViewModel);
                RefreshStirrupHorizontal(installRebarBeamV2ViewModel, 1);

                var uiElement1 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);

                ////thêm sự kiện click
                //var index1 = 0;
                //foreach (var uiElement in uiElement1)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionStart
                //        .RebarBeamTop.RebarBeamTopLevel1.Hooks[index1];

                //    //var path = uiElement as Ellipse;
                //    //if (path == null) continue;
                //    //path.Tag = new MainRebarCanvas { Index = index1++, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    //path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}

                var uiElement2 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex2 = false;
                foreach (var uiElement in uiElement2)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionStart
                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex2)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex2 = true;
                    }
                }

                var uiElement3 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex3 = false;
                foreach (var uiElement in uiElement3)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionStart
                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex3)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex3 = true;
                    }
                }

                var uiElement4 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);
                //thêm sự kiện click
                //var index4 = 0;
                //foreach (var uiElement in uiElement4)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionMid
                //        .RebarBeamTop.RebarBeamTopLevel1.Hooks[index4];

                //    var path = uiElement as Ellipse;
                //    if (path == null) continue;
                //    path.Tag = new MainRebarCanvas { Index = index4++, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}
                var uiElement5 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex5 = false;
                foreach (var uiElement in uiElement5)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionMid
                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex5)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex5 = true;
                    }
                }
                var uiElement6 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex6 = false;
                foreach (var uiElement in uiElement6)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionMid
                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex6)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex6 = true;
                    }
                }

                var uiElement7 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);
                //thêm sự kiện click
                //var index7 = 0;
                //foreach (var uiElement in uiElement7)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionEnd
                //        .RebarBeamTop.RebarBeamTopLevel1.Hooks[index7];

                //    var path = uiElement as Ellipse;
                //    if (path == null) continue;
                //    path.Tag = new MainRebarCanvas { Index = index7++, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}

                var uiElement8 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex8 = false;
                foreach (var uiElement in uiElement8)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionEnd
                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex8)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex8 = true;
                    }
                }

                var uiElement9 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarTop,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex9 = false;
                foreach (var uiElement in uiElement9)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionEnd
                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex9)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex9 = true;
                    }
                }
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement1);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement2);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement3);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement4);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement5);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement6);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement7);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement8);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarTopUIElementStirrup.AddRange(uiElement9);

            }
            return results;
        }

        private List<UIElement> _drawSectionBeamMainBarBot(RebarBeam rebarBeam, InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {
            var results = new List<UIElement>();
            {
                var canvasStart = installRebarBeamV2ViewModel.CanvasPageSectionStart;
                var canvasMid = installRebarBeamV2ViewModel.CanvasPageSectionMid;
                var canvasEnd = installRebarBeamV2ViewModel.CanvasPageSectionEnd;

                foreach (var item in installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElement)
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

                var uiElement1 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement2 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement3 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

                var uiElement4 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement5 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement6 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);

                var uiElement7 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement8 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                var uiElement9 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi);
                results.AddRange(uiElement1);
                results.AddRange(uiElement2);
                results.AddRange(uiElement3);
                results.AddRange(uiElement4);
                results.AddRange(uiElement5);
                results.AddRange(uiElement6);
                results.AddRange(uiElement7);
                results.AddRange(uiElement8);
                results.AddRange(uiElement9);
            }

            {
                var canvasStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
                var canvasMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
                var canvasEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;

                foreach (var item in installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup)
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

                // xóa thép đai nếu có sự thay đổi số lượng
                //RefreshStirrupVertical(installRebarBeamV2ViewModel); // không cần xóa đai vì ở trên top đã xóa rồi
                RefreshStirrupHorizontal(installRebarBeamV2ViewModel, 2);

                var uiElement1 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);

                //thêm sự kiện click
                //var index1 = 0;
                //foreach (var uiElement in uiElement1)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionStart
                //        .RebarBeamBot.RebarBeamBotLevel1.Hooks[index1];

                //    //var path = uiElement as Ellipse;
                //    //if (path == null) continue;
                //    //path.Tag = new MainRebarCanvas { Index = index1++, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    //path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}

                var uiElement2 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex2 = false;
                foreach (var uiElement in uiElement2)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionStart
                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex2)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex2 = true;
                    }
                }
                var uiElement3 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasStart,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionStart,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex3 = false;
                foreach (var uiElement in uiElement3)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionStart
                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex3)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex3 = true;
                    }
                }

                var uiElement4 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);

                //thêm sự kiện click
                //var index4 = 0;
                //foreach (var uiElement in uiElement4)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionMid
                //        .RebarBeamBot.RebarBeamBotLevel1.Hooks[index4];

                //    var path = uiElement as Ellipse;
                //    if (path == null) continue;
                //    path.Tag = new MainRebarCanvas { Index = index4++, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}
                var uiElement5 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex5 = false;
                foreach (var uiElement in uiElement5)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionMid
                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex5)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex5 = true;
                    }
                }

                var uiElement6 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasMid,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionMid,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex6 = false;
                foreach (var uiElement in uiElement6)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionMid
                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex6)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex6 = true;
                    }
                }


                var uiElement7 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel1,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14, true);

                //thêm sự kiện click
                //var index7 = 0;
                //foreach (var uiElement in uiElement7)
                //{
                //    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                //        .RebarBeamSectionEnd
                //        .RebarBeamBot.RebarBeamBotLevel1.Hooks[index7];

                //    var path = uiElement as Ellipse;
                //    if (path == null) continue;
                //    path.Tag = new MainRebarCanvas { Index = index7++, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                //    path.MouseLeftButtonUp += CircleLevel1MouseUp;
                //    //DrawHookVerticalAtMainRebarCanvas(path, findIndex);
                //}
                var uiElement8 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel2,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex8 = false;
                foreach (var uiElement in uiElement8)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionEnd
                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 1, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex8)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex8 = true;
                    }
                }
                var uiElement9 = _drawSectionBeamMainBar(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    installRebarBeamV2ViewModel.ElementInstances.CoverMm,
                    canvasEnd,
                    RebarBeamMainBarLevelType.RebarBot,
                    RebarBeamSectionType.SectionEnd,
                    RebarBeamMainBarGroupType.GroupLevel3,
                    installRebarBeamV2ViewModel.ElementInstances.BeamFukashi, 14);
                //thêm sự kiện click
                var flagIndex9 = false;
                foreach (var uiElement in uiElement9)
                {
                    var findIndex = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive
                        .RebarBeamSectionEnd
                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook;

                    var path = uiElement as Ellipse;
                    if (path == null) continue;
                    path.Tag = new MainRebarNotLevel1Canvas { Index = 2, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel };
                    path.MouseLeftButtonUp += CircleForHorizontalStirrupMouseUp;
                    if (!flagIndex9)
                    {
                        DrawHookHorizontalAtMainRebarCanvas(path, findIndex);
                        flagIndex9 = true;
                    }
                }

                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement1);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement2);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement3);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement4);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement5);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement6);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement7);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement8);
                installRebarBeamV2ViewModel.ElementInstances.MainRebarBotUIElementStirrup.AddRange(uiElement9);
            }
            return results;
        }

        private Path DrawHook(Point start, Point end)
        {
            const double hookRadius = 7;
            const int extend = 5;

            var direction = end - start;
            var path1 = new Path { StrokeThickness = 1, Stroke = Brushes.Black };
            var pathFigure = new PathFigure
            {
                IsClosed = false
            };

            if (!direction.X.IsEqual(0))
            {
                start = start with { X = start.X + hookRadius };
                end = end with { X = end.X - hookRadius };
                var p2 = start with { Y = start.Y + hookRadius * 2 };
                var p1 = p2 with { X = p2.X + extend };
                var p3 = start;
                var p4 = end;
                var p5 = end with { Y = end.Y + hookRadius * 2 };
                var p6 = p5 with { X = p5.X - extend };

                pathFigure.StartPoint = p1;
                pathFigure.Segments.Add(new LineSegment(p2, true));
                var arcSegment = new ArcSegment
                {
                    Point = p3,
                    Size = new Size(hookRadius, hookRadius),
                    SweepDirection = SweepDirection.Clockwise
                };
                pathFigure.Segments.Add(arcSegment);
                pathFigure.Segments.Add(new LineSegment(p4, true));

                var arcSegment2 = new ArcSegment
                {
                    Point = p5,
                    Size = new Size(hookRadius, hookRadius),
                    SweepDirection = SweepDirection.Clockwise
                };
                pathFigure.Segments.Add(arcSegment2);
                pathFigure.Segments.Add(new LineSegment(p6, true));
            }
            else
            {
                start = start with { Y = start.Y + hookRadius };
                end = end with { Y = end.Y - hookRadius };

                var p2 = start with { X = start.X + hookRadius * 2 };
                var p1 = p2 with { Y = p2.Y + extend };
                var p3 = start;
                var p4 = end;
                var p5 = end with { X = end.X + hookRadius * 2 };
                var p6 = p5 with { Y = p5.Y - extend };

                pathFigure.StartPoint = p1;
                pathFigure.Segments.Add(new LineSegment(p2, true));
                var arcSegment = new ArcSegment
                {
                    Point = p3,
                    Size = new Size(hookRadius, hookRadius),
                    SweepDirection = SweepDirection.Counterclockwise
                };
                pathFigure.Segments.Add(arcSegment);
                pathFigure.Segments.Add(new LineSegment(p4, true));

                var arcSegment2 = new ArcSegment
                {
                    Point = p5,
                    Size = new Size(hookRadius, hookRadius),
                    SweepDirection = SweepDirection.Counterclockwise
                };
                pathFigure.Segments.Add(arcSegment2);
                pathFigure.Segments.Add(new LineSegment(p6, true));
            }

            var pathGeometry = new PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            path1.Data = pathGeometry;

            return path1;
        }

        private void RefreshStirrupVertical(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel)
        {

            var canvasStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            var canvasMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            var canvasEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
            // xử lý thép đai
            var stirrupVerticalStartCanvas = canvasStart.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupVerticalCanvas).ToList();

            foreach (var path in stirrupVerticalStartCanvas)
            {
                canvasStart.Parent.Children.Remove(path);
            }

            var stirrupVerticalMidCanvas = canvasMid.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupVerticalCanvas).ToList();

            foreach (var path in stirrupVerticalMidCanvas)
            {
                canvasMid.Parent.Children.Remove(path);
            }

            var stirrupVerticalEndCanvas = canvasEnd.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupVerticalCanvas).ToList();

            foreach (var path in stirrupVerticalEndCanvas)
            {
                canvasEnd.Parent.Children.Remove(path);
            }
            return;
            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamTop
            //    .RebarBeamTopLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}

            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamTop
            //    .RebarBeamTopLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}

            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamTop
            //    .RebarBeamTopLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamBot
            //        .RebarBeamBotLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}

            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamBot
            //    .RebarBeamBotLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}

            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamBot
            //    .RebarBeamBotLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}

            //if (installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamBot
            //    .RebarBeamBotLevel1.Hooks.Values.All(x => !x))
            //{
            //    installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd.RebarBeamTop
            //        .RebarBeamTopLevel1.Hooks.ToDictionary(x => x.Key, x => false);
            //}
            //end xử lý thép đai
        }

        private void RefreshStirrupHorizontal(InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel, int position)
        {
            var canvasStart = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            var canvasMid = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            var canvasEnd = installRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;

            var stirrupVerticalStartCanvas = canvasStart.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupHorizontalCanvas tag && tag.Position == position).ToList();

            foreach (var path in stirrupVerticalStartCanvas)
            {
                canvasStart.Parent.Children.Remove(path);
            }

            var stirrupVerticalMidCanvas = canvasMid.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupHorizontalCanvas tag && tag.Position == position).ToList();

            foreach (var path in stirrupVerticalMidCanvas)
            {
                canvasMid.Parent.Children.Remove(path);
            }

            var stirrupVerticalEndCanvas = canvasEnd.Parent.Children
                .OfType<Path>()
                .Where(path => path.Tag is SecondaryStirrupHorizontalCanvas tag && tag.Position == position).ToList();

            foreach (var path in stirrupVerticalEndCanvas)
            {
                canvasEnd.Parent.Children.Remove(path);
            }

            //var sectionStart = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart;
            //if (sectionStart.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook == false
            //    || sectionStart.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook == false
            //    || sectionStart.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook == false
            //    || sectionStart.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook == false)
            //{
            //    sectionStart.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = false;
            //    sectionStart.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = false;
            //    sectionStart.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = false;
            //    sectionStart.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = false;
            //}

            //var sectionMid = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid;
            //if (sectionMid.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook == false
            //    || sectionMid.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook == false
            //    || sectionMid.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook == false
            //    || sectionMid.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook == false)
            //{
            //    sectionMid.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = false;
            //    sectionMid.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = false;
            //    sectionMid.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = false;
            //    sectionMid.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = false;
            //}

            //var sectionEnd = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd;
            //if (sectionEnd.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook == false
            //    || sectionEnd.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook == false
            //    || sectionEnd.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook == false
            //    || sectionEnd.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook == false)
            //{
            //    sectionEnd.RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = false;
            //    sectionEnd.RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = false;
            //    sectionEnd.RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = false;
            //    sectionEnd.RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = false;
            //}

        }

        private void DrawHookVerticalAtMainRebarCanvas(Ellipse ellipse, bool hasDraw)
        {
            //if (ellipse.Tag is not MainRebarCanvas tag)
            //{
            //    return;
            //}

            //var parent = ellipse.Parent as Canvas;
            //var parentName = parent.Name;
            //var topQuantity = 0;
            //var bottomQuantity = 0;
            //CanvasPageBase canvasPage;
            //switch (parentName)
            //{
            //    case "CanvasSectionStart":
            //        topQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
            //            .RebarBeamTop.RebarBeamTopLevel1.Quantity;
            //        bottomQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
            //            .RebarBeamBot.RebarBeamBotLevel1.Quantity;
            //        canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
            //        break;
            //    case "CanvasSectionMid":
            //        topQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
            //            .RebarBeamTop.RebarBeamTopLevel1.Quantity;
            //        bottomQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
            //            .RebarBeamBot.RebarBeamBotLevel1.Quantity;
            //        canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
            //        break;
            //    case "CanvasSectionEnd":
            //        topQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
            //            .RebarBeamTop.RebarBeamTopLevel1.Quantity;
            //        bottomQuantity = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
            //            .RebarBeamBot.RebarBeamBotLevel1.Quantity;
            //        canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
            //        break;
            //    default: throw new Exception("Not found CanvasSection");
            //}
            //;

            //var positionIndex = new PositionIndex
            //{
            //    Index = tag.Index
            //};

            //var isTop = tag.Position == 1;
            //var canAnchor = new Tuple<int, int, bool>(-1, -1, false);
            //canAnchor = bottomQuantity > topQuantity
            //    ? positionIndex.Valid(topQuantity, bottomQuantity, isTop)
            //    : positionIndex.Valid2(topQuantity, bottomQuantity, isTop);

            //if (!canAnchor.Item3)
            //{
            //    return;
            //}

            //var top = Canvas.GetTop(ellipse);
            //var left = Canvas.GetLeft(ellipse);

            //var rebarBeam = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive;
            //var scale = canvasPage.RatioScale * canvasPage.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            //var coverMm = tag.InstallRebarBeamV2ViewModel.ElementInstances.CoverMm;
            //var beamFukashi = tag.InstallRebarBeamV2ViewModel.ElementInstances.BeamFukashi;
            //var centerCanvas = canvasPage.Center;
            //var p1 = beamFukashi == null
            //    ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
            //    : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            //var p2 = beamFukashi == null
            //    ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
            //    : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            //var p3 = beamFukashi == null
            //    ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
            //    : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            //var p4 = beamFukashi == null
            //    ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
            //    : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));

            //var startPoint = p1 with { X = left };
            //var endPoint = p4 with { X = left };

            //if (hasDraw)
            //{
            //    var path1 = DrawHook(startPoint, endPoint);
            //    path1.Tag = new SecondaryStirrupVerticalCanvas { IndexBottom = canAnchor.Item2, IndexTop = canAnchor.Item1 };
            //    parent.Children.Add(path1);


            //}
            //else
            //{
            //    UIElement elementStirrup = null;
            //    foreach (UIElement element in canvasPage.Parent.Children)
            //    {
            //        if (element is Path { Tag: SecondaryStirrupVerticalCanvas positionMainRebar })
            //        {
            //            var parentTop = positionMainRebar.IndexTop;
            //            var parentBottom = positionMainRebar.IndexBottom;
            //            if (parentTop == tag.Index || parentBottom == tag.Index)
            //            {
            //                elementStirrup = element;
            //            }
            //        }
            //    }
            //    if (elementStirrup != null)
            //    {
            //        canvasPage.Parent.Children.Remove(elementStirrup);
            //    }


            //}

            //switch (parentName)
            //{
            //    case "CanvasSectionStart":
            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
            //                .RebarBeamTop.RebarBeamTopLevel1.Hooks[canAnchor.Item1] =
            //            hasDraw;

            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
            //                .RebarBeamBot.RebarBeamBotLevel1.Hooks[canAnchor.Item2] =
            //            hasDraw;
            //        break;
            //    case "CanvasSectionMid":
            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
            //                .RebarBeamTop.RebarBeamTopLevel1.Hooks[canAnchor.Item1] = hasDraw;

            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
            //                .RebarBeamBot.RebarBeamBotLevel1.Hooks[canAnchor.Item2] = hasDraw;
            //        break;
            //    case "CanvasSectionEnd":
            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
            //                .RebarBeamTop.RebarBeamTopLevel1.Hooks[canAnchor.Item1] = hasDraw;

            //        tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
            //                .RebarBeamBot.RebarBeamBotLevel1.Hooks[canAnchor.Item2] = hasDraw;
            //        break;
            //}
        }

        private void DrawHookVerticalAtMainRebarCanvas2(Ellipse ellipse, bool hasDraw, bool X)
        {
            if (hasDraw)
            {
            }

            if (ellipse.Tag is not MainRebarCanvas tag)
            {
                return;
            }

            var parent = ellipse.Parent as Canvas;
            var parentName = parent.Name;

            var flag = false;
            foreach (UIElement element in parent.Children)
            {
                if (element is Ellipse path)
                {
                    if (path.Tag is MainRebarCanvas mainRebarTag && mainRebarTag.Position != tag.Position && mainRebarTag.Index == tag.Index)
                    {
                        flag = true;
                    }
                }
            }

            CanvasPageBase canvasPage;
            switch (parentName)
            {
                case "CanvasSectionStart":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
                    if (tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                           .RebarBeamTop.RebarBeamTopLevel1.Hooks2.Count != tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2.Count)
                    {
                        return;
                    }

                    break;
                case "CanvasSectionMid":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
                    if (tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                           .RebarBeamTop.RebarBeamTopLevel1.Hooks2.Count != tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2.Count)
                    {
                        return;
                    }
                    break;
                case "CanvasSectionEnd":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
                    if (tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                           .RebarBeamTop.RebarBeamTopLevel1.Hooks2.Count != tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2.Count)
                    {
                        return;
                    }
                    break;
                default: throw new Exception("Not found CanvasSection");
            }

            var positionIndex = new PositionIndex
            {
                Index = tag.Index
            };

            if (!flag && X) return;

            var top = Canvas.GetTop(ellipse);
            var left = Canvas.GetLeft(ellipse);

            var rebarBeam = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive;
            var scale = canvasPage.RatioScale * canvasPage.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var coverMm = tag.InstallRebarBeamV2ViewModel.ElementInstances.CoverMm;
            var beamFukashi = tag.InstallRebarBeamV2ViewModel.ElementInstances.BeamFukashi;
            var centerCanvas = canvasPage.Center;
            var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));

            var startPoint = p1 with { X = left };
            var endPoint = p4 with { X = left };

            if (hasDraw)
            {
                var path1 = DrawHook(startPoint, endPoint);
                path1.Tag = new SecondaryStirrupVerticalCanvas { IndexBottom = tag.Index };
                parent.Children.Add(path1);


            }
            else
            {
                UIElement elementStirrup = null;
                foreach (UIElement element in canvasPage.Parent.Children)
                {
                    if (element is Path { Tag: SecondaryStirrupVerticalCanvas positionMainRebar })
                    {
                        var parentTop = positionMainRebar.IndexTop;
                        var parentBottom = positionMainRebar.IndexBottom;
                        if (parentTop == tag.Index || parentBottom == tag.Index)
                        {
                            elementStirrup = element;
                        }
                    }
                }
                if (elementStirrup != null)
                {
                    canvasPage.Parent.Children.Remove(elementStirrup);
                }
            }

            switch (parentName)
            {
                case "CanvasSectionStart":
                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                            .RebarBeamTop.RebarBeamTopLevel1.Hooks2[tag.Index] =
                        hasDraw;

                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2[tag.Index] =
                        hasDraw;
                    break;
                case "CanvasSectionMid":
                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                            .RebarBeamTop.RebarBeamTopLevel1.Hooks2[tag.Index] = hasDraw;

                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2[tag.Index] = hasDraw;
                    break;
                case "CanvasSectionEnd":
                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                            .RebarBeamTop.RebarBeamTopLevel1.Hooks2[tag.Index] = hasDraw;

                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                            .RebarBeamBot.RebarBeamBotLevel1.Hooks2[tag.Index] = hasDraw;
                    break;
            }
        }

        private void CircleLevel1MouseUp(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;

            if (ellipse.Tag is not MainRebarCanvas tag)
            {
                return;
            }

            UIElement elementStirrup = null;
            var canvasParent = ellipse.Parent as Canvas;
            foreach (UIElement element in canvasParent.Children)
            {
                if (element is Path path)
                {
                    if (path.Tag is SecondaryStirrupVerticalCanvas positionMainRebar)
                    {
                        var parentTop = positionMainRebar.IndexTop;
                        var parentBottom = positionMainRebar.IndexBottom;
                        if (parentTop == tag.Index || parentBottom == tag.Index)
                        {
                            elementStirrup = element;
                        }
                    }
                }
            }

            if (elementStirrup == null)
            {
                DrawHookVerticalAtMainRebarCanvas(ellipse, true);
            }
            else
            {
                DrawHookVerticalAtMainRebarCanvas(ellipse, false);
            }
        }


        private void CircleLevel1MouseUp2(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;

            if (ellipse.Tag is not MainRebarCanvas tag)
            {
                return;
            }

            UIElement elementStirrup = null;
            var canvasParent = ellipse.Parent as Canvas;
            foreach (UIElement element in canvasParent.Children)
            {
                if (element is Path path)
                {
                    if (path.Tag is SecondaryStirrupVerticalCanvas positionMainRebar)
                    {
                        var parentTop = positionMainRebar.IndexTop;
                        var parentBottom = positionMainRebar.IndexBottom;
                        if (parentTop == tag.Index || parentBottom == tag.Index)
                        {
                            elementStirrup = element;
                        }
                    }
                }
            }

            if (elementStirrup == null)
            {
                DrawHookVerticalAtMainRebarCanvas2(ellipse, true, true);
            }
            else
            {
                DrawHookVerticalAtMainRebarCanvas2(ellipse, false, true);
            }
        }


        private void CircleForHorizontalStirrupMouseUp(object sender, MouseButtonEventArgs e)
        {
            var ellipse = sender as Ellipse;

            if (ellipse.Tag is not MainRebarNotLevel1Canvas tag)
            {
                return;
            }

            UIElement elementStirrup = null;
            var canvasParent = ellipse.Parent as Canvas;
            foreach (UIElement element in canvasParent.Children)
            {
                if (element is Path path)
                {
                    if (path.Tag is SecondaryStirrupHorizontalCanvas positionMainRebar && positionMainRebar.Index == tag.Index && positionMainRebar.Position == tag.Position)
                    {
                        elementStirrup = element;
                    }
                }
            }

            if (elementStirrup == null)
            {
                DrawHookHorizontalAtMainRebarCanvas(ellipse, true);
            }
            else
            {
                DrawHookHorizontalAtMainRebarCanvas(ellipse, false);
            }
        }

        private void DrawHookHorizontalAtMainRebarCanvas(Ellipse ellipse, bool hasDraw)
        {
            if (ellipse.Tag is not MainRebarNotLevel1Canvas tag)
            {
                return;
            }

            var parent = ellipse.Parent as Canvas;
            var parentName = parent.Name;

            CanvasPageBase canvasPage;
            switch (parentName)
            {
                case "CanvasSectionStart":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageStart;
                    break;
                case "CanvasSectionMid":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageMid;
                    break;
                case "CanvasSectionEnd":
                    canvasPage = tag.InstallRebarBeamV2ViewModel.SettingStirrupSectionViewModel.CanvasPageEnd;
                    break;
                default: throw new Exception("Not found CanvasSection");
            }

            var top = Canvas.GetTop(ellipse);
            var left = Canvas.GetLeft(ellipse);

            var rebarBeam = tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive;
            var scale = canvasPage.RatioScale * canvasPage.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var coverMm = tag.InstallRebarBeamV2ViewModel.ElementInstances.CoverMm;
            var beamFukashi = tag.InstallRebarBeamV2ViewModel.ElementInstances.BeamFukashi;
            var centerCanvas = canvasPage.Center;
            var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));

            var startPoint = p1 with { Y = top };
            var endPoint = p2 with { Y = top };

            if (hasDraw)
            {
                var path1 = DrawHook(startPoint, endPoint);
                path1.Tag = new SecondaryStirrupHorizontalCanvas { Index = tag.Index, Position = tag.Position };
                parent.Children.Add(path1);
            }
            else
            {
                UIElement elementStirrup = null;
                foreach (UIElement element in canvasPage.Parent.Children)
                {
                    if (element is Path x1)
                    {
                        if (x1.Tag is SecondaryStirrupHorizontalCanvas positionMainRebar && positionMainRebar.Index == tag.Index && positionMainRebar.Position == tag.Position)
                        {
                            elementStirrup = element;
                        }
                    }
                }
                if (elementStirrup != null)
                {
                    canvasPage.Parent.Children.Remove(elementStirrup);
                }
            }

            switch (parentName)
            {
                case "CanvasSectionStart":
                    switch (tag.Position)
                    {
                        // top rebar
                        case 1:
                            switch (tag.Index)
                            {
                                // top 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                // top 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }

                            break;
                        //bottom rebar
                        case 2:
                            switch (tag.Index)
                            {
                                //bottom 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                //bottom 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart
                                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }

                            break;
                    }
                    break;
                case "CanvasSectionMid":
                    switch (tag.Position)
                    {
                        // top rebar
                        case 1:
                            switch (tag.Index)
                            {
                                // top 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                // top 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }

                            break;
                        //bottom rebar
                        case 2:
                            switch (tag.Index)
                            {
                                //bottom 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                //bottom 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid
                                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }

                            break;
                    }
                    break;
                case "CanvasSectionEnd":
                    switch (tag.Position)
                    {
                        // top rebar
                        case 1:
                            switch (tag.Index)
                            {
                                // top 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                                        .RebarBeamTop.RebarBeamTopLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                // top 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                                        .RebarBeamTop.RebarBeamTopLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }
                            break;
                        //bottom rebar
                        case 2:
                            switch (tag.Index)
                            {
                                //bottom 2
                                case 1:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                                        .RebarBeamBot.RebarBeamBotLevel2.HasHorizontalHook = hasDraw;
                                    break;
                                //bottom 3
                                case 2:
                                    tag.InstallRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd
                                        .RebarBeamBot.RebarBeamBotLevel3.HasHorizontalHook = hasDraw;
                                    break;
                            }
                            break;
                    }
                    break;
            }
        }
        private List<UIElement> _drawSectionBeamSideBar(
            RebarBeam rebarBeam,
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double coverMm,
            CanvasPageBase canvasPageBase,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamSectionType sectionType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            BeamFukashi beamFukashi = null,
            int diameterMm = 7)
        {
            var results = new List<UIElement>();
            try
            {
                var centerCanvas = canvasPageBase.Center;
                var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
                var option = OptionStyleInstanceInCanvas.OPTION_REBAR;
                var rebarBarTypeCustoms = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms;
                var distanceRToRMm = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm;
                var coverUpMm = coverMm;
                var coverSideMm = coverMm;
                _getCover(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    coverMm,
                    canvasPageBase,
                    sectionType,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType,
                    out double _coverUpMm,
                    out double _coverSideMm);
                coverUpMm = _coverUpMm + 40;
                coverSideMm = _coverSideMm + 40;
                var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiTop.ValueMm));
                var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiTop.ValueMm));
                var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiBot.ValueMm));
                var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiBot.ValueMm));
                var ps = new List<Point>() { p1, p2, p3, p4 };
                //var diameterMm = 7;
                InstanceInCanvasCircel circleL = null;
                InstanceInCanvasCircel circleR = null;
                int qty = 0;
                int qtyHaft = 0;
                RebarBeamSection section = null;
                double distance = (p1 - p4).Length - 50 * 6 * scale;
                double spacing = 0;
                var midL = new Point((p1.X + p4.X) * 0.5, (p1.Y + p4.Y) * 0.5);
                var midR = new Point((p2.X + p3.X) * 0.5, (p2.Y + p3.Y) * 0.5);
                var pL = midL;
                var pR = midR;
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart;
                        qty = section.RebarBeamSideBar.QuantitySide;
                        qtyHaft = (qty - 1) / 2;
                        spacing = distance / (qty + 1);
                        pL = qty % 2 == 0
                            ? new System.Windows.Point(midL.X, midL.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midL.X, midL.Y - qtyHaft * spacing);
                        pR = qty % 2 == 0
                            ? new System.Windows.Point(midR.X, midR.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midR.X, midR.Y - qtyHaft * spacing);
                        for (int i = 0; i < qty; i++)
                        {
                            var ppL = pL.Translate(new System.Windows.Point(0, spacing * i));
                            var ppR = pR.Translate(new System.Windows.Point(0, spacing * i));
                            circleL = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppL, new System.Windows.Point(0, 0), "");
                            circleR = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppR, new System.Windows.Point(0, 0), "");
                            circleL.DrawInCanvas();
                            circleR.DrawInCanvas();
                            results.Add(circleL.UIElement);
                            results.Add(circleR.UIElement);
                        }
                        break;
                    case RebarBeamSectionType.SectionMid:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid;
                        qty = section.RebarBeamSideBar.QuantitySide;
                        qtyHaft = (qty - 1) / 2;
                        spacing = distance / (qty + 1);
                        pL = qty % 2 == 0
                            ? new System.Windows.Point(midL.X, midL.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midL.X, midL.Y - qtyHaft * spacing);
                        pR = qty % 2 == 0
                            ? new System.Windows.Point(midR.X, midR.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midR.X, midR.Y - qtyHaft * spacing);
                        for (int i = 0; i < qty; i++)
                        {
                            var ppL = pL.Translate(new System.Windows.Point(0, spacing * i));
                            var ppR = pR.Translate(new System.Windows.Point(0, spacing * i));
                            circleL = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppL, new System.Windows.Point(0, 0), "");
                            circleR = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppR, new System.Windows.Point(0, 0), "");
                            circleL.DrawInCanvas();
                            circleR.DrawInCanvas();
                            results.Add(circleL.UIElement);
                            results.Add(circleR.UIElement);
                        }
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd;
                        qty = section.RebarBeamSideBar.QuantitySide;
                        spacing = distance / (qty + 1);
                        qtyHaft = (qty - 1) / 2;
                        pL = qty % 2 == 0
                            ? new System.Windows.Point(midL.X, midL.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midL.X, midL.Y - qtyHaft * spacing);
                        pR = qty % 2 == 0
                            ? new System.Windows.Point(midR.X, midR.Y - spacing / 2 - qtyHaft * spacing)
                            : new System.Windows.Point(midR.X, midR.Y - qtyHaft * spacing);
                        for (int i = 0; i < qty; i++)
                        {
                            var ppL = pL.Translate(new System.Windows.Point(0, spacing * i));
                            var ppR = pR.Translate(new System.Windows.Point(0, spacing * i));
                            circleL = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppL, new System.Windows.Point(0, 0), "");
                            circleR = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, ppR, new System.Windows.Point(0, 0), "");
                            circleL.DrawInCanvas();
                            circleR.DrawInCanvas();
                            results.Add(circleL.UIElement);
                            results.Add(circleR.UIElement);
                        }

                        break;
                }
            }
            catch (Exception)
            {
            }
            return results;
        }
        private List<UIElement> _drawSectionBeamMainBar(
            RebarBeam rebarBeam,
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double coverMm,
            CanvasPageBase canvasPageBase,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamSectionType sectionType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            BeamFukashi beamFukashi = null, int diameterMm = 7, bool isSectionStirrupLevel1 = false)
        {
            var results = new List<UIElement>();
            try
            {
                var rebarBeams = installRebarBeamV2ViewModel.ElementInstances.RebarBeams;
                var subBeams = installRebarBeamV2ViewModel.ElementInstances.Beam.ElementSubs;
                var qRebarBeams = rebarBeams.Count;

                var rebarGroupInfos = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosStart = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionStart,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosMid = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionMid,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                var rebarGroupInfosEnd = _subInstallRebarBeamInModelService.GetRebarBeamGroupInfo(
                    installRebarBeamV2ViewModel,
                    RebarBeamSectionType.SectionEnd,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType);
                // all rebarInfo of Beams
                var rebarBeamMains = _subInstallRebarBeamInModelService.GetRebarBeamAllSection(installRebarBeamV2ViewModel);
                var rqMax = rebarBeamMains.Max(x => x.Quantity);
                var rqMin = rebarBeamMains.Min(x => x.Quantity);
                // all RebarInfo on Beam, include Start, Mid, End
                var rebarInfos = new List<RebarBeamMainBar>();
                for (int i = 0; i < qRebarBeams; i++)
                {
                    rebarInfos.Add(rebarGroupInfosStart[i]);
                    rebarInfos.Add(rebarGroupInfosMid[i]);
                    rebarInfos.Add(rebarGroupInfosEnd[i]);
                }

                var qRebarsMax = rebarInfos.Max(x => x.Quantity) > rqMax ? rebarInfos.Max(x => x.Quantity) : rqMax;

                var centerCanvas = canvasPageBase.Center;
                var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
                var option = OptionStyleInstanceInCanvas.OPTION_REBAR;
                var rebarBarTypeCustoms = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms;
                var distanceRToRMm = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm;
                var coverUpMm = coverMm;
                var coverSideMm = coverMm;
                _getCover(
                    rebarBeam,
                    installRebarBeamV2ViewModel,
                    coverMm,
                    canvasPageBase,
                    sectionType,
                    rebarBeamMainBarLevelType,
                    rebarBeamMainBarGroupType,
                    out double _coverUpMm,
                    out double _coverSideMm);
                coverUpMm = _coverUpMm + 40;
                coverSideMm = _coverSideMm + 40;
                var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiTop.ValueMm));
                var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiTop.ValueMm));
                var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiBot.ValueMm));
                var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverSideMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverUpMm - beamFukashi.FukashiBot.ValueMm));
                var ps = new List<Point>() { p1, p2, p3, p4 };
                //var diameterMm = 7;
                InstanceInCanvasCircel circle = null;
                int qty = 0;
                RebarBeamSection section = null;
                double distance = (p1 - p2).Length;
                double spacingMin = distance / (qRebarsMax - 1);
                double spacing = 0;
                switch (sectionType)
                {
                    case RebarBeamSectionType.SectionStart:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart;
                        var hardCode = section.GetHashCode().ToString();
                        switch (rebarBeamMainBarLevelType)
                        {
                            case RebarBeamMainBarLevelType.RebarTop:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamTop.RebarBeamTopLevel1.Hooks2.Any();

                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamTop.RebarBeamTopLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();

                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;

                                                    var findIndex = section
                                                               .RebarBeamTop.RebarBeamTopLevel1.Hooks2[i];

                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }

                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                            case RebarBeamMainBarLevelType.RebarBot:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamBot.RebarBeamBotLevel1.Hooks2.Any();
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamBot.RebarBeamBotLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();

                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;
                                                    var findIndex = section
                                                               .RebarBeamBot.RebarBeamBotLevel1.Hooks2[i];
                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }

                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                    case RebarBeamSectionType.SectionMid:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid;
                        switch (rebarBeamMainBarLevelType)
                        {
                            case RebarBeamMainBarLevelType.RebarTop:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamTop.RebarBeamTopLevel1.Hooks2.Any();
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamTop.RebarBeamTopLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;

                                                    var findIndex = section.RebarBeamTop.RebarBeamTopLevel1.Hooks2[i];
                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                            case RebarBeamMainBarLevelType.RebarBot:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamBot.RebarBeamBotLevel1.Hooks2.Any();
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamBot.RebarBeamBotLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;

                                                    var findIndex = section.RebarBeamBot.RebarBeamBotLevel1.Hooks2[i];
                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                    case RebarBeamSectionType.SectionEnd:
                        section = installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionEnd;
                        switch (rebarBeamMainBarLevelType)
                        {
                            case RebarBeamMainBarLevelType.RebarTop:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamTop.RebarBeamTopLevel1.Hooks2.Any();
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamTop.RebarBeamTopLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();

                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;

                                                    var findIndex = section.RebarBeamTop.RebarBeamTopLevel1.Hooks2[i];
                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 1, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = false };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }

                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamTop.RebarBeamTopLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p1.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                            case RebarBeamMainBarLevelType.RebarBot:
                                switch (rebarBeamMainBarGroupType)
                                {
                                    case RebarBeamMainBarGroupType.GroupLevel1:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel1.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            var flag = section.RebarBeamBot.RebarBeamBotLevel1.Hooks2.Any();
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!flag)
                                                {
                                                    section.RebarBeamBot.RebarBeamBotLevel1.Hooks2[i] = false;
                                                }
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                if (isSectionStirrupLevel1)
                                                {
                                                    var path = circle.UIElement as Ellipse;
                                                    if (path == null) continue;

                                                    var findIndex = section.RebarBeamBot.RebarBeamBotLevel1.Hooks2[i];
                                                    path.Tag = new MainRebarCanvas { Index = i, Position = 2, InstallRebarBeamV2ViewModel = installRebarBeamV2ViewModel, IsHook = findIndex };
                                                    path.MouseLeftButtonUp += CircleLevel1MouseUp2;
                                                    DrawHookVerticalAtMainRebarCanvas2(path, findIndex, false);
                                                }
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel2:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel2.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                    case RebarBeamMainBarGroupType.GroupLevel3:
                                        qty = section.RebarBeamBot.RebarBeamBotLevel3.Quantity;
                                        if (qty != 0)
                                        {
                                            spacing = distance / (qty - 1);
                                            for (int i = 0; i < qRebarsMax; i++)
                                            {
                                                var dk = SubInstallRebarBeamInModelService.CheckIndexRebarMain(i, qty, qRebarsMax, spacing, spacingMin);
                                                if (!dk) continue;
                                                var pp = p4.Translate(new System.Windows.Point(spacingMin * i, 0));
                                                circle = new InstanceInCanvasCircel(canvasPageBase, option, centerCanvas, diameterMm, pp, new System.Windows.Point(0, 0), "");
                                                circle.DrawInCanvas();
                                                results.Add(circle.UIElement);
                                            }
                                        }
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }
            catch (Exception)
            {
            }
            return results;
        }
        private void _getCover(
            RebarBeam rebarBeam,
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double coverMm,
            CanvasPageBase canvasPageBase,
            RebarBeamSectionType sectionType,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            out double coverUpMm,
            out double coverSideMm)
        {
            coverUpMm = coverMm;
            coverSideMm = coverMm;
            var rebarBarTypeCustoms = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms;
            var distanceRToRMm = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm;
            switch (sectionType)
            {
                case RebarBeamSectionType.SectionStart:
                    _getCoverSub(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        coverMm,
                        canvasPageBase,
                        installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionStart,
                        rebarBeamMainBarLevelType,
                        rebarBeamMainBarGroupType,
                        out double start_coverUpMm,
                        out double start_coverSideMm);
                    coverUpMm = start_coverUpMm;
                    coverSideMm = start_coverSideMm;
                    break;
                case RebarBeamSectionType.SectionMid:
                    _getCoverSub(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        coverMm,
                        canvasPageBase,
                        installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid,
                        rebarBeamMainBarLevelType,
                        rebarBeamMainBarGroupType,
                        out double mid_coverUpMm,
                        out double mid_coverSideMm);
                    coverUpMm = mid_coverUpMm;
                    coverSideMm = mid_coverSideMm;
                    break;
                case RebarBeamSectionType.SectionEnd:
                    _getCoverSub(
                        rebarBeam,
                        installRebarBeamV2ViewModel,
                        coverMm,
                        canvasPageBase,
                        installRebarBeamV2ViewModel.ElementInstances.RebarBeamActive.RebarBeamSectionMid,
                        rebarBeamMainBarLevelType,
                        rebarBeamMainBarGroupType,
                        out double end_coverUpMm,
                        out double end_coverSideMm);
                    coverUpMm = end_coverUpMm;
                    coverSideMm = end_coverSideMm;
                    break;
            }
        }
        private void _getCoverSub(
            RebarBeam rebarBeam,
            InstallRebarBeamV2ViewModel installRebarBeamV2ViewModel,
            double coverMm,
            CanvasPageBase canvasPageBase,
            RebarBeamSection rebarBeamSection,
            RebarBeamMainBarLevelType rebarBeamMainBarLevelType,
            RebarBeamMainBarGroupType rebarBeamMainBarGroupType,
            out double coverUpMm,
            out double coverSideMm)
        {
            coverUpMm = coverMm;
            coverSideMm = coverMm;
            var rebarBarTypeCustoms = installRebarBeamV2ViewModel.ElementInstances.RebarBarTypeCustoms;
            var distanceRToRMm = installRebarBeamV2ViewModel.ElementInstances.DistanceRebarToRebarMm;
            RebarBarTypeCustom rebarbarTypeStp = null;
            RebarBarTypeCustom rebarbarTypeMainBar1 = null;
            RebarBarTypeCustom rebarbarTypeMainBar2 = null;
            RebarBarTypeCustom rebarbarTypeMainBar3 = null;

            switch (rebarBeamMainBarLevelType)
            {
                case RebarBeamMainBarLevelType.RebarTop:
                    switch (rebarBeamMainBarGroupType)
                    {
                        case RebarBeamMainBarGroupType.GroupLevel1:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                        case RebarBeamMainBarGroupType.GroupLevel2:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar2 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar2 == null ? 0 : rebarbarTypeMainBar2.ModelBarDiameter.FootToMm() / 2;

                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                        case RebarBeamMainBarGroupType.GroupLevel3:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel1.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar2 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel2.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar3 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamTop.RebarBeamTopLevel3.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar2 == null ? 0 : rebarbarTypeMainBar2.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar3 == null ? 0 : rebarbarTypeMainBar3.ModelBarDiameter.FootToMm() / 2;

                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                    }
                    break;
                case RebarBeamMainBarLevelType.RebarBot:
                    switch (rebarBeamMainBarGroupType)
                    {
                        case RebarBeamMainBarGroupType.GroupLevel1:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                        case RebarBeamMainBarGroupType.GroupLevel2:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar2 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar2 == null ? 0 : rebarbarTypeMainBar2.ModelBarDiameter.FootToMm() / 2;

                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                        case RebarBeamMainBarGroupType.GroupLevel3:
                            rebarbarTypeStp = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamStirrup.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar1 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel1.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar2 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter, rebarBarTypeCustoms);
                            rebarbarTypeMainBar3 = RebarBarTypeCustomUtils
                                .GetRebarBarTypeCustom(rebarBeamSection.RebarBeamBot.RebarBeamBotLevel2.Diameter, rebarBarTypeCustoms);
                            coverUpMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverUpMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar2 == null ? 0 : rebarbarTypeMainBar2.ModelBarDiameter.FootToMm() + distanceRToRMm;
                            coverUpMm += rebarbarTypeMainBar3 == null ? 0 : rebarbarTypeMainBar3.ModelBarDiameter.FootToMm() / 2;

                            coverSideMm += rebarbarTypeStp == null ? 0 : rebarbarTypeStp.ModelBarDiameter.FootToMm();
                            coverSideMm += rebarbarTypeMainBar1 == null ? 0 : rebarbarTypeMainBar1.ModelBarDiameter.FootToMm() / 2;
                            break;
                    }
                    break;
            }
        }
        private void _drawSectionBeamStirrup(RebarBeam rebarBeam, double coverMm, CanvasPageBase canvasPageBase, BeamFukashi beamFukashi = null)
        {
            var centerCanvas = canvasPageBase.Center;
            var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var option = OptionStyleInstanceInCanvas.OPTION_REBAR_LINE;
            var p1 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p2 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y - scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiTop.ValueMm));
            var p3 = beamFukashi == null
                ? new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X + scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiRight.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var p4 = beamFukashi == null
                ? new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm))
                : new Point(centerCanvas.X - scale * (rebarBeam.BeamWidthMm / 2 - coverMm - beamFukashi.FukashiLeft.ValueMm), centerCanvas.Y + scale * (rebarBeam.BeamHeightMm / 2 - coverMm - beamFukashi.FukashiBot.ValueMm));
            var ps = new List<Point>() {
                p1,p2, p3,p4
            };
            var stirrup = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
            stirrup.DrawInCanvas();
        }
        private void _drawSectionBeamConcrete(RebarBeam rebarBeam, CanvasPageBase canvasPageBase)
        {
            var centerCanvas = canvasPageBase.Center;
            var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
            var option = OptionStyleInstanceInCanvas.OPTION_CONCRETE_STRUCTURE;

            var p1 = new Point(centerCanvas.X - scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y - scale * rebarBeam.BeamHeightMm / 2);
            var p2 = new Point(centerCanvas.X + scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y - scale * rebarBeam.BeamHeightMm / 2);
            var p3 = new Point(centerCanvas.X + scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y + scale * rebarBeam.BeamHeightMm / 2);
            var p4 = new Point(centerCanvas.X - scale * rebarBeam.BeamWidthMm / 2, centerCanvas.Y + scale * rebarBeam.BeamHeightMm / 2);
            var ps = new List<Point>() {
                p1, p2, p3, p4
            };
            var sectionBeam = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
            sectionBeam.DrawInCanvas();
        }
        private void _drawSectionBeamFukashi(RebarBeam rebarBeam, CanvasPageBase canvasPageBase, BeamFukashi beamFukashi)
        {
            try
            {
                if (beamFukashi.FukashiTop.Parameter == null) return;
                if (beamFukashi.FukashiRight.Parameter == null) return;
                if (beamFukashi.FukashiBot.Parameter == null) return;
                if (beamFukashi.FukashiLeft.Parameter == null) return;
                var centerCanvas = canvasPageBase.Center;
                var scale = canvasPageBase.RatioScale * canvasPageBase.DistanceCrossScreen / Math.Sqrt(rebarBeam.BeamWidthMm * rebarBeam.BeamWidthMm + rebarBeam.BeamHeightMm * rebarBeam.BeamHeightMm);
                var option = OptionStyleInstanceInCanvas.OPTION_FUKASHI;

                var p1 = new Point(
                    centerCanvas.X - scale * (-beamFukashi.FukashiLeft.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y - scale * (-beamFukashi.FukashiTop.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p2 = new Point(
                    centerCanvas.X + scale * (-beamFukashi.FukashiRight.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y - scale * (-beamFukashi.FukashiTop.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p3 = new Point(
                    centerCanvas.X + scale * (-beamFukashi.FukashiRight.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y + scale * (-beamFukashi.FukashiBot.ValueMm + rebarBeam.BeamHeightMm / 2));
                var p4 = new Point(
                    centerCanvas.X - scale * (-beamFukashi.FukashiLeft.ValueMm + rebarBeam.BeamWidthMm / 2),
                    centerCanvas.Y + scale * (-beamFukashi.FukashiBot.ValueMm + rebarBeam.BeamHeightMm / 2));
                var ps = new List<Point>() {
                    p1, p2, p3, p4
                };
                var sectionBeam = new InstanceInCanvasPolygon(canvasPageBase, option, ps);
                sectionBeam.DrawInCanvas();
            }
            catch (Exception)
            {
            }
        }
    }
}


