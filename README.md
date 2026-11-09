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
- **Bản có hệ thống Skill** (nhánh `claude/he-thong-skill`): sau khi kéo về và Unity biên dịch xong, chạy **Tools ▸ Neon Breaker ▸ Thêm hệ thống Skill (không đụng Scene)** một lần — tạo Prefab `Part` (linh kiện), `Resources/NeonBreaker/SkillConfig.asset`, 9 kỹ năng mẫu trong `Assets/NeonBreaker/Skills/`, gán `ArtBank ▸ Part Prefab`. Chỉ tạo cái còn thiếu, không ghi đè; chạy lại được (ví dụ sau khi thêm quái Heavy/Rusher). Rồi commit các file `.meta`, Prefab, asset Unity vừa tạo. Chi tiết ở mục 7b.
- **Bản có cấp bậc / dầu / cinematic / căn cứ** (nhánh `claude/base-cinematic`): sau khi kéo về và Unity biên dịch xong, chạy lần lượt (mỗi menu chỉ tạo cái còn thiếu, không ghi đè chỉnh sửa của bạn)
  1. **Tools ▸ Neon Breaker ▸ Thêm bẫy + kho đồ + rương** và **Thêm hệ thống Skill** nếu bạn chưa chạy (xem trên).
  2. **Tools ▸ Neon Breaker ▸ Cập nhật cấp bậc + dầu + căn cứ (không đụng Scene Main)** — gán bậc cho quái (Walker/Flyer = D, Heavy/Rusher = C, Boss = SS cấp 1), tạo Prefab `Oil` + `WorldItem`, 4 nguyên liệu, `BaseConfig.asset` (công thức chế tác, luật ghép linh kiện, bảng vật phẩm rải đường), thêm nguyên liệu vào rương, và **tạo Scene `Base.unity`** (nếu chưa có) + thêm vào Build Settings. Scene Main không bị đụng.
  3. Mở `Main.unity` và `Base.unity`, bấm Play thử, rồi commit các file `.meta`, Prefab, asset, Scene mới.
  Muốn dựng lại căn cứ từ đầu: **Tools ▸ Neon Breaker ▸ Tạo lại Scene Căn cứ Base (GHI ĐÈ Base.unity)**.
- **Bản chỉnh boss / UI / menu B / máy tính khổng lồ** (nhánh `claude/boss-ui-polish`): chỉ có code + sprite mới, **không đụng Scene / Prefab của bạn** (chỉnh thông số vẫn do bạn). Sau khi kéo về và Unity biên dịch xong:
  1. Chạy **Tools ▸ Neon Breaker ▸ Thêm máy tính khổng lồ vào Scene Base (giữ nguyên chỉnh sửa của bạn)** — thêm đúng 1 object `ResurrectionComputer` (máy + màn hình động + 2 bó dây cáp) cạnh lồng hồi sinh trong `Base.unity`; chạy lại thì báo "đã có", không thêm lần nữa. Nên commit git trước khi chạy. Nếu Base đang mở trong Unity thì nhớ **Ctrl+S**.
  2. Bấm Play thử rồi commit các file `.meta`, PNG `bigpc_*`, Scene Unity vừa tạo / sửa.
- **Bản có menu chính / lưu game / nhiệm vụ / Phố Neon** (nhánh `claude/menu-nhiem-vu-pho-neon`): chỉ thêm code + sprite + công cụ, **không ghi đè Scene / Prefab / asset của bạn**. Sau khi kéo về và Unity biên dịch xong, chạy **một menu**: **Tools ▸ Neon Breaker ▸ Cập nhật Nhiệm vụ + Phố Neon + Lưu game (giữ nguyên chỉnh sửa của bạn)**. Menu này (chạy lại nhiều lần vẫn an toàn):
  - tạo 2 nguyên liệu `Item_iron_ingot` (Thỏi sắt) + `Item_carbon_fiber` (Sợi cacbon) và `Resources/NeonBreaker/ItemDatabase.asset` (danh sách vật phẩm để file lưu game tra lại — thêm vật phẩm mới thì chạy lại menu này);
  - trong `Base.unity` có sẵn: chỉ **thêm** `BaseStash` vào object `Shelf` (kho căn cứ) và object `Mailbox` (hộp thư nhiệm vụ, x ≈ 12.2) nếu chưa có — không đụng gì khác (Base đang mở thì nhớ **Ctrl+S**);
  - tạo Scene còn thiếu: `MainMenu` (menu chính), `Tutorial` (huấn luyện), `PhoNeon`, `RiaPhoNeon`, `Story` (cốt truyện mở đầu, mục 7i) + prefab NPC `Npc_*`; Scene đã có thì **giữ nguyên** (muốn dựng lại: xoá file `.unity` đó rồi chạy lại menu);
  - thêm Scene vào Build Settings, `MainMenu` đứng đầu.
  Rồi commit các file `.meta`, PNG `city_* rim_* npc_* tut_* op_*`, prefab, asset, Scene Unity vừa tạo. **Thử luồng thật bằng cách mở `MainMenu.unity` rồi bấm Play** (menu chính vẽ bằng IMGUI lúc chạy nên ở chế độ Edit Game view đen là bình thường; `MainMenuScreen` tự vẽ menu ở lớp trên cùng và nếu việc vẽ lỗi thì in lỗi thẳng lên màn hình + Console) (bấm Play thẳng ở `Main.unity` vẫn chạy kiểu cũ, có màn hình tựa cũ).
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
| **B** | **Mở / đóng menu nhân vật** gồm 3 mục: **Kho đồ · Skill · Player** (game tạm dừng khi mở). Đổi mục: bấm chuột vào thẻ phía trên, phím **1 / 2 / 3** hoặc **Tab** |
| **F** | **Tương tác**: nhặt vật phẩm nằm trên đường (thùng dầu, nguyên liệu), mở rương, nói chuyện với Albert, dùng bàn chế tác / máy ghép / cửa căn cứ. Đứng gần là hiện gợi ý "[F] …" |
| **M** | **Bản đồ** (hiện chỉ là màn đen; ở căn cứ có thêm điểm "khu thử nghiệm" để quay lại màn chơi) |
| **H** | **Bảng chọn minibot**: khi mở, người chơi đứng yên; **A / D** đổi ô, **S** triệu hồi (hoặc thu hồi nếu đang triệu hồi); bấm chuột vào ô cũng được |
| **P** | **Chế độ thử nghiệm (bật/tắt)**: không thể chết · sát thương ×3 · không hồi chiêu · hồi máu không tốn lượt |
| Esc | Tạm dừng · Enter: bắt đầu / tiếp tục · R: chơi lại · V: đổi ngôn ngữ |

Xu, linh kiện và giọt dầu vẫn tự hút về khi lại gần (không cần bấm F).

Phím **N cũ đã được gộp vào B**. Trong menu B:
- **Kho đồ** [1]: **A D W S / mũi tên** chọn ô · **Enter** dùng (đồ tiêu hao) hoặc **trang bị** (đồ trang bị) · **X** vứt 1 · **chuột**: bấm chọn, bấm đúp = dùng / trang bị.
- **Skill** [2]: cây kỹ năng, chế tạo, lưu trữ minibot — **Q / E** đổi tab con (xem 7b).
- **Player** [3]: bên trái là **hình nhân vật** (cấp bậc + cấp độ ở góc) và **8 ô trang bị** bên dưới (chỉ nhận đồ loại TRANG BỊ, không có đồ tiêu hao); bên phải là **thông tin**: máu, cấp bậc, cấp độ, sát thương chém / phi tiêu, tốc độ chạy, hồi chiêu lướt, lượng hồi máu, sát thương nhận vào (đã tính trang bị). **A / D** chọn ô trang bị · **Enter / X** hoặc bấm đúp = gỡ.
- **B / Esc** đóng.

Hệ số sát thương của chế độ thử nghiệm chỉnh ở `Systems ▸ GameManager ▸ Cheat Damage Mul` (mặc định 3). Khi bật, trên màn hình có nhãn vàng "[P] CHẾ ĐỘ THỬ NGHIỆM".

## 3. Luồng chơi (~10 phút)

Bấm **Chơi** → **cảnh mở đầu** (camera bay tới địa bàn boss: bạn đến lấy lại linh kiện bậc SS bị cướp; Enter = tiếp, Esc = bỏ qua) → phá hết **các trụ triệu hồi** (mỗi trụ sinh quái khi bạn lại gần) → cổng khu boss mở → vào khu boss → boss xuất hiện.
Boss (bậc **SS**, cấp 1): **Phase 1** chậm, máu trâu, 1 kỹ năng (nhảy đập + sóng xung kích). **Phase 2** nhanh, máu ít hơn, 3 kỹ năng (chớp bước chém, chùm đạn quạt, laser giáng từ trời) và **hút máu**. **Phase 3:** khi hạ phase 2, boss gục, hồi **đầy máu** (thanh máu đầy lại 100% như phase 1), đứng im 2 giây rồi **tự nổ 3 lần** (sát thương lớn + sóng xung kích làm bạn choáng), rồi đứng dậy đánh tiếp với tốc độ/sát thương phase 2, độ trâu phase 1, dùng kỹ năng cả hai phase + 1 kỹ năng mới (**Bão sét**: các cột sét giáng lan ra hai bên).
**Thanh máu boss chỉ có 2 mốc** (phase 1 → phase 2, có 1 vạch đánh dấu trên thanh); sang phase 3 thanh đầy lại và không còn vạch. **Bảo hiểm:** nếu người chơi vẫn đánh hết máu phase 3, boss **dừng lại, nhấp nháy 7 màu rồi phát nổ** thật lớn (sát thương cả đấu trường, chắc chắn hạ người chơi) — cảnh báo "BOSS ĐANG TỰ HUỶ" hiện trên màn hình. Boss vẫn được tính là đã hạ (nhiệm vụ của Albert). Ở chế độ thử nghiệm **[P]** người chơi không chết, boss thua bình thường.
**Khi chết:** cảnh người chơi gục, cơ thể tan ra lộ bộ khung robot, màn hình tối rồi chuyển về **căn cứ của Albert** (mục 7e), hồi sinh trong lồng kính. Ra khỏi căn cứ bằng cửa → bản đồ → quay lại màn chơi.
Quái nhỏ rơi vật phẩm hồi máu (mặc định 20%) — nhặt = +1 lượt hồi. Quái bị hạ còn **rơi xu** (mục 9).
Map dài ~260 đơn vị: 6 trụ cách nhau ~43 đơn vị, bục 3 tầng, **5 loại bẫy** (mục 8), rương rải rác (mục 9).

### Các loại quái

| Quái | Prefab | Bậc | Máu | Sát thương | Tốc độ | Ghi chú |
|---|---|---|---|---|---|---|
| Walker | `Walker` | **D** | 48 | 13 (lao) | 3.7 | Quái thường: đuổi → báo hiệu → lao |
| Flyer | `Flyer` | **D** | 30 | 9 (đạn) | 5 | Bay lơ lửng, bắn đạn |
| **Heavy** | `Heavy` | **C** | 220 | 8 (lao) | 2.0 | "Máu trâu": to, giáp nặng, gần như không bị đẩy lùi, báo hiệu đòn lâu (0.75s) |
| **Rusher** | `Rusher` | **C** | 18 | 24 (lao) | 7.0 | "Dao găm": nhỏ, lao rất nhanh (tốc độ lao 21), báo hiệu ngắn (0.4s) |
| Boss | `Boss` | **SS** | — | — | — | Cấp 1; không rơi linh kiện |

Chỉ số ở bảng là mặc định **ở cấp 1**; cấp độ quái (mục 4) nhân thêm lên.

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

**Cấp độ quái (thay cho cách "mạnh dần theo trụ" cũ):** quái càng **cấp cao** càng khoẻ. Cấp của quái do từng trụ sinh ra lấy từ `Level ▸ Tower Levels` theo thứ tự trụ từ trái sang phải — mặc định **5, 5, 7, 7, 7, 10** (2 trụ đầu cấp 5, 3 trụ sau cấp 7, trụ cuối cấp 10); ép riêng một trụ bằng `Tower ▸ Level Override` (> 0). Boss cấp 1. Công thức nhân theo cấp (tăng đều, không nhảy vọt) nằm ở `PowerRules` (`PowerSystem.cs`): `HpPerLevel` = 5% máu/cấp, `DamagePerLevel` = 3% sát thương/cấp, `SpeedPerLevel` = 0.6% tốc độ/cấp, `RewardPerLevel` = 4% xu/cấp. Ví dụ cấp 10: máu ×1.45, sát thương ×1.27. Chỉ số của người chơi chưa scale theo cấp.
`Tower ▸ Auto By Order / Power First / Power Last / Power` giờ chỉ còn quyết định **loại quái nào được ra** (cột `Min Power` trong `Spawn Table`) và tốc độ triệu hồi — **không làm quái khoẻ hơn** nữa.

## 6. Bẫy (5 loại)

Mọi bẫy kế thừa `Trap` (chung: `Damage`, `Knockback`, `Active Range` — chỉ chạy khi người chơi ở gần). Prefab ở `Assets/NeonBreaker/Prefabs/`, bản đặt sẵn nằm ở `Level/Traps`. Chỉ cần kéo Prefab vào Scene để thêm; mọi số chỉnh trên Inspector.

| Bẫy | Prefab | Cách hoạt động | Ô chỉnh chính |
|---|---|---|---|
| **Gai** | `TrapSpike` | Gai thụt → nhấp nháy báo hiệu → bật lên gây sát thương → thụt xuống (theo chu kỳ) | `Down Time`, `Warn Time`, `Up Time`, `Start Offset` (lệch pha), `Always Up` |
| **Laser** | `TrapLaser` | Hai trụ phát tia; tắt → báo hiệu → bật. Dọc (cổng laser, lướt `I` qua khi tia tắt) hoặc ngang sát đất (phải nhảy) | `Vertical`, `Length`, `Horizontal Height`, `Off/Warn/On Time`, `Always On` |
| **Cưa quay** | `TrapSaw` | Lưỡi cưa chạy qua lại trên ray | `Travel` (điểm cuối so với điểm đầu, ví dụ (9,0) hoặc (0,4)), `Speed`, `Radius`, `Start Phase` |
| **Súng tự động** | `TrapTurret` | Nòng **xoay nửa vòng (180°)** ngắm theo người chơi, tụ năng lượng rồi bắn loạt đạn. **Phá huỷ được** như trụ (ít máu hơn, rơi xu) | `Aim Center Angle` (90 = hướng lên), `Aim Arc` (180 = nửa vòng, 360 = quay tròn), `Turn Speed`, `Health` (110), `Reward Coins`, `Fire Interval`, `Charge Time`, `Burst Count`, `Range` |
| **Mìn** | `TrapMine` | **Vô hình** lúc đầu; giẫm lên thì **lộ ra** rồi đếm ngược và nổ (vòng đỏ báo vùng nổ). **Chỉ nổ một lần** rồi biến mất | `Step Radius`, `Step Height` (nhảy qua mìn thì không kích), `Fuse Time`, `Blast Radius`, `Hurts Enemies` |

## 7. Kho đồ, vật phẩm, xu, rương

**Xu:** quái bị hạ rơi xu (`Enemy ▸ Coin Chance / Coin Min / Coin Max`; quái do trụ mạnh sinh ra rơi nhiều hơn theo Power). Xu rơi xuống sàn, hút về phía bạn khi lại gần. UI xu nằm ngay dưới thanh máu (có danh sách buff tạm thời). Ví tiền khởi đầu chỉnh ở `GameManager ▸ Start Coins`; số ô kho ở `GameManager ▸ Inventory Slots` (mặc định 24).

**Vật phẩm (`ItemData`)** — file ScriptableObject trong `Assets/NeonBreaker/Items/`. Tạo mới: chuột phải trong Project ▸ **Create ▸ Neon Breaker ▸ Item**. Ba loại (`Kind`):
- **Passive (trang bị):** chỉ có tác dụng khi được **gắn vào 1 trong 8 ô trang bị** (menu **B ▸ Player**, hoặc chọn trong Kho đồ rồi bấm **TRANG BỊ / Enter**); món gắn vào được lấy khỏi kho, gỡ ra thì trả về kho (kho đầy thì không gỡ được). Chỉ số tính qua danh sách `Bonuses`; có thể gắn nhiều món giống nhau. Số ô trang bị: hằng số `Inventory.EquipSlots` (8).
- **Consumable (tiêu hao):** bấm DÙNG. Hồi máu (`Heal Hp`), cộng lượt hồi (`Heal Charges`), hoặc `Bonuses` — nếu `Buff Duration` > 0 là buff tạm thời, = 0 là tăng VĨNH VIỄN.
- **Valuable (đồ giá trị):** chỉ để bán (`Sell Value` xu / cái). Chưa có cửa hàng — thương nhân sẽ làm sau; hàm `Inventory.Sell` đã sẵn.

Chỉ số cộng được (`Bonuses ▸ Stat`): `MaxHealth` (cộng thẳng), `MoveSpeedPct`, `MeleeDamagePct`, `ShurikenDamagePct`, `DamageTakenPct`, `DashCooldownPct`, `HealPowerPct`, `CoinGainPct`, `MagnetRangePct` (cộng %, âm = giảm).

Mặc định có 17 vật phẩm: 8 trang bị (Lõi năng lượng, Giáp neon, Giày tia chớp, Lưỡi dao tần số, Ray phóng phi tiêu, Cuộn pha lướt, Nam châm xu, Bộ nano hồi phục), 5 tiêu hao (Ống hồi máu, Pin hồi năng lượng, Huyết thanh cuồng nộ, Huyết thanh tốc độ, Mảnh sinh lực), 4 đồ giá trị (Pin phế liệu, Chip dữ liệu, Tinh thể neon, Lõi cổ đại).

**Rương (`Chest`)** — bấm **F** khi đứng gần. **Bảng vật phẩm công khai:** chọn rương trong Scene (hoặc Prefab `Chest`) ▸ component **Chest**:
- `Guaranteed`: vật phẩm LUÔN có.
- `Loot Table`: kéo file `ItemData` vào ô **Item**, chỉnh **Weight** (trọng số), **Min/Max Count**. Mỗi lần mở bốc `Rolls Min..Max` lần theo trọng số.
- `No Duplicates`, `Coins Min/Max` (xu rơi ra khi mở), `Interact Range`.
Sửa Prefab `Chest` = đổi bảng mặc định cho mọi rương chưa ghi đè riêng; sửa từng rương trong Scene để cho mỗi rương một bảng riêng. Kho đầy thì rương không mở (báo "KHO ĐẦY").

## 7b. Hệ thống Skill (cây kỹ năng · chế tạo · minibot)

Mục **Skill** của menu **B** (phím **2**) gồm các tab: **CÂY SKILL**, **CHẾ TẠO**, **LƯU TRỮ MINIBOT** (và một tab trống dành cho mục sau). Toàn bộ thông số nằm ở một file: `Assets/Resources/NeonBreaker/SkillConfig.asset` (chỉnh trong Inspector).

**Cây kỹ năng — 2 nhánh.** *Nhánh bị động* (xanh lá) và *nhánh chủ động* (hồng). Mỗi kỹ năng là một file `SkillNodeData` (Create ▸ Neon Breaker ▸ Skill Node) trong `Assets/NeonBreaker/Skills/`: nhánh (`Branch`), kỹ năng cha (`Parent` — phải mở cha trước), `Start Unlocked`, `Conditions` (điều kiện mở khoá), `Max Level` + giá nâng cấp bằng xu (`Upgrade Cost Base/Growth`), `Effects` (chỉ số cộng thêm mỗi cấp). Kỹ năng khởi đầu: **Thu thập linh kiện** (bị động) và **Triệu hồi minibot** (chủ động), có sẵn từ đầu. 7 kỹ năng mẫu còn lại (Nhặt nhạnh, Chế tạo tiết kiệm, Biên đội, Bảng chọn mở rộng, Rút lui chiến thuật, Huấn luyện, Sửa chữa nhanh) đang để điều kiện **"sẽ được bổ sung sau"** → hiện KHOÁ; bật **chế độ thử nghiệm (P)** để thấy nút "MỞ (THỬ NGHIỆM)".
- Loại điều kiện (`UnlockType`): `Manual` (chưa có điều kiện), `None`, `KillCount` (tổng số quái đã hạ), `DefeatedEnemy` (đã hạ loại quái `Enemy Id`), `MinibotsCrafted`, `PartsCollected`. Kỹ năng tự mở khi **mọi** điều kiện thoả và kỹ năng cha đã mở. Thêm loại điều kiện mới: thêm vào enum `UnlockType` rồi viết nhánh `case` trong `SkillSystem.ConditionMet` + `ConditionText`.
- Chỉ số kỹ năng có thể cộng (`SkillStat`): `PartsDropPct`, `CraftCostPct` (số âm = giảm), `PanelSlots`, `MaxSummoned`, `RecallDelayPct` (âm = ngắn hơn), `MaturityGainPct`, `StorageHealPct`, `MinibotHpPct`, `MinibotDamagePct`. Thêm kỹ năng mới: tạo file `Skill Node`, chọn nhánh + cha, kéo vào `SkillConfig ▸ Nodes`.

**Skill bị động khởi đầu — linh kiện & chế tạo.** Khi bạn **kết liễu** một con quái, quái rơi **linh kiện** của *đúng loại quái đó* (mặc định 1 cái/lần, xác suất 100%; chỉnh ở `SkillConfig ▸ Craftables ▸ Parts Drop Chance/Min/Max`). Nhặt như nhặt xu. Loại quái nào bạn **đã từng hạ** thì tab **CHẾ TẠO** mở công thức: đủ **10 linh kiện** (`Parts Cost`) → chế tạo 1 minibot **cùng cấp bậc với quái gốc, cấp độ 1**, cất trong **Lưu trữ minibot** (tối đa 30). Loại quái chế tạo được: danh sách `SkillConfig ▸ Craftables` (mặc định Walker, Flyer, Heavy, Rusher). Mã loại = `Enemy ▸ Type Id` (để trống = tên Prefab).

**Skill chủ động khởi đầu — minibot.** Có ≥ 1 minibot thì **H** mở **bảng chọn** (4 ô). Quy tắc:
- Đưa minibot từ *Lưu trữ* ra *Bảng chọn* ở tab LƯU TRỮ MINIBOT (nút "ĐƯA RA BẢNG CHỌN"), làm được ở mọi nơi. Bảng chọn (phím **H**) dùng bằng bàn phím: A/D chọn ô, S triệu hồi hoặc thu hồi.
- Bấm minibot trong bảng chọn → **triệu hồi** (tối đa 2 con cùng lúc, `Max Summoned`). Minibot đi theo bạn, tự tấn công quái gần (Walker/Heavy/Rusher lao cận chiến, Flyer bay bắn đạn), **bị quái và bẫy gây sát thương như bạn** (bất tử 0.5s sau mỗi lần trúng). Mặc định minibot **không đánh trụ** (`Attack Towers`).
- Minibot **chết** khi đang triệu hồi → biến mất và **rơi linh kiện** (`Death Parts Base + cấp độ × Per Level`).
- **Thu hồi:** thoát giao tranh **5 giây** (không có quái trong `Combat Range` quanh bạn) rồi **bấm vào minibot** trong màn chơi (dòng chữ trên đầu hiện đếm ngược "GIAO TRANH 3s" → "[CLICK] THU HỒI"). Minibot về bảng chọn **giữ nguyên máu còn lại**.
- **Cất về lưu trữ** (từ bảng chọn, tab LƯU TRỮ MINIBOT) để **hồi máu dần** (`Storage Heal Pct Per Sec`, 3%/giây). Việc này **chỉ làm được ở căn cứ**: khi Scene `Base` đã có trong Build Settings thì `SkillSystem.AtBase` chỉ đúng ở căn cứ (ô `Anywhere Is Base` chỉ còn tác dụng khi chưa có Scene Base, để bạn thử trong Scene Main).
- **Độ trưởng thành:** +10 khi minibot kết liễu quái, +4 khi chỉ hỗ trợ (đã gây sát thương mà người khác hạ quái). Đầy thanh (`Maturity Base + (cấp-1) × Per Level`) thì bấm **TĂNG CẤP ĐỘ** (miễn phí; đặt `Level Up Coin Cost` > 0 để tính xu). Chỉ tăng **cấp độ** (1–200), **không** tăng cấp bậc. Cấp độ tăng máu (+6%/cấp) và sát thương (+5%/cấp) của minibot.
- Chưa có hệ thống lưu game: kỹ năng, linh kiện, minibot **reset khi chơi lại / thoát** (giống xu và kho đồ).

**Hệ thống sức mạnh.** `PowerSystem.cs`: **Cấp bậc** `Rank` F < D < C < B < A < S < SS < SSS và **Cấp độ** 1–200 (`PowerRules.MinLevel/MaxLevel`). **Cấp độ đã ảnh hưởng chỉ số quái** (mục 4); cấp bậc hiện chỉ quyết định loại linh kiện rơi ra (mục 7c) và vẫn chưa nhân chỉ số (`PowerRules.RankMul` là chỗ cắm về sau). Chỉ số người chơi chưa scale theo cấp. Minibot lấy cấp bậc từ quái gốc, cấp độ riêng.

**Bảng cấu hình `SkillConfig`:** kho (`Storage Slots`, `Panel Slots`, `Max Summoned`, `Storage Heal Pct Per Sec`, `Anywhere Is Base`), giao tranh (`Recall Delay`=5, `Combat Range`), chỉ số theo cấp (`Hp/Damage Per Level`, `Follow Distance`, `Engage Range`, `Attack Towers`), trưởng thành (`Maturity Kill/Assist/Base/Per Level`, `Level Up Coin Cost`), khi chết (`Death Parts Base/Per Level`).

## 7c. Linh kiện theo bậc, ghép linh kiện, dầu

- **Bậc linh kiện:** F, D, C, B, A, S. Quái bậc nào rơi **linh kiện đúng bậc đó** (Walker/Flyer → D, Heavy/Rusher → C). Quái bậc **SS / SSS** (boss) **không rơi** linh kiện. Linh kiện hiển thị trong kho kèm nhãn bậc.
- **Ghép linh kiện** (chỉ ở **căn cứ**, máy ghép): 3 linh kiện bậc thấp + **dầu** + xu = 1 linh kiện bậc kế tiếp. Mặc định (`BaseConfig ▸ Fusion`): F→D = 3 mảnh + 10 dầu + 20 xu · D→C = 3 mảnh + 20 dầu + 50 xu · C→B = 3 mảnh + 40 dầu + 120 xu. **Không ghép lên A và S** (chỉ rơi từ quái mạnh). **May mắn:** `Luck Chance` (mặc định 15%) → nhận thêm `Luck Bonus Parts` (2) mảnh thành phẩm.
- **Dầu** là nhiên liệu cho cả ghép linh kiện và chế tác. Nhặt giọt dầu quái rơi ra, thùng dầu nằm trên đường (bấm **F**), hoặc trong rương. Số thùng / nguyên liệu rải mỗi lần vào màn: `Level ▸ Scatter Oil Cans / Scatter Materials`; lượng dầu mỗi thùng và bảng vật phẩm rải: `BaseConfig ▸ Oil Can Amount / Scatter`.
- **Nguyên liệu chế tác** (4 loại: Sắt vụn, Dây đồng, Bo mạch, Gel năng lượng): nằm trong rương hoặc rải trên đường, nhặt bằng **F**. Thêm nguyên liệu mới: tạo `Item` loại `Material` rồi thêm vào `BaseConfig ▸ Scatter` và bảng rơi của rương.

## 7d. Cinematic

Điều khiển cảnh: **Enter** tiếp / **Esc** bỏ qua. Kịch bản nằm ở `Core/CineScripts.cs` (lời thoại, thời gian, **tên vật phẩm SS bị cướp**: `StolenItemVi/En` — hiện là "Core-T04"; các lõi SS khác rải rác sẽ đặt tên cùng kiểu, chỉ khác số, ví dụ Core-T03, Core-T06). Công cụ dựng cảnh: `Cine` (thanh đen, màn đen, camera bay, chờ), `PixelDissolve` (tan / ghép từng điểm ảnh), `RobotBit` (mảnh linh kiện bay). Tắt cảnh mở đầu: `Systems ▸ GameManager ▸ Play Intro`. Trong cảnh, quái / trụ / HUD tạm dừng.
- **Mở đầu** khi bấm Chơi (hình ảnh boss lúc đầu chỉ là **một bóng đen**); **boss phase 3** (gục → đầy máu → nổ 3 lần → choáng người chơi → đứng dậy); **chết** (gục, tan ra lộ bộ khung robot — số mảnh rơi ra đã **giảm đi nhiều**, tối dần rồi sang căn cứ).
- Thông số phase 3 của boss ở `Boss ▸ P3 …` (máu `P3 Hp`, tốc độ, sát thương `P3 Damage Mul`, hút máu `P3 Lifesteal`, thời gian choáng `Stun Time`, bão sét `Thunder …`). Màn tự huỷ khi phase 3 hết máu: `Boss ▸ Phase 3: tự huỷ ▸ Doom Charge Time` (thời gian nhấp nháy 7 màu) và `Doom Radius` (bán kính nổ).
- **Boss phase 2/3 từng bị trong suốt:** do `Flasher` ghi nhớ nhầm màu đang mờ làm màu gốc khi đổi phase — đã sửa (nhớ màu / material gốc đúng 1 lần; nhấp nháy 7 màu dùng `Flasher.Tint`, shader `NeonFlash` có thêm `_Tint`).

## 7e. Căn cứ của Albert (Scene `Base`)

Hầm công nghệ cũ pha đá / đất, dài ~120 đơn vị, đèn trần chập chờn, nhiều phát minh của Albert. Có 2 điểm: **lồng kính hồi sinh** (xuất hiện) và **cửa ra**.
- **Hồi sinh:** sét giáng từ trên xuống lồng kính, lồng toả sét, người chơi được ghép lại từ vô số mảnh linh kiện robot.
- **Máy tính khổng lồ** (`ResurrectionComputer`, cao ~7 đơn vị) đứng cạnh lồng (mặc định bên trái; sát mép trái thì sang bên phải), nối với lồng bằng 2 bó dây cáp (cáp thấp vào chân lồng, cáp cao vòng lên khung lồng). Màn hình động: quét cơ thể, sóng tim, thanh tiến độ, đèn nhấp nháy. Khi lồng nạp điện hồi sinh, vầng sáng màn hình mạnh lên, dây cáp sáng lên và phóng tia lửa điện. Thêm vào Scene có sẵn bằng menu *Thêm máy tính khổng lồ vào Scene Base*; sau đó kéo object `ResurrectionComputer` (dây cáp là object con nên đi theo) để chỉnh vị trí. Thông số: `Idle Glow`, `Charged Glow`.
- **Hội thoại:** khi Albert ↔ người chơi nói chuyện, camera **zoom vào người đang nói** (mức zoom `BaseConfig ▸ Dialogue Zoom`, mặc định 2) và khung thoại nhỏ gọn hơn ở đáy màn hình, không che nhân vật.
- **Lần đầu về căn cứ:** hội thoại Albert ↔ XbAst-013 (lời thoại ở `BaseManager.FirstArrivalLines`), sau đó đi lại tự do.
- **Albert** (áo choàng trắng, kính tròn): đi lại quanh căn cứ, thỉnh thoảng dừng lại ngắm một phát minh (kéo công trình vào `AlbertNpc ▸ Interest Points`). **F** để nói chuyện: kể cốt truyện (lần đầu), **giao nhiệm vụ** / nhận thưởng (nhiệm vụ mở đầu "Nguyên liệu hồi sinh" nhận ở hộp thư, rồi các nhiệm vụ hạ quái, phá trụ, ghép linh kiện, chế tác, hạ boss… trong `BaseMissions.cs`), mẹo chơi.
- **Bàn chế tác** (F): chọn công thức (W/S, Enter chế tác) — nguyên liệu + dầu (+ xu). Công thức ở `BaseConfig ▸ Recipes`.
- **Máy ghép** (F): 3 ô xếp **tam giác** + 1 ô giữa là thành phẩm; chọn bậc bằng A/D, bấm GHÉP (mục 7c).
- **Cửa ra** (F): mở **bản đồ** — chọn Phố Neon / Rìa Phố Neon / Khu thử nghiệm Dyber (mục 7h).
- **Giá kệ `Shelf`** (F): **kho căn cứ** — cất đồ để không mất khi chết (mục 7f). **Hộp thư `Mailbox`** (F): nhận nhiệm vụ (mục 7g).
- Dữ liệu (xu, kho đồ, skill, minibot, nhiệm vụ) được giữ khi chuyển Main ↔ Base (`GameFlow.Carry`). Khi thua game / chơi lại từ menu thì reset như cũ.
- Trong Scene `Base`, mọi thứ là object thật để bạn kéo thả: `BaseManager` (giới hạn trái/phải, lồng, Albert, cửa), `Props` (phát minh, máy), `Lamps`, `Environment`. Muốn đổi lời thoại / nhiệm vụ: `BaseManager.cs`, `BaseMissions.cs`.

## 7f. Menu chính, lưu game, hình phạt khi chết

- **Menu chính** (Scene `MainMenu`): tên game ở giữa trên cùng, **Trò chơi mới · Tiếp tục · Cài đặt · Thoát**. Cài đặt: ngôn ngữ, nhạc, hiệu ứng âm thanh, toàn màn hình (lưu vào PlayerPrefs, nhớ giữa các lần mở game). Bấm **Trò chơi mới** → màn đen hỏi "Bạn có muốn xem cốt truyện & hướng dẫn không?" (xem mục 7i). Trong Editor có thêm nút `[DEV]` nhảy thẳng vào màn Dyber để thử nhanh.
- **10 ô lưu.** Mỗi ván mới dùng 1 ô trống (đầy thì phải xoá bớt: nút **XÓA** hoặc phím **X** ở danh sách, có hỏi xác nhận). Mỗi ô là 1 file JSON dễ đọc `Application.persistentDataPath/saves/slot_N.json` (ghi qua `.tmp`, giữ `.bak`; file chính hỏng thì tự đọc `.bak`). Mã: `SaveManager`, `SaveData`.
- **Lưu tự động ở "điểm an toàn"** (không lưu giữa lúc đang đánh nhau ngoài màn): ngay sau khi chết (đã trừ hình phạt), khi về căn cứ, khi rời căn cứ, khi nhận / nhận thưởng nhiệm vụ, khi đóng kho căn cứ, khi xong màn hướng dẫn. Nội dung lưu: cấp bậc / cấp độ, xu, dầu, chỉ số vĩnh viễn, kho đồ + trang bị + linh kiện, minibot, cây skill, kho căn cứ, nhiệm vụ, cờ cốt truyện, địa điểm đã mở. Nạp lại = vào thẳng căn cứ.
- **Hình phạt khi chết** (`DeathPenalty`): mỗi lần chết là một lần Albert tái tạo cơ thể — **MẤT** toàn bộ vật phẩm + linh kiện **mang trên người** và minibot mang theo; **cấp độ −10**, **cấp bậc −1** (không xuống dưới mức tối thiểu). **GIỮ**: đồ cất ở **kho căn cứ** (giá kệ `Shelf`, bấm F; đồ chưa cất thì mất), minibot đã cất vào lưu trữ ở căn cứ, **cây skill**, xu, dầu, chỉ số vĩnh viễn. Về căn cứ có bảng "đã mất gì" trong 10 giây. Hằng số ở `DeathPenalty.cs` (`LevelLoss`, `RankLoss`).
- **Màn hướng dẫn chết = làm lại màn**, không bị phạt. Chơi lại bằng **R** (Restart) thì tách khỏi ô lưu (ván thử, không tự lưu) — muốn tiếp tục ván thật thì dùng **Tiếp tục** ở menu. Trong màn chơi, ESC ▸ **VỀ MENU CHÍNH** (có hỏi xác nhận).
- Cấp độ / cấp bậc hiện **chỉ được lưu và trừ khi chết** — chưa có hệ thống kinh nghiệm hay chỉ số chiến đấu gắn vào chúng.

## 7g. Nhiệm vụ & hộp thư

- Sau lần nói chuyện đầu với Albert, **hộp thư nhiệm vụ** (`Mailbox`, object `MissionBoard`) hiện lên ở căn cứ và có thông báo bảo ra đó nhận nhiệm vụ (bấm **F**). Cờ trên hộp thư dựng lên khi có việc mới. Hộp thư không tự nhận hộ — phải đi tới và bấm **NHẬN NHIỆM VỤ**.
- **Nhiệm vụ đầu "Nguyên liệu hồi sinh":** mang về căn cứ **10 Thỏi sắt + 20 Sợi cacbon + 50 dầu** (đồ trên người và trong kho căn cứ đều tính), nói chuyện với Albert để nộp (nộp xong thì trừ đồ). Thưởng 60 xu (mặc định, chỉnh ở `BaseMissions.cs`). Các nhiệm vụ cũ (hạ quái, phá trụ, ghép linh kiện, chế tác, hạ boss) nối tiếp sau. Mục tiêu hiện ở góc phải HUD.
- Thêm nhiệm vụ: sửa danh sách trong `BaseMissions.cs` (mỗi nhiệm vụ có thể có nhiều yêu cầu `MissionReq`: tên, số lượng cần, số đang có, có trừ đồ khi nộp không).

## 7h. Địa điểm & bản đồ

- **Khu huấn luyện** (Scene `Tutorial`, chỉ ở ván mới): đường dài thẳng tắp dạy lần lượt di chuyển → nhảy → lướt → chém → phi tiêu → hạ 4 quái (rào chắn mở khi hạ hết) → **chế tạo 2 minibot** (hệ thống tự cấp đủ linh kiện; chế ở **B ▸ Skill ▸ Chế tạo**) → bước qua cánh cửa cuối đường (bấm F) → cảnh mở đầu → màn chơi chính. Hướng dẫn hiện giữa phía trên màn hình; **Tab hai lần** để bỏ qua. Yêu cầu từng bước chỉnh ở `TutorialManager` (Inspector). Chưa có Scene `Tutorial` thì ván mới vào thẳng cảnh mở đầu.
- **Phố Neon** (Scene `PhoNeon`, 2 đầu lâu): thành phố công nghệ ban đêm, nhà cao tầng, biển neon / bảng quảng cáo nhấp nháy, nhiều NPC đi qua đi lại — người, robot, cả quái vô hại (không ai tấn công). Rải vật phẩm trên đường (thùng dầu, Thỏi sắt, Sợi cacbon, vật liệu — bấm **F** nhặt). Cổng về căn cứ ở đầu đường.
- **Rìa Phố Neon** (Scene `RiaPhoNeon`, 4 đầu lâu): vùng trũng rác thải + khí thải của thành phố trên cao: đống rác công nghệ, ruồi nhặng, thùng độc, lều trại, ống xả nhỏ giọt, NPC đeo mặt nạ mang vũ khí (hiện **chưa** tấn công — để dành cho nhiệm vụ phụ sau này). Cũng rải vật phẩm.
- Cả hai dùng `PlaceManager` (Inspector: số thùng dầu, số vật phẩm, **bảng Loot**, khoảng cách tối thiểu, màu hạt bay), `PlaceGate` (cổng F), `CityNpc` (NPC đi lại: tốc độ, thời gian đi / nghỉ, vùng đi lại), `FlySwarm` (đàn ruồi). Bố cục là object thật trong Scene, kéo thả để sửa; chỉnh NPC bằng prefab `Npc_*`.
- **Bản đồ** (phím **M**, hoặc cửa ra của căn cứ): mỗi địa điểm là một biểu tượng — **tên ở trên, hàng 5 đầu lâu ở dưới** (luôn vẽ đủ 5, đầu lâu thừa mờ đi: 1 đầu lâu = 1 sáng + 4 tối). **Phố Neon ở giữa**, **Rìa Phố Neon là vùng hình chữ L** ôm Phố Neon từ rìa Tây xuống rìa Nam, Khu thử nghiệm Dyber ở phía đông bắc. A D chọn, ENTER / F hoặc nút ĐI để tới (chỉ khi đang ở căn cứ). Dữ liệu địa điểm (tên, mô tả, đầu lâu, vị trí, vùng tô) ở `Places.cs`. `BaseConfig ▸ Map Test Destination` không còn dùng.


## 7i. Cốt truyện mở đầu (Cinematic)

- **Luồng ván mới:** *Trò chơi mới* → màn đen + câu hỏi **"Bạn có muốn xem cốt truyện & hướng dẫn không?"** → **Skip** (vào thẳng căn cứ, lần hồi sinh đầu tiên; coi như đã qua cốt truyện + màn hướng dẫn + cảnh mở đầu Dyber, tự lưu) hoặc **"Tất nhiên là xem rồi (khuyến nghị nên chọn)"** (Scene `Story` → màn hướng dẫn → cảnh mở đầu Dyber → …). Chưa có Scene `Story` (chưa chạy menu Editor) thì chọn Xem sẽ vào màn hướng dẫn như cũ. Thoát giữa chừng rồi *Tiếp tục* thì vào màn hướng dẫn (không chiếu lại cốt truyện). Mã: `MainMenuUi` (trang hỏi), `GameFlow.StartNewGame(bool watchStory)` / `FinishStory()`.
- **Scene `Story`** (10 cảnh, mỗi cảnh một hình ghép với một đoạn chữ; tên cảnh / vị trí nằm ở `StoryIntro`, chữ ở `StoryTexts.cs`): hầm + cỗ máy Möbius (ăn mừng, pháo giấy) → Nancelas sửa máy (màn hình chớp) → thay lõi vàng bằng lõi đen (chớp đen) → các nhà khoa học thấy bất thường (đen 3 giây) → hầm đổ nát, robot 2,5× vỡ nát → sụp đổ nặng hơn, camera trôi nhanh dần sang trái → cỗ máy rung, tia vật chất đen, sét, khe nứt, sét khổng lồ → một robot lao vào điểm kỳ dị, ánh sáng trắng 3 giây → phế tích La Mã + nhà chọc trời + xe bay, camera chớp đen sang đám đông (chớp đen) → chiến tranh các thời đại, camera chạy chậm trái → phải, mưa bão → tiến hóa vượn → người → vũ khí lạnh → nóng → trang phục Neon (các đường kẻ trắng gợn sóng) → chỉ chữ giữa màn hình đen.
- Chữ ở khung riêng chiếm 22% dưới màn hình (hình chỉ dùng 78% trên) nên chữ không đè lên hình. **ENTER / Space / bấm chuột** = qua đoạn chữ · **ESC** = bỏ qua cả cốt truyện. Chữ gõ từng ký tự, có bản Việt + Anh (bản Anh do mình dịch, "Đại cải tiến" = "Great Upgrade" — sửa trong `StoryTexts.cs`).
- Hình do `Tools/PixelArt/gen_opening.py` sinh (71 sprite `op_*`, đã commit sẵn PNG; chạy lại script chỉ khi muốn đổi hình — Scene `Story` dùng chúng qua menu Editor). Scene `Story` dựng bằng menu *Cập nhật Nhiệm vụ + Phố Neon + Lưu game* (idempotent; muốn dựng lại thì xoá `Story.unity` rồi chạy lại). Mọi object cảnh đều là object thật trong Scene (nhóm con của "Story") — kéo thả để chỉnh vị trí; **đừng đổi tên** các object con vì `StoryIntro` tìm theo tên.

## 8. Cấu trúc thư mục

```
Assets/
├─ Editor/                    NeonArtImporter (import PNG), NeonBuilder + NeonBuilderExtras + NeonBuilderSkill + NeonBuilderBase + NeonBuilderBaseScene + NeonBuilderStory (menu Tools ▸ Neon Breaker)
├─ Shaders/                   NeonAdditive (phát sáng), NeonFlash (chớp trắng khi trúng đòn)
├─ NeonBreaker/
│  ├─ Art/                    PNG pixel art + manifest.json (PPU, pivot, tile)
│  ├─ Audio/  Materials/  Prefabs/  Scenes/  Items/ (vật phẩm)  Skills/ (kỹ năng)
├─ Resources/NeonBreaker/     ArtBank, AudioBank, SkillConfig, BaseConfig
└─ Scripts/
   ├─ Core/                   Cfg, GameManager, GameFlow (menu / huấn luyện / căn cứ / địa điểm), SaveManager + SaveData (lưu 10 ô), GameSettings, GameInput, PixelScreen, SpriteAnimator, Fx, Sfx, Cine, CineScripts, PixelDissolve, ...
   ├─ Gameplay/               StoryIntro, StoryTexts (cốt truyện mở đầu), Player, Enemy, Walker, Flyer, Boss, Tower, Projectile, Pickup, Level, Trap*, Coin, Chest, Inventory, ItemData, SkillSystem, SkillConfig, SkillNodeData, Minibot, PartDrop, PowerSystem, WorldItem, OilDrop, PartFusion, BaseConfig, BaseManager, BaseMissions, MissionBoard, BaseStash + Stash, DeathPenalty, PlayerProgress, ItemDatabase, Places, PlaceManager, PlaceGate, CityNpc, FlySwarm, TutorialManager, MainMenuScreen, BaseCrafting, BaseStation, AlbertNpc, ResurrectionCapsule, ResurrectionComputer, LampFlicker, RobotBit ...
   └─ UI/                     Hud.cs (HUD / menu / màn thắng-thua, IMGUI), GameMenu.cs (menu B: khung + 3 thẻ mục), InventoryUi.cs (mục Kho đồ), SkillUi.cs (mục Skill, bảng minibot H), PlayerUi.cs (mục Player: hình + 8 ô trang bị + thông tin), BaseUi / DialogueUi / CraftUi / FusionUi / StashUi / MissionUi / MapUi (căn cứ), MainMenuUi (menu chính)
Tools/PixelArt/gen_art.py       Sinh toàn bộ PNG (tuỳ chọn)
Tools/PixelArt/gen_enemies2.py  Sinh sprite Heavy + Rusher, CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_extras.py    Sinh sprite bẫy, xu, rương, icon vật phẩm, CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_skill.py     Sinh sprite linh kiện + 9 icon kỹ năng, CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_stage2.py    Sinh sprite dầu, nguyên liệu, icon linh kiện theo bậc
Tools/PixelArt/gen_cine.py      Sinh bộ khung robot + mảnh linh kiện cho cinematic
Tools/PixelArt/gen_base.py      Sinh art căn cứ: gạch tường/sàn/trần, đèn, Albert, lồng kính, máy ghép, bàn chế tác, cửa, phát minh
Tools/PixelArt/gen_bigpc.py     Sinh sprite máy tính khổng lồ cạnh lồng hồi sinh (thân, 4 khung màn hình, 2 bó dây cáp), CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_story.py     Sinh sprite Thỏi sắt, Sợi cacbon, hộp thư nhiệm vụ + cờ, CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_opening.py   Sinh 71 sprite cốt truyện mở đầu `op_*` (hầm, cỗ máy Möbius, nhà khoa học, Nancelas, robot 2,5×/1,5×, thành phố đổ nát, đám đông, tiến hóa...), CẬP NHẬT manifest.json không ghi đè
Tools/PixelArt/gen_places.py    Sinh art Phố Neon (nền, nhà cao tầng, biển neon), Rìa Phố Neon (đống rác, lều, thùng độc, ruồi), NPC (người / robot), cửa huấn luyện, CẬP NHẬT manifest.json không ghi đè
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
| Thêm **kỹ năng / loại minibot** | Kỹ năng: tạo `Skill Node`, kéo vào `SkillConfig ▸ Nodes`. Loại minibot: thêm một dòng vào `SkillConfig ▸ Craftables` (kéo Prefab quái vào `Enemy Prefab`). |
| Thêm / dời **phát minh, máy** trong căn cứ | Mở `Base.unity`: kéo Prefab / object trong `Props`; muốn Albert ngắm nó thì kéo vào `AlbertNpc ▸ Interest Points`. |
| Đổi **cấp độ quái** theo trụ | `Level ▸ Tower Levels` (hoặc `Tower ▸ Level Override`); độ tăng mỗi cấp: `PowerRules` trong `PowerSystem.cs`. |
| Tắt hiệu ứng pixel | `Main Camera ▸ Pixel Screen ▸ Enable Pixel Render` = tắt. |

Sprite pixel art: 16 pixel = 1 đơn vị thế giới, màn hình tham chiếu 480×270.

## 10. Xử lý sự cố

- **Sprite hồng / không hiện:** chạy *Tools ▸ Neon Breaker ▸ Chỉ tạo lại Prefab* (gán lại material); kiểm tra Console xem shader có lỗi không.
- **Quái đứng yên, không đánh được / "The referenced script ... is missing":** mỗi class MonoBehaviour phải nằm trong file trùng tên (Walker.cs, Flyer.cs...). Sau khi code biên dịch xong chạy *Chỉ tạo lại Prefab*.
- **Heavy/Rusher không xuất hiện:** chạy menu *Thêm quái Heavy + Rusher*; kiểm tra Prefab `Tower` → `Spawn Table` có đủ dòng; nhớ `Min Power` của Heavy (1.3) và Rusher (1.1) — trụ có `Power` thấp hơn sẽ không ra loại đó.
- **Không thấy xu rơi / rương / bẫy:** chạy hai menu ở mục 1 (bản có bẫy + kho đồ) theo đúng thứ tự; kiểm tra `ArtBank ▸ Coin Prefab` đã có Prefab `Coin` (nếu trống, xu được cộng thẳng vào ví thay vì rơi ra).
- **Bấm B / F không có phản ứng:** bấm vào cửa sổ Game trước; F chỉ dùng được khi đứng trong tầm vật / rương / máy (xem gợi ý "[F] …").
- **Heavy/Rusher hiện ô vuông/không có hình:** Unity chưa import xong PNG mới trong `Art/` — bấm Assets ▸ Refresh rồi chạy lại menu.
- **Bấm H không thấy gì, hoặc mục Skill (B ▸ 2) báo "Chưa có dữ liệu Skill":** chạy menu *Thêm hệ thống Skill (không đụng Scene)* (mục 1). Bấm H chỉ mở bảng khi có ≥ 1 minibot.
- **Đồ trang bị không còn cộng chỉ số khi chỉ để trong kho:** đúng thiết kế mới — phải gắn vào ô trang bị (B ▸ Player, hoặc chọn món trong Kho đồ rồi bấm TRANG BỊ).
- **Menu *Thêm máy tính khổng lồ* báo thiếu sprite `bigpc_…`:** Unity chưa import PNG mới — bấm Assets ▸ Refresh (hoặc chạy `python3 Tools/PixelArt/gen_bigpc.py`) rồi chạy lại menu.
- **Nền Scene Main vẫn sáng:** chỉnh `Parallax ▸ Darken` (0 = giữ nguyên, 0.45 mặc định, tối đa 0.9) trên từng lớp nền; bản làm tối chỉ áp dụng khi chạy, không sửa Scene của bạn.
- **Quái chết nhưng linh kiện cộng thẳng vào kho thay vì rơi ra:** `ArtBank ▸ Part Prefab` đang trống — chạy lại menu *Thêm hệ thống Skill*.
- **Không chế tạo được:** cần mở kỹ năng "Thu thập linh kiện", đã hạ loại quái đó ít nhất 1 lần và đủ linh kiện (cả ba điều kiện hiện trong tab CHẾ TẠO).
- **Thiếu dòng Heavy/Rusher trong tab CHẾ TẠO:** tạo Prefab Heavy/Rusher (menu *Thêm quái Heavy + Rusher*) rồi chạy lại menu *Thêm hệ thống Skill*.
- **Chết xong game không chuyển sang căn cứ / bấm cửa không đi đâu:** Scene `Base` chưa có trong Build Settings — chạy menu *Cập nhật cấp bậc + dầu + căn cứ* (hoặc *Tạo lại Scene Căn cứ Base*); nếu thiếu Scene Base game vẫn chơi bình thường như trước.
- **Menu căn cứ báo "Thiếu sprite":** chạy `python3 Tools/PixelArt/gen_stage2.py`, `gen_cine.py`, `gen_base.py` (hoặc kéo đủ thư mục `Art/`) rồi chạy lại menu.
- **Bấm F không thấy gợi ý:** đứng sát vật / máy / Albert hơn; gợi ý "[F] …" chỉ hiện khi không đang mở bảng nào.
- **Màn hình đen khi Play lần 2:** đã xử lý bằng `Bootstrap` (reset static, hỗ trợ tắt Reload Domain).
- **Báo "Scene không có GameManager":** mở `Main.unity`.
- Các menu tạo lại: *Chỉ tạo lại Prefab / Chỉ tạo lại Scene Main / Xuất lại âm thanh WAV* (tạo lại Scene Main cũng mở rộng map + đặt bẫy + rương như menu ở mục 1). **Cảnh báo:** các menu này ghi đè bản Prefab/Scene bạn đã chỉnh.
