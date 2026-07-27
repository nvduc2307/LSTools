using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HcBimUtils;
using HcBimUtils.DocumentUtils;
using HcBimUtils.MoreLinq;
using HcBimUtils.WPFUtils;
using Newtonsoft.Json;
using RIMT.BeamRebar.ViewModel;
using RIMT.CreateRebarAssemblies.model;
using LSTool.Tools.Beams.InstallRebarBeamV2.iservices;
using LSTool.Tools.Beams.InstallRebarBeamV2.models;
using LSTool.Tools.Beams.InstallRebarBeamV2.viewModels;
using LSTool.Tools.Beams.InstallRebarBeamV2.views;
using LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy;
using LSTool.Tools.Beams.InstallRebarBeamV2.UI.Preview;
using RIMT.Utils;
using RIMT.Utils.canvass;
using RIMT.Utils.Entities;
using RIMT.Utils.RevitElements;
using RIMT.Utils.RevParameters;
using RIMT.Utils.RevRebars;
using RIMT.Utils.SelectFilters;
using RIMT.Utils.SkipWarning;
using System.IO;
using System.Windows.Controls;
using Rebar = Autodesk.Revit.DB.Structure.Rebar;
using RebarBeamAnchorType = LSTool.Tools.Beams.InstallRebarBeamV2.models.RebarBeamAnchorType;
using UserControl = System.Windows.Controls.UserControl;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.viewModels
{
    public partial class InstallRebarBeamV2ViewModel
    {
        private void QueuePreviewRefresh(PreviewRegion regions)
        {
            _previewRefreshCoordinator.Request(regions);
        }

        private void QueueMainBarPreview()
        {
            QueuePreviewRefresh(PreviewRegion.MainBars);
        }

        private void QueueSideBarPreview()
        {
            QueuePreviewRefresh(PreviewRegion.SideBars);
        }

        private void RefreshPreview(PreviewRegion regions)
        {
            if (ElementInstances?.RebarBeamActive == null ||
                CanvasPageSectionStart == null ||
                CanvasPageSectionMid == null ||
                CanvasPageSectionEnd == null)
                return;

            if ((regions & PreviewRegion.MainBars) != 0)
            {
                ElementInstances.MainRebarTopUIElement =
                    _drawRebarBeamInCanvasSerice.DrawSectionBeamMainBar(ElementInstances.RebarBeamActive, this);
            }

            if ((regions & PreviewRegion.SideBars) != 0)
            {
                ElementInstances.SideBarUIElement =
                    _drawRebarBeamInCanvasSerice.DrawSectionBeamSideBar(ElementInstances.RebarBeamActive, this);
            }
        }

        private void MainView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {

            _previewRefreshCoordinator.CancelPending();
            ElementInstances.GenerateCoordinateBeam();
            CanvasPageSectionStart = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionStart") as Canvas);
            CanvasPageSectionMid = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionMid") as Canvas);
            CanvasPageSectionEnd = new CanvasPageBase(SettingRebarSectionView.FindName("CanvasSectionEnd") as Canvas);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamConcrete(ElementInstances.RebarBeamActive, this);
            _drawRebarBeamInCanvasSerice.DrawSectionBeamStirrup(ElementInstances.RebarBeamActive, this);
            RefreshPreview(PreviewRegion.AllBars);
        }
        private void RefreshAllVerticalStirrup(RebarBeam rebarBeam)
        {
            rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
            rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.Hooks2 = new();
        }
        private void InitAction()
        {
            ElementInstances.RebarBeamAnchorTypeChange = () =>
            {
                switch ((RebarBeamAnchorType)ElementInstances.RebarBeamAnchorType.Id)
                {
                    case RebarBeamAnchorType.Type1:
                        UserControlAnchorBeamTypeViewCurrent = AnchorBeamType1View;
                        break;
                    case RebarBeamAnchorType.Type2:
                        UserControlAnchorBeamTypeViewCurrent = AnchorBeamType2View;
                        break;
                }
            };
            ElementInstances.RebarBeamActiveChange = () =>
            {
                QueueMainBarPreview();
                QueueSideBarPreview();
                var beamId = new ElementId(ElementInstances.RebarBeamActive.BeamId);
                AC.UiDoc.Selection.SetElementIds(new List<ElementId>() { beamId });
            };
            foreach (var rebarBeam in ElementInstances.RebarBeams)
            {
                rebarBeam.RebarBeamSectionStart.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    QueueSideBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    QueueSideBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamSideBar.QuantitySideChange = () =>
                {
                    QueueSideBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamTop.RebarBeamTopLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel1.QuantityChange = () =>
                {
                    RefreshAllVerticalStirrup(rebarBeam);
                    QueueMainBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel2.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };

                rebarBeam.RebarBeamSectionStart.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionMid.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
                rebarBeam.RebarBeamSectionEnd.RebarBeamBot.RebarBeamBotLevel3.QuantityChange = () =>
                {
                    QueueMainBarPreview();
                };
            }
        }
    }
}
