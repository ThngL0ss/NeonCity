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
- **Bản có bẫy / kho đồ / rương / xu / map dài** (nhánh `claude/map-bay-kho-do`): sau khi kéo về, đợi Unity biên dịch xong rồi chạy lần lượt
  1. **Tools ▸ Neon Breaker ▸ Thêm bẫy + kho đồ + rương (không đụng Scene)** — tạo Prefab `TrapSpike/TrapLaser/TrapSaw/TrapTurret/TrapMine`, `Coin`, `Chest`, 17 file vật phẩm trong `Assets/NeonBreaker/Items/`, gán Coin vào `ArtBank`. Không ghi đè gì đã có.
  2. **Tools ▸ Neon Breaker ▸ Mở rộng map + đặt bẫy + rương vào Scene Main** — chỉnh Scene hiện có: kéo dài map, giãn 6 trụ, thêm bục, đặt bẫy (`Level/Traps`) và rương (`Level/Chests`). **Không tạo lại Scene**, các chỉnh sửa khác của bạn được giữ. Nên commit git trước khi chạy. Chạy lại lần 2 chỉ đặt lại bẫy + rương (có hỏi xác nhận).
  Rồi commit các file `.meta`, Prefab, Item, Scene mà Unity vừa tạo.
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
| **B** | **Mở / đóng kho đồ** (game tạm dừng khi mở) |
| **T** | **Mở rương** gần nhất (đứng trong tầm, hiện gợi ý "[T] MỞ RƯƠNG") |
| **P** | **Chế độ thử nghiệm (bật/tắt)**: không thể chết · sát thương ×3 · không hồi chiêu · hồi máu không tốn lượt |
| Esc | Tạm dừng · Enter: bắt đầu / tiếp tục · R: chơi lại · V: đổi ngôn ngữ |

Trong kho đồ: **A D W S / mũi tên** chọn ô · **Enter** dùng · **X** vứt 1 · **chuột**: bấm chọn, bấm đúp = dùng · **B / Esc** đóng.

Hệ số sát thương của chế độ thử nghiệm chỉnh ở `Systems ▸ GameManager ▸ Cheat Damage Mul` (mặc định 3). Khi bật, trên màn hình có nhãn vàng "[P] CHẾ ĐỘ THỬ NGHIỆM".

## 3. Luồng chơi (~10 phút)

Phá hết **các trụ triệu hồi** (mỗi trụ sinh quái khi bạn lại gần) → cổng khu boss mở → vào khu boss → boss xuất hiện.
Boss: **Phase 1** chậm, máu trâu, 1 kỹ năng (nhảy đập + sóng xung kích). **Phase 2** nhanh, máu ít hơn, 3 kỹ năng (chớp bước chém, chùm đạn quạt, laser giáng từ trời) và **hút máu**.
Quái nhỏ rơi vật phẩm hồi máu (mặc định 20%) — nhặt = +1 lượt hồi. Quái bị hạ còn **rơi xu** (mục 9).
Map dài ~260 đơn vị: 6 trụ cách nhau ~43 đơn vị, bục 3 tầng, **5 loại bẫy** (mục 8), rương rải rác (mục 9).

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

## 6. Bẫy (5 loại)

Mọi bẫy kế thừa `Trap` (chung: `Damage`, `Knockback`, `Active Range` — chỉ chạy khi người chơi ở gần). Prefab ở `Assets/NeonBreaker/Prefabs/`, bản đặt sẵn nằm ở `Level/Traps`. Chỉ cần kéo Prefab vào Scene để thêm; mọi số chỉnh trên Inspector.

| Bẫy | Prefab | Cách hoạt động | Ô chỉnh chính |
|---|---|---|---|
| **Gai** | `TrapSpike` | Gai thụt → nhấp nháy báo hiệu → bật lên gây sát thương → thụt xuống (theo chu kỳ) | `Down Time`, `Warn Time`, `Up Time`, `Start Offset` (lệch pha), `Always Up` |
| **Laser** | `TrapLaser` | Hai trụ phát tia; tắt → báo hiệu → bật. Dọc (cổng laser, lướt `I` qua khi tia tắt) hoặc ngang sát đất (phải nhảy) | `Vertical`, `Length`, `Horizontal Height`, `Off/Warn/On Time`, `Always On` |
| **Cưa quay** | `TrapSaw` | Lưỡi cưa chạy qua lại trên ray | `Travel` (điểm cuối so với điểm đầu, ví dụ (9,0) hoặc (0,4)), `Speed`, `Radius`, `Start Phase` |
| **Súng tự động** | `TrapTurret` | Ngắm theo người chơi, tụ năng lượng rồi bắn loạt đạn | `Facing` (-1 trái / 1 phải), `Fire Interval`, `Charge Time`, `Burst Count`, `Projectile Speed`, `Range` |
| **Mìn** | `TrapMine` | Lại gần thì đếm ngược rồi nổ (vòng đỏ báo vùng nổ), nạp lại sau một lúc | `Trigger Radius`, `Fuse Time`, `Blast Radius`, `Rearm Time`, `Hurts Enemies` |

## 7. Kho đồ, vật phẩm, xu, rương

**Xu:** quái bị hạ rơi xu (`Enemy ▸ Coin Chance / Coin Min / Coin Max`; quái do trụ mạnh sinh ra rơi nhiều hơn theo Power). Xu rơi xuống sàn, hút về phía bạn khi lại gần. UI xu nằm ngay dưới thanh máu (có danh sách buff tạm thời). Ví tiền khởi đầu chỉnh ở `GameManager ▸ Start Coins`; số ô kho ở `GameManager ▸ Inventory Slots` (mặc định 24).

**Vật phẩm (`ItemData`)** — file ScriptableObject trong `Assets/NeonBreaker/Items/`. Tạo mới: chuột phải trong Project ▸ **Create ▸ Neon Breaker ▸ Item**. Ba loại (`Kind`):
- **Passive (trang bị):** có tác dụng chỉ cần nằm trong kho (cộng dồn theo số lượng), tính qua danh sách `Bonuses`.
- **Consumable (tiêu hao):** bấm DÙNG. Hồi máu (`Heal Hp`), cộng lượt hồi (`Heal Charges`), hoặc `Bonuses` — nếu `Buff Duration` > 0 là buff tạm thời, = 0 là tăng VĨNH VIỄN.
- **Valuable (đồ giá trị):** chỉ để bán (`Sell Value` xu / cái). Chưa có cửa hàng — thương nhân sẽ làm sau; hàm `Inventory.Sell` đã sẵn.

Chỉ số cộng được (`Bonuses ▸ Stat`): `MaxHealth` (cộng thẳng), `MoveSpeedPct`, `MeleeDamagePct`, `ShurikenDamagePct`, `DamageTakenPct`, `DashCooldownPct`, `HealPowerPct`, `CoinGainPct`, `MagnetRangePct` (cộng %, âm = giảm).

Mặc định có 17 vật phẩm: 8 trang bị (Lõi năng lượng, Giáp neon, Giày tia chớp, Lưỡi dao tần số, Ray phóng phi tiêu, Cuộn pha lướt, Nam châm xu, Bộ nano hồi phục), 5 tiêu hao (Ống hồi máu, Pin hồi năng lượng, Huyết thanh cuồng nộ, Huyết thanh tốc độ, Mảnh sinh lực), 4 đồ giá trị (Pin phế liệu, Chip dữ liệu, Tinh thể neon, Lõi cổ đại).

**Rương (`Chest`)** — bấm **T** khi đứng gần. **Bảng vật phẩm công khai:** chọn rương trong Scene (hoặc Prefab `Chest`) ▸ component **Chest**:
- `Guaranteed`: vật phẩm LUÔN có.
- `Loot Table`: kéo file `ItemData` vào ô **Item**, chỉnh **Weight** (trọng số), **Min/Max Count**. Mỗi lần mở bốc `Rolls Min..Max` lần theo trọng số.
- `No Duplicates`, `Coins Min/Max` (xu rơi ra khi mở), `Interact Range`.
Sửa Prefab `Chest` = đổi bảng mặc định cho mọi rương chưa ghi đè riêng; sửa từng rương trong Scene để cho mỗi rương một bảng riêng. Kho đầy thì rương không mở (báo "KHO ĐẦY").

## 8. Cấu trúc thư mục

```
Assets/
├─ Editor/                    NeonArtImporter (import PNG), NeonBuilder (menu Tools ▸ Neon Breaker)
├─ Shaders/                   NeonAdditive (phát sáng), NeonFlash (chớp trắng khi trúng đòn)
├─ NeonBreaker/
│  ├─ Art/                    PNG pixel art + manifest.json (PPU, pivot, tile)
│  ├─ Audio/  Materials/  Prefabs/  Scenes/  Items/ (vật phẩm)
├─ Resources/NeonBreaker/     ArtBank, AudioBank
└─ Scripts/
   ├─ Core/                   Cfg, GameManager, GameInput, PixelScreen, SpriteAnimator, Fx, Sfx, ...
   ├─ Gameplay/               Player, Enemy, Walker, Flyer, Boss, Tower, Projectile, Pickup, Level, Trap*, Coin, Chest, Inventory, ItemData ...
   └─ UI/                     Hud.cs (HUD / menu / màn thắng-thua, IMGUI), InventoryUi.cs (kho đồ)
Tools/PixelArt/gen_art.py       Sinh toàn bộ PNG (tuỳ chọn)
Tools/PixelArt/gen_enemies2.py  Sinh sprite Heavy + Rusher, CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_extras.py    Sinh sprite bẫy, xu, rương, icon vật phẩm, CẬP NHẬT manifest.json không ghi đè
```

`Cfg.cs` chỉ còn giữ: ngôn ngữ, layer, mốc chiều dài màn, bảng màu. Mốc cổng/boss/bàn thờ chỉnh bằng object `Level` trong Scene.

## 9. Chỉnh sửa khác trong Unity

| Muốn… | Làm thế này |
|---|---|
| Dời / thêm / xoá **trụ** | Kéo prefab `Tower` vào `Level/Towers` (hoặc xoá/di chuyển). Số trụ tự đếm lúc Play. |
| Thêm **bẫy / rương** | Kéo Prefab `TrapSpike`… hoặc `Chest` vào `Level/Traps` / `Level/Chests`, rồi chỉnh số trên Inspector. |
| Thêm / sửa **bục** | Duplicate một object trong `Level/Platforms`; đổi `Size` của SpriteRenderer và BoxCollider2D cho khớp. |
| Đổi chiều dài màn, vị trí cổng, chỗ boss | Chọn `Level`: kéo `LimitLeft`, `LimitRight`, `Gate`, `BossSpawn` (có Gizmos). |
| Đổi **hình** | Thay PNG trong `Art/` (giữ tên, kích thước), hoặc trong Prefab chọn `Visual` → **Sprite Animator ▸ Clips**. Tên clip phải giữ nguyên. |
| Đổi **âm thanh / nhạc** | `Resources/NeonBreaker/AudioBank` → kéo AudioClip vào Id tương ứng. |
| Thêm **loại quái mới** | Duplicate Prefab `Heavy` hoặc `Rusher`, đổi chỉ số và sprite, kéo vào `Spawn Table` của trụ. |
| Tắt hiệu ứng pixel | `Main Camera ▸ Pixel Screen ▸ Enable Pixel Render` = tắt. |

Sprite pixel art: 16 pixel = 1 đơn vị thế giới, màn hình tham chiếu 480×270.

## 10. Xử lý sự cố

- **Sprite hồng / không hiện:** chạy *Tools ▸ Neon Breaker ▸ Chỉ tạo lại Prefab* (gán lại material); kiểm tra Console xem shader có lỗi không.
- **Quái đứng yên, không đánh được / "The referenced script ... is missing":** mỗi class MonoBehaviour phải nằm trong file trùng tên (Walker.cs, Flyer.cs...). Sau khi code biên dịch xong chạy *Chỉ tạo lại Prefab*.
- **Heavy/Rusher không xuất hiện:** chạy menu *Thêm quái Heavy + Rusher*; kiểm tra Prefab `Tower` → `Spawn Table` có đủ dòng; nhớ `Min Power` của Heavy (1.3) và Rusher (1.1) — trụ có `Power` thấp hơn sẽ không ra loại đó.
- **Không thấy xu rơi / rương / bẫy:** chạy hai menu ở mục 1 (bản có bẫy + kho đồ) theo đúng thứ tự; kiểm tra `ArtBank ▸ Coin Prefab` đã có Prefab `Coin` (nếu trống, xu được cộng thẳng vào ví thay vì rơi ra).
- **Bấm B / T không có phản ứng:** bấm vào cửa sổ Game trước; T chỉ mở được khi đứng trong tầm rương (xem gợi ý "[T] MỞ RƯƠNG").
- **Heavy/Rusher hiện ô vuông/không có hình:** Unity chưa import xong PNG mới trong `Art/` — bấm Assets ▸ Refresh rồi chạy lại menu.
- **Màn hình đen khi Play lần 2:** đã xử lý bằng `Bootstrap` (reset static, hỗ trợ tắt Reload Domain).
- **Báo "Scene không có GameManager":** mở `Main.unity`.
- Các menu tạo lại: *Chỉ tạo lại Prefab / Chỉ tạo lại Scene Main / Xuất lại âm thanh WAV* (tạo lại Scene Main cũng mở rộng map + đặt bẫy + rương như menu ở mục 1). **Cảnh báo:** các menu này ghi đè bản Prefab/Scene bạn đã chỉnh.
