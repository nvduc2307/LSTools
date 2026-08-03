using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using LSTool.Tools.Generals.SettingDiameters.models;
using LSTool.Tools.Generals.SettingDiameters.viewModels;
using LSTool.Tools.Generals.SettingDiameters.views;
using LSTool.Utils;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
                OkCommand = new RelayCommand(_OkCommand),
                ResetCommand = new RelayCommand(_ResetCommand),
                CancelCommand = new RelayCommand(_CancelCommand),
            };
            _viewModel.RebarBarTypes = new ObservableCollection<RebarBarTypeModel>(GetRebarBarTypes());
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
            SynchronizeRebarBarTypes(_document, rebarBarTypeModels);
        }

        public static void SynchronizeRebarBarTypes(
            Document document,
            IEnumerable<RebarBarTypeModel> rebarBarTypeModels)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (!document.IsModifiable)
                throw new InvalidOperationException(
                    "A transaction is required to synchronize Rebar Bar Types.");

            var configuredTypes = rebarBarTypeModels?
                .Where(type => type != null)
                .ToList() ?? new List<RebarBarTypeModel>();
            if (configuredTypes.Count == 0) return;

            var rebarBarTypes = new FilteredElementCollector(document)
                .WhereElementIsElementType()
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .ToList();
            var failures = new List<Exception>();
            foreach (var type in configuredTypes)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(type.NameStyle))
                        throw new InvalidOperationException(
                            "A configured Rebar Bar Type has no name.");
                    if (type.BarDiameterReal <= 0.0
                        || double.IsNaN(type.BarDiameterReal)
                        || double.IsInfinity(type.BarDiameterReal))
                    {
                        throw new InvalidOperationException(
                            $"Rebar Bar Type '{type.NameStyle}' has an invalid "
                            + $"configured diameter {type.BarDiameterReal} mm.");
                    }

                    var typeTarget = rebarBarTypes.FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            type.NameStyle,
                            StringComparison.OrdinalIgnoreCase));
                    var configuredDiameterFt =
                        type.BarDiameterReal.FromMillimeters();
                    var configuredBendDiameterFt =
                        type.StandardBendDiameter.FromMillimeters();
                    if (typeTarget == null)
                    {
                        typeTarget = RebarBarTypeHelper.CreateNewType(
                            document,
                            type.NameStyle,
                            configuredDiameterFt,
                            configuredBendDiameterFt);
                        rebarBarTypes.Add(typeTarget);
                    }
                    else
                    {
                        RebarBarTypeHelper.SetRebarDiameter(
                            typeTarget,
                            configuredDiameterFt);
                        RebarBarTypeHelper.SetRebarBendDiameter(
                            typeTarget,
                            configuredBendDiameterFt);
                    }

                    VerifySynchronizedDiameter(
                        typeTarget,
                        configuredDiameterFt);
                }
                catch (Exception exception)
                {
                    failures.Add(new InvalidOperationException(
                        $"Failed to synchronize Rebar Bar Type "
                        + $"'{type.NameStyle ?? "<unnamed>"}'.",
                        exception));
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "One or more configured Rebar Bar Types could not be "
                    + "synchronized.",
                    failures);
            }
        }

        public List<RebarBarTypeModel> GetRebarBarTypes()
        {
            return ReadConfiguredRebarBarTypes(_document);
        }

        public static List<RebarBarTypeModel> ReadConfiguredRebarBarTypes(
            Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            var pathData = $"{PathHelper.Datas}\\RebarbarTypeData.json";
            if (!File.Exists(pathData))
                return new List<RebarBarTypeModel>();
            var schema = new RebarBarTypeSchema(
                RebarBarTypeSchema.GUID,
                RebarBarTypeSchema.NAME);
            var dataInModel = schema.Read(document.ProjectInformation);
            var data = string.IsNullOrEmpty(dataInModel)
                ? JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(File.ReadAllText(pathData))
                : JsonConvert.DeserializeObject<List<RebarBarTypeModel>>(dataInModel);
            return data ?? new List<RebarBarTypeModel>();
        }

        private static void VerifySynchronizedDiameter(
            RebarBarType rebarBarType,
            double expectedDiameterFt)
        {
            var nominalParameter = rebarBarType.get_Parameter(
                BuiltInParameter.REBAR_BAR_DIAMETER);
            var modelParameter = rebarBarType.get_Parameter(
                BuiltInParameter.REBAR_MODEL_BAR_DIAMETER);
            if (nominalParameter == null || modelParameter == null)
            {
                throw new InvalidOperationException(
                    $"Rebar Bar Type '{rebarBarType.Name}' does not expose "
                    + "both nominal and modeled diameter parameters.");
            }

            var expectedMm = expectedDiameterFt.ToMillimeters();
            var nominalMm = nominalParameter.AsDouble().ToMillimeters();
            var modelMm = modelParameter.AsDouble().ToMillimeters();
            if (Math.Abs(nominalMm - expectedMm) > 1.0
                || Math.Abs(modelMm - expectedMm) > 1.0)
            {
                throw new InvalidOperationException(
                    $"Rebar Bar Type '{rebarBarType.Name}' remains "
                    + "inconsistent after synchronization: configured "
                    + $"{expectedMm:0.###} mm, nominal "
                    + $"{nominalMm:0.###} mm, modeled "
                    + $"{modelMm:0.###} mm.");
            }
        }
        public void Execute()
        {
            _view.ShowDialog();
        }
    }
}
