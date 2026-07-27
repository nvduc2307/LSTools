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
    public partial class DrawRebarBeamInCanvasSerice
    {
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
    }
}

