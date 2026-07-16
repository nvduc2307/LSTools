namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void _externalColumnRebarCmdInvoke()
        {
            _columnConcreteAction.SetRebarSetting(_document, _viewModel.ColumnConcreteModels);
            _columnRebarStirrupAction.CreateStirrupMain(_viewModel.ColumnConcreteModels);
            _columnRebarAnchorAction.SaveColumnRebarAnchor(_viewModel.ColumnRebarAnchorModelUI);
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
