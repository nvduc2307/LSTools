using LSTool.Compatibility;
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
    public partial class DrawRebarBeamInCanvasSerice
    {
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
    }
}

