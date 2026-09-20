using Newtonsoft.Json;

namespace LSTool.Tools.Columns.ColumnRebar.models
{
    public class ColumnRebarPresetModel
    {
        public string Name { get; set; }

        // Dir X
        public string DiameterDX { get; set; }
        public double SpacingDX { get; set; }

        // Dir Y
        public string DiameterDY { get; set; }
        public double SpacingDY { get; set; }

        // Stirrup Mid
        public string DiameterST { get; set; }
        public double SpacingST { get; set; }

        // Stirrup End
        public double SpacingSTE { get; set; }

        /// <summary>
        /// True nếu preset này đến từ file default trong Resources — không được phép xoá.
        /// Không lưu vào file JSON của người dùng.
        /// </summary>
        [JsonIgnore]
        public bool IsDefault { get; set; }
    }
}
