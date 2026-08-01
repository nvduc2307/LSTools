# Quản lý thời hạn LSTools bằng Google Sheet

## Mô hình đang dùng

- Khách hàng không thấy nút License, hộp nhập key, mã thiết bị hay thông tin Google Sheet.
- Mỗi khách nhận một bộ cài riêng. Bộ cài chứa một mã kích hoạt đóng gói nội bộ.
- Mỗi license có cột `MaxDevices`; cùng một bộ cài có thể kích hoạt trên số máy tối đa đã đặt.
- Lần chạy đầu tiên trên mỗi máy, LSTools tự kích hoạt ngầm và ghi máy vào sheet `Activations`.
- Server trả về một credential riêng cho máy; credential được Windows DPAPI mã hóa tại
  `%LocalAppData%\LSTools\runtime-state.dat`.
- Gỡ rồi cài lại trên cùng máy không làm thời hạn bắt đầu lại. Nếu file cục bộ bị xóa,
  LSTools chỉ xin cấp lại credential cho activation của đúng máy và ngày hết hạn vẫn giữ nguyên.
- Máy mới được chấp nhận khi số activation trạng thái `Active` còn nhỏ hơn `MaxDevices`.
- Mỗi lần xác nhận online, Apps Script cấp lease ký RSA có hiệu lực tối đa 72 giờ. Trong khoảng
  này phần mềm có thể tiếp tục dùng khi mất mạng.

Google Apps Script là endpoint serverless; không cần VPS hoặc máy cá nhân chạy 24/24. Google
Sheet chỉ là bảng quản trị riêng của nhà cung cấp và không chia sẻ cho khách.

## Tạo mã đóng gói cho một khách hàng

1. Mở Google Sheet quản trị.
2. Chọn menu **LSTools License > Tạo mã đóng gói mới**.
3. Nhập tên khách hàng, số ngày sử dụng, số máy tối đa, danh sách tính năng và ghi chú.
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
Trước khi Inno Setup chạy, script dùng ConfuserEx2 để làm rối DLL của từng phiên bản, đặt bản đã bảo
vệ trong `installer\staging` và kiểm tra các entry point Revit bằng metadata. Các file
`symbols.map` được giữ riêng trong `installer\protection-maps`; tuyệt đối không gửi chúng cho khách.

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
- Log ConfuserEx2 có số lượng `Renamed symbols` cho cả R24, R25 và R26.
- Kiểm tra metadata xác nhận đủ các entry point Revit và embedded release profile.
- Mở Revit với kết nối Internet, chạy một lệnh LSTools và xác nhận lệnh hoạt động.
- Mở các cửa sổ WPF chính để kiểm tra binding/BAML sau obfuscation.

## Quản lý khách hàng

- **Đặt số máy:** chọn dòng trong `Licenses`, rồi dùng
  **LSTools License > Đặt số máy tối đa**. Có thể sửa trực tiếp cột `MaxDevices`, giá trị hợp lệ
  từ 1 đến 100.
- **Xem máy:** chọn dòng trong `Licenses`, rồi dùng
  **Xem máy của license đang chọn** để chuyển tới sheet `Activations`.
- **Thu hồi một máy:** chọn dòng tương ứng trong `Activations`, rồi dùng
  **Thu hồi máy đang chọn**. Activation chuyển thành `Revoked` và không còn chiếm chỗ.
- **Mở lại một máy:** chọn dòng `Revoked` trong `Activations`, rồi dùng
  **Mở lại máy đang chọn**. Thao tác bị từ chối nếu license đã đủ số máy.
- **Thu hồi toàn bộ máy:** chọn license rồi dùng
  **Thu hồi toàn bộ máy của license**. Dùng khi cần chuyển toàn bộ quyền sang nhóm máy mới.
- **Khóa:** chọn dòng rồi dùng **Khóa dòng đang chọn**.
- **Mở lại:** chọn dòng rồi dùng **Mở lại dòng đang chọn**.
- **Gia hạn:** sửa `ExpiresUtc` thành thời điểm UTC mới. Không tạo lại mã đóng gói và không cần
  gửi lại bộ cài nếu khách vẫn dùng đúng máy.
- **Giới hạn tính năng:** cột `Features` dùng tên tính năng, phân cách bằng dấu phẩy; `*` cho phép
  toàn bộ.

Các license cũ được tự động đặt `MaxDevices = 1`; `DeviceHash` cũ được chuyển thành một dòng
`Active` trong `Activations`. Cột `DeviceHash` trong `Licenses` chỉ được giữ để tương thích và
xem nhanh máy đầu tiên. Giảm `MaxDevices` không tự thu hồi các máy đang hoạt động; hãy thu hồi
từng dòng trong `Activations` nếu cần.

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
mới**. Giữ nguyên deployment ID để URL `/exec` trong `ReleaseChannel.json` không thay đổi. Sau
khi triển khai phiên bản có thay đổi cấu trúc dữ liệu, chạy `setupLicenseSheet()` một lần để bổ
sung cột và sheet mới.

Kiểm tra logic giới hạn máy tại local:

```powershell
node .\tests\GoogleAppsScriptLicenseKernelTests\run-tests.js
```

## Lưu ý bảo mật và vận hành

- Cách này ẩn hoàn toàn quy trình license khỏi trải nghiệm thông thường của khách, nhưng không thể
  làm bí mật tuyệt đối trước người có khả năng reverse-engineer DLL. Quyền kiểm soát thực tế vẫn
  nằm ở bind máy, ngày hết hạn, chữ ký RSA và quyết định của server.
- ConfuserEx2 làm tăng đáng kể chi phí dịch ngược bằng rename nội bộ, constants và control flow,
  nhưng không biến DLL .NET thành mã không thể dịch ngược. Không đặt private key hoặc bí mật server
  trong DLL dù đã obfuscate.
- Client refresh sau 24 giờ. Khi server tạm thời không truy cập được, lease đã ký còn hạn vẫn được
  chấp nhận.
- Nếu server trả lời từ chối rõ ràng, client chặn ngay và không dùng lease cache để vượt qua quyết
  định đó.
- Apps Script/Google Sheet phù hợp với thử nghiệm hoặc quy mô nhỏ. Khi số lượng request lớn, có thể
  giữ nguyên giao thức client và chuyển endpoint sang backend chuyên dụng.
