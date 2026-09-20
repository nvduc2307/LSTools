using LSTool.Utils;
using Newtonsoft.Json;
using System.IO;

namespace LSTool.Tools.Columns.ColumnRebar.models
{
    /// <summary>
    /// Quản lý đọc/ghi danh sách preset cốt thép cột.
    /// - Preset người dùng: %AppData%\LSTool\ColumnRebar\presets.json
    /// - Preset mặc định:   [AssemblyDir]\Resources\Datas\ColumnRebarPresets.json
    /// Khi chưa có preset người dùng, tự động nạp preset mặc định từ Resources.
    /// </summary>
    public class ColumnRebarPresetStore
    {
        // File do người dùng lưu (AppData)
        private static readonly string _userFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LSTool", "ColumnRebar", "presets.json");

        // File default đi kèm theo assembly (Resources/Datas)
        private static string DefaultFilePath =>
            Path.Combine(PathHelper.Datas, "ColumnRebarPresets.json");

        /// <summary>
        /// Đọc danh sách preset.
        /// Ưu tiên file của người dùng; nếu chưa có thì dùng file default từ Resources.
        /// </summary>
        public List<ColumnRebarPresetModel> Load()
        {
            // 1. Thử đọc file người dùng trước
            if (File.Exists(_userFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_userFilePath);
                    var list = JsonConvert.DeserializeObject<List<ColumnRebarPresetModel>>(json);
                    if (list != null && list.Count > 0)
                        return list;
                }
                catch { }
            }

            // 2. Fallback: đọc preset mặc định từ Resources
            return LoadDefaults();
        }

        /// <summary>
        /// Ghi danh sách preset vào file người dùng (AppData).
        /// </summary>
        public void Save(List<ColumnRebarPresetModel> presets)
        {
            var dir = Path.GetDirectoryName(_userFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonConvert.SerializeObject(presets, Formatting.Indented);
            File.WriteAllText(_userFilePath, json);
        }

        /// <summary>
        /// Đọc preset mặc định từ file Resources — dùng khi reset hoặc lần đầu.
        /// </summary>
        public List<ColumnRebarPresetModel> LoadDefaults()
        {
            try
            {
                if (!File.Exists(DefaultFilePath))
                    return new List<ColumnRebarPresetModel>();

                var json = File.ReadAllText(DefaultFilePath);
                var list = JsonConvert.DeserializeObject<List<ColumnRebarPresetModel>>(json)
                           ?? new List<ColumnRebarPresetModel>();

                // Đánh dấu preset mặc định — không cho phép xoá
                foreach (var item in list)
                    item.IsDefault = true;

                return list;
            }
            catch
            {
                return new List<ColumnRebarPresetModel>();
            }
        }
    }
}
