namespace LSTool.Tools.Beams.BeamRebar.action
{
    public partial class BeamRebarAction
    {
        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _OkCommand()
        {

            //create rebar stirrup

            //create rebar top 1
            //create rebar top 2
            //create rebar top 3

            //create rebar bot 1
            //create rebar bot 2
            //create rebar bot 3

            //create rebar side
            _mainBarWarnings.Clear();
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.Start();
                CreateStirrup();

                CreateTop1();
                CreateTop2();
                CreateTop3();

                CreateBot1();
                CreateBot2();
                CreateBot3();

                CreateSideBar();
                CreateStirrupSub();
                SaveBeamTies();
                ts.Commit();
            }

            if (_mainBarWarnings.Any())
                LSTool.Utils.IO.ShowWarning(string.Join("\n", _mainBarWarnings.Distinct().Take(10)));
            _view.Close();

        }
    }
}
