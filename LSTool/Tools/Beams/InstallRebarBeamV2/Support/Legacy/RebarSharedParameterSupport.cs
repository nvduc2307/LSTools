using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System.IO;
using System.Reflection;

namespace LSTool.Tools.Beams.InstallRebarBeamV2.Support.Legacy
{
    internal static class RebarSharedParameterSupport
    {
        private static readonly HashSet<string> RequiredParameterNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "REBAR_TYPE",
            "SCHEDULE_REBAR_GEOMETRY_SHAPE",
            "SEGMENT_A",
            "SEGMENT_B",
            "SEGMENT_C",
            "SEGMENT_D",
            "SEGMENT_E",
            "SEGMENT_F",
            "SEGMENT_G",
            "SEGMENT_H",
            "SEGMENT_J",
            "SEGMENT_K",
            "SEGMENT_O",
            "SEGMENT_R"
        };

        private static readonly ForgeTypeId IdentityDataGroup =
            new("autodesk.parameter.group:identityData-1.0.0");

        public static void EnsureRequiredParameters(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!document.IsModifiable)
                throw new InvalidOperationException("A transaction is required to bind rebar shared parameters.");

            var parameterFilePath = GetParameterFilePath();
            if (!File.Exists(parameterFilePath))
                throw new FileNotFoundException("The rebar shared parameter file was not found.", parameterFilePath);

            var application = document.Application;
            var originalFile = application.SharedParametersFilename;
            try
            {
                application.SharedParametersFilename = parameterFilePath;
                var definitionFile = application.OpenSharedParameterFile()
                    ?? throw new InvalidOperationException("The rebar shared parameter file could not be opened.");
                var definitions = definitionFile.Groups
                    .Cast<DefinitionGroup>()
                    .SelectMany(group => group.Definitions.Cast<Definition>())
                    .OfType<ExternalDefinition>()
                    .Where(definition => RequiredParameterNames.Contains(definition.Name))
                    .ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);

                var missingDefinitions = RequiredParameterNames
                    .Where(name => !definitions.ContainsKey(name))
                    .OrderBy(name => name)
                    .ToList();
                if (missingDefinitions.Count > 0)
                    throw new InvalidOperationException(
                        $"Missing required rebar shared parameter definitions: {string.Join(", ", missingDefinitions)}");

                var bindingsByName = GetBindingsByName(document);
                var sharedParametersByName = new FilteredElementCollector(document)
                    .OfClass(typeof(SharedParameterElement))
                    .Cast<SharedParameterElement>()
                    .GroupBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
                var rebarCategory = Category.GetCategory(document, BuiltInCategory.OST_Rebar)
                    ?? throw new InvalidOperationException("The Rebar category is unavailable in this document.");

                foreach (var definition in definitions.Values)
                    EnsureRebarBinding(
                        document,
                        definition,
                        bindingsByName,
                        sharedParametersByName,
                        rebarCategory);
            }
            finally
            {
                application.SharedParametersFilename = originalFile;
            }
        }

        public static void SetRequiredStringParameter(
            Element element,
            string parameterName,
            string value)
        {
            if (element == null || !element.IsValidObject)
                throw new InvalidOperationException(
                    $"Cannot write required parameter '{parameterName}' to an invalid element.");

            var parameter = element.LookupParameter(parameterName)
                ?? throw new InvalidOperationException(
                    $"Required parameter '{parameterName}' is missing on element {element.Id.Value}.");
            if (parameter.IsReadOnly)
                throw new InvalidOperationException(
                    $"Required parameter '{parameterName}' is read-only on element {element.Id.Value}.");
            if (parameter.StorageType != StorageType.String)
                throw new InvalidOperationException(
                    $"Required parameter '{parameterName}' is not a text parameter on element {element.Id.Value}.");
            if (!parameter.Set(value))
                throw new InvalidOperationException(
                    $"Revit rejected the value for required parameter '{parameterName}' on element {element.Id.Value}.");
        }

        public static void SetRequiredStringParameter(
            Element element,
            BuiltInParameter parameterId,
            string value)
        {
            if (element == null || !element.IsValidObject)
                throw new InvalidOperationException(
                    $"Cannot write required parameter '{parameterId}' to an invalid element.");
            var parameter = element.get_Parameter(parameterId)
                ?? throw new InvalidOperationException(
                    $"Required parameter '{parameterId}' is missing on element {element.Id.Value}.");
            if (parameter.IsReadOnly || parameter.StorageType != StorageType.String)
                throw new InvalidOperationException(
                    $"Required parameter '{parameterId}' is not a writable text parameter on element {element.Id.Value}.");
            if (!parameter.Set(value ?? string.Empty))
                throw new InvalidOperationException(
                    $"Revit rejected the value for required parameter '{parameterId}' on element {element.Id.Value}.");
        }

        private static void EnsureRebarBinding(
            Document document,
            ExternalDefinition externalDefinition,
            IReadOnlyDictionary<string, (Definition Definition, Autodesk.Revit.DB.Binding Binding)> bindingsByName,
            IReadOnlyDictionary<string, SharedParameterElement> sharedParametersByName,
            Category rebarCategory)
        {
            bindingsByName.TryGetValue(externalDefinition.Name, out var existing);
            sharedParametersByName.TryGetValue(externalDefinition.Name, out var sameNameSharedParameter);
            if (sameNameSharedParameter != null
                && sameNameSharedParameter.GuidValue != externalDefinition.GUID)
            {
                throw new InvalidOperationException(
                    $"Shared parameter '{externalDefinition.Name}' exists with a different GUID.");
            }
            if (existing.Definition != null && sameNameSharedParameter == null)
                throw new InvalidOperationException(
                    $"Parameter '{externalDefinition.Name}' exists but is not the required shared parameter.");
            if (existing.Definition is InternalDefinition existingInternal
                && sameNameSharedParameter?.GetDefinition() is InternalDefinition expectedInternal
                && existingInternal.Id.Value != expectedInternal.Id.Value)
                throw new InvalidOperationException(
                    $"Parameter '{externalDefinition.Name}' is bound from a different definition.");

            if (existing.Binding is TypeBinding)
                throw new InvalidOperationException(
                    $"Shared parameter '{externalDefinition.Name}' is type-bound; an instance binding is required.");

            var categories = document.Application.Create.NewCategorySet();
            if (existing.Binding is ElementBinding existingBinding)
            {
                foreach (Category category in existingBinding.Categories)
                    categories.Insert(category);
            }
            categories.Insert(rebarCategory);

            if (existing.Binding is ElementBinding currentBinding
                && currentBinding.Categories.Cast<Category>()
                    .Any(category => category.Id.Value == rebarCategory.Id.Value))
                return;

            var instanceBinding = document.Application.Create.NewInstanceBinding(categories);
            var success = existing.Definition == null
                ? document.ParameterBindings.Insert(externalDefinition, instanceBinding, IdentityDataGroup)
                : document.ParameterBindings.ReInsert(existing.Definition, instanceBinding, IdentityDataGroup);
            if (!success)
                throw new InvalidOperationException(
                    $"Unable to bind shared parameter '{externalDefinition.Name}' to Rebar.");
        }

        private static Dictionary<string, (Definition Definition, Autodesk.Revit.DB.Binding Binding)>
            GetBindingsByName(Document document)
        {
            var result = new Dictionary<string, (Definition, Autodesk.Revit.DB.Binding)>(
                StringComparer.OrdinalIgnoreCase);
            var iterator = document.ParameterBindings.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                var definition = iterator.Key;
                if (definition == null || string.IsNullOrWhiteSpace(definition.Name)) continue;
                result[definition.Name] = (
                    definition,
                    iterator.Current as Autodesk.Revit.DB.Binding);
            }
            return result;
        }

        private static string GetParameterFilePath()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("The add-in directory could not be resolved.");
            return Path.Combine(
                assemblyDirectory,
                "Resources",
                "ShareParameters",
                "RTOOL_SHARE_PARAMETER_REBAR_SCHEDULE.txt");
        }
    }
}
