using Newtonsoft.Json;
using LSTool.MVVM.model.Structures;
using LSTool.MVVM.view;
using LSTool.MVVM.viewModel;
using LSTool.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace LSTool.Tools.Generals.ManageRebarAnchorageHookMainBar.action
{
    public class ManageRebarAnchorageHookMainBarAction
    {
        private Document _document;
        private ManageRebarAnchorageHookMainBarVM _viewModel;
        private ManageRebarAnchorageHookMainBarView _view;
        public ManageRebarAnchorageHookMainBarAction(Document document)
        {
            _document = document;
            _viewModel = new ManageRebarAnchorageHookMainBarVM()
            {
                RebarAnchorageHookMainBars = new ObservableCollection<RebarAnchorageHookMainBarModel>(GetRebarAnchorageHookMainBars()),
                OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Okcommand),
                CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Cancelcommand),
            };
            _view = new ManageRebarAnchorageHookMainBarView() { DataContext = _viewModel };
        }

        private void _Cancelcommand()
        {
            _view.Close();
        }

        private void _Okcommand()
        {
            var path = $"{PathHelper.Appdatas}\\DataRebarAnchorageHookMainBar.json";
            if (!File.Exists(path)) return;
            var content = JsonConvert.SerializeObject(
                _viewModel.RebarAnchorageHookMainBars
                .Select(
                    x => new RebarAnchorageHookMainBarModel()
                    {
                        Id = x.Id,
                        Diameter = x.Diameter,
                        A = x.A,
                        R = x.R,
                        B = x.B,
                        C = x.C
                    })
                .ToList());
            File.WriteAllText(path, content);
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        public List<RebarAnchorageHookMainBarModel> GetRebarAnchorageHookMainBars()
        {
            var result = new List<RebarAnchorageHookMainBarModel>();
            var pathTemplate = $"{PathHelper.FolderDatas}\\DataRebarAnchorageHookMainBar.json";
            var path = $"{PathHelper.Appdatas}\\DataRebarAnchorageHookMainBar.json";
            if (!File.Exists(path))
            {
                if (!System.IO.Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<List<RebarAnchorageHookMainBarModel>>(File.ReadAllText(path));
            if (datas == null) return result;
            result = datas
                .Select(x => new RebarAnchorageHookMainBarModel()
                {
                    Id = x.Id,
                    Diameter = x.Diameter,
                    A = x.A,
                    R = x.R,
                    B = x.B,
                    C = x.C
                })
                .ToList();
            return result;
        }

        public void Execute()
        {
            _view?.ShowDialog();
        }
    }
}
