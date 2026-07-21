using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.viewModels;
using LSTool.Tools.Columns.ColumnRebar.views;
using LSTool.Tools.Generals.SettingRebarStandard.actions;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils.ExternalEvent;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ColumnRebarView _view;

        private CustomExternalCommand _externalColumnRebarCmd;
        private ExternalEvent _externalColumnRebarCmdEvent;
        private SettingRebarStandardSchema _settingRebarStandardSchema;

        private ColumnRebarVM _viewModel;

        private SettingRebarStandardAction _settingRebarStandardAction;
        private ColumnConcreteAction _columnConcreteAction;
        private ColumnRebarAnchorAction _columnRebarAnchorAction;
        private ColumnRebarStirrupAction _columnRebarStirrupAction;
        private ColumnRebarMainAction _columnRebarMainAction;
        private CanvasSectionPreViewAction _canvasSectionPreViewAction;

        private Element _host;
        public ColumnRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _columnConcreteAction = new ColumnConcreteAction(_uidocument);
            _settingRebarStandardAction = new SettingRebarStandardAction(_uidocument);
            _settingRebarStandardSchema = new SettingRebarStandardSchema(
                SettingRebarStandardSchema.GUID,
                SettingRebarStandardSchema.NAME);
            _columnRebarAnchorAction = new ColumnRebarAnchorAction(_uidocument);
            _externalColumnRebarCmd = new CustomExternalCommand("columnRebarCmd")
            {
                Action = _externalColumnRebarCmdInvoke
            };
            _externalColumnRebarCmdEvent = ExternalEvent.Create(_externalColumnRebarCmd);
            Validate();
            _viewModel = new ColumnRebarVM()
            {
                SettingRebarStandardModel = _settingRebarStandardAction.GetSetting(),
                ColumnRebarAnchorModelUI = _columnRebarAnchorAction.GetColumnRebarAnchor(),
                ColumnConcreteModelAction = _ColumnConcreteModelAction,
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _columnRebarStirrupAction = new ColumnRebarStirrupAction(_uidocument, _host);
            _columnRebarMainAction = new ColumnRebarMainAction(
                _uidocument,
                _viewModel.ColumnRebarAnchorModelUI,
                _viewModel.SettingRebarStandardModel,
                _host);
            _view = new ColumnRebarView() { DataContext = _viewModel };
            _view.Loaded += _view_Loaded;
        }

        private void _view_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _canvasSectionPreViewAction = new CanvasSectionPreViewAction(_view.CanvasSectionPreView);
        }

        public void Execute()
        {
            var cls = _columnConcreteAction.SelectColumns();
            _columnConcreteAction.QtyActionChange = _QtyActionChange;
            _viewModel.ColumnConcreteModels = _columnConcreteAction.GetColumnConcreteModels(cls);
            _viewModel.ColumnConcreteModel = _viewModel.ColumnConcreteModels.FirstOrDefault();
            _view.Show();
            _canvasSectionPreViewAction?.DrawSection(
                _viewModel.ColumnConcreteModels,
                _viewModel.ColumnConcreteModel);
        }
    }
}
