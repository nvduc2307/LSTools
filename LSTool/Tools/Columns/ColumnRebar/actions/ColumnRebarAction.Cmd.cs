using LSTool.Utils;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private void _externalColumnRebarCmdInvoke()
        {
            ValidateQtyRebar();
            _columnConcreteAction.SetRebarSetting(_document, _viewModel.ColumnConcreteModels);
            _columnRebarStirrupAction.CreateStirrupMain(_viewModel.ColumnConcreteModels);''
            _columnRebarMainAction.CreateRebarMain(_viewModel.ColumnConcreteModels);
            _columnRebarAnchorAction.SaveColumnRebarAnchor(_viewModel.ColumnRebarAnchorModelUI);
        }
        private void _ColumnConcreteModelAction()
        {
            if (_viewModel.ColumnConcreteModel == null) return;
            var ele = _document.GetElement(_viewModel.ColumnConcreteModel.Id);
            _uidocument.Selection.SetElementIds(new List<ElementId>() { ele.Id });
            UpdateCanvas();
        }
        private void _QtyActionChange()
        {
            UpdateCanvas();
        }
        private void UpdateCanvas()
        {
            var maxX = int.Parse(Math.Round(_viewModel.ColumnConcreteModels.Max(x => x.SpacingDX), 0).ToString());
            var maxY = int.Parse(Math.Round(_viewModel.ColumnConcreteModels.Max(x => x.SpacingDY), 0).ToString());
            _canvasSectionPreViewAction?.DrawSection(
                _viewModel.ColumnConcreteModel.Height,
                _viewModel.ColumnConcreteModel.Width,
                _viewModel.ColumnConcreteModel.Cover,
                int.Parse(Math.Round(_viewModel.ColumnConcreteModel.SpacingDX, 0).ToString()),
                int.Parse(Math.Round(_viewModel.ColumnConcreteModel.SpacingDY, 0).ToString()),
                maxX, maxY);
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
