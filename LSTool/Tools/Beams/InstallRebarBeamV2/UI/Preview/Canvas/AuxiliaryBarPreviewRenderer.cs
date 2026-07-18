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
    }
}

