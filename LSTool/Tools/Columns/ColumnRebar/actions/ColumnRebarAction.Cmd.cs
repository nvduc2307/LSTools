using Autodesk.Revit.UI;
using LSTool.Tools.Columns.ColumnRebar.viewModels;
using LSTool.Tools.Columns.ColumnRebar.views;
using LSTool.Tools.Generals.SettingDiameters.action;
using LSTool.Tools.Generals.SettingRebarStandard.models;
using LSTool.Utils.ExternalEvent;
using Newtonsoft.Json;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
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
    }
}
