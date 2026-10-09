using Newtonsoft.Json;
using LSTool.MVVM.Models;
using LSTool.Tools.Beams.BeamRebar.models;
using LSTool.Utils;
using System.IO;

namespace LSTool.Tools.Beams.BeamRebar.action
{
    /// <summary>Đọc / ghi danh sách setting thép dầm (RebarBeamSetting.json) và áp dụng vào model dầm.</summary>
    public class BeamRebarSettingAction
    {
        private static string FilePath => $"{PathHelper.Appdatas}\\RebarBeamSetting.json";

        public List<BeamRebarSettingModel> GetSettings()
        {
            try
            {
                var path = FilePath;
                if (!File.Exists(path))
                {
                    var pathTemplate = $"{PathHelper.FolderDatas}\\RebarBeamSetting.json";
                    if (!File.Exists(pathTemplate)) return new List<BeamRebarSettingModel>();
                    if (!Directory.Exists(PathHelper.Appdatas))
                        Directory.CreateDirectory(PathHelper.Appdatas);
                    File.Copy(pathTemplate, path);
                }
                return JsonConvert.DeserializeObject<List<BeamRebarSettingModel>>(File.ReadAllText(path))
                       ?? new List<BeamRebarSettingModel>();
            }
            catch (Exception)
            {
                return new List<BeamRebarSettingModel>();
            }
        }

        public void SaveSettings(List<BeamRebarSettingModel> settings)
        {
            if (!Directory.Exists(PathHelper.Appdatas))
                Directory.CreateDirectory(PathHelper.Appdatas);
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }

        /// <summary>Chụp cấu hình thép hiện tại của dầm.</summary>
        public static void Capture(BeamRebarModel beam, BeamRebarSettingModel setting)
        {
            setting.SectionStart = CaptureSection(beam?.SectionStart);
            setting.SectionMid = CaptureSection(beam?.SectionMid);
            setting.SectionEnd = CaptureSection(beam?.SectionEnd);
        }

        /// <summary>Áp dụng setting đã lưu lên dầm (đường kính không có trong danh sách của dầm thì bỏ qua).</summary>
        public static void Apply(BeamRebarSettingModel setting, BeamRebarModel beam)
        {
            if (setting == null || beam == null) return;
            ApplySection(setting.SectionStart, beam.SectionStart);
            ApplySection(setting.SectionMid, beam.SectionMid);
            ApplySection(setting.SectionEnd, beam.SectionEnd);
        }

        private static BeamRebarSectionSettingModel CaptureSection(BeamRebarSectionModel sec)
        {
            var r = new BeamRebarSectionSettingModel();
            if (sec == null) return r;
            r.RebarTop1 = Item(sec.RebarTop1);
            r.RebarTop2 = Item(sec.RebarTop2);
            r.RebarTop3 = Item(sec.RebarTop3);
            r.RebarBot1 = Item(sec.RebarBot1);
            r.RebarBot2 = Item(sec.RebarBot2);
            r.RebarBot3 = Item(sec.RebarBot3);
            r.SideBar = Item(sec.SideBar);
            r.Stirrup = Item(sec.Stirrup);
            return r;
        }

        private static BeamRebarItemSettingModel Item(RebarModel rb) =>
            new BeamRebarItemSettingModel { Name = rb?.Name, Spacing = rb?.Spacing ?? 0 };

        private static void ApplySection(BeamRebarSectionSettingModel s, BeamRebarSectionModel sec)
        {
            if (s == null || sec == null) return;
            Set(s.RebarTop1, sec.RebarTop1);
            Set(s.RebarTop2, sec.RebarTop2);
            Set(s.RebarTop3, sec.RebarTop3);
            Set(s.RebarBot1, sec.RebarBot1);
            Set(s.RebarBot2, sec.RebarBot2);
            Set(s.RebarBot3, sec.RebarBot3);
            Set(s.SideBar, sec.SideBar, keepQty: true);
            Set(s.Stirrup, sec.Stirrup);
        }

        private static void Set(BeamRebarItemSettingModel item, RebarModel rb, bool keepQty = false)
        {
            if (item == null || rb == null) return;
            if (!string.IsNullOrWhiteSpace(item.Name) && rb.Diameters != null && rb.Diameters.Contains(item.Name))
                rb.Name = item.Name;
            if (!keepQty) rb.Spacing = item.Spacing;
        }
    }
}
