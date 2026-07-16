using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.viewModels;
using LSTool.Tools.Columns.ColumnRebar.views;
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
        private SettingRebarStandardModel _settingRebarStandardModel;

        private ColumnConcreteAction _columnConcreteAction;
        private ColumnRebarStirrupAction _columnRebarStirrupAction;
        private ColumnRebarAnchorAction _columnRebarAnchorAction;
        public ColumnRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _columnConcreteAction = new ColumnConcreteAction(_uidocument);
            _columnRebarStirrupAction = new ColumnRebarStirrupAction(_uidocument);
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
                ColumnRebarAnchorModelUI = _columnRebarAnchorAction.GetColumnRebarAnchor(),
                ColumnConcreteModelAction = _ColumnConcreteModelAction,
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _view = new ColumnRebarView() { DataContext = _viewModel };
        }
        // create rebar stirrup
        // create rebar face right
        // create rebar face top
        // create rebar face left
        // create rebar face bot
        public void Execute()
        {
            var cls = _columnConcreteAction.SelectColumns();
            _viewModel.ColumnConcreteModels = _columnConcreteAction.GetColumnConcreteModels(cls);
            _viewModel.ColumnConcreteModel = _viewModel.ColumnConcreteModels.FirstOrDefault();
            _view.Show();
        }
    }
}
