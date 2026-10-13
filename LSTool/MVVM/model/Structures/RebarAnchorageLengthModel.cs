using Newtonsoft.Json;

namespace LSTool.MVVM.model.Structures
{
    public class RebarAnchorageLengthModel
    {
        public int Id { get; set; }
        public int ConcreteStrengthRatingId { get; set; }
        public int StructureRebarType { get; set; }
        public int Ldt { get; set; }

        [JsonIgnore]
        public string ConcreteStrengthRatingName { get; set; }
        [JsonIgnore]
        public string StructureRebarTypeName { get; set; }
    }
}
