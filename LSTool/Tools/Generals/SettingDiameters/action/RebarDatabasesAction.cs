using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Tools.Generals.SettingDiameters.models;
using LSTool.Tools.Generals.SettingDiameters.viewModels;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using TPTool.Tools.SettingDiameters.RebarDatabases.models;
using TPTool.Tools.SettingDiameters.RebarDatabases.views;
using TPTool.Utils;

namespace LSTool.Tools.Generals.SettingDiameters.action
{
    public partial class RebarDatabasesAction
    {
        private UIDocument _uidocument;
        private Document _document;
        private RebarDatabasesViewModel _viewModel;
        private RebarDatabasesView _view;
        private RebarBarTypeSchema _rebarBarTypeSchema;
        public RebarDatabasesAction(UIDocument uidocument)
        {
            _uidocument = uidocument;
            _document = _uidocument.Document;
            _rebarBarTypeSchema = new RebarBarTypeSchema(RebarBarTypeSchema.GUID, RebarBarTypeSchema.NAME);
            _viewModel = new RebarDatabasesViewModel()
            {
                RebarBarTypes = new ObservableCollection<RebarBarTypeModel>(GetRebarBarTypes()),
                OkCommand = new RelayCommand(_OkCommand),
                ResetCommand = new RelayCommand(_ResetCommand),
                CancelCommand = new RelayCommand(_CancelCommand),
            };
            _view = new RebarDatabasesView() { DataContext = _viewModel };
        }

        private void _CancelCommand()
        {
            _view.Close();
        }

        private void _ResetCommand()
        {
            var pathData = $"{PathHelper.Datas}\\RebarbarTypeData.json";
            var data = JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(File.ReadAllText(pathData));
            if (data == null) return;
            _viewModel.RebarBarTypes = new ObservableCollection<RebarBarTypeModel>(data);
        }

        private void _OkCommand()
        {
            if (!_viewModel.RebarBarTypes.Any()) return;
            using (var ts = new Transaction(_document, "new transaction"))
            {
                ts.SkipAllWarnings();
                ts.Start();
                var content = JsonConvert.SerializeObject(_viewModel.RebarBarTypes);
                _rebarBarTypeSchema.Write(_document.ProjectInformation, content);
                CreateRebarBarType(_viewModel.RebarBarTypes.ToList());
                ts.Commit();
            }
            _view.Close();
        }
        public void CreateRebarBarType(List<RebarBarTypeModel> rebarBarTypeModels)
        {
            if (!rebarBarTypeModels.Any()) return;
            var rebarBarTypes = new FilteredElementCollector(_document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .ToList();
            foreach (var type in rebarBarTypeModels)
            {
                try
                {
                    var typeTarget = rebarBarTypes.FirstOrDefault(x=>x.Name == type.NameStyle);
                    if (typeTarget != null)
                    {
                        RebarBarTypeHelper.SetRebarDiameter(typeTarget, type.BarDiameterReal.FromMillimeters());
                        RebarBarTypeHelper.SetRebarBendDiameter(typeTarget, type.StandardBendDiameter.FromMillimeters());
                    }
                    else
                    {
                        RebarBarTypeHelper.CreateNewType(
                                _document,
                                type.NameStyle,
                                type.BarDiameterReal.FromMillimeters(),
                                type.StandardBendDiameter.FromMillimeters());
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        public List<RebarBarTypeModel> GetRebarBarTypes()
        {
            var result = new List<RebarBarTypeModel>();
            var pathData = $"{PathHelper.Datas}\\RebarbarTypeData.json";
            var dataInModel = _rebarBarTypeSchema.Read(_document.ProjectInformation);
            var data = string.IsNullOrEmpty(dataInModel)
                ? JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(File.ReadAllText(pathData))
                : JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(dataInModel);
            if (data == null) return result;
            result = data;
            return result;
        }
        public void Execute()
        {
            _view.ShowDialog();
        }
    }
}
