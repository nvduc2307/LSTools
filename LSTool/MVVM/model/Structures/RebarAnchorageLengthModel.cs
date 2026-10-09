using Newtonsoft.Json;

namespace LSTool.MVVM.model.Structures
{
    public class RebarAnchorageLengthModel
    {
        public int Id { get; set; }
        public int ConcreteStrengthRatingId { get; set; }// cấp độ bền bê tông
        public int StructureRebarType { get; set; } //[trơn, có gờ]
        public int Ldt { get; set; } // ANCHORAGE LENGTH

        /// <summary>Tên cấp độ bền bê tông để hiển thị (không lưu vào file).</summary>
        [JsonIgnore]
        public string ConcreteStrengthRatingName { get; set; }
        /// <summary>Tên loại thép để hiển thị (không lưu vào file).</summary>
        [JsonIgnore]
        public string StructureRebarTypeName { get; set; }
    }
}
