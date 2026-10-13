using LSTool.Utils;

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




            _mainBarWarnings.Clear();
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.SkipAllWarnings();
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
