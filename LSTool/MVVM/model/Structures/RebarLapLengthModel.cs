using Newtonsoft.Json;

namespace LSTool.MVVM.model.Structures
{
    public class RebarLapLengthModel
    {
        public int Id { get; set; }
        public int ConcreteStrengthRatingId { get; set; }// cấp độ bền bê tông
        public int StructureRebarType { get; set; } //[trơn, có gờ]
        public int Lst { get; set; } // LAP LENGTH
        public int Gap { get; set; }

        /// <summary>Tên cấp độ bền bê tông để hiển thị (không lưu vào file).</summary>
        [JsonIgnore]
        public string ConcreteStrengthRatingName { get; set; }
        /// <summary>Tên loại thép để hiển thị (không lưu vào file).</summary>
        [JsonIgnore]
        public string StructureRebarTypeName { get; set; }
    }
}
