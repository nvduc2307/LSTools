# Quản lý thời hạn LSTools bằng Google Sheet

## Mô hình đang dùng

- Khách hàng không thấy nút License, hộp nhập key, mã thiết bị hay thông tin Google Sheet.
- Mỗi khách nhận một bộ cài riêng. Bộ cài chứa một mã kích hoạt đóng gói nội bộ.
- Lần chạy đầu tiên, LSTools tự kích hoạt ngầm và gắn quyền sử dụng với máy đó.
- Server trả về một credential riêng cho máy; credential được Windows DPAPI mã hóa tại
  `%LocalAppData%\LSTools\runtime-state.dat`.
- Gỡ rồi cài lại trên cùng máy không làm thời hạn bắt đầu lại. Nếu file cục bộ bị xóa,
  LSTools chỉ xin cấp lại credential cho đúng máy đã bind và ngày hết hạn vẫn giữ nguyên.
- Cài bộ đó trên máy khác sẽ bị từ chối cho tới khi quản trị viên reset thiết bị trong Sheet.
- Mỗi lần xác nhận online, Apps Script cấp lease ký RSA có hiệu lực tối đa 72 giờ. Trong khoảng
  này phần mềm có thể tiếp tục dùng khi mất mạng.

Google Apps Script là endpoint serverless; không cần VPS hoặc máy cá nhân chạy 24/24. Google
Sheet chỉ là bảng quản trị riêng của nhà cung cấp và không chia sẻ cho khách.

## Tạo mã đóng gói cho một khách hàng

1. Mở Google Sheet quản trị.
2. Chọn menu **LSTools License > Tạo mã đóng gói mới**.
3. Nhập tên khách hàng, số ngày sử dụng, danh sách tính năng và ghi chú.
4. Hộp thoại trả về một chuỗi Base64. Đây là mã dùng để build bộ cài, không gửi chuỗi này cho
   khách hàng.
5. Ghi nguyên chuỗi Base64 vào:

   ```text
   LSTool\Resources\Settings\ReleaseProfile.dat
   ```

6. Build bộ cài riêng cho khách hàng rồi gửi bộ cài đó.

`ReleaseProfile.dat` được nhúng vào DLL dưới dạng embedded resource, không được copy thành file
rời trong thư mục phát hành. Giá trị hiện tại có thể tạm ghi đè khi kiểm thử bằng biến môi trường
`LSTOOLS_BOOTSTRAP_CREDENTIAL`.

## Build bộ cài và phát hành

Sau khi thay `ReleaseProfile.dat`, chạy:

```powershell
.\installer\build-installer.ps1 `
  -CustomerName "Tên-khách" `
  -AppVersion "1.0.0"
```

Script tự build Release R24–R26, kiểm tra gói phát hành rồi tạo file `.exe` riêng cho khách trong
`installer\dist`. Bộ cài tự phát hiện Revit 2024–2026 trên máy khách và chỉ cài add-in tương ứng.

Endpoint server nằm trong:

```text
LSTool\Resources\Settings\ReleaseChannel.json
```

File này chỉ chứa URL web app. Có thể ghi đè URL khi kiểm thử bằng biến môi trường
`LSTOOLS_LICENSE_API_URL`.

Trước khi giao, kiểm tra:

- Không có nút hoặc cửa sổ License trên ribbon.
- Không có `ReleaseProfile.dat` rời trong output.
- Có `Resources\Settings\ReleaseChannel.json`.
- Không còn `Resources\Settings\LicenseServer.json`.
- Mở Revit với kết nối Internet, chạy một lệnh LSTools và xác nhận lệnh hoạt động.

## Quản lý khách hàng

- **Đổi máy:** chọn dòng tương ứng rồi dùng
  **LSTools License > Reset máy của dòng đang chọn**. Sau đó khách mở LSTools trên máy mới để
  phần mềm tự bind lại.
- **Khóa:** chọn dòng rồi dùng **Khóa dòng đang chọn**.
- **Mở lại:** chọn dòng rồi dùng **Mở lại dòng đang chọn**.
- **Gia hạn:** sửa `ExpiresUtc` thành thời điểm UTC mới. Không tạo lại mã đóng gói và không cần
  gửi lại bộ cài nếu khách vẫn dùng đúng máy.
- **Giới hạn tính năng:** cột `Features` dùng tên tính năng, phân cách bằng dấu phẩy; `*` cho phép
  toàn bộ.

Khóa hoặc thay đổi thời hạn có hiệu lực ở lần kiểm tra online tiếp theo, hoặc muộn nhất khi lease
72 giờ đã lưu trên máy hết hạn.

## Cấu hình Apps Script

Script Properties cần có:

| Property | Giá trị |
| --- | --- |
| `LSTOOLS_SHEET_ID` | ID của Google Sheet quản trị |
| `LSTOOLS_PRIVATE_KEY` | Nội dung private key RSA dùng ký lease |

Private key chỉ nằm trong Script Properties và bản sao lưu an toàn của nhà cung cấp. Không đưa
private key vào DLL, file phát hành, source public hoặc một ô trong Sheet.

Mã nguồn server:

- `license-server\google-apps-script\Code.gs`
- `license-server\google-apps-script\appsscript.json`

Sau mỗi lần sửa `Code.gs`, lưu project rồi cập nhật deployment đang dùng bằng một **phiên bản
mới**. Giữ nguyên deployment ID để URL `/exec` trong `ReleaseChannel.json` không thay đổi.

## Lưu ý bảo mật và vận hành

- Cách này ẩn hoàn toàn quy trình license khỏi trải nghiệm thông thường của khách, nhưng không thể
  làm bí mật tuyệt đối trước người có khả năng reverse-engineer DLL. Quyền kiểm soát thực tế vẫn
  nằm ở bind máy, ngày hết hạn, chữ ký RSA và quyết định của server.
- Client refresh sau 24 giờ. Khi server tạm thời không truy cập được, lease đã ký còn hạn vẫn được
  chấp nhận.
- Nếu server trả lời từ chối rõ ràng, client chặn ngay và không dùng lease cache để vượt qua quyết
  định đó.
- Apps Script/Google Sheet phù hợp với thử nghiệm hoặc quy mô nhỏ. Khi số lượng request lớn, có thể
  giữ nguyên giao thức client và chuyển endpoint sang backend chuyên dụng.
