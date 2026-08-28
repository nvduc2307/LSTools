using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Tools.Beams.BeamRebar.Utils;
using LSTool.Tools.Beams.BeamRebar.view;
using LSTool.Tools.Beams.BeamRebar.viewModel;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private BeamRebarVM _viewModel;
        private BeamRebarView _view;
        private BeamConcreteAction _beamConcreteAction;

        private List<RebarBarType> _diamters;
        private List<string> _diamterNames;
        public BeamRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _beamConcreteAction = new BeamConcreteAction(_uidocument);
            _viewModel = new BeamRebarVM()
            {
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _view = new BeamRebarView() { DataContext = _viewModel};
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _OkCommand()
        {
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
            _view.Show();
        }

        //create rebar stirrup

        //create rebar top 1
        //create rebar top 2
        //create rebar top 3

        //create rebar bot 1
        //create rebar bot 2
        //create rebar bot 3

        //create rebar side
    }
}
