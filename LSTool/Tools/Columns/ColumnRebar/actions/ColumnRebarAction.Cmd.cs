using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void _externalColumnRebarCmdInvoke()
        {
            ValidateQtyRebar();
            _columnConcreteAction.SetRebarSetting(_document, _viewModel.ColumnConcreteModels);
            _columnRebarStirrupAction.CreateStirrupMain(_viewModel.ColumnConcreteModels);
            _columnRebarMainAction.CreateRebarMain(_viewModel.ColumnConcreteModels);
            _columnRebarAnchorAction.SaveColumnRebarAnchor(_viewModel.ColumnRebarAnchorModelUI);
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
