namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void _externalColumnRebarCmdInvoke()
        {
            ValidateQtyRebar();
            _columnConcreteAction.SetRebarSetting(_document, _viewModel.ColumnConcreteModels);
            _columnRebarStirrupAction.CreateStirrupMain(_viewModel.ColumnConcreteModels,
                _viewModel.SettingRebarStandardModel);
            _columnRebarMainAction.CreateRebarMain(_viewModel.ColumnConcreteModels,
                _viewModel.SettingRebarStandardModel);
            _columnRebarStirrupAction.CreateStirrupSub(_viewModel.ColumnConcreteModels);
            _columnRebarAnchorAction.SaveColumnRebarAnchor(_viewModel.ColumnRebarAnchorModelUI);
            _columnRebarStirrupAction.SaveSettingColumnStirrupPosition(
                _viewModel.ColumnConcreteModels, 
                _columnStirrupPositionSchema);
        }
        private void _ColumnConcreteModelAction()
        {
            if (_viewModel.ColumnConcreteModel == null) return;
            var ele = _document.GetElement(_viewModel.ColumnConcreteModel.Id);
            _uidocument.Selection.SetElementIds(new List<ElementId>() { ele.Id });
            _canvasSectionPreViewAction?.DrawSection(
                _viewModel.ColumnConcreteModels,
                _viewModel.ColumnConcreteModel);
        }
        private void _QtyActionChange()
        {
            _canvasSectionPreViewAction?.DrawSection(
                _viewModel.ColumnConcreteModels,
                _viewModel.ColumnConcreteModel);
        }
        private void _CreateTeiCommand()
        {
            _canvasSectionPreViewAction.CreateTies(_viewModel.ColumnConcreteModel);
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
