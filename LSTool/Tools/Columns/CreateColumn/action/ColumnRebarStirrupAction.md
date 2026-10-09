# ColumnRebarStirrupAction — Implementation Plan

## Overview

Class tạo toàn bộ thép đai (stirrups + ties) cho cột BTCT.  
Có 2 loại đai cần tạo:

| Loại | Method | Mục đích |
|---|---|---|
| **Main stirrup** | `CreateStirrupMain` → `InstallStirrupMain` | Đai chính hình chữ nhật bao quanh toàn bộ tiết diện |
| **Sub stirrup (tie)** | `CreateStirrupSub` → `InstallStirrupSub` | Đai phụ / móc liên kết nối các thanh thép chính bên trong |

Ngoài ra có 2 method lưu/đọc cấu hình vị trí đai phụ qua Schema.

---

## Fields

| Field | Kiểu | Mô tả |
|---|---|---|
| `_uidocument` | `UIDocument` | Revit session |
| `_document` | `Document` | Revit Document |
| `_host` | `Element` | Element chứa rebar |
| `_settingRebarColumnModel` | `SettingColumnModel` | Cài đặt: `StressZone` (tỷ lệ vùng đầu cột), `E0` |
| `_rebarBarTypes` | `List<RebarBarType>` | Danh sách loại thép (lọc tên "D") |
| `_columnStirrupPositionSchema` | `ColumnStirrupPositionSchema` | Schema lưu vị trí đai phụ vào ExtensibleStorage |

---

## Data từ `ColumnConcreteModel`

| Property | Dùng cho |
|---|---|
| `DiameterST` | Đường kính đai |
| `Cover` | Lớp bảo vệ |
| `SpacingST` | Khoảng cách đai vùng giữa (mm) |
| `SpacingSTE` | Khoảng cách đai vùng đầu/cuối cột (mm) |
| `HeightBeamZone` | Chiều cao vùng beam zone (cắt bỏ khi cột giữa) |
| `Center`, `VTZ`, `Length` | Tâm, vector chiều cao, chiều dài cột |
| `VTX` | Vector ngang (dùng trong sắp xếp điểm đai phụ) |
| `FaceLeft/Top/Right/Bottom` | Góc mặt cột (Pb1, Pb2) để dựng shape đai |
| `Ties` | `List<List<ColumnStirrupPositionModel>>` — cấu hình đai phụ |
| `RebarMainPositionss` | Vị trí tâm thép chính (dùng dựng tọa độ đai phụ) |

---

## Schema — `ColumnStirrupPositionSchema`

Lưu `col.Ties` dưới dạng JSON vào `ExtensibleStorage` của Element.

| Method | Logic |
|---|---|
| `SaveSettingColumnStirrupPosition` | `JsonConvert.SerializeObject(col.Ties)` → `schema.Write(ele, content)` |
| `GetSettingColumnStirrupPosition` | `schema.Read(ele)` → `JsonConvert.DeserializeObject<List<List<...>>>(content)` → `col.Ties` |

---

## Zone Logic (Vùng đai)

Mỗi cột được chia thành **3 vùng theo chiều cao** dựa trên `StressZone` (tỷ lệ 0→1):

```
start  = Center - VTZ × Length/2           (đáy cột)
end    = Center + VTZ × Length/2
       - VTZ × (hasBeamZone ? HeightBeamZone : 0)  (cột giữa: trừ beam zone phía trên)

length = start.DistanceTo(end)

Zone 1 (đầu dưới):  [start            ,  start + VTZ × length × StressZone]
Zone 2 (giữa):      [start + VTZ×l×SZ ,  end   - VTZ × length × StressZone]
Zone 3 (đầu trên):  [end   - VTZ×l×SZ ,  end                              ]
```

**`hasBeamZone`**:
- `index > 0 && index < qty - 1` → `true` (cột giữa, cột trên của multi-span)
- `index == 0` hoặc `index == qty - 1` → `false`

**Khoảng cách đai**:

| Vùng | Spacing | Offset đầu | Offset cuối |
|---|---|---|---|
| Zone 1 (stress) | `SpacingSTE` | 50mm | `SpacingSTE/2` |
| Zone 2 (mid) | `SpacingST` | `SpacingST/2` | `SpacingST/2` |
| Zone 3 (stress) | `SpacingSTE` | 50mm | `SpacingSTE/2` |

---

## `InstallStirrupMain` — Đai chính hình chữ nhật

### Các bước

**1. Tính cover**
```
cover = (Cover + diameterST/2).FromMillimeters()
```

**2. Dựng `baseShape` (4 góc tiết diện đai, tại Z = start)**
```
ps = [FaceLeft.Pb1, FaceTop.Pb1, FaceRight.Pb1, FaceBottom.Pb1]
baseShapes = CurveLoop.CreateViaOffset(ps.PointsToCurveLoop(), cover, -VTZ)
           → .Select(x => x.GetEndPoint(1))    // 4 điểm đã offset vào trong
```

**3. Điều chỉnh điểm bắt đầu/kết thúc đai (hook overlap)**
```
p1 = baseShapes[0]   (góc TL)
p2 = baseShapes[1]   (góc BL)
p3 = baseShapes[2]   (góc BR)
p4 = baseShapes[3]   (góc TR)

vtStart = (p2 - p1).Normalize()
vtEnd   = (p1 - p4).Normalize()

baseShapes = [
    p1 + vtEnd   × dST/2,   ← điểm kết thúc (overlap với điểm bắt đầu)
    p4,
    p3,
    p2,
    p1 - vtStart × dST/2,   ← điểm bắt đầu
]
```

**4. Sinh danh sách shapes theo 3 vùng**
```
_installStirrup(zone1_start, zone1_end, baseShapes, SpacingSTE, 50, SpacingSTE/2)
_installStirrup(zone2_start, zone2_end, baseShapes, SpacingST,  ST/2, ST/2)
_installStirrup(zone3_start, zone3_end, baseShapes, SpacingSTE, 50, SpacingSTE/2)
```

**5. Tạo rebar**
```
hook135 = RebarHookType với HookAngle ≈ 135°
hookLengthMm = Max(dST × 10, 100)
foreach shape → RebarHelper.CreateRebarStirrupTie(doc, shape, DiameterST, VTZ, hook135, hook135, ...)
```

---

## `InstallStirrupSub` — Đai phụ / móc liên kết (tie)

Đai phụ được định nghĩa bởi `col.Ties` — mỗi phần tử là danh sách các `ColumnStirrupPositionModel {Index, Face}` xác định thanh thép chính cần nối.

### Các bước

**1. Lấy vị trí tâm thép chính**
```
rebarPos = col.RebarMainPositionss  (flatten)
         | GetRebarPositions(col)   (fallback: tính lại từ geometry)
```

**2. Với mỗi `tie` trong `col.Ties`**
- Tìm các `ColumnRebarPositionModel` tương ứng (`Index + Face` match)
- Xác định `posTargets.Count`:

| Count | Xử lý |
|---|---|
| < 2 | Skip |
| == 2 | **Tie thẳng** (U-shape / móc liên kết 2 điểm) |
| > 2 | **Đai đa giác** (3, 4, 5, 6... đỉnh) |

**3a. Tie thẳng (2 điểm)**
```
vt       = (ps[1] - ps[0]).Normalize()
nor      = vt × VTZ (pháp tuyến)
extend   = dST + dMain/2
p1_shape = ps[0] - vt×extend + nor×(dMain+dST)/2
p2_shape = ps[1] + vt×extend + nor×(dMain+dST)/2
```
Hook orientation (Left/Right) xác định theo hướng `vt × VTZ` vs hướng vào trong lõi.

**3b. Đai đa giác (> 2 điểm)**
1. Chiếu tất cả điểm xuống mặt phẳng `Plane(VTZ, Center)` → `pts`
2. Tính trọng tâm `cMid`
3. Sắp xếp `pts` theo thứ tự **ngược chiều kim đồng hồ** (dùng `Atan2` trên hệ `VTX/VTY`)
4. Tính `edgeVecs[i]` và `edgeNormals[i]` (pháp tuyến hướng ra ngoài) cho từng cạnh
5. Tính **điểm giao nhau** của 2 đường thẳng offset của 2 cạnh liền kề → đỉnh đai đa giác
6. Dựng `baseShapes` có overlap start/end giống đai chính:
   ```
   pStart - vtStart×dST ... ps[1..n-1] ... pStart + vtEnd×dST
   ```

**4. Sinh vị trí theo 3 vùng và tạo rebar (giống Main)**

---

## `_installStirrup` — Helper phân bổ đai theo khoảng cách

```
Input:  start, end (XYZ), baseShapes (list điểm), spacingMm, extendS, extendE
Output: List<List<Curve>>  (mỗi phần tử = 1 đai tại 1 vị trí Z)
```

### Logic
```
vt       = (end - start).Normalize()
distance = start.DistanceTo(end).ToMm() - (extendS + extendE)
qty      = 1 + floor(distance / spacingMm)
duSpacing = distance % spacingMm

baseS = start + vt × extendS.ToFt()
// Chiếu baseShapes lên mặt phẳng tại baseS
baseShapes = baseShapes.Select(x => x.RayIntersectPlane(vt, baseS))

for i = 0 .. qty-1:
    shapes_i = baseShapes + i × vt × spacingMm.ToFt()
    result.Add(shapes_i.PointsToCurves())
    if i == qty-1 && duSpacing >= 0.3 × spacingMm:
        result.Add((shapes_i + vt × duSpacing.ToFt()).PointsToCurves())  // đai bù dư
```

---

## `GetRebarPositions` — Fallback khi `RebarMainPositionss` chưa có

Tính lại vị trí tâm thép chính cho 4 face từ geometry thuần túy:

```
For each face (Left/Top/Right/Bottom):
    cover = (Cover + dST + dMain/2).FromMm()
    sp = face.Pb2 + vtY×cover + vtX×cover   (góc bắt đầu, offset vào trong)
    ep = face.Pb1 + vtY×cover - vtX×cover   (góc kết thúc)
    SolvePositionInstallRebar(sp, ep, qty, qty, face)
```

---

## Flow tổng thể

```
CreateStirrupMain(ccRInfos)
  │
  └─ foreach col:
       InstallStirrupMain(col, hasBeamZone)
         ├─ Tính cover, start/end, 3 zones
         ├─ Dựng baseShapes (4 góc offset)
         ├─ _installStirrup × 3 vùng
         └─ foreach shape → CreateRebarStirrupTie(hook135×2)

CreateStirrupSub(ccRInfos)
  │
  └─ foreach col:
       InstallStirrupSub(col, hasBeamZone)
         ├─ Lấy rebarPos (từ RebarMainPositionss hoặc GetRebarPositions)
         ├─ foreach tie in col.Ties:
         │    ├─ Resolve posTargets (Index+Face match)
         │    ├─ count==2  → Tie thẳng (2 điểm)
         │    └─ count>2   → Đai đa giác (sort + offset + intersect)
         └─ _InstallSub(shape) → _installStirrup × 3 vùng → CreateRebarStirrupTie

SaveSettingColumnStirrupPosition(ccRInfos)
  └─ foreach col: schema.Write(ele, JSON(col.Ties))

GetSettingColumnStirrupPosition(cols)
  └─ foreach col: col.Ties = schema.Read(ele) → deserialize JSON
```

---

## Shape Summary

```
Main stirrup:
  [p1+vtE×r, p4, p3, p2, p1-vtS×r]   ← hình chữ nhật, hook 135° tại 2 đầu

Sub tie thẳng (2 điểm):
  [ps[0]-vt×ext+nor×off, ps[1]+vt×ext+nor×off]  ← U-bar / móc liên kết

Sub đai đa giác (n>2):
  [p_start-vtS×dST, vertex_1, ..., vertex_n-1, p_start+vtE×dST]
```

---

## Ghi chú kỹ thuật

- **`CurveLoop.CreateViaOffset`**: offset tiết diện vào trong theo `-VTZ` để tính 4 góc đai.
- **Hook135**: tra `RebarHookType` với `HookAngle ≈ 135°` → fallback tên chứa "135" → fallback đầu tiên.
- **`hookLengthMm = Max(dST × 10, 100)`**: chiều dài đuôi hook tối thiểu.
- **`duSpacing >= 0.3 × spacingMm`**: chỉ thêm đai bù dư khi khoảng dư > 30% spacing (tránh sinh đai quá gần).
- **`SubTransaction`**: cả `CreateStirrupMain` và `CreateStirrupSub` đều bọc trong `SubTransaction` độc lập.
- **Hook orientation** (Left/Right): xác định bằng `DotProduct(rightVec, vInward)` để đuôi hook luôn quay vào lõi.
