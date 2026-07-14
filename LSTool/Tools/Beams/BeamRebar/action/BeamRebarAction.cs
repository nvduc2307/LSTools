using Autodesk.Revit.UI;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private BeamConcreteAction _beamConcreteAction;
        public BeamRebarAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _beamConcreteAction = new BeamConcreteAction(_uidocument);
        }
        public void Execute()
        {
            _beamConcreteAction.SelectBeams();
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
