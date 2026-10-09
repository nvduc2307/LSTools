using LSTool.Cores;

namespace LSTool.Tools.Beams.BeamRebar.schemas
{
    /// <summary>Lưu danh sách đai phụ (BeamTieModel) vào dầm để mở lại tool vẫn còn.</summary>
    public class BeamTiePositionSchema : SchemaEntityBase
    {
        public const string GUID = "c7137002-15a4-4179-86aa-c4b310afeb8f";
        public const string NAME = "BeamTiePositionSchema";
        public BeamTiePositionSchema(string guid, string name) : base(guid, name)
        {
        }
    }
}
