# ColumnRebarMainAction — Implementation Plan

## Overview

Class chịu trách nhiệm tạo thép chính (main rebar) cho cột BTCT.  
Nhận vào danh sách `ColumnConcreteModel` (các nhịp cột theo phương đứng), phân tích từng face (Left/Top/Right/Bottom) rồi sinh thép theo hình dạng phù hợp.

---

## Fields

| Field | Mô tả |
|---|---|
| `_document` | Revit Document |
| `_host` | Element chứa rebar |
| `_settingRebarColumnModel` | Cài đặt chung (E0, …) |
| `_rebarLapLengthModels` | Bảng chiều dài nối (Lst, Gap theo đường kính) |
| `_rebarAnchorageHookMainBarModels` | Bảng hook (B = chiều dài hook 90°) |
| `_rebarAnchorageLengthModels` | Bảng neo (Ldt × d) |
| `_rebarBarTypes` | Danh sách RebarBarType từ Document (lọc theo tên "D") |
| `_columnConcreteModels` | Toàn bộ danh sách cột (dùng tra cứu HostId, neo móng) |

---

## Entry Point — `CreateRebarMain`

### Mục đích
Điểm khởi đầu công khai. Nhận `List<ColumnConcreteModel>` (đã sắp xếp Z tăng dần).

### Các bước
1. **Gán thông số face** cho từng `ColumnConcreteModel`:
   - `FaceLeft / FaceRight`: `RebarQty = SpacingDY`, đường kính DY, cover = cover + ØDY/2 + ØST
   - `FaceTop / FaceBottom`: `RebarQty = SpacingDX`, đường kính DX, cover = cover + ØDX/2 + ØST
   - Gán `CoverBase`, `HeightBeamZone` cho tất cả face
2. **Tập hợp face lists** theo từng phương: `facess = [faceLefts, faceTops, faceRights, faceBots]`
3. **Gọi `InstallRebarFace`** cho 4 phương (Left/Right bỏ thanh đầu-cuối để tránh trùng góc).
4. **Lưu kết quả** `RebarMainPositionss` vào từng `ColumnConcreteModel`.

---

## Dispatcher — `InstallRebarFace`

```
faces.Count == 1  →  InstallRebarFace_Column_Single
faces.Count >= 2  →  InstallRebarFace_Column_Multi
```

---

## `InstallRebarFace_Column_Single`

### Mục đích
Tạo thép cho cột **1 nhịp**.

### Các bước
1. Lấy `face = faces[0]`
2. Tính `diameterMm`, `cover`
3. Tra bảng **minHookFt** (field `B`) theo đường kính gần nhất
4. Tính **anchorFoundationFt** (chỉ cột thấp nhất): gọi `GetAnchorFoundationFt`
5. Tính **hệ trục**: `vtX = Pb2-Pb1`, `vtY = -Normal`, `vtZ = BasisZ`
6. Tính **sp / ep** (điểm đầu/cuối phân bố thép)
7. Gọi `SolvePositionInstallRebar`
8. Với mỗi thanh:
   - `rbStart = (posXY, Pb1.Z)`, `rbEnd = (posXY, Pt1.Z - CoverBase)`
   - **isBottomColumn = true**: 5 điểm `[hook_out, pAnchor, Pb1, Pt1-cB, hook_in]`
   - **isBottomColumn = false**: 3 điểm `[Pb1, Pt1-cB, hook_in]`

---

## `InstallRebarFace_Column_Multi`

Dispatcher cho nhiều nhịp:

```
InstallRebarFace_Column_Base   →  cột index 0
InstallRebarFace_Column_Mid    →  cột index 1 .. fCount-2
InstallRebarFace_Column_Roof   →  cột index fCount-1
```

---

## `InstallRebarFace_Column_Base`

### Mục đích
Cột dưới cùng trong chuỗi nhiều nhịp.

### Điều kiện `isLapDiff`
Kiểm tra trên **toàn bộ 4 face** (`facess`). True nếu:
- Bất kỳ face: khoảng cách chiếu `Pt1(base)` → mặt phẳng cột trên ≥ E0
- Hoặc: `|diameter_base - diameter_next| > 0.5mm`

### Phần đáy (neo móng)
Giống Single: `pAnchorBot = rbStart - vtZ × anchorFt`, hook ra ngoài `-vtY × minHook`

### Phần đỉnh — Case A (`isLapDiff = true`)
```
[hook_out, pAnchor, Pb1, Pt1-coverBase, hook_in]
```

### Phần đỉnh — Case B (`isLapDiff = false`)
1. Tính **sole**: `isSole`, `lapGap = lapN + (isSole ? lapN + gapN : 0)`
2. Tìm `posNext` (matching theo `Index`)
3. **Có matching**:
   - `p2 = (posXY, Pt1 - beamZone)` — bẻ tại BeamZone
   - `p3 = (posNXY, Pb1N + coverNB)` — vào cột trên sát đai
   - `p4 = (posNXY, Pb1N + coverNB + lapGap)` — kết thúc nối
4. **Không matching** (cột dưới nhiều thép hơn):
   - Tính `deltaXY` từ thanh kề gần nhất có matching
   - `tgt = posXY + deltaXY`; shape tương tự với `tgt`

---

## `InstallRebarFace_Column_Roof`

### Mục đích
Cột trên cùng trong chuỗi nhiều nhịp.

### `isLapDiff`
So sánh `faces[fCount-2]` vs `faces[fCount-1]`.

### Phần đỉnh (luôn)
```
rbTop = (posXY, Pt1.Z - coverBase)
hookTop = rbTop + vtY × minHook
```

### Phần đáy — Case A (`isLapDiff = true`)
```
[Pb1-anchor, Pb1, Pt1-cB, hook_in]
```

### Phần đáy — Case B (`isLapDiff = false`)
- `isSolePrev = true`  → `soleOff = lapPrev + gapPrev`
- `isSolePrev = false` → `soleOff = 0`
- `rbBot = (posXY, Pb1 + coverBase + soleOff)`
- `shape = [rbBot, Pt1-cB, hook_in]`

---

## `InstallRebarFace_Column_Mid`

### Mục đích
Tất cả cột trung gian (`fi = 1..fCount-2`).

### Kiểm tra kép
- `isLapDiffTop`: `faces[fi]` vs `faces[fi+1]`
- `isLapDiffBot`: `faces[fi-1]` vs `faces[fi]`

### Phần đáy

| Điều kiện | Shape đáy |
|---|---|
| `isLapDiffBot = true` | `[Pb1-anchor, Pb1, …]` |
| `isLapDiffBot = false` | `[Pb1 + coverBase + soleOff, …]` |

soleOff: `isSolePrev=true` → `lapPrev+gapPrev`; `false` → `0`

### Phần đỉnh

| Điều kiện | Shape đỉnh |
|---|---|
| `isLapDiffTop = true` | `[…, Pt1-coverBase, hook_in]` |
| `isLapDiffTop = false` | `[…, BeamZoneBend, p3, p4=p3+lapGap]` |

`tgtXY`: dùng `posNext.Position` nếu có matching; nếu không thì `posXY + deltaXY` (từ thanh kề gần nhất).

**Sole đỉnh**: `isSole=true → lapGap = 2*lapN + gapN`; `false → lapGap = lapN`

---

## Shape Summary

```
Single — Bottom:  hook_out → pAnchor → Pb1 → Pt1-cB → hook_in
Single — Other:   Pb1 → Pt1-cB → hook_in

Base — Case A:    hook_out → pAnchor → Pb1 → Pt1-cB → hook_in
Base — Case B:    hook_out → pAnchor → Pb1 → BeamZone → p3 → p4

Roof — Case A:    Pb1-anchor → Pb1 → Pt1-cB → hook_in
Roof — Case B:    Pb1+cB+soleOff → Pt1-cB → hook_in

Mid AA:  Pb1-anchor → Pb1 → Pt1-cB → hook_in
Mid AB:  Pb1-anchor → Pb1 → BeamZone → p3 → p4
Mid BA:  Pb1+cB+soleOff → Pt1-cB → hook_in
Mid BB:  Pb1+cB+soleOff → BeamZone → p3 → p4
```

---

## Helpers

### `CheckPositionSole(faceType, face)`

| Face | Logic |
|---|---|
| Left | Luôn `true` |
| Bottom | `RebarQtyNext % 2 != 0` |
| Right | Phụ thuộc `isOddBot` và `RebarQtyNext` |
| Top | Phụ thuộc `isOddBot`, `isOddRight`, `RebarQtyNext` |

### `SolvePositionInstallRebar(start, end, qty, maxQty, hostFace)`

Phân bố đối xứng, spacing = `distance / (maxQty-1)`:
- Nửa trái `(i=0..haft-1)`: `start + i*spacing*vt`, Index = `i+1`
- Trung tâm (qty lẻ): `midpoint(start,end)`, Index = `1 + maxQty/2`
- Nửa phải `(i=0..haft-1)`: `end - i*spacing*vt`, Index = `maxQty-i`

Kết quả: `OrderBy(Index)`.
