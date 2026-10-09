using Autodesk.Revit.UI;
using LSTool.AutoCad.Action.Actions;
using LSTool.Tools.Columns.CreateColumn.view;
using LSTool.Tools.Columns.CreateColumn.viewModel;
using LSTool.Tools.Generals.ManageRebarAnchorageHookMainBar.action;
using LSTool.Utils;

namespace LSTool.Tools.Columns.CreateColumn.action
{
    public partial class CreateColumnAction
    {
        private UIDocument _uidocument;
        private Document _document;

        private Element _host;

        private ColumnConcreteAction _columnConcreteAction;
        private ColumnRebarMainAction _columnRebarMainAction;
        private ColumnRebarStirrupAction _columnRebarStirrupAction;

        private SettingColumnAction _settingRebarColumnAction;
        private ManageRebarLapLengthAction _manageRebarLapLengthAction;
        private ManageRebarAnchorageHookMainBarAction _manageRebarAnchorageHookMainBarAction;
        private ManageRebarAnchorageLengthAction _manageRebarAnchorageLengthAction;

        private CreateColumnVM _viewModel;
        private ColumnRebarView _view;
        private ColumnCanvasSectionPreViewAction _canvasAction;

        public CreateColumnAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _settingRebarColumnAction = new SettingColumnAction(_document);
            _manageRebarLapLengthAction = new ManageRebarLapLengthAction(_document);
            _manageRebarAnchorageHookMainBarAction = new ManageRebarAnchorageHookMainBarAction(_document);
            _manageRebarAnchorageLengthAction = new ManageRebarAnchorageLengthAction(_document);
            CreateColumnHost();
            _columnConcreteAction = new ColumnConcreteAction(_uidocument);
            _columnRebarMainAction = new ColumnRebarMainAction(
                _uidocument,
                _host,
                _settingRebarColumnAction.GetSettingRebarColumnModel(),
                _manageRebarLapLengthAction.GetRebarLapLengths(),
                _manageRebarAnchorageHookMainBarAction.GetRebarAnchorageHookMainBars(),
                _manageRebarAnchorageLengthAction.GetRebarAnchorageLengths());
            _columnRebarStirrupAction = new ColumnRebarStirrupAction(
                _uidocument,
                _host,
                _settingRebarColumnAction.GetSettingRebarColumnModel());

            var rebarColumnSettings = _settingRebarColumnAction.GetRebarColumnSettings();
            _viewModel = new CreateColumnVM()
            {
                ColumnConcreteModelAction = _ColumnConcreteModelAction,
                RebarColumnSettings       = rebarColumnSettings,
                RebarColumnSetting        = rebarColumnSettings.FirstOrDefault(),
                ColumnSettingName         = rebarColumnSettings.FirstOrDefault()?.Name ?? string.Empty,
                SaveAsCommand             = new RelayCommand(_SaveAsCommand),
                SaveCommand               = new RelayCommand(_SaveCommand),
                DeleteCommand             = new RelayCommand(_DeleteCommand),
                LoadCommand               = new RelayCommand(_LoadCommand),
                OkCommand                 = new RelayCommand(_OkCommand),
                CreateTeiCommand          = new RelayCommand(_CreateTeiCommand),
                CancelCommand             = new RelayCommand(_CancelCommand)
            };
            _view = new ColumnRebarView() { DataContext = _viewModel };
            _canvasAction = new ColumnCanvasSectionPreViewAction(_view.CanvasSectionPreViewControl);

            _view.ContentRendered += (s, e) =>
            {
                _canvasAction.DrawSection(_viewModel.ColumnConcreteModels, _viewModel.ColumnConcreteModel);
            };
            _columnConcreteAction.QtyActionChange = () =>
            {
                _canvasAction?.Redraw();
            };
        }

        private void CreateColumnHost()
        {
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.Start();
                _host = RebarHelper.CreateRebarHost(_document);
                _document.Regenerate();
                ts.Commit();
            }
        }

        public void Execute()
        {
            var cls = _columnConcreteAction.SelectColumns();
            _viewModel.ColumnConcreteModels = _columnConcreteAction
                .GetColumnConcreteModels(cls);
            _viewModel.ColumnConcreteModel = _viewModel.ColumnConcreteModels.FirstOrDefault();
            _columnRebarStirrupAction.GetSettingColumnStirrupPosition(
                _viewModel.ColumnConcreteModels);
            _canvasAction?.DrawSection(_viewModel.ColumnConcreteModels, _viewModel.ColumnConcreteModel);
            _view.ShowDialog();
        }
    }
}
