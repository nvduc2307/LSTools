using LSTool.Tools.Columns.ColumnRebar.models;
using System.Collections.ObjectModel;

namespace LSTool.Tools.Columns.ColumnRebar.actions
{
    public partial class ColumnRebarAction
    {
        private ColumnRebarPresetStore _presetStore = new ColumnRebarPresetStore();

        /// <summary>
        /// Hiển thị thông báo trạng thái inline, tự ẩn sau <paramref name="durationMs"/> ms.
        /// </summary>
        private void _ShowStatus(string message, int durationMs = 2500)
        {
            _viewModel.StatusText = message;
            var timer = new System.Timers.Timer(durationMs) { AutoReset = false };
            timer.Elapsed += (_, _) =>
            {
                _viewModel.StatusText = string.Empty;
                timer.Dispose();
            };
            timer.Start();
        }

        /// <summary>
        /// Khởi tạo danh sách preset từ file JSON khi mở tool.
        /// </summary>
        private void _InitPresets()
        {
            var list = _presetStore.Load();
            _viewModel.Presets = new ObservableCollection<ColumnRebarPresetModel>(list);
            _viewModel.SelectedPreset = _viewModel.Presets.FirstOrDefault();
        }

        /// <summary>
        /// Load — nạp preset đang chọn vào ColumnConcreteModel hiện tại.
        /// </summary>
        private void _LoadCommand()
        {
            var preset = _viewModel.SelectedPreset;
            if (preset == null)
            {
                _ShowStatus("⚠ Vui lòng chọn một preset.");
                return;
            }

            var col = _viewModel.ColumnConcreteModel;
            if (col == null) return;

            col.DiameterDX = preset.DiameterDX;
            col.SpacingDX = preset.SpacingDX;
            col.DiameterDY = preset.DiameterDY;
            col.SpacingDY = preset.SpacingDY;
            col.DiameterST = preset.DiameterST;
            col.SpacingST = preset.SpacingST;
            col.SpacingSTE = preset.SpacingSTE;

            _ShowStatus($"✔ Đã load preset \"{preset.Name}\"");
        }

        /// <summary>
        /// Save — ghi đè preset đang chọn bằng giá trị hiện tại của form.
        /// </summary>
        private void _SaveCommand()
        {
            var preset = _viewModel.SelectedPreset;
            if (preset == null)
            {
                _ShowStatus("⚠ Vui lòng chọn một preset để ghi đè.");
                return;
            }

            var col = _viewModel.ColumnConcreteModel;
            if (col == null) return;

            preset.DiameterDX = col.DiameterDX;
            preset.SpacingDX = col.SpacingDX;
            preset.DiameterDY = col.DiameterDY;
            preset.SpacingDY = col.SpacingDY;
            preset.DiameterST = col.DiameterST;
            preset.SpacingST = col.SpacingST;
            preset.SpacingSTE = col.SpacingSTE;

            _presetStore.Save(_viewModel.Presets.ToList());
            _ShowStatus($"✔ Đã lưu preset \"{preset.Name}\"");
        }

        /// <summary>
        /// SaveAs — tạo preset mới với tên nhập từ TextBox, lưu vào file.
        /// </summary>
        private void _SaveAsCommand()
        {
            var name = _viewModel.NewPresetName?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _ShowStatus("⚠ Vui lòng nhập tên preset.");
                return;
            }

            if (_viewModel.Presets.Any(p => p.Name == name))
            {
                var result = System.Windows.MessageBox.Show(
                    $"Preset \"{name}\" đã tồn tại. Bạn có muốn ghi đè không?",
                    "Save As", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result != System.Windows.MessageBoxResult.Yes) return;

                var existing = _viewModel.Presets.First(p => p.Name == name);
                _viewModel.Presets.Remove(existing);
            }

            var col = _viewModel.ColumnConcreteModel;
            var newPreset = new ColumnRebarPresetModel
            {
                Name = name,
                DiameterDX = col?.DiameterDX,
                SpacingDX = col?.SpacingDX ?? 0,
                DiameterDY = col?.DiameterDY,
                SpacingDY = col?.SpacingDY ?? 0,
                DiameterST = col?.DiameterST,
                SpacingST = col?.SpacingST ?? 0,
                SpacingSTE = col?.SpacingSTE ?? 0,
            };

            _viewModel.Presets.Add(newPreset);
            _viewModel.SelectedPreset = newPreset;
            _viewModel.NewPresetName = string.Empty;

            _presetStore.Save(_viewModel.Presets.ToList());
            _ShowStatus($"✔ Đã tạo preset \"{name}\"");
        }

        /// <summary>
        /// Remove — xoá preset đang chọn khỏi danh sách và file.
        /// </summary>
        private void _RemoveCommand()
        {
            var preset = _viewModel.SelectedPreset;
            if (preset == null)
            {
                _ShowStatus("⚠ Vui lòng chọn một preset để xoá.");
                return;
            }

            if (preset.IsDefault)
            {
                _ShowStatus($"⚠ Preset mặc định \"{preset.Name}\" không thể xoá.");
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"Bạn có chắc muốn xoá preset \"{preset.Name}\" không?",
                "Remove Preset", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            var removedName = preset.Name;
            _viewModel.Presets.Remove(preset);
            _viewModel.SelectedPreset = _viewModel.Presets.FirstOrDefault();

            _presetStore.Save(_viewModel.Presets.ToList());
            _ShowStatus($"✔ Đã xoá preset \"{removedName}\"");
        }
    }
}
