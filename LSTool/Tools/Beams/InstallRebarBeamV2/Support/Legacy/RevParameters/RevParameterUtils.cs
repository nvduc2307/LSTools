using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using LSTool.Compatibility;
using RIMT.Utils;
using RIMT.Utils.FilterElementsInRevit;
using System.IO;

namespace RIMT.Utils.RevParameters
{
    public static class RevParameterUtils
    {
        public static List<RevParameter> GetAllParametersUserDefine(this Element element)
        {
            try
            {
                var parameters = element.GetOrderedParameters();
                var parametersUserDefine = parameters.Where(x =>
                {
                    return x.IsReadOnly ? false : x.Definition is InternalDefinition { BuiltInParameter: BuiltInParameter.INVALID };
                });
                return parametersUserDefine.Select(x => new RevParameter(x)).ToList();
            }
            catch (Exception)
            {
            }
            return new List<RevParameter>();
        }
        public static List<RevParameter> GetAllParameters(this Element element)
        {
            try
            {
                var parameters = element.GetOrderedParameters();
                var parametersUserDefine = parameters.Where(x =>
                {
                    return x.IsReadOnly ? false : x.Definition is InternalDefinition;
                });
                return parametersUserDefine.Select(x => new RevParameter(x)).ToList();
            }
            catch (Exception)
            {
            }
            return new List<RevParameter>();
        }
        public static List<RevParameter> GetAllTypeParameters(this ElementType elementType)
        {
            try
            {
                var parameters = elementType.GetOrderedParameters();
                var parametersUserDefine = parameters.Where(x =>
                {
                    return x.IsReadOnly ? false : x.Definition is InternalDefinition;
                });
                return parametersUserDefine.Select(x => new RevParameter(x)).ToList();
            }
            catch (Exception)
            {
            }
            return new List<RevParameter>();
        }
        public static string GetParameterValue(this Element element, BuiltInParameter paraName)
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
        public static string GetParameterValue(this Element element, string paraName)
        {
            var hasPara = LSTool.Utils.ParameterHelper.HasParameter(element, paraName);
            if (!hasPara) return string.Empty;
            try
            {
                var result = string.Empty;
                var para = element.LookupParameter(paraName);
                if (para == null) return string.Empty;
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
        public static void SetParameterValue(this Element element, string paraName, string paraValue)
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
                        para.Set(new ElementId(long.Parse(paraValue)));
                        break;
                }

            }
            catch (Exception)
            {
            }
        }
        public static void SetParameterValue(this Element element, BuiltInParameter paraName, string paraValue)
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
                        para.Set(new ElementId(long.Parse(paraValue)));
                        break;
                }

            }
            catch (Exception)
            {
            }
        }
        public static void DeleteParameter(this Document document, string nameParameter)
        {
            try
            {
                var par = document.GetElementsFromClass<ParameterElement>()
                        .FirstOrDefault(x => x.Name == nameParameter);
                if (par == null) return;
                document.Delete(par.Id);
            }
            catch (Exception)
            {
            }
        }
        public static void DeleteParameter(this Document document, List<string> nameParameters)
        {
            try
            {
                foreach (var namePara in nameParameters)
                {
                    var par = document.GetElementsFromClass<ParameterElement>()
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
            this Document document,
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
                var parameterElements = document.GetElementsFromClass<ParameterElement>();
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
            this Document doc,
            Autodesk.Revit.ApplicationServices.Application app,
            string pathShareParameter,
            BuiltInCategory cateToAdd,
            List<string> paraNameIgnores)
        {
            string originalFile = app.SharedParametersFilename;
            try
            {
                var rebar = doc.GetElementsFromClass<Rebar>(false).FirstOrDefault();
                Category category = doc.Settings.Categories.get_Item(cateToAdd);
                CategorySet categorySet = app.Create.NewCategorySet();
                categorySet.Insert(category);
                if (!File.Exists(pathShareParameter)) return;
                app.SharedParametersFilename = pathShareParameter;
                DefinitionFile sharedParameterFile = app.OpenSharedParameterFile();
                foreach (DefinitionGroup dg in sharedParameterFile.Groups)
                {
                    var definitions = dg.Definitions.OrderBy(x => x.Name);
                    foreach (var definition in definitions)
                    {
                        try
                        {
                            if (paraNameIgnores.Any(x => x == definition.Name))
                            {
                                var parameterValue = rebar.GetParameterValue(definition.Name);
                                if (!string.IsNullOrEmpty(parameterValue))
                                    continue;
                            }
                            ExternalDefinition externalDefinition_With = definition as ExternalDefinition;
                            //parameter binding 
                            InstanceBinding newIB = app.Create.NewInstanceBinding(categorySet);
                            //parameter group to text
                            var dforgeid = new ForgeTypeId("autodesk.parameter.group:identityData-1.0.0");
                            if (definition.Name.ToUpper().Contains("SEGMENT") || definition.Name.Split('_').FirstOrDefault().ToUpper().Contains("LAP"))
                                doc.ParameterBindings.Insert(externalDefinition_With, newIB, dforgeid);
                            else
                                doc.ParameterBindings.Insert(externalDefinition_With, newIB, null);
                        }
                        catch (Exception ex)
                        {
                            IO.ShowWarning(ex.Message);
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
