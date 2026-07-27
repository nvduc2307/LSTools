namespace RIMT.Utils.Entities
{
    public class SchemaField
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public SchemaField()
        {
            Name = "Content";
            Value = string.Empty;
        }
    }
}
