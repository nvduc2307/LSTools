using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.AutoCad.Action.Actions;
using LSTool.MVVM.model.Structures;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Tools.Beams.BeamRebar.types;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Tools.Beams.BeamRebar.view;
using LSTool.Tools.Beams.BeamRebar.viewModel;
using LSTool.Tools.Generals.ManageConcreteCover.action;
using LSTool.Tools.Generals.ManageRebarAnchorageHookMainBar.action;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private BeamRebarVM _viewModel;
        private BeamRebarView _view;
        private BeamConcreteAction _beamConcreteAction;
        private BeamCanvasSectionPreViewAction _canvasAction;

        private List<RebarBarType> _diamters;
        private List<string> _diamterNames;
        private ConcreteCoverModel _concreteCoverModel;
        private ManageConcreteCoverAction _manageConcreteCoverAction;

        // setting & bảng tra cho thép chủ
        private SettingBeamModel _settingBeamModel;
        private List<RebarAnchorageLengthModel> _rebarAnchorageLengthModels;
        private List<RebarAnchorageHookMainBarModel> _rebarAnchorageHookMainBarModels;
        public BeamRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _manageConcreteCoverAction = new ManageConcreteCoverAction(_document);
            _concreteCoverModel = _manageConcreteCoverAction.GetBeamCover();   // cover dầm trong setting
            _beamConcreteAction = new BeamConcreteAction(_uidocument, _concreteCoverModel?.CoverValue ?? 0);
            _settingBeamModel = BeamRebarUtils.GetSettingBeamModel();
            _rebarAnchorageLengthModels = new ManageRebarAnchorageLengthAction(_document).GetRebarAnchorageLengths();
            _rebarAnchorageHookMainBarModels = new ManageRebarAnchorageHookMainBarAction(_document).GetRebarAnchorageHookMainBars();
            var beamSettings = _settingAction.GetSettings();
            _viewModel = new BeamRebarVM()
            {
                BeamRebarSettings = beamSettings,
                BeamRebarSetting = beamSettings.FirstOrDefault(),
                BeamSettingName = beamSettings.FirstOrDefault()?.Name ?? string.Empty,
                SaveAsCommand = new RelayCommand(_SaveAsCommand),
                SaveCommand = new RelayCommand(_SaveCommand),
                DeleteCommand = new RelayCommand(_DeleteCommand),
                LoadCommand = new RelayCommand(_LoadCommand),
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand),
                CreateTieVerticalCommand = new RelayCommand(() => _canvasAction?.CreateTies(BeamTieType.Vertical)),
                CreateTieHorizontalCommand = new RelayCommand(() => _canvasAction?.CreateTies(BeamTieType.Horizontal))
            };
            _diamters = new List<RebarBarType>();
            _diamterNames = new List<string>();
            _view = new BeamRebarView() { DataContext = _viewModel };

            // hook sau khi window content render xong → canvas đã có kích thước thực
            _view.ContentRendered += (s, e) =>
            {
                _canvasAction = new BeamCanvasSectionPreViewAction(
                    _view.CanvasStart,
                    _view.CanvasMid,
                    _view.CanvasEnd);
                _canvasAction.DrawSection(_viewModel.BeamRebarModel, _viewModel.BeamRebarModels);
            };

            // vẽ lại khi user đổi dầm trong ComboBox
            _viewModel.BeamRebarModelChangeAction = () =>
            {
                _canvasAction?.DrawSection(_viewModel.BeamRebarModel, _viewModel.BeamRebarModels);
            };
        }

        public void Execute()
        {
            var objs = _beamConcreteAction.SelectBeams();
            var beams = _beamConcreteAction.GetConcreteModels(objs);
            if (!beams.Any())
                throw new Exception("beam is not found");
            _diamters = BeamRebarUtils.GetDiamters(_document);
            if (!_diamters.Any())
                throw new Exception("diameter is not found");
            _diamterNames = _diamters.Select(x => x.Name).ToList();
            BeamRebarUtils.UpdateDiamterToBeamRebarConcreate(beams, _diamters, _concreteCoverModel?.CoverValue > 0 ? _concreteCoverModel.CoverValue : BeamRebarModel.COVER);
            LoadBeamTies(beams);
            _viewModel.BeamRebarModels = [.. beams];
            _viewModel.BeamRebarModel = _viewModel.BeamRebarModels.FirstOrDefault();
            _view.ShowDialog();
        }

    }
}
