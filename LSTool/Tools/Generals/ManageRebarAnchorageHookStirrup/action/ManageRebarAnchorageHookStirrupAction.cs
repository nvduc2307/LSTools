using Newtonsoft.Json;
using LSTool.MVVM.model.Structures;
using LSTool.MVVM.view;
using LSTool.MVVM.viewModel;
using LSTool.Utils;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace LSTool.AutoCad.Action.Actions
{
    public class ManageRebarAnchorageHookStirrupAction
    {
        private Document _document;
        private ManageRebarAnchorageHookStirrupVM _viewModel;
        private ManageRebarAnchorageHookStirrupView _view;

        public ManageRebarAnchorageHookStirrupAction(Document document)
        {
            _document = document;
            _viewModel = new ManageRebarAnchorageHookStirrupVM()
            {
                RebarAnchorageHookStirrups = new ObservableCollection<RebarAnchorageHookStirrupModel>(GetRebarAnchorageHookStirrups()),
                OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Okcommand),
                CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Cancelcommand),
            };
            _view = new ManageRebarAnchorageHookStirrupView() { DataContext = _viewModel };
        }

        private void _Cancelcommand()
        {
            _view.Close();
        }

        private void _Okcommand()
        {
            var path = $"{PathHelper.Appdatas}\\DataRebarAnchorageHookStirrup.json";
            if (!File.Exists(path)) return;
            var content = JsonConvert.SerializeObject(_viewModel.RebarAnchorageHookStirrups.ToList());
            File.WriteAllText(path, content);
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        private List<RebarAnchorageHookStirrupModel> GetRebarAnchorageHookStirrups()
        {
            var result = new List<RebarAnchorageHookStirrupModel>();
            var pathTemplate = $"{PathHelper.FolderDatas}\\DataRebarAnchorageHookStirrup.json";
            var path = $"{PathHelper.Appdatas}\\DataRebarAnchorageHookStirrup.json";
            if (!File.Exists(path))
            {
                if (!Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<List<RebarAnchorageHookStirrupModel>>(File.ReadAllText(path));
            if (datas == null) return result;
            return datas;
        }

        public void Execute()
        {
            _view?.ShowDialog();
        }
    }
}
