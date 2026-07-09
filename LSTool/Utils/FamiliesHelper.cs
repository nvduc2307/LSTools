
namespace LSTool.Utils.Families
{
    public static class FamiliesHelper
    {
        public static void LoadFamily(
            Document document,
            string pathFamily)
        {
            var optionLoadF = new FamilyLoadOptionCustom();
            document.LoadFamily(pathFamily, optionLoadF, out Family family);
        }

        public class FamilyLoadOptionCustom : IFamilyLoadOptions
        {
            public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
            {
                // Cho phép ghi đè lên Family cũ (Tương đương việc bấm nút Overwrite trong Revit)
                // true = Tiến hành load và ghi đè; false = Hủy bỏ việc load Family này
                bool loadFamily = true;

                if (familyInUse)
                {
                    // Nếu Family đang được sử dụng (đã có instance được đặt trong mô hình)
                    // true = Ghi đè luôn cả giá trị Parameter (Nút thứ 2 trong bảng cảnh báo của Revit)
                    // false = Chỉ ghi đè hình học, giữ nguyên giá trị Parameter cũ (Nút thứ 1)
                    overwriteParameterValues = true;
                }
                else
                {
                    // Nếu Family mới chỉ được load vào mà chưa được đặt ra mô hình
                    overwriteParameterValues = true;
                }

                return loadFamily;
            }

            // 2. Hàm này chạy khi Revit tìm thấy một "Shared Family" (Family con được share bên trong Family chính)
            public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
            {
                // Xác định nguồn sẽ được ưu tiên sử dụng
                // FamilySource.Family = Lấy file shared family từ trong bộ cài Family chính đang load vào
                // FamilySource.Project = Giữ nguyên bản shared family đang có sẵn trong dự án hiện tại
                source = FamilySource.Family;

                // Cho phép ghi đè giá trị tham số của Shared Family
                overwriteParameterValues = true;

                // Luôn cho phép tiếp tục load
                return true;
            }
        }
    }
}
