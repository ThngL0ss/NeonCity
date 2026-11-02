# NEON BREAKER — Game 2D platform pixel-art neon (Unity)

Game 2D platformer đồ họa **pixel art neon tương lai**. Scene, Prefab, Sprite, Audio là file thật để mở và chỉnh trong Unity. **Mọi thông số của người chơi, quái, boss, trụ đều nằm trên component (Inspector)** — không cần sửa code để cân bằng.

> **Lưu ý trung thực:** code và công cụ Editor được viết mà **chưa từng được biên dịch hay chạy trong Unity** ở môi trường tạo ra chúng. Có thể còn lỗi biên dịch nhỏ hoặc lệch cân bằng. Nếu gặp lỗi, copy nguyên dòng đỏ trong Console gửi lại để sửa.

## 1. Cài đặt / cập nhật

- Mở project bằng Unity **2021.3 trở lên** (kể cả Unity 6), 2D (Built-in hoặc URP).
- Lần đầu: menu **Tools ▸ Neon Breaker ▸ Build Project (tạo Prefab + Scene)**.
- **Đã có project cũ và vừa kéo bản mới về:** chỉ cần chạy **Tools ▸ Neon Breaker ▸ Thêm quái Heavy + Rusher (không đụng Scene)** một lần. Menu này:
  - tạo Prefab `Heavy` và `Rusher` nếu chưa có (không ghi đè nếu đã có),
  - điền sẵn **Spawn Table** cho Prefab `Tower` nếu bảng đang trống,
  - sửa hộp va chạm của `Walker`/`Flyer` nếu còn là giá trị mặc định cũ,
  - **không đụng tới Scene** của bạn.
- Ảnh PNG tự được cài đặt pixel art (Point filter, không nén, PPU, pivot) nhờ `Assets/Editor/NeonArtImporter.cs`.

## 2. Điều khiển

| Phím | Hành động |
|---|---|
| A / D | Di chuyển |
| Space | Nhảy (giữ để nhảy cao); S + Space: xuyên xuống bục |
| J | Chém cận chiến (tầm ngắn — vừa đánh vừa lùi) |
| I | Lướt (bất tử trong lúc lướt) |
| K | Ném phi tiêu |
| L | Hồi máu (bắt đầu 3 lượt, tối đa 5; nhặt vật phẩm xanh để cộng dồn) |
| **P** | **Chế độ thử nghiệm (bật/tắt)**: không thể chết · sát thương ×3 · không hồi chiêu · hồi máu không tốn lượt |
| Esc | Tạm dừng · Enter: bắt đầu / tiếp tục · R: chơi lại · V: đổi ngôn ngữ |

Hệ số sát thương của chế độ thử nghiệm chỉnh ở `Systems ▸ GameManager ▸ Cheat Damage Mul` (mặc định 3). Khi bật, trên màn hình có nhãn vàng "[P] CHẾ ĐỘ THỬ NGHIỆM".

## 3. Luồng chơi (~10 phút)

Phá hết **các trụ triệu hồi** (mỗi trụ sinh quái khi bạn lại gần) → cổng khu boss mở → vào khu boss → boss xuất hiện.
Boss: **Phase 1** chậm, máu trâu, 1 kỹ năng (nhảy đập + sóng xung kích). **Phase 2** nhanh, máu ít hơn, 3 kỹ năng (chớp bước chém, chùm đạn quạt, laser giáng từ trời) và **hút máu**.
Quái nhỏ rơi vật phẩm hồi máu (mặc định 20%) — nhặt = +1 lượt hồi.

### Các loại quái

| Quái | Prefab | Máu | Sát thương | Tốc độ | Ghi chú |
|---|---|---|---|---|---|
| Walker | `Walker` | 48 | 13 (lao) | 3.7 | Quái thường: đuổi → báo hiệu → lao |
| Flyer | `Flyer` | 30 | 9 (đạn) | 5 | Bay lơ lửng, bắn đạn |
| **Heavy** (mới) | `Heavy` | 220 | 8 (lao) | 2.0 | "Máu trâu": to, giáp nặng, gần như không bị đẩy lùi, báo hiệu đòn lâu (0.75s) |
| **Rusher** (mới) | `Rusher` | 18 | 24 (lao) | 7.0 | "Dao găm": nhỏ, lao rất nhanh (tốc độ lao 21), báo hiệu ngắn (0.4s) |

Heavy và Rusher dùng chung script `Walker` nhưng khác chỉ số (xem từng Prefab). Các con số trên là mặc định lúc tạo Prefab; chỉnh trực tiếp trên Prefab.

## 4. Chỉnh thông số ngay trong Inspector

Mở Prefab (nhấp đúp trong `Assets/NeonBreaker/Prefabs/`) hoặc chọn object trong Scene, rồi chỉnh component. Mỗi ô có Tooltip giải thích (rê chuột vào).

| Đối tượng | Component | Gồm những gì |
|---|---|---|
| Người chơi | `Player` | máu, thời gian bất tử khi bị đánh, số lượt hồi, tốc độ, nhảy, trọng lực, coyote/jump buffer, chém (sát thương/tầm/hồi chiêu/đẩy lùi…), lướt, phi tiêu, hồi máu |
| Quái đi bộ | `Walker` (Prefab Walker/Heavy/Rusher) | máu, tốc độ, tầm phát hiện, thời gian báo hiệu/lao/hồi, sát thương lao và chạm, lực đẩy, kích thước hộp trúng đòn, màu hiệu ứng, độ cao xuất hiện |
| Quái bay | `Flyer` | máu, tốc độ, sát thương và tốc độ đạn, chu kỳ bắn, vị trí lơ lửng |
| Boss | `Boss` | máu 2 phase, tốc độ, sát thương từng chiêu, hút máu, ngưỡng cuồng nộ, thông số sóng/laser/quạt đạn/chớp bước |
| Trụ | `Tower` | máu, tầm kích hoạt, **bảng triệu hồi** (mục 5), sức mạnh quái |
| Vật phẩm hồi máu | `Pickup` | thời gian tồn tại, tầm hút, lượng hồi trực tiếp |
| Màn chơi | `Level` | số quái tối đa trên màn (`Max Enemies Alive`, mặc định 8), tỉ lệ rơi vật phẩm (`Drop Chance`) |
| Hệ thống | `GameManager` | hệ số sát thương test mode, ngôn ngữ khởi đầu |

Prefab là nguồn của chỉ số: sửa Prefab thì mọi bản sao trong Scene cùng đổi (trừ ô nào bạn đã ghi đè riêng trên bản sao — chữ đậm).

> Bản trước dùng file `GameConfig` (ScriptableObject) để chỉnh cân bằng — **đã bỏ**. Chỉ số giờ nằm trực tiếp trên Prefab.

## 5. Hệ thống trụ triệu hồi (`Tower`)

| Ô | Ý nghĩa |
|---|---|
| `Spawn Table` | Danh sách quái trụ này được phép triệu hồi. Mỗi dòng: **Prefab** (kéo Walker/Flyer/Heavy/Rusher hoặc prefab riêng), **Weight** (trọng số — càng cao càng hay ra; 0 = không ra), **Power Mul** (nhân thêm sức mạnh riêng cho loại này), **Min Power** (chỉ ra khi sức mạnh của trụ ≥ giá trị này) |
| `Max Alive` | Số quái do **trụ này** được sống cùng lúc |
| `Total Spawn Limit` | Tổng số quái trụ này sinh ra cả ván (0 = vô hạn) |
| `Spawn Interval` | Số giây giữa 2 lần triệu hồi |
| `Interval Jitter` | Dao động ngẫu nhiên của khoảng thời gian (0.12 = ±12%) |
| `First Spawn Delay` | Chờ bao lâu từ lúc bạn lại gần đến lần triệu hồi đầu |
| `Spawn Distance` | Quái xuất hiện cách trụ từ min đến max đơn vị |
| `Power Speeds Up Spawn` | Bật = trụ mạnh hơn thì triệu hồi nhanh hơn |
| `Auto By Order`, `Power First`, `Power Last`, `Power` | Quái mạnh dần theo thứ tự trụ (xem dưới) |

Ví dụ:
- Trụ chỉ ra quái nhanh: để `Spawn Table` còn đúng 1 dòng `Rusher`.
- Trụ chỉ ra quái trâu, thưa: 1 dòng `Heavy`, `Spawn Interval` = 9, `Max Alive` = 2.
- Trụ cuối gần boss ra đủ loại: để cả 4 dòng; `Min Power` làm Heavy/Rusher chỉ xuất hiện ở các trụ đã đủ mạnh.

Giới hạn toàn màn hình là `Level ▸ Max Enemies Alive`: dù trụ cho phép, tổng quái trên màn không vượt con số này.
Nếu `Spawn Table` rỗng, trụ tự dùng Walker + Flyer như bản cũ.

**Quái mạnh dần theo trụ (gần boss càng khỏe):** chọn từng trụ trong `Level/Towers`, bật `Auto By Order` rồi đặt `Power First` (trụ đầu) và `Power Last` (trụ cuối). Hoặc tắt Auto và gõ `Power` riêng. Power 1 = chỉ số gốc; 1.5 = máu ×1.5, sát thương ×1.3, nhanh hơn ~15%, quái ngả đỏ dần.

## 6. Cấu trúc thư mục

```
Assets/
├─ Editor/                    NeonArtImporter (import PNG), NeonBuilder (menu Tools ▸ Neon Breaker)
├─ Shaders/                   NeonAdditive (phát sáng), NeonFlash (chớp trắng khi trúng đòn)
├─ NeonBreaker/
│  ├─ Art/                    PNG pixel art + manifest.json (PPU, pivot, tile)
│  ├─ Audio/  Materials/  Prefabs/  Scenes/
├─ Resources/NeonBreaker/     ArtBank, AudioBank
└─ Scripts/
   ├─ Core/                   Cfg, GameManager, GameInput, PixelScreen, SpriteAnimator, Fx, Sfx, ...
   ├─ Gameplay/               Player, Enemy, Walker, Flyer, Boss, Tower, Projectile, Pickup, Level, ...
   └─ UI/Hud.cs               HUD / menu / màn thắng-thua (IMGUI)
Tools/PixelArt/gen_art.py       Sinh toàn bộ PNG (tuỳ chọn)
Tools/PixelArt/gen_enemies2.py  Sinh sprite Heavy + Rusher, CẬP NHẬT manifest.json không ghi đè
```

`Cfg.cs` chỉ còn giữ: ngôn ngữ, layer, mốc chiều dài màn, bảng màu. Mốc cổng/boss/bàn thờ chỉnh bằng object `Level` trong Scene.

## 7. Chỉnh sửa khác trong Unity

| Muốn… | Làm thế này |
|---|---|
| Dời / thêm / xoá **trụ** | Kéo prefab `Tower` vào `Level/Towers` (hoặc xoá/di chuyển). Số trụ tự đếm lúc Play. |
| Thêm / sửa **bục** | Duplicate một object trong `Level/Platforms`; đổi `Size` của SpriteRenderer và BoxCollider2D cho khớp. |
| Đổi chiều dài màn, vị trí cổng, chỗ boss | Chọn `Level`: kéo `LimitLeft`, `LimitRight`, `Gate`, `BossSpawn` (có Gizmos). |
| Đổi **hình** | Thay PNG trong `Art/` (giữ tên, kích thước), hoặc trong Prefab chọn `Visual` → **Sprite Animator ▸ Clips**. Tên clip phải giữ nguyên. |
| Đổi **âm thanh / nhạc** | `Resources/NeonBreaker/AudioBank` → kéo AudioClip vào Id tương ứng. |
| Thêm **loại quái mới** | Duplicate Prefab `Heavy` hoặc `Rusher`, đổi chỉ số và sprite, kéo vào `Spawn Table` của trụ. |
| Tắt hiệu ứng pixel | `Main Camera ▸ Pixel Screen ▸ Enable Pixel Render` = tắt. |

Sprite pixel art: 16 pixel = 1 đơn vị thế giới, màn hình tham chiếu 480×270.

## 8. Xử lý sự cố

- **Sprite hồng / không hiện:** chạy *Tools ▸ Neon Breaker ▸ Chỉ tạo lại Prefab* (gán lại material); kiểm tra Console xem shader có lỗi không.
- **Quái đứng yên, không đánh được / "The referenced script ... is missing":** mỗi class MonoBehaviour phải nằm trong file trùng tên (Walker.cs, Flyer.cs...). Sau khi code biên dịch xong chạy *Chỉ tạo lại Prefab*.
- **Heavy/Rusher không xuất hiện:** chạy menu *Thêm quái Heavy + Rusher*; kiểm tra Prefab `Tower` → `Spawn Table` có đủ dòng; nhớ `Min Power` của Heavy (1.3) và Rusher (1.1) — trụ có `Power` thấp hơn sẽ không ra loại đó.
- **Heavy/Rusher hiện ô vuông/không có hình:** Unity chưa import xong PNG mới trong `Art/` — bấm Assets ▸ Refresh rồi chạy lại menu.
- **Màn hình đen khi Play lần 2:** đã xử lý bằng `Bootstrap` (reset static, hỗ trợ tắt Reload Domain).
- **Báo "Scene không có GameManager":** mở `Main.unity`.
- Các menu tạo lại: *Chỉ tạo lại Prefab / Chỉ tạo lại Scene Main / Xuất lại âm thanh WAV*. **Cảnh báo:** các menu này ghi đè bản Prefab/Scene bạn đã chỉnh.
