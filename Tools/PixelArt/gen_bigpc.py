#!/usr/bin/env python3
"""
Vẽ MÁY TÍNH KHỔNG LỒ nối với lồng kính hồi sinh trong căn cứ của Albert (dùng chung bảng màu với gen_base.py):

  bigpc_body           thân máy (80x112 px = 5 x 7 đơn vị, chân ở dưới giữa). Dây cáp ở bên PHẢI (phía lồng hồi sinh).
  bigpc_screen_0..3    lớp màn hình động đè lên thân máy (cùng kích thước): quét cơ thể, sóng tim, thanh tiến độ, đèn nhấp nháy
  bigpc_cable_low_0..1   bó dây cáp nối cổng dưới của máy với chân lồng hồi sinh (2 khung: xung dữ liệu chạy dọc dây)
  bigpc_cable_high_0..1  bó dây cáp vòng lên nối cổng trên của máy với khung lồng hồi sinh

Chạy:  python3 Tools/PixelArt/gen_bigpc.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Chỉ tạo các sprite ở trên, không đụng sprite khác.
Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import math
import os

import gen_base as B          # bảng màu + Cv (gen_base chỉ chạy main() khi chạy trực tiếp)
import gen_art as G
from gen_art import Cv, lerp, CYAN, CYAN_D, MAG, LIME, ORG, RED, YEL, WHITE, NAVY2

MET_DD, MET_D, MET, MET_L, MET_LL = B.MET_DD, B.MET_D, B.MET, B.MET_L, B.MET_LL
AMB, AMB_L, AMB_D = B.AMB, B.AMB_L, B.AMB_D
RUST_D = B.RUST_D
SCREEN_BG = (8, 20, 28)

W, H = 80, 112

# toạ độ các cổng cáp (pixel, y xuống) — khớp với sprite cáp bên dưới
PORT_LOW = (79, 98)
PORT_HIGH = (79, 30)


# ================================================================ THÂN MÁY
def body():
    c = Cv(W, H)

    # ---- vương miện: ăng-ten + khe tản nhiệt
    c.rect(10, 1, 69, 4, MET_D)
    c.rect(10, 1, 69, 1, MET_L)
    for x in range(14, 66, 4):
        c.px(x, 3, MET_DD)
    c.line(20, 0, 20, 0, MET_LL)
    c.rect(58, 0, 59, 1, MET)
    c.px(58, 0, AMB)
    c.px(22, 0, RED)

    # ---- khung màn hình chính
    c.rect(2, 4, 77, 53, MET_D)
    c.rect(2, 4, 77, 5, MET_L)
    c.rect(2, 4, 3, 53, MET)
    c.rect(75, 5, 77, 53, MET_DD)
    c.rect(2, 52, 77, 53, MET_DD)
    for x in (6, 40, 73):                           # ốc vít
        c.px(x, 6, MET_LL)
        c.px(x, 51, MET_LL)
    c.rect(5, 8, 74, 49, MET_DD)
    c.rect(6, 9, 73, 48, SCREEN_BG)
    # vạch dọc chia màn hình thành 3 vùng
    c.rect(28, 9, 28, 48, (20, 44, 56))
    c.rect(52, 9, 52, 48, (20, 44, 56))

    # ---- dải điều khiển dưới màn hình
    c.rect(0, 54, 79, 61, MET)
    c.rect(0, 54, 79, 54, MET_LL)
    c.rect(0, 61, 79, 62, MET_DD)
    for i in range(4):                              # 4 màn hình trạng thái nhỏ
        x = 4 + i * 11
        c.rect(x, 56, x + 8, 60, MET_DD)
        c.rect(x + 1, 57, x + 7, 59, (10, 26, 34))
    for i in range(7):                              # nút xoay / nút bấm
        x = 50 + i * 4
        c.rect(x, 56, x + 2, 59, MET_D)
        c.px(x + 1, 56, MET_LL)
    c.rect(48, 56, 48, 60, MET_DD)

    # ---- bàn phím nhô ra phía trước
    c.rect(2, 63, 77, 70, MET_D)
    c.rect(2, 63, 77, 63, MET_L)
    c.rect(2, 70, 77, 71, MET_DD)
    for y in (65, 67, 69):
        for x in range(5, 74, 3):
            c.rect(x, y, x + 1, y, MET_DD if (x + y) % 2 else MET)
    c.rect(26, 69, 50, 69, MET_L)

    # ---- thân tủ: ổ dữ liệu bên trái, lưới tản nhiệt bên phải
    c.rect(4, 72, 75, 107, MET_D)
    c.rect(4, 72, 5, 107, MET)
    c.rect(74, 72, 75, 107, MET_DD)
    for i in range(5):                               # 5 khay ổ
        y = 74 + i * 7
        c.rect(8, y, 36, y + 5, MET_DD)
        c.rect(9, y + 1, 35, y + 4, (24, 28, 52))
        c.rect(13, y + 2, 28, y + 3, MET_D)
        c.px(31, y + 2, MET_L)
    # lưới tản nhiệt lớn
    c.rect(40, 74, 71, 105, MET_DD)
    for y in range(76, 104, 3):
        c.rect(42, y, 69, y, (22, 26, 44))
        c.rect(42, y + 1, 69, y + 1, MET)
    c.rect(40, 74, 71, 74, MET_L)
    # sọc cảnh báo cam ở chân
    c.rect(0, 107, 79, 111, MET_DD)
    c.rect(0, 107, 79, 107, MET_L)
    for x in range(1, 79, 6):
        c.rect(x, 109, x + 2, 110, AMB)

    # ---- cổng cáp bên phải: mặt bích + ống lồi ra
    for (px, py, ph) in ((PORT_LOW[0], PORT_LOW[1], 9), (PORT_HIGH[0], PORT_HIGH[1], 9)):
        c.rect(px - 5, py - ph // 2, px, py + ph // 2, MET_L)
        c.rect(px - 5, py - ph // 2, px, py - ph // 2, MET_LL)
        c.rect(px - 5, py + ph // 2, px, py + ph // 2, MET_DD)
        c.rect(px - 3, py - 2, px, py + 2, RUST_D)
        c.px(px - 4, py - ph // 2 + 1, MET_LL)
        c.px(px - 4, py + ph // 2 - 1, MET_DD)
    # ống dẫn dọc chạy dọc mép phải nối hai cổng
    c.rect(74, 30, 76, 98, MET_D)
    c.rect(74, 30, 74, 98, MET_L)
    c.rect(76, 30, 76, 98, MET_DD)
    for y in range(36, 96, 10):
        c.rect(73, y, 77, y + 1, MET_DD)
        c.px(74, y, MET_LL)

    c.save("bigpc_body", pivot=(0.5, 0.0))


# ================================================================ MÀN HÌNH ĐỘNG (4 khung)
def screens():
    for f in range(4):
        c = Cv(W, H)

        # --- vùng giữa: quét cơ thể (khung xương người + tia quét chạy từ đầu đến chân)
        cx = 40
        col = (60, 210, 230)
        c.ell(cx - 4, 12, cx + 4, 20, (*col, 90))             # đầu
        c.ell(cx - 3, 13, cx + 3, 19, (*col, 0))
        c.rect(cx, 20, cx, 21, col)
        c.rect(cx - 7, 22, cx + 7, 22, col)                   # vai
        c.rect(cx - 7, 22, cx - 7, 36, col)                   # cánh tay
        c.rect(cx + 7, 22, cx + 7, 36, col)
        c.rect(cx - 4, 22, cx - 4, 45, col)                   # thân + chân
        c.rect(cx + 4, 22, cx + 4, 45, col)
        c.rect(cx - 4, 36, cx + 4, 36, col)
        c.rect(cx, 36, cx, 45, col)
        c.rect(cx - 4, 45, cx + 4, 45, col)
        scan = 12 + f * 9                                      # tia quét
        c.rect(30, scan, 50, scan, (200, 255, 255, 230))
        c.rect(30, scan + 1, 50, scan + 1, (80, 220, 240, 110))
        for y in range(12, 46, 4):                             # lưới mờ
            c.rect(30, y, 50, y, (40, 120, 140, 50))

        # --- vùng trái: cột đo năng lượng nhảy theo khung
        for i in range(6):
            x = 9 + i * 3
            hh = 6 + ((i * 7 + f * 5) % 22)
            colb = LIME if hh < 20 else AMB
            c.rect(x, 46 - hh, x + 1, 46, colb)
        c.rect(8, 12, 25, 12, (60, 170, 190))
        c.rect(8, 14, 8 + 6 + (f * 4) % 10, 14, CYAN)

        # --- vùng phải: sóng tim + thanh trạng thái
        pts = [(55, 26), (58, 26), (60, 18 - f), (62, 33 + f), (64, 26), (68, 26), (70, 22), (72, 26)]
        for a, b in zip(pts, pts[1:]):
            c.line(a[0], a[1], b[0], b[1], LIME, 1)
        for i in range(4):
            y = 34 + i * 3
            ln = 6 + ((i * 5 + f * 3) % 12)
            c.rect(55, y, 55 + ln, y + 1, (RED if (i + f) % 4 == 0 else CYAN_D))
        c.rect(55, 12, 71, 12, (60, 170, 190))
        # thanh tiến độ hồi sinh tăng dần theo khung
        c.rect(55, 46, 71, 46, (20, 50, 60))
        c.rect(55, 46, 55 + 4 * (f + 1), 46, YEL)

        # --- 4 màn hình trạng thái nhỏ
        for i in range(4):
            x = 4 + i * 11
            lit = (i + f) % 4
            colr = (LIME, AMB, CYAN, MAG)[i]
            c.rect(x + 1, 57, x + 7, 59, (10, 26, 34))
            c.rect(x + 1 + lit, 58, x + 3 + lit, 58, colr)

        # --- đèn LED trên khay ổ + vương miện nhấp nháy
        for i in range(5):
            y = 74 + i * 7
            on = (i * 2 + f) % 4 < 2
            colr = (LIME, AMB, CYAN, LIME, AMB)[i]
            c.px(31, y + 2, colr if on else lerp(colr, NAVY2, 0.75))
            c.px(33, y + 2, WHITE if (f + i) % 4 == 0 else lerp(CYAN, NAVY2, 0.6))
        c.px(22, 0, RED if f % 2 == 0 else (90, 20, 24))
        c.px(58, 0, AMB if f % 2 == 1 else AMB_D)

        c.save("bigpc_screen_%d" % f, pivot=(0.5, 0.0), outline=False)


# ================================================================ DÂY CÁP
def bezier(p0, p1, p2, n=64):
    pts = []
    for i in range(n + 1):
        t = i / float(n)
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        pts.append((x, y))
    return pts


def draw_bundle(c, pts, pulse_t):
    """Vẽ bó 3 sợi (cam / lam / xám) dọc theo đường pts, kèm một xung sáng chạy dọc dây."""
    layers = ((3, MET_DD), (2, MET_DD), (1, AMB_D), (0, MET_D), (-1, (30, 130, 150)), (-2, MET_D))
    for off, col in layers:
        for (x, y) in pts:
            c.px(round(x), round(y) + off, col)
    for (x, y) in pts:                                     # viền sáng trên cùng
        c.px(round(x), round(y) - 3, MET_L)
    # xung dữ liệu: vài pixel sáng chạy dọc dây
    for k in range(3):
        idx = int(((pulse_t + k * 0.34) % 1.0) * (len(pts) - 1))
        x, y = pts[idx]
        c.px(round(x), round(y), CYAN)
        c.px(round(x), round(y) - 1, WHITE)
        c.px(round(x), round(y) + 1, CYAN)


def cables():
    # ---- cáp thấp: từ cổng dưới của máy (0, 6) xuống chân lồng (27, 14), võng xuống ở giữa
    for f in range(2):
        c = Cv(28, 22)
        pts = bezier((0, 8), (14, 23), (27, 16))
        draw_bundle(c, pts, f * 0.5)
        for (x, y) in ((0, 8), (27, 16)):                  # đầu nối
            c.rect(x if x == 0 else x - 1, y - 3, x + (1 if x == 0 else 0), y + 3, MET_LL)
        c.save("bigpc_cable_low_%d" % f, pivot=(0.0, 0.0), outline=False)

    # ---- cáp cao: từ cổng trên của máy vòng lên rồi cắm xuống khung lồng
    for f in range(2):
        c = Cv(28, 28)
        pts = bezier((0, 10), (16, -8), (27, 24))
        draw_bundle(c, pts, f * 0.5)
        for (x, y) in ((0, 10), (27, 24)):
            c.rect(x if x == 0 else x - 1, y - 3, x + (1 if x == 0 else 0), y + 3, MET_LL)
        c.save("bigpc_cable_high_%d" % f, pivot=(0.0, 0.0), outline=False)


def main():
    body()
    screens()
    cables()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (may tinh khong lo) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
