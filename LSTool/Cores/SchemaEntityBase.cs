using Autodesk.Revit.DB.ExtensibleStorage;

namespace LSTool.Cores
{
    public abstract class SchemaEntityBase
    {
        private const string VALUE_NAME = "Content";
        public string Guid { get; }
        public string Name { get; }
        private Schema _schema;
        protected SchemaEntityBase(string guid, string name)
        {
            Guid = guid;
            Name = name;
            _schema = CreateBaseSchema(guid, name);
        }
        private Schema CreateBaseSchema(string guid, string name)
        {
            var schemaBuilder = new SchemaBuilder(new Guid(guid));
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);
            schemaBuilder.SetSchemaName(name);
            schemaBuilder.AddSimpleField("Content", typeof(string));
            var schema = Schema.Lookup(new Guid(guid)) ?? schemaBuilder.Finish();
            return schema;
        }
        public void Write(Element element, string content)
        {
            var entity = new Entity(_schema);
            var field = _schema.GetField(VALUE_NAME);
            entity.Set(field, content);
            element.SetEntity(entity);
        }
        public string Read(Element element)
        {
            var result = string.Empty;
            if (element == null) return result;
            if (_schema == null) return result;
            var field = _schema.GetField(VALUE_NAME);
            if (field == null) return result;
            var entity = element?.GetEntity(_schema);
            if (entity == null) return result;
            if (!entity.IsValid()) return result;
            result = entity.Get<string>(field);
            return result;
        }
    }
}
