using Newtonsoft.Json;
using LSTool.MVVM.model.Structures;
using LSTool.MVVM.view;
using LSTool.MVVM.viewModel;
using LSTool.Utils;
using System.IO;
using System.Windows;

namespace LSTool.Tools.Generals.ManageConcreteCover.action
{
    public class ManageConcreteCoverAction
    {
        private Document _document;
        private ManageConcreteCoverView _view;
        private ManageConcreteCoverVM _viewModel;
        private List<ConcreteCoverModel> _concreteCoverDatas;
        public ManageConcreteCoverAction(Document document)
        {
            _document = document;
            _concreteCoverDatas = GetConcreteCoverDatas();
            _viewModel = new ManageConcreteCoverVM()
            {
                ColumnCover = GetColumnCover(),
                WallCover = GetWallCover(),
                BeamCover = GetBeamCover(),
                SlabCover = GetSlabCover(),
                OkCommand = new RelayCommand(_OkCommand),
                CancelCommand = new RelayCommand(_CancelCommand)
            };
            _view = new ManageConcreteCoverView() { DataContext = _viewModel };
        }
        public void Execute()
        {
            _view.ShowDialog();
        }

        private void _OkCommand()
        {
            var path = $"{PathHelper.Appdatas}\\DataConcreteCover.json";
            if (!File.Exists(path)) return;
            foreach (var data in _concreteCoverDatas)
            {
                if (data.StructureConcreteType == _viewModel.ColumnCover.StructureConcreteType)
                    data.CoverValue = _viewModel.ColumnCover.CoverValue;
                if (data.StructureConcreteType == _viewModel.WallCover.StructureConcreteType)
                    data.CoverValue = _viewModel.WallCover.CoverValue;
                if (data.StructureConcreteType == _viewModel.BeamCover.StructureConcreteType)
                    data.CoverValue = _viewModel.BeamCover.CoverValue;
                if (data.StructureConcreteType == _viewModel.SlabCover.StructureConcreteType)
                    data.CoverValue = _viewModel.SlabCover.CoverValue;
            }
            var content = JsonConvert.SerializeObject(
                _concreteCoverDatas
                .Select(x => new ConcreteCoverModel() { Id = x.Id, CoverValue = x.CoverValue, StructureConcreteType = x.StructureConcreteType })
                .ToList());
            File.WriteAllText(path, content);
            IO.ShowInfo("Complete!");
            _view.Close();
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        public ConcreteCoverModel GetColumnCover()
        {
            ConcreteCoverModel result = null;
            if (!_concreteCoverDatas.Any()) return result;
            var coverTarget = _concreteCoverDatas
                .FirstOrDefault(x => (StructureConcreteType)x.StructureConcreteType == StructureConcreteType.COLUMN);
            if (coverTarget == null) return result;
            result = coverTarget;
            return result;
        }

        public ConcreteCoverModel GetFoundationCover()
        {
            ConcreteCoverModel result = null;
            if (!_concreteCoverDatas.Any()) return result;
            var coverTarget = _concreteCoverDatas
                .FirstOrDefault(x => (StructureConcreteType)x.StructureConcreteType == StructureConcreteType.FOUNDATION);
            if (coverTarget == null) return result;
            result = coverTarget;
            return result;
        }

        public ConcreteCoverModel GetSlabCover()
        {
            ConcreteCoverModel result = null;
            if (!_concreteCoverDatas.Any()) return result;
            var coverTarget = _concreteCoverDatas
                .FirstOrDefault(x => (StructureConcreteType)x.StructureConcreteType == StructureConcreteType.SLAB);
            if (coverTarget == null) return result;
            result = coverTarget;
            return result;
        }

        public ConcreteCoverModel GetBeamCover()
        {
            ConcreteCoverModel result = null;
            if (!_concreteCoverDatas.Any()) return result;
            var coverTarget = _concreteCoverDatas
                .FirstOrDefault(x => (StructureConcreteType)x.StructureConcreteType == StructureConcreteType.BEAM);
            if (coverTarget == null) return result;
            result = coverTarget;
            return result;
        }

        public ConcreteCoverModel GetWallCover()
        {
            ConcreteCoverModel result = null;
            if (!_concreteCoverDatas.Any()) return result;
            var coverTarget = _concreteCoverDatas
                .FirstOrDefault(x => (StructureConcreteType)x.StructureConcreteType == StructureConcreteType.WALL);
            if (coverTarget == null) return result;
            result = coverTarget;
            return result;
        }
        public List<ConcreteCoverModel> GetConcreteCoverDatas()
        {
            var result = new List<ConcreteCoverModel>();
            var pathTemplate = $"{PathHelper.FolderDatas}\\DataConcreteCover.json";
            var path = $"{PathHelper.Appdatas}\\DataConcreteCover.json";
            if (!File.Exists(path))
            {
                if (!System.IO.Directory.Exists(PathHelper.Appdatas))
                    Directory.CreateDirectory(PathHelper.Appdatas);
                File.Copy(pathTemplate, path);
            }
            var datas = JsonConvert.DeserializeObject<List<ConcreteCoverModel>>(File.ReadAllText(path));
            if (datas == null) return result;
            result = datas
                .Select(x => new ConcreteCoverModel() { Id = x.Id, CoverValue = x.CoverValue, StructureConcreteType = x.StructureConcreteType })
                .ToList();
            return result;
        }
    }
}
