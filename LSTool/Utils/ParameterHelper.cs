using System.IO;

namespace LSTool.Utils
{
    public class ParameterHelper
    {
        public static bool HasParameter(Document document, string parameterName)
        {
            var parameterElements = new FilteredElementCollector(document)
                       .WhereElementIsNotElementType()
                       .OfClass(typeof(ParameterElement))
                       .Cast<ParameterElement>()
                       .Where(x => x != null)
                       .ToList();
            var parameterElement = parameterElements.FirstOrDefault(x => x.Name == parameterName);
            if (parameterElement == null) return false;
            return true;
        }
        public static bool HasParameter(Element element, string parameterName)
        {
            if (element != null)
            {
                foreach (Parameter parameter in element.GetParameters(parameterName))
                {
                    if (parameterName == parameter.Definition.Name)
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }
        public static string GetParameterValue(Element element, BuiltInParameter paraName)
        {
            try
            {
                var result = string.Empty;
                var para = element.get_Parameter(paraName);
                var storageType = para.StorageType;
                switch (storageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        result = para.AsInteger().ToString();
                        break;
                    case StorageType.Double:
                        result = para.AsDouble().ToString();
                        break;
                    case StorageType.String:
                        result = para.AsValueString();
                        break;
                    case StorageType.ElementId:
                        result = para.AsElementId().ToString();
                        break;
                }
                return result;

            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
        public static string GetParameterValue(Element element, string paraName)
        {
            var hasPara = HasParameter(element, paraName);
            if (!hasPara) return string.Empty;
            try
            {
                var result = string.Empty;
                var para = element.LookupParameter(paraName);
                var storageType = para.StorageType;
                switch (storageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        result = para.AsInteger().ToString();
                        break;
                    case StorageType.Double:
                        result = para.AsDouble().ToString();
                        break;
                    case StorageType.String:
                        result = para.AsValueString();
                        break;
                    case StorageType.ElementId:
                        result = para.AsElementId().ToString();
                        break;
                }
                return result;

            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
        public static void SetParameterValue(Element element, string paraName, string paraValue)
        {
            try
            {
                var para = element.LookupParameter(paraName);
                if (para == null) return;
                var storageType = para.StorageType;
                switch (storageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        para.Set(int.Parse(paraValue));
                        break;
                    case StorageType.Double:
                        para.Set(double.Parse(paraValue));
                        break;
                    case StorageType.String:
                        para.Set(paraValue);
                        break;
                    case StorageType.ElementId:
                        para.Set(new ElementId(int.Parse(paraValue)));
                        break;
                }

            }
            catch (Exception)
            {
            }
        }
        public static void SetParameterValue(Element element, BuiltInParameter paraName, string paraValue)
        {
            try
            {
                var para = element.get_Parameter(paraName);
                var storageType = para.StorageType;
                switch (storageType)
                {
                    case StorageType.None:
                        break;
                    case StorageType.Integer:
                        para.Set(int.Parse(paraValue));
                        break;
                    case StorageType.Double:
                        para.Set(double.Parse(paraValue));
                        break;
                    case StorageType.String:
                        para.Set(paraValue);
                        break;
                    case StorageType.ElementId:
                        para.Set(new ElementId(int.Parse(paraValue)));
                        break;
                }

            }
            catch (Exception)
            {
            }
        }
        public static void DeleteParameter(Document document, string namePara)
        {
            try
            {
                var par = new FilteredElementCollector(document)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(ParameterElement))
                        .Cast<ParameterElement>()
                        .Where(x => x != null)
                        .FirstOrDefault(x => x.Name == namePara);
                if (par == null) return;
                document.Delete(par.Id);
            }
            catch (Exception)
            {
            }
        }
        public static void DeleteParameter(Document document, List<string> nameParameters)
        {
            try
            {
                foreach (var namePara in nameParameters)
                {
                    var par = new FilteredElementCollector(document)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(ParameterElement))
                        .Cast<ParameterElement>()
                        .Where(x => x != null)
                        .FirstOrDefault(x => x.Name == namePara);
                    if (par == null) continue;
                    document.Delete(par.Id);
                }
            }
            catch (Exception)
            {
            }
        }
        public static void DeleteParameter(
            Document document,
            Autodesk.Revit.ApplicationServices.Application app,
            string pathShareParameter,
            List<string> paraNameIgnores)
        {
            string originalFile = app.SharedParametersFilename;
            try
            {
                if (!File.Exists(pathShareParameter)) return;
                app.SharedParametersFilename = pathShareParameter;
                DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();
                var parameterElements = new FilteredElementCollector(document)
                       .WhereElementIsNotElementType()
                       .OfClass(typeof(ParameterElement))
                       .Cast<ParameterElement>()
                       .Where(x => x != null)
                       .ToList();
                var parameterElementDeletes = new List<ParameterElement>();
                foreach (DefinitionGroup dg in sharedParameterFile.Groups)
                {
                    var definitions = dg.Definitions;
                    foreach (var definition in definitions)
                    {
                        try
                        {
                            if (paraNameIgnores.Any(x => x == definition.Name))
                                continue;
                            var parameterElement = parameterElements.FirstOrDefault(x => x.Name == definition.Name);
                            if (parameterElement == null) continue;
                            parameterElementDeletes.Add(parameterElement);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                if (parameterElementDeletes.Any())
                    document.Delete(parameterElementDeletes.Select(x => x.Id).ToList());
            }
            catch { }
            finally
            {
                app.SharedParametersFilename = originalFile;
            }
        }
        public static void CreateSharedParameters(
            Document document,
            string pathShareParameter,
            BuiltInCategory cateToAdd)
        {
            if (document == null) return;
            var app = document.Application;
            string originalFile = app.SharedParametersFilename;
            try
            {
                Category category = document.Settings.Categories.get_Item(cateToAdd);
                CategorySet categorySet = app.Create.NewCategorySet();
                categorySet.Insert(category);
                if (!File.Exists(pathShareParameter)) return;
                app.SharedParametersFilename = pathShareParameter;
                DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();
                var parameters = new FilteredElementCollector(document)
                       .WhereElementIsNotElementType()
                       .OfClass(typeof(ParameterElement))
                       .Cast<ParameterElement>()
                       .Where(x => x != null)
                       .ToList();
                foreach (DefinitionGroup dg in sharedParameterFile.Groups)
                {
                    var definitions = dg.Definitions;
                    foreach (var definition in definitions)
                    {
                        try
                        {
                            if (parameters.Any(x => x.Name == definition.Name))
                                continue;
                            var externalDefinition_With = definition as ExternalDefinition;
                            //parameter binding 
                            InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                            //parameter group to text
                            var dforgeid = new ForgeTypeId();
                            //document.ParameterBindings.Insert(externalDefinition_With, newIB, dforgeid);
                            document.ParameterBindings.Insert(externalDefinition_With, newIB);
                        }
                        catch (Exception)
                        {
                            //IO.ShowWarning(ex.Message);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                app.SharedParametersFilename = originalFile;
            }
        }
    }
}
