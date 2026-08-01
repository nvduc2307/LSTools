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
- tải và kiểm tra SHA-256 của ConfuserEx2 CLI chính thức trong lần chạy đầu;
- làm rối từng `LSTool.dll` bằng rename nội bộ, constants và control flow;
- giữ nguyên public API/entry point Revit, sau đó kiểm tra lại bằng metadata;
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

`-SkipBuild` chỉ bỏ qua bước biên dịch C#. DLL vẫn được làm rối lại, đưa vào
`installer\staging` và kiểm tra trước khi Inno Setup chạy.

ConfuserEx2 được cache tại `.tools\confuserex2\1.6.0`. Nếu máy build đã có sẵn
CLI, có thể truyền đường dẫn riêng:

```powershell
.\installer\build-installer.ps1 `
  -CustomerName "A" `
  -AppVersion "1.0.0" `
  -ConfuserCliPath "D:\Tools\ConfuserEx2\Confuser.CLI.exe"
```

File `symbols.map` và DLL trung gian nằm trong
`installer\protection-maps\<ten-bo-cai>\<revit-version>`. Đây là dữ liệu riêng
dùng để đọc stack trace khi hỗ trợ; không đưa thư mục này cho khách.

Profile hiện tại cố ý chưa bật anti-debug, anti-tamper và resource encryption.
Các lớp đó có rủi ro xung đột với Revit host, WPF/BAML và resource kích hoạt;
chỉ bật sau khi có bộ kiểm thử runtime riêng.

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
- Có kết nối Internet trong lần đầu để tải ConfuserEx2 CLI, hoặc truyền
  `-ConfuserCliPath` tới bản CLI đã tải sẵn.

## Kiểm tra trước khi gửi khách

- Xác nhận log có `Renamed symbols` cho đủ R24, R25 và R26.
- Xác nhận kiểm tra metadata báo đủ 7 entry point Revit.
- Chạy bộ cài trên máy thử, mở đúng phiên bản Revit và mở từng cửa sổ WPF quan
  trọng.
- Chạy ít nhất các lệnh Beam Rebar, Install Rebar Beam V2 và Column Rebar trên
  model thử.
- Không gửi `symbols.map`, DLL build gốc hoặc thư mục `protection-maps`.

## Chữ ký số

Bộ cài hiện chưa được ký Authenticode nên Windows SmartScreen có thể cảnh báo khi khách tải và
chạy file. Trước khi phát hành rộng rãi nên dùng chứng thư code-signing để ký file `.exe` cuối.
