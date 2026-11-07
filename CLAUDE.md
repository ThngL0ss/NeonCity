# NEON BREAKER — ghi chú cho Claude (đọc đầu mỗi phiên)

Game 2D platformer pixel-art neon (Unity). Tác giả học làm game cùng AI, nói tiếng Việt; báo cáo bằng tiếng Việt, ngắn gọn, trung thực (nói rõ chưa biên dịch / chưa chạy thử trong Unity).

## Cốt truyện
**Toàn bộ cốt truyện, nhân vật, phe phái nằm ở [`docs/STORY.md`](docs/STORY.md).** Mọi lời thoại, nhiệm vụ, tên gọi, mô tả vật phẩm / quái / cảnh phải khớp file đó. Đọc nó trước khi viết hay sửa nội dung có chữ.
Tóm tắt rất ngắn: Trái đất 2136, 100 năm sau "Đại cải tiến" (2036, cỗ máy thời gian gộp mọi dòng thời gian vào Earth-01836 → Tân Địa Cầu). Player **XbAst-013** (gạch ngang) = con người bị số hóa não vào vi máy tính, có liên kết với Albert (cố vấn phe Pyber, hòa bình), được Albert tái tạo cơ thể, mỗi lần tái sinh mất một phần ký ức — không được gọi player đơn thuần là "cỗ máy Albert chế tạo". Cần lõi bậc SS để lưu trữ ký ức vĩnh viễn: lõi bị mất tên **Core-T04**, các lõi khác cùng kiểu tên chỉ khác số (Core-T03, Core-T06, ...), nằm ở boss **OVERLORD** thuộc phe Dyber (chiến tranh, cướp bóc). Các phe khác chưa đặt tên, có hiệp ước đình chiến ngầm.
Mục "Đã chốt" / "Chưa chốt" ở cuối `docs/STORY.md`: phe phái và cách nhắc tới chúng trong game **tác giả sẽ yêu cầu chỉnh sau** — hiện lời thoại chưa nhắc tên phe nào, đừng tự thêm. Không tự quyết thay tác giả; khi cần thì hỏi.
Khi tác giả cung cấp thêm chi tiết cốt truyện: cập nhật `docs/STORY.md` (và tóm tắt ở đây nếu đổi ý chính).

## Quy ước làm việc (đã thống nhất với tác giả)
- Làm trên nhánh `claude/<tên-việc>` tách từ `develop` mới nhất, push từng giai đoạn. Tác giả tự mở PR vào **`develop`** và thử trong Unity.
- Chỉ **thêm code / công cụ / sprite**. **Không ghi đè** chỉnh sửa Scene / Prefab / asset của tác giả; thông số do tác giả tự chỉnh trong Inspector. Việc cần đụng Scene thì làm bằng menu Editor idempotent ("giữ nguyên chỉnh sửa của bạn").
- Không chạy được Unity: chỉ kiểm tra tĩnh. Báo rõ menu nào cần chạy trong Unity.
- Mỗi MonoBehaviour một file trùng tên; reload domain tắt nên static phải reset trong `GameManager.ResetStatics()`.
- Mô tả đầy đủ hệ thống, phím, menu Editor: xem `README.md`.
