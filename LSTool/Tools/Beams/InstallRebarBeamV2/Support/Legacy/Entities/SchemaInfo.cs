using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace RIMT.Utils.Entities
{
    public class SchemaInfo
    {
        public string SchemaGuidID { get; }
        public string SchemaName { get; }
        public SchemaField SchemaField { get; }
        public Schema SchemaBase { get; }
        public SchemaInfo(string schemaGuidID, string schemaName, SchemaField schemaField)
        {
            SchemaGuidID = schemaGuidID;
            SchemaName = schemaName;
            SchemaField = schemaField;
            SchemaBase = CreateBaseSchema(SchemaGuidID, SchemaName, SchemaField);
        }
        public static Schema CreateBaseSchema(string schemaGuidID, string schemaName, SchemaField schemaField)
        {
            var schemaBuilder = new SchemaBuilder(new Guid(schemaGuidID));
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);
            schemaBuilder.SetSchemaName(schemaName);
            schemaBuilder.AddSimpleField(schemaField.Name, typeof(string));
            var schema = Schema.Lookup(new Guid(schemaGuidID)) ?? schemaBuilder.Finish();// register the Schema object
            return schema;
        }

        public static Schema Write(Schema schemaBase, Element element, SchemaField schemaField)
        {
            try
            {
                if (schemaBase == null) throw new ArgumentNullException(nameof(schemaBase));
                if (element == null) throw new ArgumentNullException(nameof(element));
                if (schemaField == null) throw new ArgumentNullException(nameof(schemaField));
                var entity = new Entity(schemaBase);
                var field = schemaBase.GetField(schemaField.Name)
                    ?? throw new InvalidOperationException(
                        $"Schema field '{schemaField.Name}' was not found.");
                entity.Set(field, schemaField.Value);
                element.SetEntity(entity); // store the entity in the element
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to write rebar metadata to element {element?.Id.Value}.", ex);
            }
            return schemaBase;
        }

        public static Schema Write(Schema schemaBase, Element element, Dictionary<string, string> pairs)
        {
            var entity = new Entity(schemaBase);
            foreach (var pair in pairs)
            {
                var field = schemaBase.GetField(pair.Key);
                entity.Set(field, pair.Value);
            }
            element.SetEntity(entity); // store the entity in the element
            return schemaBase;
        }

        public static string Read(Schema schemaBase, Element element, string fieldName)
        {
            var views = "";
            if (schemaBase == null) return views;

            var field = schemaBase.GetField(fieldName);
            if (field == null) return views;

            var entity = element.GetEntity(schemaBase);
            if (entity != null && entity.IsValid())
                views = entity.Get<string>(field);
            return views;
        }

        public static Dictionary<string, string> ReadAll(Schema schemaBase, List<string> schemaBaseKeys, Element element)
        {
            var resultDict = new Dictionary<string, string>();
            if (schemaBase == null)
            {
                return resultDict;
            }
            foreach (var schemaAddField in schemaBaseKeys)
            {
                resultDict.Add(schemaAddField, "");
                var field = schemaBase.GetField(schemaAddField);
                if (field == null) continue;

                var entity = element?.GetEntity(schemaBase);
                if (entity != null && entity.IsValid())
                {
                    resultDict[schemaAddField] = entity.Get<string>(field);
                }
            }
            return resultDict;
        }

        public static SchemaField ReadAll(Schema schemaBase, SchemaField schemaField, Element element)
        {
            SchemaField results = null;
            if (schemaBase != null)
            {
                var field = schemaBase.GetField(schemaField.Name);
                if (field != null)
                {
                    var entity = element?.GetEntity(schemaBase);
                    if (entity != null && entity.IsValid())
                    {
                        results = new SchemaField()
                        {
                            Name = schemaField.Name,
                            Value = entity.Get<string>(field),
                        };
                    }
                }
            }
            return results;
        }
    }
}
