# Báo cáo triển khai và audit `InstallRebarBeamV2Cmd`

- Ngày audit ban đầu: 2026-07-18
- Ngày cập nhật triển khai: 2026-07-18
- Nguồn đối chiếu: `E:\OvertimeWork\RimT`
- Dự án đích: `E:\OvertimeWork\LSTools`
- Branch: `codex/port-install-rebar-beam-v2`
- Baseline trước sửa: `f4a8841`
- Phạm vi: selection/assembly, resource, shared parameter, preset đường kính, `ElementId`, cache shape, transaction, tạo rebar, opening và build R24-R26
- Giới hạn: chưa chạy tự động ma trận runtime trong Revit sau thay đổi này

## Kết luận hiện tại

Các blocker và sai lệch tĩnh đã phát hiện trong audit đã được triển khai. Nguyên nhân trực tiếp làm tool chỉ chạy một đoạn dầm — filter từ chối `AssemblyInstance` — đã được sửa.

Mã hiện build thành công trên R24, R25 và R26, resource bắt buộc có trong output, đường ghi model đã được chuyển sang cơ chế fail-and-rollback thay vì commit một phần. Tuy nhiên, **chưa nên coi là sẵn sàng phát hành production** cho tới khi chạy xong ma trận runtime trong Revit, đặc biệt là assembly nhiều đoạn, opening, model trắng và multi-document.

## Nội dung đã triển khai

### 1. Khôi phục scope toàn bộ assembly

- Selection filter chấp nhận `AssemblyInstance` khi assembly có member và tất cả member thuộc `OST_StructuralFraming`.
- Vẫn cho phép chọn trực tiếp một beam đơn.
- Sau selection có guard xác nhận danh sách member không rỗng và chỉ gồm structural-framing `FamilyInstance`; input sai dừng trước khi tạo rebar.
- Các lỗi tính kích thước section và hệ tọa độ beam không còn bị đổi thành giá trị `0` hoặc bị bỏ qua.

### 2. Bổ sung contract shared parameter

- Đã port `RTOOL_SHARE_PARAMETER_REBAR_SCHEDULE.txt` và cấu hình copy vào output.
- Trước khi tạo rebar, command bind idempotent các instance parameter bắt buộc vào category Rebar:
  - `REBAR_TYPE`
  - `SCHEDULE_REBAR_GEOMETRY_SHAPE`
  - `SEGMENT_A/B/C/D/E/F/G/H/J/K/O/R`
- Nếu cùng tên nhưng sai GUID, là non-shared parameter, type binding, read-only hoặc sai storage type, command báo lỗi và rollback.
- Các thao tác ghi metadata bắt buộc không còn silently no-op.

### 3. Chuẩn hóa `ElementId` cho Revit 2024+

- Các DTO và model mang Revit ID đã chuyển từ `int` sang `long`.
- Thay các đường `int.Parse(element.Id.ToString())` bằng `ElementId.Value`.
- Constructor `ElementId` trong phạm vi feature nhận `long`.
- Bổ sung symbol `R24` và `R25` cho đúng configuration.
- Static scan không còn đường ép Revit Element ID về `int` trong feature; các field `int Id` còn lại là ID enum/index/rule nội bộ, không phải `ElementId`.

### 4. Cache shape theo document

- Cả bảy cache `RebarShape` đều kiểm tra shape thuộc đúng `AC.Document` hiện tại trước khi tái sử dụng.
- Tránh dùng type/shape của document A khi chuyển sang document B trong cùng phiên Revit.

### 5. Atomic transaction và báo lỗi

- Toàn bộ mutation vẫn nằm trong một transaction, nhưng exception từ các nhóm top/bottom/side/main stirrup/secondary stirrup giờ được đẩy lên transaction ngoài.
- Khi một nhóm bắt buộc, schema, assembly metadata, opening replacement hoặc reset host lỗi, transaction rollback và UI hiển thị chuỗi inner exception.
- Ghi schema kiểm tra schema/field/element hợp lệ thay vì bỏ qua.
- Đồng bộ group top/bottom, áp preset, tính section và coordinate không còn nuốt lỗi dữ liệu đầu vào.
- Các `catch` rỗng còn lại chủ yếu nằm ở drawing preview, canvas hoặc helper tùy chọn; chúng không commit model. Đường tạo và ghi model đã được siết chặt.

### 6. Opening và chiều dài hiệu chỉnh

- Đã port `DataRebarLenght.json`: 33 rule, nội dung đối chiếu theo dòng khớp nguồn.
- `GetRebaLengthRealFromData()` đã có lại phân tích segment/bend/hook, chọn rule theo đường kính và round-up 10 mm.
- `SCHEDULE_REBAR_GEOMETRY_SHAPE` được ghi bắt buộc; không phân loại được ghi giá trị `不明` thay vì im lặng.
- Khi copy stirrup để né opening, metadata mới cập nhật lại `Id`, `UniqueId`, `Name`; `HostId` được giữ theo ý nghĩa host segment gốc.
- Không tìm được source group/ray hoặc copy thất bại sẽ rollback thay vì xóa/copy dở dang.

### 7. Diameter setting

- Nếu schema LSTools tồn tại nhưng JSON hỏng, thiếu hoặc có dưới ba đường kính dương khác nhau, command fail rõ ràng.
- Nếu model chưa từng có schema, command dùng fallback xác định từ các `RebarBarType` thực có trong document; fallback cũng yêu cầu ít nhất ba đường kính khác nhau và cả hai nhóm main/stirrup phải có phần tử.
- Đây là adaptation có chủ đích cho LSTools vì command setting RimT gốc không được port đầy đủ. Cần runtime test với bộ D10/D13/D16/... để xác nhận rule phân nhóm đúng dữ liệu dự án thực tế.

### 8. Lỗi Top3 phát hiện thêm trong vòng triển khai

- Helper gom main bar của mã nguồn thêm `Top2` hai lần và bỏ `Top3` tại cả Start/Mid/End.
- Đã sửa thành `Top1 + Top2 + Top3` cho cả ba section.
- Lỗi này có thể làm sai `qRebarsMax` khi lượng thép Top3 lớn hơn Top2; đây là bug kế thừa từ nguồn, không phải chỉ riêng bản port.

## Trạng thái các finding ban đầu

| Finding | Mức | Trạng thái sau triển khai |
|---|---:|---|
| IRBV2-001 — Assembly bị selection filter loại | P0 | Đã sửa; cần smoke test assembly 2/3/nhiều đoạn |
| IRBV2-002 — Thiếu shared parameter/resource | P1 | Đã bổ sung resource, bind idempotent và strict write |
| IRBV2-003 — Revit ID bị ép `int` | P1 | Đã chuyển đường ID của feature sang `long`/`.Value` |
| IRBV2-004 — Static shape cache cross-document | P1 | Đã kiểm tra `shape.Document` cho toàn bộ cache |
| IRBV2-005 — Lỗi con bị nuốt, commit thiếu | P1/P2 | Đã sửa đường mutation chính thành fail atomic |
| IRBV2-006 — Thiếu symbol R24/R25 | P2 | Đã bổ sung `R24`/`R25` trong project configuration |
| IRBV2-007 — Diameter source/fallback | P2 | Đã validate nghiêm; fallback model trắng được giữ có chủ đích |
| IRBV2-008 — Opening length bị giản lược | P2 | Đã port dataset và classifier/correction |
| IRBV2-009 — Middleware thiếu | P3 | Không port; vẫn là dead code không có call site trong nguồn |
| IRBV2-010 — Top2 lặp, Top3 bị bỏ | P1 | Phát hiện thêm và đã sửa ở Start/Mid/End |

## Đối chiếu resource và build

- Output R26 có file shared parameter với 37 definition.
- Output R26 có `DataRebarLenght.json` với 33 rule.
- Dataset đích và nguồn có 0 dòng khác biệt khi đọc UTF-8.
- `git diff --check`: pass; chỉ có thông báo chuẩn hóa LF/CRLF của Git trên Windows.

| Configuration | Kết quả cuối | Warning toàn project | Error |
|---|---:|---:|---:|
| Debug R24 | Pass | 202 | 0 |
| Debug R25 | Pass | 250 | 0 |
| Debug R26 | Pass | 249 | 0 |

Rebuild R26 và lọc riêng đường dẫn `InstallRebarBeamV2` cho kết quả **0 warning, 0 error**. Warning trong bảng là nợ kỹ thuật toàn project, chủ yếu nằm ngoài feature này.

Build được chạy với `/p:DeployRevitAddin=false` để chỉ xác minh compile/output trong workspace, không tự copy add-in vào thư mục Revit của máy.

## Ma trận runtime bắt buộc trước release

1. Chọn trực tiếp assembly có 2, 3 và nhiều beam member; xác nhận rebar phủ đủ mọi đoạn và thứ tự member đúng.
2. Beam ngang, nghiêng, rotated, đảo hướng, khác section và có Fukashi.
3. Đủ 11 nhóm output: top 1-3, bottom 1-3, side, main stirrup, secondary vertical/horizontal.
4. Opening: một lỗ, nhiều lỗ, gần đầu dầm và tại vùng đổi bước stirrup.
5. Project trắng chưa có shared parameter RimT; kiểm tra parameter, GUID, schedule và giá trị segment.
6. Diameter schema đầy đủ, schema hỏng/thiếu và model chưa có schema.
7. Mở đồng thời document A và B, chạy A → B → A.
8. Model có `ElementId` lớn hơn `2,147,483,647`.
9. Ca cố tình thiếu rebar shape/type hoặc parameter xung đột; xác nhận không còn rebar/assembly/schema tạo dở sau rollback.
10. Chạy riêng trên Revit 2024, 2025 và 2026.

## Trạng thái Git

- Các thay đổi triển khai và báo cáo hiện nằm trong working tree của branch `codex/port-install-rebar-beam-v2`.
- Chưa stage, chưa commit và chưa push phần triển khai sau audit trong lượt này.
