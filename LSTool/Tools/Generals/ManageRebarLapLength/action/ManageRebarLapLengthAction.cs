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
    public class ManageRebarLapLengthAction
    {
        private Document _document;
        private ManageRebarLapLengthVM _viewModel;
        private ManageRebarLapLengthView _view;

        public ManageRebarLapLengthAction(Document document)
        {
            _document = document;
            _viewModel = new ManageRebarLapLengthVM()
            {
                RebarLapLengths = new ObservableCollection<RebarLapLengthModel>(GetRebarLapLengthsWithNames()),
                OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Okcommand),
                CancelCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(_Cancelcommand),
            };
            _view = new ManageRebarLapLengthView() { DataContext = _viewModel };
        }

        private void _Cancelcommand()
        {
            _view.Close();
        }

        private void _Okcommand()
        {
            var path = $"{PathHelper.Appdatas}\\DataRebarLapLength.json";
            if (!File.Exists(path)) return;
            var content = JsonConvert.SerializeObject(_viewModel.RebarLapLengths.ToList());
            File.WriteAllText(path, content);
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        /// <summary>Danh sách kèm tên hiển thị của cấp độ bền bê tông và loại thép (thay cho Id).</summary>
        private List<RebarLapLengthModel> GetRebarLapLengthsWithNames()
        {
            var items = GetRebarLapLengths();
            var ratingNames = GetConcreteStrengthRatingNames();
            foreach (var item in items)
            {
                item.ConcreteStrengthRatingName = item.ConcreteStrengthRatingId >= 0
                    && item.ConcreteStrengthRatingId < ratingNames.Count
                        ? ratingNames[item.ConcreteStrengthRatingId]
                        : item.ConcreteStrengthRatingId.ToString();
                item.StructureRebarTypeName = (StructureRebarType)item.StructureRebarType switch
                {
                    StructureRebarType.PLAIN => "Plain (thép trơn)",
                    StructureRebarType.DEFORMED => "Deformed (thép có gờ)",
                    _ => item.StructureRebarType.ToString(),
                };
            }
            return items;
        }

        private static List<string> GetConcreteStrengthRatingNames()
        {
            try
            {
                var path = $"{PathHelper.FolderDatas}\\DataConcreteStrengthRating.json";
                if (!File.Exists(path)) return new List<string>();
                var list = JsonConvert.DeserializeObject<List<ConcreteStrengthRatingModel>>(File.ReadAllText(path));
                return list?.OrderBy(x => x.Id).Select(x => x.Name).ToList() ?? new List<string>();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public List<RebarLapLengthModel> GetRebarLapLengths()
        {
            var result = new List<RebarLapLengthModel>();
            var pathTemplate = $"{PathHelper.FolderDatas}\\DataRebarLapLength.json";
            var path = $"{PathHelper.Appdatas}\\DataRebarLapLength.json";
            if (!File.Exists(path))
            {
                if (!Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<List<RebarLapLengthModel>>(File.ReadAllText(path));
            if (datas == null) return result;
            return datas;
        }

        public void Execute()
        {
            _view?.ShowDialog();
        }
    }
}
