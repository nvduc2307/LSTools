# Tạo bộ cài LSTools theo khách hàng

## Quy trình

1. Trong Google Sheet, dùng **LSTools License > Tạo mã đóng gói mới**.
2. Ghi chuỗi Base64 nhận được vào:

   ```text
   LSTool\Resources\Settings\ReleaseProfile.dat
   ```

3. Từ thư mục gốc của repository, chạy:

   ```powershell
   .\installer\build-installer.ps1 `
     -CustomerName "A" `
     -AppVersion "1.0.0"
   ```

Script sẽ:

- kiểm tra profile đóng gói;
- build `Release R24`, `Release R25` và `Release R26`;
- kiểm tra profile không bị copy thành file rời;
- từ chối đóng gói nếu còn cấu hình license cũ;
- tạo file `.exe` trong `installer\dist`;
- in SHA-256 để đối chiếu khi gửi khách.

Nếu DLL đã build và chỉ muốn compile lại installer:

```powershell
.\installer\build-installer.ps1 `
  -CustomerName "A" `
  -AppVersion "1.0.0" `
  -SkipBuild
```

## Hành vi trên máy khách

- Bộ cài yêu cầu đóng Revit trước khi chạy.
- Tự phát hiện Revit 2024, 2025 và 2026.
- Chỉ cài phiên bản add-in tương ứng với Revit có trên máy.
- Cài theo tài khoản Windows hiện tại, không yêu cầu quyền Administrator.
- Gỡ cài đặt từ **Installed apps > LSTools**.
- Dữ liệu phiên sử dụng được mã hóa tại
  `%LocalAppData%\LSTools\runtime-state.dat` và không bị xóa khi gỡ add-in.

## Yêu cầu máy build

- .NET SDK dùng được với dự án.
- Inno Setup 6, mặc định tại:

  ```text
  C:\Program Files (x86)\Inno Setup 6\ISCC.exe
  ```

## Chữ ký số

Bộ cài hiện chưa được ký Authenticode nên Windows SmartScreen có thể cảnh báo khi khách tải và
chạy file. Trước khi phát hành rộng rãi nên dùng chứng thư code-signing để ký file `.exe` cuối.
