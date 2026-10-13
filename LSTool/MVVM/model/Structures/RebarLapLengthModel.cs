using Newtonsoft.Json;

namespace LSTool.MVVM.model.Structures
{
    public class RebarLapLengthModel
    {
        public int Id { get; set; }
        public int ConcreteStrengthRatingId { get; set; }
        public int StructureRebarType { get; set; }
        public int Lst { get; set; }
        public int Gap { get; set; }

        [JsonIgnore]
        public string ConcreteStrengthRatingName { get; set; }
        [JsonIgnore]
        public string StructureRebarTypeName { get; set; }
    }
}
