using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.viewModels;
using LSTool.Tools.Columns.ColumnRebar.views;
using LSTool.Tools.Generals.SettingDiameters.action;
using LSTool.Utils.ExternalEvent;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public class ColumnRebarAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private ColumnRebarView _view;
        private ColumnRebarVM _viewModel;
        private ColumnConcreteAction _columnConcreteAction;

        private ColumnRebarStirrupAction _columnRebarStirrupAction;

        private CustomExternalCommand _externalColumnRebarCmd;
        private ExternalEvent _externalColumnRebarCmdEvent;
        public ColumnRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _columnConcreteAction = new ColumnConcreteAction(_uidocument);
            _columnRebarStirrupAction = new ColumnRebarStirrupAction(_uidocument);
            _externalColumnRebarCmd = new CustomExternalCommand("columnRebarCmd")
            {
                Action = _externalColumnRebarCmdInvoke
            };
            _externalColumnRebarCmdEvent = ExternalEvent.Create(_externalColumnRebarCmd);
            ValidateRebarDiameter();
            _columnConcreteAction.ValidateShareParameter();
            _viewModel = new ColumnRebarVM()
            {
                ColumnConcreteModelAction = _ColumnConcreteModelAction,
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _view = new ColumnRebarView() { DataContext = _viewModel };
        }

        private void _externalColumnRebarCmdInvoke()
        {
            _columnConcreteAction.SetRebarSetting(_document, _viewModel.ColumnConcreteModels);
            _columnRebarStirrupAction.CreateStirrupMain(_viewModel.ColumnConcreteModels);
        }

        private void _ColumnConcreteModelAction()
        {
            if (_viewModel.ColumnConcreteModel == null) return;
            var ele = _document.GetElement(_viewModel.ColumnConcreteModel.Id);
            _uidocument.Selection.SetElementIds(new List<ElementId>() { ele.Id });
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _OkCommand()
        {
            _view.Close();
            _externalColumnRebarCmdEvent?.Raise();
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
        private void ValidateRebarDiameter()
        {
            var action = new RebarDatabasesAction(_uidocument);
            var rebarDatas = action.GetRebarBarTypes();

            using (var ts = new Transaction(_document, "RebarDatabasesAction"))
            {
                ts.Start();
                action.CreateRebarBarType(rebarDatas);
                ts.Commit();
            }
        }
    }
}
