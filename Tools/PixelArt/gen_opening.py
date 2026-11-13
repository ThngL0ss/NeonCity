#!/usr/bin/env python3
"""
Vẽ toàn bộ hình ảnh cho CẢNH CỐT TRUYỆN MỞ ĐẦU (Scene Story): 10 cảnh kể về cỗ máy thời gian, Nancelas và cuộc "Đại cải tiến".
Dùng chung công cụ vẽ + bảng màu của gen_art.py. Mọi sprite tên bắt đầu bằng  op_  (opening). Khung hình chuẩn 480 x 210 px
(phần 60 px dưới cùng của màn hình 480 x 270 dành cho khung chữ, hình không bị chữ đè).

  CẢNH 1-4  hầm công nghệ
    op_bunker_bg                 hầm (tường, ống, đèn, sàn)                          480x210
    op_machine                   cỗ máy khổng lồ dựa trên dải Mobius (đen / tím)      160x140
    op_machine_glow              quầng sáng tím (Unity đổi độ trong suốt cho nhấp nháy)
    op_sci_{a,b,c}_{idle,cheer0,cheer1}   nhà khoa học áo choàng trắng (3 kiểu), đứng / reo hò 2 khung
    op_nan_work_0..1, op_nan_idle        Nancelas (áo dài trắng, tóc đen dài che mặt) sửa máy
    op_nan_big_reach / _hold             Nancelas cận cảnh (gấp đôi) cảnh 3
    op_s3_bg, op_s3_panel_{closed,yellow,empty,black}   cận cảnh khoang máy + nắp mở / lõi
    op_core_yellow, op_core_black        lõi vàng (ổn định) / lõi đen tuyền (hỗn loạn)
    op_excl_0..1                         dấu "!" nhấp nháy
    op_dot                               chấm trắng 4x4 (giấy pháo hoa, tia lửa, hạt... Unity tô màu)
  CẢNH 5-6  trận chiến + cỗ máy
    op_hall_bg_a / op_hall_bg_b          hầm đổ nát (5a) / sập hẳn, thành phố cháy (5b)   960x210
    op_mech_xl_{ok,dent,wreck_a,wreck_b,dash}   robot to gấp 2,5 lần người thường
    op_mech_l_{dent,ruin}                robot to gấp 1,5 lần (của Nancelas): hơi vỡ / nát, lộ người lái
    op_sci_dead_0..2, op_fire_0..2, op_smoke, op_debris_0..2
    op_ray, op_bolt_0..2, op_bolt_big, op_rift_0..2   tia vật chất đen, sét, khe nứt không gian
  CẢNH 7-9
    op_s7a_bg  (phế tích La Mã lẫn nhà chọc trời)     op_s7b_crowd (đám đông 6 kiểu / 3 thời)
    op_car_0..1  ô tô bay                              op_s8_war_bg (chiến tranh, 1280x210)   op_rain
    op_evo_0..5  tiến hóa: vượn 4 chân → khom → 2 chân → cầm vũ khí lạnh → vũ khí nóng → trang phục Neon
    op_evo_line_0..4  sợi dây trắng nối các hình (lớp phủ 480x210)

Chạy:  python3 Tools/PixelArt/gen_opening.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import math
import os
import random
import sys

import numpy as np
from PIL import Image, ImageDraw

import gen_art as G
from gen_art import (Cv, lerp, save_array, OUT, NAVY, NAVY2, NAVYL, CYAN, CYAN_D, MAG, MAG_D,
                     PURP, PURP_D, LIME, ORG, ORG_D, RED, RED_D, YEL, WHITE)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

FW, FH = 480, 210            # khung hình cốt truyện


# Cv.rect / Cv.ell của gen_art báo lỗi khi toạ độ ngược hoặc làm tròn ra x1 < x0 (hay gặp khi vẽ robot nhỏ theo tỉ lệ) -> bản an toàn cho script này
def _safe_rect(self, x0, y0, x1, y1, c):
    x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
    self.d.rectangle([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], fill=self._c(c))


def _safe_ell(self, x0, y0, x1, y1, c):
    x0, y0, x1, y1 = int(x0), int(y0), int(x1), int(y1)
    self.d.ellipse([min(x0, x1), min(y0, y1), max(x0, x1), max(y0, y1)], fill=self._c(c))


Cv.rect = _safe_rect
Cv.ell = _safe_ell
INK = (8, 6, 18)
VOID = (12, 6, 28)
COAT = (236, 240, 250)
COAT_S = (172, 180, 206)
COAT_D = (112, 120, 152)
PANTS = (56, 60, 88)
SHOE = (20, 20, 30)
SKIN_A = (236, 190, 150)
SKIN_B = (190, 132, 96)
SKIN_C = (120, 78, 56)
SKIN_P = (232, 214, 224)
HAIR_BR = (74, 46, 30)
HAIR_GR = (196, 198, 206)
HAIR_BK = (16, 14, 24)
STEEL = (92, 100, 134)
STEEL_L = (150, 160, 196)
STEEL_D = (44, 50, 80)
STEEL_DD = (26, 30, 52)
BLK = (14, 10, 26)
BLK_L = (40, 30, 70)
VIO = (176, 110, 255)
VIO_L = (226, 190, 255)
FIRE_A = (255, 170, 40)
FIRE_B = (255, 80, 30)
SMOKE = (40, 36, 46)


def save_img(name, im, pivot=(0.5, 0.5), tile=False):
    """Lưu một ảnh PIL RGBA (không viền) + ghi vào manifest."""
    arr = np.array(im.convert("RGBA"))
    save_array(name, arr, ppu=16, pivot=pivot, tile=tile)


def noise_rng(seed):
    return random.Random(seed)


# ================================================================ NHÀ KHOA HỌC (nhìn thẳng, 32x32, chân ở y=31)
SCI_STYLES = {
    "a": (SKIN_A, HAIR_BR, False),
    "b": (SKIN_B, HAIR_GR, True),
    "c": (SKIN_C, HAIR_BK, True),
}


def scientist(key, pose):
    skin, hair, glasses = SCI_STYLES[key]
    dy = {"idle": 0, "cheer0": -1, "cheer1": -3}[pose]
    c = Cv(32, 32)
    # chân + giày
    c.rect(12, 25 + dy, 14, 30 + dy, PANTS)
    c.rect(17, 25 + dy, 19, 30 + dy, PANTS)
    c.rect(11, 31 + dy, 14, 31 + dy, SHOE)
    c.rect(17, 31 + dy, 20, 31 + dy, SHOE)
    # áo choàng trắng dài tới gối
    c.poly([(10, 12 + dy), (21, 12 + dy), (23, 27 + dy), (9, 27 + dy)], COAT)
    c.line(16, 14 + dy, 16, 27 + dy, COAT_S)
    c.line(22, 14 + dy, 23, 27 + dy, COAT_S)
    c.line(9, 26 + dy, 23, 26 + dy, COAT_S)
    c.rect(13, 12 + dy, 18, 13 + dy, COAT_S)
    c.px(19, 17 + dy, CYAN)                                  # thẻ nhận dạng
    # tay
    if pose == "idle":
        c.rect(8, 13, 9, 22, COAT)
        c.rect(22, 13, 23, 22, COAT)
        c.rect(8, 23, 9, 24, skin)
        c.rect(22, 23, 23, 24, skin)
        c.rect(8, 13, 9, 14, COAT_S)
        c.rect(22, 13, 23, 14, COAT_S)
    else:
        lx, ly, rx, ry = (6, 6 + dy, 25, 6 + dy) if pose == "cheer0" else (4, 10 + dy, 27, 4 + dy)
        c.line(10, 14 + dy, lx, ly + 2, COAT, 2)
        c.line(21, 14 + dy, rx, ry + 2, COAT, 2)
        c.rect(lx - 1, ly - 1, lx + 1, ly + 1, skin)
        c.rect(rx - 1, ry - 1, rx + 1, ry + 1, skin)
    # đầu + tóc
    c.ell(12, 3 + dy, 19, 11 + dy, skin)
    if key == "a":
        c.rect(12, 2 + dy, 19, 4 + dy, hair)
        c.px(11, 5 + dy, hair)
        c.px(20, 5 + dy, hair)
    elif key == "b":
        c.rect(12, 2 + dy, 19, 3 + dy, hair)
        c.rect(11, 4 + dy, 11, 7 + dy, hair)
        c.rect(20, 4 + dy, 20, 7 + dy, hair)
        c.rect(13, 10 + dy, 18, 11 + dy, hair)                # râu
    else:
        c.rect(12, 2 + dy, 19, 4 + dy, hair)
        c.line(20, 4 + dy, 24, 10 + dy, hair, 2)               # tóc đuôi ngựa
    c.px(14, 7 + dy, INK)
    c.px(17, 7 + dy, INK)
    if glasses:
        c.line(13, 7 + dy, 18, 7 + dy, (60, 70, 110))
        c.px(14, 6 + dy, WHITE)
    if pose == "idle":
        c.line(15, 9 + dy, 16, 9 + dy, (160, 80, 70))
    else:
        c.rect(15, 9 + dy, 16, 10 + dy, (150, 40, 50))        # miệng há reo hò
    return c


def scientists():
    for k in "abc":
        for pose in ("idle", "cheer0", "cheer1"):
            scientist(k, pose).save("op_sci_%s_%s" % (k, pose), pivot=(0.5, 0.0))


# ================================================================ NANCELAS (nhìn thẳng, 32x32)
def nancelas(pose):
    """Áo dài trắng chấm gót, tóc đen dài rủ che gần hết mặt, trẻ. pose: idle | work0 | work1 | reach | hold."""
    c = Cv(32, 32)
    # áo dài
    c.poly([(10, 12), (21, 12), (25, 31), (7, 31)], COAT)
    c.line(16, 13, 16, 31, COAT_S)
    c.line(22, 14, 25, 31, COAT_S)
    c.line(9, 20, 7, 31, COAT_S)
    c.rect(13, 12, 18, 14, COAT_S)
    c.rect(11, 22, 20, 22, COAT_D)                          # thắt lưng
    c.px(16, 22, VIO)
    # tay
    if pose == "idle":
        c.rect(8, 13, 9, 23, COAT)
        c.rect(22, 13, 23, 23, COAT)
        c.rect(8, 24, 9, 25, SKIN_P)
        c.rect(22, 24, 23, 25, SKIN_P)
    elif pose in ("work0", "work1"):
        up = 0 if pose == "work0" else 2
        c.line(10, 14, 6, 9 + up, COAT, 2)
        c.line(21, 14, 25, 8 - up + 2, COAT, 2)
        c.rect(5, 7 + up, 7, 9 + up, SKIN_P)
        c.rect(24, 6 - up + 2, 26, 8 - up + 2, SKIN_P)
        c.line(26, 7 - up + 2, 29, 4 - up + 2, STEEL_L, 1)  # dụng cụ
        if pose == "work1":
            c.px(30, 3, YEL)
            c.px(29, 2, WHITE)
            c.px(31, 5, FIRE_A)
    elif pose == "reach":
        c.rect(8, 13, 9, 23, COAT)
        c.line(21, 14, 28, 16, COAT, 3)
        c.rect(28, 15, 30, 17, SKIN_P)
        c.rect(8, 24, 9, 25, SKIN_P)
    else:                                                    # hold: tay phải đưa trước ngực
        c.rect(8, 13, 9, 23, COAT)
        c.line(21, 14, 25, 19, COAT, 3)
        c.rect(25, 18, 27, 20, SKIN_P)
        c.rect(8, 24, 9, 25, SKIN_P)
    # tóc đen dài che gần hết mặt
    c.rect(11, 2, 20, 4, HAIR_BK)
    c.rect(10, 4, 21, 8, HAIR_BK)
    c.rect(10, 9, 12, 20, HAIR_BK)
    c.rect(19, 9, 21, 20, HAIR_BK)
    c.rect(13, 9, 18, 10, HAIR_BK)
    c.rect(14, 11, 17, 12, SKIN_P)                           # chỉ lộ một mảng mặt nhỏ + cằm
    c.px(15, 10, VIO_L)                                      # ánh mắt tím lấp ló sau tóc
    c.px(16, 12, (170, 60, 90))
    c.line(10, 8, 11, 17, (40, 36, 60))
    return c


def nancelas_sprites():
    nancelas("idle").save("op_nan_idle", pivot=(0.5, 0.0))
    nancelas("work0").save("op_nan_work_0", pivot=(0.5, 0.0))
    nancelas("work1").save("op_nan_work_1", pivot=(0.5, 0.0))
    for p in ("reach", "hold"):                              # cận cảnh: phóng gấp đôi
        cv = Cv(64, 64)
        cv.im = nancelas(p).im.resize((64, 64), Image.NEAREST)
        cv.save("op_nan_big_" + p, pivot=(0.5, 0.0))


# ================================================================ CỖ MÁY THỜI GIAN (dải Mobius, đen + tím)
def mobius_ring(w, h, yaw_deg=38.0, pitch_deg=14.0, radius=1.0, width=0.30):
    """Dải Mobius dựng đứng như một cánh cổng (nửa vòng xoắn), quay nghiêng một góc để thấy rõ chỗ xoắn.
    Xếp các mảnh tứ giác từ xa tới gần (painter), tô sáng theo pháp tuyến, viền mép sáng tím. Trả về ảnh RGBA w x h."""
    from PIL import ImageDraw
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    yaw, pit = math.radians(yaw_deg), math.radians(pitch_deg)
    N, M = 260, 5

    def world(u, v):
        r = radius + v * width * math.cos(u / 2.0)
        return (r * math.cos(u), r * math.sin(u), v * width * math.sin(u / 2.0))

    def proj(p):
        x, y, z = p
        X = x * math.cos(yaw) + z * math.sin(yaw)
        D = -x * math.sin(yaw) + z * math.cos(yaw)
        Y = y * math.cos(pit) - D * math.sin(pit)
        D2 = y * math.sin(pit) + D * math.cos(pit)
        return X, Y, D2

    # khung bao để co giãn vừa ảnh
    xs, ys = [], []
    for i in range(N):
        for v in (-1.0, 1.0):
            X, Y, _ = proj(world(4 * math.pi * i / N, v))
            xs.append(X); ys.append(Y)
    sc = min((w - 4) / (max(xs) - min(xs)), (h - 4) / (max(ys) - min(ys)))
    ox, oy = (max(xs) + min(xs)) / 2.0, (max(ys) + min(ys)) / 2.0

    def scr(P):
        return (w / 2.0 + (P[0] - ox) * sc, h / 2.0 - (P[1] - oy) * sc)

    quads = []
    for i in range(N):
        u0, u1 = 2 * math.pi * i / N, 2 * math.pi * (i + 1) / N
        for j in range(M):
            v0, v1 = -1 + 2.0 * j / M, -1 + 2.0 * (j + 1) / M
            w00, w10, w11, w01 = world(u0, v0), world(u1, v0), world(u1, v1), world(u0, v1)
            P = [proj(q) for q in (w00, w10, w11, w01)]
            dep = sum(q[2] for q in P) / 4.0
            ax, ay, az = (w10[0] - w00[0], w10[1] - w00[1], w10[2] - w00[2])
            bx, by, bz = (w01[0] - w00[0], w01[1] - w00[1], w01[2] - w00[2])
            nx, ny, nz = (ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx)
            nl = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
            lit = abs((nx * 0.35 + ny * 0.5 + nz * 0.8) / nl)
            quads.append((dep, [scr(q) for q in P], lit, i))
    quads.sort(key=lambda q: -q[0])                    # D2 càng lớn càng xa người xem (pitch quy ước) -> vẽ trước
    for dep, pts, lit, i in quads:
        col = lerp(BLK, (86, 52, 160), lit ** 1.6)
        if (i // 7) % 2 == 0:
            col = lerp(col, BLK, 0.4)
        d.polygon([(int(round(x)), int(round(y))) for x, y in pts], fill=col + (255,))
    # mép sáng: dải Mobius chỉ có MỘT mép liên tục, đi hết 4π
    edge = []
    for i in range(0, 2 * N + 1):
        P = proj(world(4 * math.pi * i / (2 * N), 1.0))
        edge.append((P[2], scr(P)))
    for k in range(len(edge) - 1):
        (d0, p0), (d1, p1) = edge[k], edge[k + 1]
        col = lerp((84, 50, 150), VIO_L, 0.35 + 0.65 * (0.5 + 0.5 * math.sin(k * 0.09)))
        d.line([(int(round(p0[0])), int(round(p0[1]))), (int(round(p1[0])), int(round(p1[1])))], fill=col + (255,), width=1)
    return im


def machine():
    """Cỗ máy khổng lồ 160x140: đế thép đen, 4 trụ, vòng Mobius đen-tím, lõi ở giữa."""
    W, H = 160, 140
    c = Cv(W, H)
    # đế
    c.rect(6, 118, 153, 139, STEEL_DD)
    c.rect(6, 118, 153, 120, STEEL_D)
    c.rect(14, 112, 145, 118, STEEL_D)
    c.rect(14, 112, 145, 113, STEEL)
    for x in range(12, 150, 8):
        c.rect(x, 126, x + 4, 128, BLK)
        c.px(x + 2, 127, VIO if (x // 8) % 3 else CYAN)
    # trụ đỡ
    for x in (30, 52, 100, 122):
        c.rect(x, 96, x + 5, 112, STEEL_D)
        c.rect(x, 96, x, 112, STEEL)
        c.rect(x - 1, 94, x + 6, 96, STEEL_L)
    # bệ giữa + ống cấp năng lượng
    c.rect(62, 100, 98, 112, BLK_L)
    c.rect(62, 100, 98, 101, VIO)
    c.rect(70, 94, 90, 100, BLK)
    for x in (66, 76, 84, 94):
        c.rect(x, 104, x + 1, 111, VIO)
    ring = mobius_ring(124, 100)
    c.im.alpha_composite(ring, (18, 0))
    # lõi ở giữa vòng
    c.rect(72, 40, 88, 62, STEEL_DD)
    c.rect(72, 40, 88, 41, STEEL)
    c.rect(75, 44, 85, 58, BLK)
    c.rect(77, 46, 83, 56, (230, 190, 30))                    # lõi vàng ổn định (cảnh 1-3)
    c.rect(78, 47, 80, 49, (255, 245, 150))
    c.line(80, 62, 80, 94, STEEL_D, 2)                        # trục nối lõi xuống bệ
    c.outline()
    return c


def machine_sprites():
    c = machine()
    c.save("op_machine", pivot=(0.5, 0.0), outline=False)
    # quầng sáng tím tròn (alpha tăng dần vào tâm)
    s = 200
    a = np.zeros((s, s, 4), np.uint8)
    yy, xx = np.mgrid[0:s, 0:s]
    r = np.sqrt((xx - s / 2 + 0.5) ** 2 + (yy - s / 2 + 0.5) ** 2) / (s / 2)
    al = np.clip(1 - r, 0, 1) ** 2
    al = (np.floor(al * 6) / 6 * 200)                         # đóng băng thành 6 bậc cho đúng kiểu pixel
    a[..., 0], a[..., 1], a[..., 2], a[..., 3] = 170, 90, 255, al
    save_array("op_machine_glow", a, ppu=16, pivot=(0.5, 0.5))


# ================================================================ HẦM CÔNG NGHỆ (cảnh 1-4)
FLOOR_Y = 176                                                  # mặt sàn (px từ trên khung)


def bunker_bg():
    c = Cv(FW, FH)
    rng = noise_rng(7)
    # tường: dải màu tối xanh tím
    for y in range(0, FLOOR_Y):
        t = y / FLOOR_Y
        c.rect(0, y, FW - 1, y, lerp((14, 12, 34), (30, 22, 58), t))
    # tấm ốp tường
    for x in range(0, FW, 40):
        c.rect(x, 24, x, FLOOR_Y - 1, (10, 8, 26))
        c.rect(x + 1, 24, x + 1, FLOOR_Y - 1, (44, 36, 82))
    for y in (60, 112, 150):
        c.rect(0, y, FW - 1, y, (10, 8, 26))
        c.rect(0, y + 1, FW - 1, y + 1, (40, 32, 74))
    # trần + ống dẫn
    c.rect(0, 0, FW - 1, 22, (10, 8, 26))
    c.rect(0, 22, FW - 1, 24, (50, 44, 96))
    for x in range(0, FW, 6):
        c.px(x, 8, (30, 26, 60))
        c.px(x + 2, 15, (30, 26, 60))
    for y, col in ((6, STEEL_D), (13, (60, 44, 110))):
        c.rect(0, y, FW - 1, y + 3, col)
        c.rect(0, y, FW - 1, y, lerp(col, WHITE, 0.25))
        for x in range(10, FW, 54):
            c.rect(x, y - 1, x + 3, y + 4, STEEL)
    # đèn trần phát sáng
    for x in (60, 170, 310, 420):
        c.rect(x, 25, x + 28, 28, (210, 230, 255))
        c.rect(x - 2, 29, x + 30, 30, (70, 70, 130))
    # màn hình điều khiển hai bên
    for x0 in (14, 394):
        c.rect(x0, 66, x0 + 70, 138, (8, 8, 22))
        c.rect(x0, 66, x0 + 70, 67, STEEL)
        for k in range(4):
            c.rect(x0 + 6 + k * 16, 74, x0 + 17 + k * 16, 94, (6, 14, 36))
            for q in range(5):
                h = rng.randint(2, 18)
                c.rect(x0 + 8 + k * 16 + q * 2, 94 - h, x0 + 8 + k * 16 + q * 2, 93, rng.choice([CYAN, VIO, LIME]))
        for q in range(8):
            c.rect(x0 + 6 + q * 8, 108, x0 + 11 + q * 8, 112, rng.choice([CYAN_D, VIO, (90, 60, 160), (60, 150, 160)]))
            c.px(x0 + 8 + q * 8, 120, rng.choice([RED, LIME, YEL]))
        c.rect(x0, 138, x0 + 70, 146, STEEL_D)
    # bàn điều khiển sát sàn
    for x0 in (96, 330):
        c.rect(x0, 150, x0 + 54, FLOOR_Y - 1, STEEL_DD)
        c.rect(x0, 150, x0 + 54, 152, STEEL)
        for q in range(9):
            c.px(x0 + 4 + q * 6, 158, rng.choice([CYAN, VIO, LIME, YEL]))
    # sàn kim loại
    for y in range(FLOOR_Y, FH):
        t = (y - FLOOR_Y) / (FH - FLOOR_Y)
        c.rect(0, y, FW - 1, y, lerp((36, 32, 70), (14, 12, 32), t))
    c.rect(0, FLOOR_Y, FW - 1, FLOOR_Y, (120, 100, 190))
    for x in range(0, FW, 24):
        c.rect(x, FLOOR_Y + 1, x, FH - 1, (10, 8, 26))
    for y in (FLOOR_Y + 12, FLOOR_Y + 24):
        c.rect(0, y, FW - 1, y, (10, 8, 26))
    # dải đèn tím trên sàn dưới cỗ máy
    c.rect(160, FLOOR_Y + 3, 320, FLOOR_Y + 4, (90, 54, 170))
    save_img("op_bunker_bg", c.im)


def small_props():
    # lõi vàng / lõi đen
    c = Cv(12, 12)
    c.ell(1, 1, 10, 10, (230, 190, 30))
    c.ell(3, 3, 8, 8, (255, 235, 110))
    c.px(4, 4, WHITE)
    c.save("op_core_yellow", pivot=(0.5, 0.5), outline=False)
    c = Cv(12, 12)
    c.ell(1, 1, 10, 10, (4, 2, 10))
    c.ell(2, 2, 9, 9, (16, 8, 30))
    c.ring = None
    for a in range(0, 360, 30):
        c.px(5.5 + 5 * math.cos(math.radians(a)), 5.5 + 5 * math.sin(math.radians(a)), (110, 50, 190))
    c.px(4, 4, (60, 30, 110))
    c.save("op_core_black", pivot=(0.5, 0.5), outline=False)
    # dấu "!"
    for f in range(2):
        c = Cv(8, 14)
        col = YEL if f == 0 else (255, 120, 60)
        c.rect(2, 0, 5, 8, col)
        c.rect(3, 0, 4, 8, (255, 255, 200) if f == 0 else (255, 200, 150))
        c.rect(2, 10, 5, 13, col)
        c.save("op_excl_%d" % f, pivot=(0.5, 0.0), outline=True)
    # chấm trắng (Unity tô màu)
    c = Cv(4, 4)
    c.rect(0, 0, 3, 3, WHITE)
    c.save("op_dot", pivot=(0.5, 0.5), outline=False)


def s3_assets():
    # nền cận cảnh: thân máy
    c = Cv(FW, FH)
    for y in range(FH):
        c.rect(0, y, FW - 1, y, lerp((16, 12, 34), (8, 6, 22), y / FH))
    for x in range(0, FW, 48):
        c.rect(x, 0, x + 1, FH - 1, (6, 4, 16))
        c.rect(x + 2, 0, x + 2, FH - 1, (40, 30, 80))
    for y in range(20, FH, 52):
        c.rect(0, y, FW - 1, y + 1, (6, 4, 16))
        c.rect(0, y + 2, FW - 1, y + 2, (40, 30, 80))
    rng = noise_rng(3)
    for _ in range(70):
        x, y = rng.randint(0, FW - 1), rng.randint(0, FH - 1)
        c.px(x, y, (60, 44, 110))
    for x in range(8, FW, 48):
        for y in (10, 62, 114, 166):
            c.rect(x, y, x + 2, y + 2, STEEL)
    # đường dẫn tím chạy dọc
    for x in (120, 360):
        c.rect(x, 0, x + 3, FH - 1, (26, 14, 56))
        c.rect(x + 1, 0, x + 1, FH - 1, (140, 80, 240))
    save_img("op_s3_bg", c.im)
    # khoang máy 112x96: 4 trạng thái
    for name in ("closed", "yellow", "empty", "black"):
        c = Cv(112, 96)
        c.rect(0, 0, 111, 95, STEEL_D)
        c.rect(0, 0, 111, 3, STEEL)
        c.rect(0, 92, 111, 95, STEEL_DD)
        for (x, y) in ((4, 4), (104, 4), (4, 88), (104, 88)):
            c.rect(x, y, x + 3, y + 3, STEEL_L)
        if name == "closed":
            c.rect(10, 10, 101, 85, (30, 24, 60))
            c.rect(10, 10, 101, 12, (110, 80, 200))
            c.rect(52, 30, 59, 66, (14, 10, 30))             # khe giữa hai cánh nắp
            c.rect(14, 44, 40, 52, (14, 10, 30))
            c.rect(72, 44, 98, 52, (14, 10, 30))
            c.line(14, 14, 98, 82, (40, 32, 80), 1)
        else:
            c.rect(10, 10, 101, 85, (6, 4, 14))              # khoang trống tối
            c.rect(10, 10, 101, 12, (90, 60, 170))
            c.rect(8, 10, 12, 85, STEEL)                      # bản lề cánh nắp mở
            c.rect(99, 10, 103, 85, STEEL)
            c.rect(30, 24, 82, 72, (20, 14, 44))
            c.ell(36, 28, 76, 68, (10, 8, 22))
            c.ell(40, 32, 72, 64, (26, 20, 56))
            for a in range(0, 360, 20):
                c.px(56 + 18 * math.cos(math.radians(a)), 48 + 18 * math.sin(math.radians(a)), VIO)
            if name == "yellow":
                c.ell(44, 36, 68, 60, (230, 190, 30))
                c.ell(48, 40, 64, 56, (255, 235, 110))
                c.rect(51, 42, 54, 45, WHITE)
            elif name == "black":
                c.ell(44, 36, 68, 60, (4, 2, 10))
                c.ell(46, 38, 66, 58, (14, 8, 28))
                for a in range(0, 360, 24):
                    c.px(56 + 11 * math.cos(math.radians(a)), 48 + 11 * math.sin(math.radians(a)), (120, 60, 200))
        c.save("op_s3_panel_" + name, pivot=(0.5, 0.5), outline=False)


# ================================================================ ROBOT (người lái trong bộ giáp khổng lồ)
MECH_STYLES = {
    # thân, thân tối, thân sáng, đèn
    "sci": ((200, 208, 226), (110, 120, 156), (240, 244, 255), CYAN),
    "nan": ((34, 24, 58), (16, 10, 32), (78, 52, 140), VIO),
}


def erase_blob(c, cx, cy, r, rng):
    """Xóa một mảng răng cưa (làm mảnh vỡ): bỏ pixel trong bán kính r, mép lởm chởm."""
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            if 0 <= x < c.w and 0 <= y < c.h:
                dd = math.hypot(x - cx, y - cy)
                if dd < r * (0.7 + 0.3 * rng.random()):
                    c.im.putpixel((x, y), (0, 0, 0, 0))


def ground_cleanup(c, rows=3):
    """Giữ lại các pixel NỐI LIỀN (8 hướng) với mặt đất (vài hàng dưới cùng); mảnh nào lơ lửng không có điểm tựa thì xoá."""
    a = np.array(c.im)
    solid = a[:, :, 3] > 0
    h, w = solid.shape
    keep = np.zeros_like(solid)
    stack = [(y, x) for y in range(max(0, h - rows), h) for x in range(w) if solid[y, x]]
    for y, x in stack:
        keep[y, x] = True
    while stack:
        y, x = stack.pop()
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and solid[ny, nx] and not keep[ny, nx]:
                    keep[ny, nx] = True
                    stack.append((ny, nx))
    a[~keep] = (0, 0, 0, 0)
    c.im = Image.fromarray(a, "RGBA")
    c.d = ImageDraw.Draw(c.im)


def mech_front(H, style, dmg, seed, pilot=False):
    """Robot nhìn thẳng cao H px. dmg: 0 nguyên · 1 xây xát (vài chỗ vỡ) · 2 vỡ nát gần hết · 3 đống đổ nát."""
    rng = noise_rng(seed)
    body, body_d, body_l, glow = MECH_STYLES[style]
    u = H / 10.0
    W = int(H * 0.8) + 8
    c = Cv(W, H + 6)
    cx = W // 2
    base = H + 5

    def R(x0, y0, x1, y1, col):
        c.rect(cx + x0 * u, base - (10 - y0) * u + 0, cx + x1 * u - 1, base - (10 - y1) * u - 1, col)

    # chân
    R(-1.9, 6.2, -0.4, 9.4, body_d)
    R(0.4, 6.2, 1.9, 9.4, body_d)
    R(-2.3, 9.4, -0.2, 10, body)
    R(0.2, 9.4, 2.3, 10, body)
    R(-1.9, 6.2, -1.7, 9.4, body_l)
    R(0.4, 6.2, 0.6, 9.4, body_l)
    # hông
    R(-1.8, 5.5, 1.8, 6.4, body_d)
    # thân
    c.poly([(cx - 2.1 * u, base - 7.6 * u), (cx + 2.1 * u, base - 7.6 * u), (cx + 1.5 * u, base - 4.2 * u), (cx - 1.5 * u, base - 4.2 * u)], body)
    R(-2.1, 2.4, -1.4, 5.8, body_l)
    R(1.4, 2.4, 2.1, 5.8, body_d)
    R(-0.9, 3.0, 0.9, 4.6, (12, 14, 30))                    # khoang lái
    R(-0.5, 3.3, 0.5, 4.2, glow)
    R(-1.2, 5.0, 1.2, 5.2, glow)
    # vai
    c.ell(cx - 3.3 * u, base - 7.9 * u, cx - 1.7 * u, base - 6.5 * u, body_l)
    c.ell(cx + 1.7 * u, base - 7.9 * u, cx + 3.3 * u, base - 6.5 * u, body_d)
    # tay
    R(-3.2, 2.9, -2.1, 6.2, body)
    R(2.1, 2.9, 3.2, 6.2, body_d)
    R(-3.4, 6.2, -1.9, 7.2, body_l)
    R(1.9, 6.2, 3.4, 7.2, body_d)
    # đầu + kính ngắm
    R(-0.9, 0.8, 0.9, 2.5, body_l)
    R(-0.7, 1.4, 0.7, 1.9, (10, 10, 22))
    R(-0.5, 1.5, 0.5, 1.7, glow)
    R(-0.1, 0.1, 0.1, 0.8, body_l)

    if dmg >= 1:
        # xây xát: vết nứt, vài chỗ vỡ, cháy xém
        for _ in range(3 + dmg * 2):
            x0, y0 = cx + rng.uniform(-2.2, 2.2) * u, base - rng.uniform(3, 9) * u
            c.line(x0, y0, x0 + rng.uniform(-3, 3), y0 + rng.uniform(2, 6), (20, 18, 34), 1)
        for _ in range(1 + dmg):
            erase_blob(c, cx + rng.uniform(-2.4, 2.4) * u, base - rng.uniform(3, 8.5) * u, rng.uniform(0.5, 0.9) * u, rng)
        for _ in range(3):
            c.px(cx + rng.uniform(-2, 2) * u, base - rng.uniform(4, 8) * u, YEL)
    if dmg >= 2:
        # vỡ nát: mất đầu + một tay + nửa thân, lộ dây điện
        erase_blob(c, cx, base - 9.2 * u, 1.3 * u, rng)
        erase_blob(c, cx - 3.0 * u, base - 5.0 * u, 2.0 * u, rng)
        erase_blob(c, cx + 1.3 * u, base - 4.0 * u, 1.8 * u, rng)
        erase_blob(c, cx - 1.0 * u, base - 1.8 * u, 1.2 * u, rng)
        for _ in range(8):
            x0, y0 = cx + rng.uniform(-2, 2) * u, base - rng.uniform(3, 6.5) * u
            c.line(x0, y0, x0 + rng.uniform(-4, 4), y0 + rng.uniform(1, 5), rng.choice([RED, YEL, ORG, (90, 90, 110)]), 1)
    if dmg >= 3:
        for _ in range(7):
            erase_blob(c, cx + rng.uniform(-3, 3) * u, base - rng.uniform(2.5, 9) * u, rng.uniform(0.8, 1.4) * u, rng)
    ground_cleanup(c)           # mảnh nào bị cắt rời khỏi chân thì bỏ (không để thân lơ lửng giữa không trung)
    if pilot:
        # khoang lái nứt toạc, lộ người điều khiển ngồi gục (tóc đen dài, áo trắng, vài vệt đỏ)
        px0, py0 = cx, base - 5.2 * u
        c.rect(px0 - 2.4 * u, py0 - 2.2 * u, px0 + 2.4 * u, py0 + 1.6 * u, (14, 10, 24))
        c.rect(px0 - 1.3 * u, py0 - 0.6 * u, px0 + 1.0 * u, py0 + 1.5 * u, COAT)
        c.rect(px0 - 1.2 * u, py0 - 1.8 * u, px0 + 1.1 * u, py0 - 0.4 * u, HAIR_BK)
        c.rect(px0 - 0.5 * u, py0 - 0.9 * u, px0 + 0.1 * u, py0 - 0.4 * u, SKIN_P)
        c.line(px0 - 1.3 * u, py0 - 1.2 * u, px0 - 1.7 * u, py0 + 1.4 * u, HAIR_BK, 1)
        for _ in range(6):
            c.px(px0 + rng.uniform(-1.4, 1.2) * u, py0 + rng.uniform(-0.6, 1.5) * u, RED)
    return c


def mech_lying(H, style, variant, seed, pilot=False):
    """
    Robot NẰM TRÊN MẶT ĐẤT (đầu bên trái, chân bên phải), cao H px khi đứng thì dài ~H px khi nằm. Mọi bộ phận đều chạm đất hoặc tựa vào phần chạm đất.
    variant "a": ngửa, một chân co gối, tay gập trên ngực · "b": gãy làm đôi (thân + đầu / hai chân tách ra, dây điện kéo lê) ·
    "c": đống sắt vụn (đầu lăn ra trước, một cánh tay chống lên) · "d": nằm nghiêng sấp, tay duỗi trước mặt.
    """
    rng = noise_rng(seed)
    body, body_d, body_l, glow = MECH_STYLES[style]
    u = H / 10.0
    W = int(10.6 * u) + 6
    Hh = int(4.6 * u) + 6
    c = Cv(W, Hh)
    ox, gy = 3, Hh - 3
    dead = (70, 74, 96) if style == "sci" else (52, 40, 84)         # đèn đã tắt

    def X(x):
        return ox + x * u

    def Y(h):
        return gy + 1 - h * u

    def Rr(x0, h0, x1, h1, col):
        c.rect(X(x0), Y(h1), X(x1) - 1, Y(h0) - 1, col)

    def Ln(x0, h0, x1, h1, col, w):
        c.line(X(x0), Y(h0), X(x1), Y(h1), col, max(1, int(w * u)))

    def P(pts, col):
        c.poly([(X(x), Y(h)) for x, h in pts], col)

    def head(hx):
        Rr(hx, 0.0, hx + 1.7, 1.9, body_l)
        Rr(hx, 0.0, hx + 0.35, 1.9, body)
        Rr(hx + 0.35, 0.8, hx + 1.4, 1.3, (10, 10, 22))
        Rr(hx + 0.55, 0.95, hx + 1.0, 1.15, dead)
        Rr(hx + 1.7, 0.0, hx + 1.95, 1.0, body_d)                   # cổ

    def leg_flat(x0, x1):
        Rr(x0, 0.0, x1, 1.25, body_d)
        Rr(x0, 1.0, x1, 1.25, body)
        Rr(x1 - 0.1, 0.0, x1 + 1.0, 0.95, body)

    if variant == "a":
        head(0.4)
        P([(2.3, 0.0), (2.3, 2.6), (3.0, 3.1), (6.2, 3.0), (6.4, 0.0)], body)       # thân ngửa
        Rr(2.3, 2.5, 6.3, 3.0, body_l)
        Rr(2.6, 0.0, 6.4, 0.5, body_d)
        Rr(3.5, 1.2, 5.3, 2.4, (12, 14, 30))                                         # khoang lái
        Rr(3.9, 1.5, 4.9, 2.1, dead)
        Rr(6.4, 0.0, 7.3, 1.7, body_d)                                               # hông
        leg_flat(7.3, 9.4)                                                           # chân duỗi
        Ln(7.3, 1.6, 8.5, 3.7, body_d, 0.85)                                         # chân kia co gối
        Ln(8.5, 3.7, 10.0, 0.6, body_d, 0.8)
        c.ell(X(8.2), Y(4.0), X(8.9), Y(3.3), body_l)                                # khớp gối
        Rr(9.6, 0.0, 10.5, 0.8, body)
        c.ell(X(2.1), Y(3.7), X(3.7), Y(2.4), body_l)                                # vai
        Ln(3.2, 3.0, 4.8, 3.9, body, 1.0)                                            # tay gập trên ngực
        Ln(4.8, 3.9, 6.3, 2.8, body, 1.0)
        Rr(6.1, 2.4, 7.0, 3.2, body_l)
        erase_blob(c, X(5.8), Y(1.4), 0.7 * u, rng)
        erase_blob(c, X(8.0), Y(0.9), 0.45 * u, rng)
    elif variant == "b":
        head(0.0)
        P([(2.0, 0.0), (2.2, 2.7), (2.9, 3.2), (5.4, 2.9), (5.7, 0.0)], body)       # thân + vai
        Rr(2.0, 2.4, 5.5, 3.0, body_l)
        Rr(2.2, 0.0, 5.7, 0.5, body_d)
        Rr(3.0, 1.1, 4.7, 2.3, (12, 14, 30))
        Rr(3.4, 1.4, 4.4, 2.0, dead)
        Ln(2.5, 0.5, 0.9, 0.4, body_d, 0.9)                                          # một tay vắt ra trước đầu
        Rr(0.0, 0.0, 1.0, 0.9, body_l)
        erase_blob(c, X(5.7), Y(1.2), 0.9 * u, rng)                                  # chỗ gãy ngang hông
        erase_blob(c, X(5.5), Y(2.7), 0.55 * u, rng)
        # nửa dưới tách ra phía phải, nằm riêng
        Rr(7.0, 0.0, 7.9, 1.5, body_d)
        leg_flat(7.9, 9.3)
        Ln(7.9, 1.3, 8.9, 2.7, body_d, 1.2)
        Ln(8.9, 2.7, 9.9, 0.6, body_d, 1.1)
        Rr(9.5, 0.0, 10.4, 0.7, body)
        # dây điện kéo lê giữa hai nửa (nằm sát đất)
        for k in range(3):
            col = (RED, YEL, ORG)[k]
            c.line(X(5.7), Y(0.2 + 0.25 * k), X(7.0), Y(0.15 + 0.2 * k), col, 1)
    elif variant == "c":
        # đống sắt vụn: thân vỡ vụn thành mảng thấp, đầu lăn ra trước, một cánh tay gãy chống lên
        head(0.0)
        P([(2.4, 0.0), (3.0, 1.6), (4.2, 2.4), (5.6, 2.0), (6.6, 1.1), (7.6, 0.0)], body)
        P([(3.4, 0.0), (4.2, 1.8), (5.4, 1.5), (6.2, 0.0)], body_d)
        Rr(2.4, 0.0, 7.6, 0.45, body_d)
        Ln(6.4, 1.1, 8.3, 3.3, body, 0.9)                                            # cánh tay chống lên
        Ln(8.3, 3.3, 9.5, 2.6, body, 0.9)
        Rr(9.3, 0.0, 9.9, 2.8, body_d)                                              # ... tựa vào mảnh chân dựng
        Rr(8.0, 0.0, 10.2, 0.9, body_d)
        c.ell(X(4.6), Y(2.9), X(6.2), Y(1.5), body_l)                                # vai lăn trên đống
        for _ in range(5):
            erase_blob(c, X(rng.uniform(2.8, 7.2)), Y(rng.uniform(0.7, 2.0)), rng.uniform(0.35, 0.6) * u, rng)
    else:
        # "d": nằm nghiêng sấp, một tay duỗi tới trước
        head(2.6)
        P([(4.6, 0.0), (4.7, 2.5), (5.6, 2.9), (8.0, 2.6), (8.2, 0.0)], body)
        Rr(4.7, 2.2, 8.0, 2.8, body_l)
        Rr(4.6, 0.0, 8.2, 0.45, body_d)
        Rr(5.3, 0.9, 7.2, 2.0, body_d)                                               # tấm lưng
        Rr(8.2, 0.0, 9.0, 1.6, body_d)
        Rr(9.0, 0.0, 10.4, 1.2, body_d)                                              # hai chân gập sát nhau
        Rr(9.9, 0.0, 10.6, 0.8, body)
        Ln(4.9, 1.0, 0.8, 0.5, body, 1.0)                                            # tay duỗi trước
        Rr(0.0, 0.0, 1.2, 1.0, body_l)
        c.ell(X(4.4), Y(3.3), X(6.0), Y(1.9), body_l)
        erase_blob(c, X(7.1), Y(1.7), 0.7 * u, rng)

    # hư hại chung: vết nứt, dây điện loà xoà, tia lửa nhỏ
    for _ in range(6):
        x0, h0 = rng.uniform(2.2, 8.0), rng.uniform(0.3, 2.3)
        c.line(X(x0), Y(h0), X(x0) + rng.uniform(-3, 3), Y(h0) + rng.uniform(-2, 2), (20, 18, 34), 1)
    for _ in range(4):
        c.px(X(rng.uniform(2.4, 7.8)), Y(rng.uniform(0.4, 2.6)), YEL if rng.random() < 0.6 else ORG)
    ground_cleanup(c)
    if pilot:
        # buồng lái nứt toạc phía trên thân: người điều khiển gục nửa người ra ngoài (tóc đen dài, áo trắng, vài vệt đỏ)
        px0 = X(4.2)
        top = Y(3.0) if variant == "a" else Y(2.9)
        c.rect(px0 - 1.3 * u, top - 0.3 * u, px0 + 1.4 * u, top + 0.5 * u, (14, 10, 24))
        c.rect(px0 - 1.0 * u, top - 0.9 * u, px0 + 0.9 * u, top + 0.2 * u, COAT)
        c.rect(px0 - 1.1 * u, top - 1.2 * u, px0 - 0.1 * u, top - 0.5 * u, HAIR_BK)
        c.line(px0 - 1.1 * u, top - 0.5 * u, px0 - 1.6 * u, top + 0.7 * u, HAIR_BK, 1)
        for _ in range(5):
            c.px(px0 + rng.uniform(-1.1, 1.0) * u, top + rng.uniform(-0.8, 0.3) * u, RED)
    return c


def mech_side(H, style):
    """Robot nhìn nghiêng sang PHẢI, đang lao tới: thân chúi, tay đưa trước, lửa phụt sau lưng."""
    body, body_d, body_l, glow = MECH_STYLES[style]
    u = H / 10.0
    W = int(H * 1.25)
    c = Cv(W, H + 6)
    ox, base = W * 0.42, H + 5

    def P(x, y):
        return (ox + x * u, base - (10 - y) * u)

    c.line(*P(-0.6, 6), *P(-2.6, 9.3), body_d, int(1.5 * u))        # chân sau duỗi
    c.rect(*P(-3.5, 9.3), *P(-1.6, 10), body)
    c.line(*P(0.3, 6), *P(1.9, 7.6), body_d, int(1.5 * u))          # chân trước co
    c.line(*P(1.9, 7.6), *P(1.2, 9.4), body_d, int(1.5 * u))
    c.rect(*P(0.8, 9.4), *P(2.4, 10), body)
    c.poly([P(-1.6, 3.6), P(0.8, 2.4), P(1.7, 5.6), P(-0.5, 6.4)], body)     # thân chúi
    c.poly([P(-1.6, 3.6), P(-0.4, 3.4), P(0.1, 6.0), P(-0.5, 6.4)], body_d)
    c.ell(*P(0.0, 2.7), *P(1.4, 4.1), body_l)
    c.line(*P(1.0, 3.2), *P(3.5, 3.9), body, int(1.3 * u))          # tay đưa trước
    c.rect(*P(3.4, 3.4), *P(4.5, 4.6), body_l)
    c.rect(*P(1.7, 1.4), *P(3.0, 2.8), body_l)                       # đầu chúi tới
    c.rect(*P(2.2, 1.9), *P(3.0, 2.3), glow)
    for k in range(7):                                               # lửa động cơ phía sau
        c.px(ox + (-2.0 - k * 0.45) * u, base - (10 - 4.2 - 0.2 * k) * u + (k % 2), FIRE_A if k < 3 else FIRE_B)
        c.px(ox + (-2.0 - k * 0.45) * u, base - (10 - 4.6 - 0.2 * k) * u, YEL if k < 2 else FIRE_B)
    return c


def mechs():
    mech_front(70, "sci", 0, 11).save("op_mech_xl_ok", pivot=(0.5, 0.0))
    mech_front(70, "sci", 1, 12).save("op_mech_xl_dent", pivot=(0.5, 0.0))
    # xác robot: NẰM trên mặt đất (không còn thân lơ lửng). wreck_a / wreck_b giữ tên cũ để Scene đã dựng tự nhận hình mới
    mech_lying(70, "sci", "a", 13).save("op_mech_xl_wreck_a", pivot=(0.5, 0.0))
    mech_lying(70, "sci", "b", 14).save("op_mech_xl_wreck_b", pivot=(0.5, 0.0))
    mech_lying(70, "sci", "c", 15).save("op_mech_xl_wreck_c", pivot=(0.5, 0.0))
    mech_lying(70, "sci", "d", 16).save("op_mech_xl_wreck_d", pivot=(0.5, 0.0))
    mech_side(70, "sci").save("op_mech_xl_dash", pivot=(0.35, 0.0))
    mech_front(42, "nan", 1, 21).save("op_mech_l_dent", pivot=(0.5, 0.0))
    mech_lying(42, "nan", "a", 22, pilot=True).save("op_mech_l_ruin", pivot=(0.5, 0.0))
    mech_lying(42, "nan", "c", 23).save("op_mech_l_wreck", pivot=(0.5, 0.0))


# ================================================================ XÁC, LỬA, KHÓI, MẢNH VỠ
def dead_scientists():
    for k in range(3):
        c = Cv(30, 12)
        rng = noise_rng(50 + k)
        c.rect(5, 4, 24, 10, COAT)                                  # thân áo choàng trắng nằm
        c.rect(5, 4, 24, 5, COAT_S)
        c.ell(0, 3, 6, 9, SKIN_A if k != 1 else SKIN_B)
        c.rect(0, 2, 3, 4, HAIR_BR if k != 2 else HAIR_BK)
        c.rect(24, 6, 28, 10, PANTS)
        if k == 0:
            c.rect(12, 0, 17, 4, COAT)                              # một tay gập lên
        elif k == 1:
            c.rect(14, 1, 20, 3, COAT_S)
            c.rect(24, 9, 29, 10, SHOE)
        else:
            c.line(8, 10, 12, 11, COAT, 2)
        for _ in range(8):
            c.px(rng.randint(5, 24), rng.randint(4, 10), RED_D if rng.random() < 0.6 else RED)
        c.save("op_sci_dead_%d" % k, pivot=(0.5, 0.0))


def fire_smoke_debris():
    for f in range(3):
        c = Cv(16, 24)
        rng = noise_rng(70 + f)
        for layer, (col, sc) in enumerate(((FIRE_B, 1.0), (FIRE_A, 0.7), (YEL, 0.45), ((255, 255, 210), 0.22))):
            hh = int(22 * sc * (0.85 + 0.15 * (f + 1) / 3))
            ww = 6.5 * sc
            pts = [(8 - ww, 23), (8 - ww * 0.7 + rng.uniform(-1, 1), 23 - hh * 0.5), (8 + rng.uniform(-2, 2), 23 - hh), (8 + ww * 0.7 + rng.uniform(-1, 1), 23 - hh * 0.5), (8 + ww, 23)]
            c.poly(pts, col)
        c.save("op_fire_%d" % f, pivot=(0.5, 0.0), outline=False)
    # khói
    c = Cv(28, 28)
    rng = noise_rng(8)
    for y in range(28):
        for x in range(28):
            d = math.hypot(x - 13.5, y - 13.5) / 13.5
            if d < 1 and rng.random() < (1 - d) ** 0.6 + 0.05:
                c.px(x, y, SMOKE + (150,))
    c.save("op_smoke", pivot=(0.5, 0.5), outline=False)
    # mảnh vỡ bê tông + cốt thép
    for k in range(3):
        c = Cv(18, 14)
        rng = noise_rng(90 + k)
        pts = [(rng.randint(0, 4), 13), (rng.randint(0, 5), rng.randint(4, 9)), (rng.randint(6, 12), rng.randint(0, 3)), (rng.randint(13, 17), rng.randint(5, 9)), (17, 13)]
        c.poly(pts, (84, 78, 104))
        c.line(pts[1][0], pts[1][1], pts[2][0], pts[2][1], (130, 122, 156))
        c.line(pts[0][0] + 2, 12, pts[3][0], 9, (60, 56, 78))
        if k != 1:
            c.line(rng.randint(5, 10), 2, rng.randint(12, 17), 0, (150, 100, 70))     # cốt thép nhô ra
        c.save("op_debris_%d" % k, pivot=(0.5, 0.0))


# ================================================================ TIA VẬT CHẤT ĐEN, SÉT, KHE NỨT KHÔNG GIAN
def bolt_points(x0, y0, x1, y1, rng, jag):
    pts = [(x0, y0)]
    n = max(6, int(abs(y1 - y0) / 7))
    for i in range(1, n):
        t = i / n
        pts.append((x0 + (x1 - x0) * t + rng.uniform(-jag, jag), y0 + (y1 - y0) * t))
    pts.append((x1, y1))
    return pts


def draw_bolt(c, pts, col_core, col_glow, width=1):
    for k in range(len(pts) - 1):
        c.line(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], col_glow, width + 2)
    for k in range(len(pts) - 1):
        c.line(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], col_core, width)


def lightning():
    # tia vật chất đen: mũi tên thon dài, lõi đen viền tím (Unity xoay quanh cỗ máy)
    c = Cv(96, 7)
    c.poly([(0, 3), (95, 0), (95, 6)], (4, 2, 10))
    c.poly([(0, 3), (95, 3)], VIO)
    c.line(0, 3, 95, 3, (130, 70, 220))
    c.line(10, 3, 80, 1, (50, 24, 96))
    c.save("op_ray", pivot=(0.0, 0.5), outline=False)
    # sét nhỏ (3 kiểu): lõi trắng, quầng tím
    for k in range(3):
        rng = noise_rng(300 + k)
        c = Cv(40, 110)
        pts = bolt_points(20, 0, 20 + rng.randint(-8, 8), 109, rng, 8)
        draw_bolt(c, pts, WHITE, (150, 110, 255), 1)
        for b in range(2):
            i = rng.randint(2, len(pts) - 4)
            bp = bolt_points(pts[i][0], pts[i][1], pts[i][0] + rng.choice([-1, 1]) * rng.randint(8, 16), pts[i][1] + rng.randint(24, 40), rng, 4)
            draw_bolt(c, bp, (230, 220, 255), (110, 80, 220), 1)
        c.save("op_bolt_%d" % k, pivot=(0.5, 1.0), outline=False)
    # tia sét khổng lồ phủ gần hết khung hình
    rng = noise_rng(777)
    c = Cv(FW, FH)
    main = bolt_points(236, 0, 244, FH - 1, rng, 16)
    draw_bolt(c, main, WHITE, (160, 120, 255), 5)
    for b in range(7):
        i = rng.randint(2, len(main) - 3)
        side = rng.choice([-1, 1])
        bp = bolt_points(main[i][0], main[i][1], main[i][0] + side * rng.randint(60, 150), main[i][1] + rng.randint(30, 80), rng, 8)
        draw_bolt(c, bp, WHITE, (140, 100, 240), 2)
    c.save("op_bolt_big", pivot=(0.5, 0.5), outline=False)
    # khe nứt không gian: lỗ hổng lởm chởm, bên trong là khoảng không đầy sao thuộc về thế giới khác
    for k in range(3):
        rng = noise_rng(400 + k)
        w, h = 28 + 8 * k, 56 + 10 * k
        c = Cv(w, h)
        left, right = [], []
        for i in range(0, h, 5):
            wd = math.sin(math.pi * i / h) * (w * 0.46)
            left.append((w / 2 - wd * rng.uniform(0.5, 1.0), i))
            right.append((w / 2 + wd * rng.uniform(0.5, 1.0), i))
        poly = left + right[::-1]
        c.poly(poly, (6, 2, 16))
        for _ in range(26):
            x, y = rng.randint(0, w - 1), rng.randint(0, h - 1)
            if c.im.getpixel((x, y))[3] > 0:
                c.px(x, y, rng.choice([WHITE, CYAN, MAG, (170, 130, 255)]))
        for _ in range(3):
            x, y = rng.randint(w // 4, w * 3 // 4), rng.randint(h // 4, h * 3 // 4)
            c.ell(x - 2, y - 2, x + 2, y + 2, (70, 20, 120))
        for k2 in range(len(poly)):
            a, b = poly[k2], poly[(k2 + 1) % len(poly)]
            c.line(a[0], a[1], b[0], b[1], (210, 170, 255), 1)
        c.save("op_rift_%d" % k, pivot=(0.5, 0.5), outline=False)


# ================================================================ HẦM ĐỔ NÁT (cảnh 5a / 5b / 5c / 6) — 960 x 210
HALL_W = 960


def paste(dst, src, x, y, flip=False):
    """Dán ảnh PIL RGBA src (góc trên trái tại x,y) lên Cv dst, có lật ngang."""
    if flip:
        src = src.transpose(Image.FLIP_LEFT_RIGHT)
    dst.im.alpha_composite(src, (int(x), int(y))) if (0 <= x and 0 <= y and x + src.width <= dst.w and y + src.height <= dst.h) else _paste_clip(dst, src, int(x), int(y))


def _paste_clip(dst, src, x, y):
    cropped = Image.new("RGBA", (dst.w, dst.h), (0, 0, 0, 0))
    cropped.paste(src, (x, y))
    dst.im.alpha_composite(cropped)


def jagged_top(c, x0, x1, ybase, htop, col, edge, rng, step=7, var=14):
    """Khối tường cao tới ybase-htop với đỉnh gãy lởm chởm (tường đổ nát)."""
    pts = [(x0, ybase)]
    x = x0
    while x < x1:
        pts.append((x, ybase - htop + rng.randint(-var, var)))
        x += rng.randint(step - 3, step + 4)
    pts.append((x1, ybase - htop + rng.randint(-var, var)))
    pts.append((x1, ybase))
    c.poly(pts, col)
    for k in range(1, len(pts) - 2):
        c.line(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], edge, 1)


def sky_gradient(c, h, top, mid, hor, mid_y):
    for y in range(h):
        if y < mid_y:
            col = lerp(top, mid, y / float(mid_y))
        else:
            col = lerp(mid, hor, min(1.0, (y - mid_y) / float(h - mid_y)))
        c.rect(0, y, c.w - 1, y, col)


def city_far(c, ybase, seed, burning):
    sk = G.skyline(c.w, 110, seed, (34, 20, 52), (120, 56, 100), [(255, 170, 60), (255, 90, 50), (200, 80, 160), (255, 220, 120)], 30, 104, 16, 38, 0.25)
    im = Image.fromarray(sk, "RGBA")
    rng = noise_rng(seed + 1)
    if burning:                                           # thành phố sụp đổ: tòa nhà gãy ngang, ngọn lửa + khói
        cv = Cv(c.w, 110)
        cv.im = im
        for _ in range(26):
            x = rng.randint(0, c.w - 1)
            erase_blob(cv, x, rng.randint(2, 40), rng.randint(6, 16), rng)
        for _ in range(60):
            x, y = rng.randint(0, c.w - 1), rng.randint(40, 108)
            if cv.im.getpixel((x, y))[3] > 0 and rng.random() < 0.6:
                cv.rect(x, y, x + rng.randint(1, 3), y + rng.randint(1, 3), rng.choice([FIRE_B, FIRE_A, FIRE_B]))
        im = cv.im
    c.im.alpha_composite(im, (0, ybase - 110))


def smoke_columns(c, xs, top, seed):
    rng = noise_rng(seed)
    for x in xs:
        for k in range(46):
            y = top + k * 3
            w = 6 + k * 0.45 + rng.uniform(-2, 2)
            a = int(max(20, 130 - k * 2.2))
            ox = x + math.sin(k * 0.3 + x) * 6 - k * 0.8
            for _ in range(int(w * 1.6)):
                px_ = ox + rng.uniform(-w, w)
                py_ = y + rng.uniform(-3, 3)
                xi, yi = int(px_), int(py_)
                if 0 <= xi < c.w - 1 and 0 <= yi < c.h - 1:
                    base = c.im.getpixel((xi, yi))
                    mix = lerp(base[:3], (22, 18, 28), a / 255.0)
                    c.rect(xi, yi, xi + 1, yi + 1, mix)


def rubble_floor(c, seed, y0=FLOOR_Y):
    rng = noise_rng(seed)
    for y in range(y0, FH):
        t = (y - y0) / float(FH - y0)
        c.rect(0, y, c.w - 1, y, lerp((44, 36, 52), (18, 14, 26), t))
    c.rect(0, y0, c.w - 1, y0, (120, 90, 120))
    for _ in range(int(c.w / 3)):
        x, y = rng.randint(0, c.w - 1), rng.randint(y0 + 2, FH - 1)
        c.rect(x, y, x + rng.randint(2, 6), y + rng.randint(1, 3), rng.choice([(70, 60, 84), (30, 26, 40), (96, 84, 110)]))
    for _ in range(int(c.w / 40)):                         # vết nứt
        x, y = rng.randint(0, c.w - 1), rng.randint(y0 + 2, FH - 6)
        for k in range(rng.randint(3, 6)):
            nx, ny = x + rng.randint(-8, 8), y + rng.randint(1, 5)
            c.line(x, y, nx, ny, (10, 8, 16), 1)
            x, y = nx, ny


def wall_detail(c, wall_col, rng):
    """Vẽ đường ghép tấm, ống dẫn, đèn báo hỏng lên những chỗ tường (đúng màu wall_col) còn sót lại."""
    def on_wall(x, y):
        return 0 <= x < c.w and 0 <= y < c.h and c.im.getpixel((x, y))[:3] == wall_col
    for x in range(10, c.w, 26):
        for y in range(40, FLOOR_Y):
            if on_wall(x, y):
                c.px(x, y, (12, 10, 30))
                if on_wall(x + 1, y):
                    c.px(x + 1, y, (44, 38, 86))
    for y in (92, 130, 160):
        for x in range(0, c.w):
            if on_wall(x, y):
                c.px(x, y, (12, 10, 30))
                if on_wall(x, y + 1):
                    c.px(x, y + 1, (44, 38, 86))
    for _ in range(int(c.w / 18)):                      # đèn báo, màn hình vỡ
        x, y = rng.randint(0, c.w - 1), rng.randint(70, FLOOR_Y - 4)
        if on_wall(x, y) and on_wall(x + 3, y + 2):
            c.rect(x, y, x + 3, y + 2, rng.choice([RED_D, (30, 120, 150), (80, 50, 140), (14, 12, 30)]))
    for _ in range(int(c.w / 60)):                      # cốt thép + ống thò ra
        x, y = rng.randint(0, c.w - 1), rng.randint(60, 150)
        if on_wall(x, y):
            c.line(x, y, x + rng.randint(-6, 6), y - rng.randint(6, 16), (120, 84, 70), 1)


def hall_bg(variant):
    c = Cv(HALL_W, FH)
    burning = variant == "b"
    sky_gradient(c, FH, (18, 8, 34), (92, 34, 70), (190, 92, 70) if burning else (150, 84, 82), 110)
    rng = noise_rng(5 if not burning else 6)
    for _ in range(80):                                     # sao + tàn lửa bay
        c.px(rng.randint(0, HALL_W - 1), rng.randint(0, 80), rng.choice([WHITE, (255, 200, 150), (200, 160, 255)]))
    city_far(c, 168, 31 if not burning else 32, burning)
    if burning:
        smoke_columns(c, [90, 210, 420, 560, 700, 830, 930], 20, 9)
    else:
        smoke_columns(c, [260, 700], 40, 9)
    # kết cấu hầm còn sót lại
    if not burning:
        # tường sau còn một nửa, trần gãy, nhiều khoảng trống thấy trời + thành phố
        jagged_top(c, 0, 250, FLOOR_Y, 112, (26, 22, 50), (90, 70, 150), rng, 9, 18)
        jagged_top(c, 250, 330, FLOOR_Y, 40, (26, 22, 50), (90, 70, 150), rng, 8, 8)
        jagged_top(c, 560, 700, FLOOR_Y, 84, (26, 22, 50), (90, 70, 150), rng, 9, 16)
        jagged_top(c, 840, 960, FLOOR_Y, 120, (26, 22, 50), (90, 70, 150), rng, 9, 16)
        # trần + dầm treo
        c.poly([(0, 0), (330, 0), (300, 12), (240, 26), (150, 34), (60, 24), (0, 40)], (14, 10, 30))
        c.poly([(560, 0), (960, 0), (960, 46), (880, 30), (800, 16), (680, 22)], (14, 10, 30))
        for x in (28, 120, 600, 780, 900):
            c.line(x, 20, x + rng.randint(-14, 14), rng.randint(50, 80), (60, 54, 96), 2)
    else:
        for (x0, x1, h) in ((0, 60, 100), (150, 190, 38), (300, 346, 62), (520, 566, 44), (700, 750, 30), (880, 960, 88)):
            jagged_top(c, x0, x1, FLOOR_Y, h, (26, 22, 50), (90, 70, 150), rng, 7, 12)
        for x in (40, 330, 760):
            c.line(x, 0, x + rng.randint(-30, 30), rng.randint(40, 90), (60, 54, 96), 2)
    wall_detail(c, (26, 22, 50), rng)
    rubble_floor(c, 15)
    # đống đổ nát lớn ở tiền cảnh
    for _ in range(7 if not burning else 13):
        x = rng.randint(0, HALL_W - 40)
        w, h = rng.randint(24, 70), rng.randint(8, 22)
        c.poly([(x, FH), (x + 6, FH - h * 0.6), (x + w * 0.4, FH - h), (x + w * 0.8, FH - h * 0.5), (x + w, FH)], (26, 22, 36))
        c.line(x + 6, FH - h * 0.6, x + w * 0.4, FH - h, (96, 84, 120), 1)
    save_img("op_hall_bg_" + variant, c.im)


# ================================================================ NGƯỜI THEO THỜI ĐẠI (đám đông cảnh 7b, tiến hóa, chiến tranh)
def era_person(kind):
    """Người nhìn thẳng 32x32 (chân y=31). Ba thời: cổ (rome_a/b), hiện đại (mod_a/b), tương lai (fut_a/b)."""
    c = Cv(32, 32)
    skin = {"rome_a": SKIN_A, "rome_b": SKIN_B, "mod_a": SKIN_B, "mod_b": SKIN_A, "fut_a": SKIN_C, "fut_b": (180, 190, 210)}[kind]
    c.ell(12, 3, 19, 11, skin)
    c.px(14, 7, INK)
    c.px(17, 7, INK)
    if kind == "rome_a":                                     # công dân La Mã: toga trắng viền đỏ + vòng nguyệt quế
        c.rect(12, 2, 19, 3, (90, 150, 60))
        c.px(11, 4, (90, 150, 60)); c.px(20, 4, (90, 150, 60))
        c.poly([(9, 12), (22, 12), (24, 29), (8, 29)], (232, 226, 206))
        c.line(10, 12, 22, 24, (170, 40, 50), 2)
        c.rect(10, 29, 21, 30, (150, 120, 84)); c.rect(11, 31, 14, 31, SHOE); c.rect(17, 31, 20, 31, SHOE)
        c.rect(7, 14, 9, 22, skin); c.rect(22, 14, 24, 22, (232, 226, 206))
    elif kind == "rome_b":                                   # lính lê dương: mũ lông đỏ, giáp đồng, váy, khiên
        c.rect(11, 2, 20, 4, (170, 120, 50))
        c.rect(14, 0, 17, 2, (190, 40, 40))
        c.rect(10, 12, 21, 22, (176, 128, 60))
        c.rect(10, 12, 21, 13, (220, 170, 80))
        c.rect(11, 22, 20, 26, (170, 50, 50))
        c.rect(12, 27, 14, 30, skin); c.rect(17, 27, 19, 30, skin)
        c.rect(11, 31, 14, 31, (90, 60, 40)); c.rect(17, 31, 20, 31, (90, 60, 40))
        c.rect(4, 14, 9, 25, (180, 40, 40)); c.rect(5, 15, 8, 24, (230, 190, 70))     # khiên
    elif kind == "mod_a":                                    # thường dân hiện đại: áo khoác đỏ + quần jean
        c.rect(12, 2, 19, 4, HAIR_BR)
        c.rect(10, 12, 21, 24, (200, 50, 60))
        c.rect(10, 12, 11, 24, (150, 30, 44))
        c.rect(12, 25, 14, 30, (60, 90, 170)); c.rect(17, 25, 19, 30, (60, 90, 170))
        c.rect(11, 31, 14, 31, WHITE); c.rect(17, 31, 20, 31, WHITE)
        c.rect(8, 13, 9, 23, (200, 50, 60)); c.rect(22, 13, 23, 23, (200, 50, 60))
    elif kind == "mod_b":                                    # nhân viên văn phòng: vest xám + cà vạt
        c.rect(12, 2, 19, 3, HAIR_BK)
        c.rect(10, 12, 21, 25, (84, 90, 110))
        c.rect(15, 12, 16, 22, (220, 220, 230))
        c.rect(15, 14, 16, 20, (200, 60, 60))
        c.rect(12, 26, 14, 30, (40, 42, 56)); c.rect(17, 26, 19, 30, (40, 42, 56))
        c.rect(11, 31, 14, 31, SHOE); c.rect(17, 31, 20, 31, SHOE)
        c.rect(8, 13, 9, 23, (84, 90, 110)); c.rect(22, 13, 23, 23, (84, 90, 110))
        c.rect(24, 22, 28, 26, (110, 70, 40))               # cặp táp
    elif kind == "fut_a":                                    # đồ bó neon + kính ngắm
        c.rect(12, 2, 19, 4, MAG)
        c.rect(12, 6, 19, 7, CYAN)
        c.rect(10, 12, 21, 25, (20, 18, 46))
        c.rect(10, 12, 10, 25, CYAN); c.rect(21, 12, 21, 25, MAG); c.rect(11, 18, 20, 18, CYAN)
        c.rect(12, 26, 14, 30, (20, 18, 46)); c.rect(17, 26, 19, 30, (20, 18, 46))
        c.rect(12, 26, 12, 30, MAG); c.rect(19, 26, 19, 30, CYAN)
        c.rect(11, 31, 14, 31, CYAN); c.rect(17, 31, 20, 31, MAG)
        c.rect(8, 13, 9, 23, (20, 18, 46)); c.rect(22, 13, 23, 23, (20, 18, 46))
        c.px(8, 17, MAG); c.px(23, 17, CYAN)
    else:                                                    # người cải tạo: giáp bạc + khớp phát sáng
        c.rect(11, 2, 20, 5, STEEL_L)
        c.rect(14, 6, 19, 7, LIME)
        c.rect(10, 12, 21, 25, STEEL)
        c.rect(10, 12, 21, 13, STEEL_L)
        c.rect(14, 15, 17, 18, LIME)
        c.rect(12, 26, 14, 30, STEEL_D); c.rect(17, 26, 19, 30, STEEL_D)
        c.rect(11, 31, 14, 31, STEEL_L); c.rect(17, 31, 20, 31, STEEL_L)
        c.rect(7, 13, 9, 23, STEEL_L); c.rect(22, 13, 24, 23, STEEL)
        c.px(8, 24, LIME); c.px(23, 24, LIME)
    return c


ERA_KINDS = ["rome_a", "rome_b", "mod_a", "mod_b", "fut_a", "fut_b"]


def tint(im, k):
    """Làm tối ảnh theo hệ số k (giữ alpha) — dùng cho các hàng người phía sau."""
    a = np.array(im).astype(np.float32)
    a[..., :3] *= k
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def crowd_bg():
    c = Cv(FW, FH)
    sky_gradient(c, FH, (22, 14, 40), (70, 40, 80), (140, 86, 90), 100)
    rng = noise_rng(61)
    # bóng phế tích xa
    jagged_top(c, 0, FW, 120, 60, (34, 26, 52), (70, 54, 100), rng, 10, 18)
    sprites = {k: era_person(k) for k in ERA_KINDS}
    ims = {k: sprites[k].im.copy() for k in ERA_KINDS}
    # 6 hàng người, hàng sau nhỏ vị trí cao + tối hơn, hàng trước thấp + sáng
    rows = [(112, 0.55), (126, 0.65), (140, 0.74), (154, 0.84), (170, 0.93), (186, 1.0), (202, 1.0)]
    for (yb, br) in rows:
        x = -10 + rng.randint(0, 6)
        while x < FW:
            k = rng.choice(ERA_KINDS)
            img = tint(ims[k], br)
            if rng.random() < 0.5:
                img = img.transpose(Image.FLIP_LEFT_RIGHT)
            dy = rng.randint(-2, 2)
            paste(c, img, x, yb - 31 + dy)
            x += rng.randint(11, 15)
        # một nền tối dưới hàng trước để người không "lơ lửng"
    save_img("op_s7b_crowd", c.im)


# ================================================================ PHẾ TÍCH LA MÃ LẪN NHÀ CHỌC TRỜI (cảnh 7a)
STONE = (176, 150, 112)
STONE_D = (110, 90, 70)
STONE_L = (222, 200, 160)


def column(c, x, ybase, h, w=9, broken=False, rng=None):
    c.rect(x, ybase - h, x + w, ybase, STONE)
    for k in range(1, w, 2):
        c.rect(x + k, ybase - h, x + k, ybase, STONE_D)
    c.rect(x - 2, ybase - 4, x + w + 2, ybase, STONE_L)
    if not broken:
        c.rect(x - 2, ybase - h - 4, x + w + 2, ybase - h, STONE_L)
        c.rect(x - 4, ybase - h - 7, x + w + 4, ybase - h - 5, STONE)
    else:
        pts = [(x, ybase - h), (x + 3, ybase - h - 5), (x + 5, ybase - h - 2), (x + 9, ybase - h - 6), (x + w, ybase - h)]
        c.poly(pts, STONE)


def arch_wall(c, x0, ybase, w, h, rows, rng, broken=False):
    """Tường vòm kiểu đấu trường La Mã; broken = đỉnh gãy bậc thang, dốc xuống về phía phải."""
    tops = []
    for x in range(x0, x0 + w + 1):
        t = (x - x0) / float(max(1, w))
        drop = (t * t * h * 0.55 + rng.randint(0, 3)) if broken else 0
        tops.append(int(ybase - h + drop))
    for k, x in enumerate(range(x0, x0 + w + 1)):
        c.rect(x, tops[k], x, ybase, STONE)
        c.px(x, tops[k], STONE_L)
    rh = h // rows
    for r in range(rows):
        yy = ybase - h + r * rh + 6
        for x in range(x0 + 8, x0 + w - 12, 22):
            k = min(len(tops) - 1, x - x0 + 6)
            if tops[k] < yy - 2:
                c.rect(x, yy + 6, x + 13, yy + rh - 4, (24, 16, 24))
                c.ell(x, yy, x + 13, yy + 12, (24, 16, 24))
        c.rect(x0, yy + rh - 4, x0 + w, yy + rh - 2, STONE_D)
    for k, x in enumerate(range(x0, x0 + w + 1)):      # trả lại màu đá cho phần trên dải gờ bị lỡ
        c.px(x, tops[k], STONE_L)


def flying_cars():
    for f in range(2):
        c = Cv(26, 11)
        c.poly([(1, 7), (4, 3), (16, 2), (23, 5), (25, 8), (3, 9)], (60, 70, 120))
        c.poly([(5, 3), (14, 2), (17, 5), (6, 5)], (150, 220, 255))
        c.rect(2, 8, 24, 9, (200, 60, 200) if f else CYAN)
        c.px(23, 6, YEL)
        c.rect(6, 10, 9, 10, CYAN if f else MAG)
        c.rect(16, 10, 19, 10, MAG if f else CYAN)
        c.save("op_car_%d" % f, pivot=(0.5, 0.5))


def ruins_mix_bg():
    """Cảnh 7: mọi thời đại bị dồn vào cùng một chỗ — nhà chọc trời chen chúc, xô đẩy, nghiêng ngả, gãy đổ chồng lên nhau,
    lẫn đấu trường / cột / cổng vòm La Mã vỡ và mái vòm tương lai bị kẹt giữa các tòa nhà."""
    c = Cv(FW, FH)
    rng = noise_rng(71)
    anchors = []
    sky_gradient(c, FH, (24, 18, 44), (96, 52, 86), (210, 130, 100), 120)
    for _ in range(40):
        c.px(rng.randint(0, FW - 1), rng.randint(0, 60), rng.choice([WHITE, (255, 220, 200)]))
    city_far(c, 150, 83, False)
    # --- phế tích La Mã phía xa (mờ khói): đấu trường vỡ + hàng vòm, lồi ra giữa các tòa nhà
    far = Cv(FW, FH)
    arch_wall(far, 118, 178, 140, 64, 2, rng, broken=True)
    arch_wall(far, 300, 178, 120, 52, 1, rng, broken=True)
    c.im.alpha_composite(shade(far.im, (80, 44, 90), 0.5))
    # --- nhà chọc trời chen chúc: lớp xa (mảnh, mờ) rồi lớp gần (to, sát rạt nhau)
    wreck_layer(c, rng, False, anchors, width=FW, gap=(13, 24))
    wreck_layer(c, rng, True, anchors, width=FW, gap=(14, 24))
    # --- từng cặp nhà xô vào nhau: tựa đầu, đỉnh chồng chéo
    for x in (30, 118, 212, 300, 392, 462):
        place_tower(c, x, FLOOR_Y + 4, rng.randint(30, 40), rng.randint(104, 138), -rng.uniform(14, 26), rng, "plain", anchors)
        place_tower(c, x + rng.randint(26, 34), FLOOR_Y + 4, rng.randint(28, 38), rng.randint(96, 130), rng.uniform(14, 26), rng, "broken" if rng.random() < 0.4 else "plain", anchors)
    # nhà cao ngã đè lên nhà khác (gần nằm ngang, gác lên đỉnh các tòa bên dưới)
    for (bx_, by_, ln, th, dd) in ((120, 92, 112, 26, 1), (300, 84, 120, 28, -1), (430, 96, 96, 26, 1)):
        bridge_tower(c, bx_, by_, ln, th, dd, rng)
    # --- mái vòm / tháp cong của nhà tương lai bị kẹt giữa các tòa
    for (x, w, h) in ((176, 36, 36), (344, 40, 44), (252, 28, 54), (448, 32, 38)):
        c.ell(x, 176 - h, x + w, 176 + h * 0.2, (50, 60, 110))
        c.ell(x + 4, 176 - h + 4, x + w - 4, 176 - h + h * 0.7, (30, 40, 80))
        c.line(x + 3, 176 - h * 0.6, x + w - 3, 176 - h * 0.6, CYAN, 1)
        c.px(x + w / 2, 176 - h - 1, MAG)
    # --- La Mã ở tiền cảnh: đấu trường vỡ (trái) + cột đứng / gãy + cổng vòm xen giữa
    arch_wall(c, 0, 180, 128, 74, 2, rng, broken=True)
    for (x, h, br) in ((140, 66, False), (158, 36, True), (386, 58, True), (404, 44, False)):
        column(c, x, 180, h, 8, br, rng)
    arch_wall(c, 330, 180, 52, 50, 1, rng, broken=True)
    dust_puffs(c, [(x_, FLOOR_Y - 8, 44, 16) for x_ in range(16, FW, 62)], rng)
    rubble_floor(c, 77)
    # --- đống gạch, cột đổ nằm ngang, nhà đổ dài trên mặt đất, dãy nhà đổ như domino
    for (hx, hw, hh) in ((14, 56, 18), (176, 60, 14), (270, 52, 16), (430, 48, 14)):
        rubble_heap(c, hx, hw, hh, rng)
    for (lx, ln, th, dd) in ((118, 88, 24, 1), (330, 84, 22, -1), (470, 70, 22, -1)):
        lying_tower(c, lx, ln, th, dd, rng, anchors)
    for (cx_, cl) in ((196, 62), (372, 52)):                                       # cột đá đổ nằm ngang, gãy đôi
        col = Cv(cl, 11)
        col.rect(0, 1, cl - 1, 9, STONE)
        for k in range(2, cl - 2, 4):
            col.rect(k, 1, k, 9, STONE_D)
        col.rect(0, 0, 3, 10, STONE_L)
        col.rect(cl - 4, 0, cl - 1, 10, STONE_L)
        erase_blob(col, cl * rng.uniform(0.4, 0.6), 5, 5, rng)
        paste(c, pad_outline(col.im, (20, 14, 22)), cx_, FLOOR_Y + rng.randint(2, 6))
    dust_puffs(c, [(x_, FLOOR_Y + 2, 36, 11) for x_ in range(30, FW, 80)], rng, alpha=56)
    # vài đám cháy nhỏ ở chỗ nhà gãy
    fp = os.path.join(G.OUT_DIR, "op_fire_0.png")
    if os.path.exists(fp):
        fires = [Image.open(os.path.join(G.OUT_DIR, "op_fire_%d.png" % k)).convert("RGBA") for k in range(3)]
        for (ax_, ay_) in anchors[::3]:
            if rng.random() < 0.3:
                paste(c, fires[rng.randint(0, 2)], int(ax_) - 8, int(ay_) - 22)
    # tiền cảnh: đống đổ nát + gai thép
    for _ in range(14):
        x = rng.randint(0, FW - 30)
        w, h = rng.randint(16, 50), rng.randint(6, 18)
        c.poly([(x, FH), (x + 5, FH - h * 0.6), (x + w * 0.4, FH - h), (x + w * 0.85, FH - h * 0.4), (x + w, FH)], rng.choice([(70, 56, 64), (30, 24, 40), (110, 92, 84)]))
    save_img("op_s7a_bg", c.im)


# ================================================================ CHIẾN TRANH (cảnh 8) — 1280 x 210
WAR_W = 1280
ARMOR = (150, 156, 176)


def fighter(kind, pose):
    """Chiến binh nhìn nghiêng sang PHẢI, 32x32. kind: rome | knight | soldier | neon. pose: stand | strike."""
    c = Cv(32, 32)
    cx = 14
    st = 2 if pose == "strike" else 0
    # chân
    leg = {"rome": SKIN_A, "knight": ARMOR, "soldier": (70, 84, 56), "neon": (24, 22, 54)}[kind]
    c.line(cx, 21, cx - 4 - st, 30, leg, 3)
    c.line(cx, 21, cx + 4 + st, 30, leg, 3)
    c.rect(cx - 7 - st, 30, cx - 2 - st, 31, SHOE)
    c.rect(cx + 2 + st, 30, cx + 7 + st, 31, SHOE)
    # thân + đầu
    if kind == "rome":
        c.rect(cx - 4, 11, cx + 4, 21, (176, 128, 60))
        c.rect(cx - 4, 11, cx + 4, 12, (230, 180, 90))
        c.rect(cx - 4, 21, cx + 4, 24, (180, 44, 44))
        c.ell(cx - 3, 2, cx + 4, 10, SKIN_A)
        c.rect(cx - 4, 1, cx + 4, 5, (176, 128, 60))
        c.rect(cx - 2, -1, cx + 3, 1, (200, 40, 40))
        c.rect(cx - 9, 12, cx - 4, 22, (190, 40, 40)); c.rect(cx - 8, 13, cx - 5, 21, (232, 190, 70))   # khiên tròn
        c.line(cx + 3, 15, cx + 9 + st * 2, 13 - st, (176, 128, 60), 2)
        c.line(cx + 9 + st * 2, 17, cx + 28, 10 - st * 2, (150, 120, 90), 1)                           # giáo
        c.poly([(cx + 28, 8 - st * 2), (cx + 31, 10 - st * 2), (cx + 28, 12 - st * 2)], STEEL_L)
    elif kind == "knight":
        c.rect(cx - 4, 11, cx + 4, 22, ARMOR)
        c.rect(cx - 4, 11, cx - 3, 22, STEEL_D)
        c.rect(cx - 4, 16, cx + 4, 17, (180, 40, 40))
        c.rect(cx - 4, 2, cx + 4, 10, ARMOR)
        c.rect(cx + 1, 5, cx + 4, 6, INK)
        c.rect(cx - 1, -1, cx + 1, 2, (200, 40, 40))
        c.poly([(cx - 10, 12), (cx - 3, 12), (cx - 3, 20), (cx - 6, 24), (cx - 10, 20)], (50, 80, 170))   # khiên
        c.line(cx - 8, 14, cx - 5, 14, WHITE)
        c.line(cx + 3, 15, cx + 10 + st * 2, 12 - st * 2, ARMOR, 2)
        c.line(cx + 10 + st * 2, 12 - st * 2, cx + 24 + st * 3, 4 - st * 2, STEEL_L, 2)               # kiếm
        c.px(cx + 10 + st * 2, 13 - st * 2, (150, 110, 40))
    elif kind == "soldier":
        c.rect(cx - 4, 11, cx + 4, 22, (84, 100, 66))
        c.rect(cx - 4, 11, cx - 3, 22, (56, 70, 44))
        c.rect(cx - 3, 14, cx + 3, 19, (60, 72, 46))
        c.ell(cx - 3, 3, cx + 4, 10, SKIN_B)
        c.rect(cx - 4, 1, cx + 4, 5, (70, 82, 54))
        c.line(cx + 3, 14, cx + 12, 15 - st, (84, 100, 66), 2)
        c.line(cx + 6, 17, cx + 26, 13 - st, (24, 24, 30), 2)                                          # súng trường
        if pose == "strike":
            c.rect(cx + 27, 11, cx + 31, 15, YEL); c.px(cx + 29, 13, WHITE)
    else:
        c.rect(cx - 4, 11, cx + 4, 22, (24, 22, 54))
        c.line(cx - 4, 11, cx - 4, 22, CYAN); c.line(cx + 4, 11, cx + 4, 22, MAG); c.line(cx - 4, 17, cx + 4, 17, CYAN)
        c.ell(cx - 3, 3, cx + 4, 10, (180, 190, 210))
        c.rect(cx - 4, 1, cx + 4, 4, MAG)
        c.rect(cx + 1, 6, cx + 4, 7, CYAN)
        c.line(cx + 3, 14, cx + 11, 15 - st, (24, 22, 54), 2)
        c.line(cx + 6, 17, cx + 22, 14 - st, (60, 60, 100), 2)                                         # súng laser
        c.rect(cx + 22, 13 - st, cx + 24, 15 - st, CYAN)
        if pose == "strike":
            c.line(cx + 25, 14 - st, cx + 31, 14 - st, WHITE, 1); c.line(cx + 25, 15 - st, cx + 31, 15 - st, CYAN, 1)
    return c


def fallen(kind):
    """Xác chiến binh nằm: xoay 90 độ + vài vệt đỏ."""
    f = fighter(kind, "stand")
    f.outline()
    im = f.im.rotate(90, expand=True).crop((0, 8, 32, 32))
    cv = Cv(im.width, im.height)
    cv.im = im
    rng = noise_rng(sum(ord(ch) * (i + 3) for i, ch in enumerate(kind)))   # ổn định giữa các lần chạy (hash() của Python thì không)
    for _ in range(7):
        x, y = rng.randint(2, 28), rng.randint(8, 22)
        if cv.im.getpixel((x, y))[3] > 0:
            cv.px(x, y, RED if rng.random() < 0.5 else RED_D)
    return cv.im


# ---------------------------------------------------------------- thành phố đổ nát (cảnh 8)
# Nhà chọc trời nghiêng ngả, gãy đôi, đổ nằm ngang, dựa vào nhau như quân domino — mọi tòa đều là một ảnh riêng
# (thân + cửa sổ + viền neon) rồi xoay / bẻ gãy / dán chồng lên nhau.
TOWER_BODY = [(30, 22, 52), (38, 26, 58), (26, 26, 56), (44, 28, 54), (24, 20, 46)]
TOWER_EDGE = [(150, 64, 140), (70, 130, 170), (170, 84, 96), (110, 80, 190)]
WIN_LIT = [(255, 200, 90), (255, 150, 60), (120, 220, 255), (255, 120, 200)]
REBAR = (136, 94, 76)


def tower_raw(w, h, seed):
    """Tòa nhà nhìn thẳng w x h (đỉnh y=0, chân y=h-1): thân, viền neon, các tầng, cửa sổ sáng / tối / cháy."""
    rng = noise_rng(seed)
    body = rng.choice(TOWER_BODY)
    edge = rng.choice(TOWER_EDGE)
    c = Cv(w, h)
    c.rect(0, 0, w - 1, h - 1, body)
    c.rect(0, 0, 1, h - 1, lerp(body, edge, 0.55))                  # cạnh sáng bên trái
    c.rect(w - 2, 0, w - 1, h - 1, lerp(body, INK, 0.5))            # cạnh tối bên phải
    c.rect(0, 0, w - 1, 0, edge)
    for y in range(6, h - 3, 7):
        c.rect(2, y - 2, w - 3, y - 2, lerp(body, INK, 0.35))       # đường sàn
        for x in range(4, w - 5, 6):
            r = rng.random()
            if r < 0.24:
                col = rng.choice(WIN_LIT)
            elif r < 0.30:
                col = FIRE_B
            elif r < 0.55:
                col = (10, 8, 20)
            else:
                col = lerp(body, (70, 70, 120), 0.35)
            c.rect(x, y, x + 2, y + 3, col)
    for _ in range(rng.choice([0, 1, 1, 2, 3])):                    # vết nổ: khoét thủng thân nhà, lộ lỗ hổng răng cưa
        erase_blob(c, rng.choice([rng.randint(0, 3), rng.randint(w - 4, w), rng.randint(0, w)]), rng.randint(0, int(h * 0.7)), rng.randint(4, max(5, min(9, w // 3))), rng)
    return c.im


def pad_outline(im, col=(10, 6, 20)):
    """Thêm viền tối 1 px quanh ảnh (tách các tòa nhà chồng lên nhau)."""
    cv = Cv(im.width + 2, im.height + 2)
    cv.im.paste(im, (1, 1))
    cv.outline(col)
    return cv.im


def split_tower(im, rng, y0, slope):
    """Bẻ gãy tòa nhà theo đường răng cưa bắt đầu ở độ cao y0, dốc 'slope'. Trả về (phần dưới còn đứng, phần trên bị bẻ, đã cắt sát)."""
    w, h = im.size
    pts = []
    x, y = 0, y0
    while x < w:
        pts.append((x, min(h - 4, max(4, y))))
        x += rng.randint(2, 6)
        y += slope * 4 + rng.randint(-6, 6)
    pts.append((w, min(h - 4, max(4, y))))
    up = Image.new("L", (w, h), 0)
    ImageDraw.Draw(up).polygon([(0, -1)] + pts + [(w, -1)], fill=255)
    top = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    top.paste(im, (0, 0), up)
    arr = np.array(im)
    arr[np.array(up) > 0] = 0
    bottom = Image.fromarray(arr, "RGBA")
    cb, ct = Cv(w, h), Cv(w, h)
    cb.im, ct.im = bottom, top
    cb.d, ct.d = ImageDraw.Draw(bottom), ImageDraw.Draw(top)
    for i in range(len(pts) - 1):                                   # mặt gãy: bê tông tối + thép chìa ra
        cb.line(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], (14, 10, 26), 3)
        ct.line(pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], (14, 10, 26), 3)
    for (px_, py_) in pts[1:-1:2]:
        cb.line(px_, py_, px_ + rng.randint(-2, 2), py_ - rng.randint(3, 9), REBAR, 1)
        ct.line(px_, py_, px_ + rng.randint(-2, 2), py_ + rng.randint(3, 8), REBAR, 1)
    box = top.getbbox()
    return cb.im, (top.crop(box) if box else top)


def rot_paste_base(c, im, bx, by, ang):
    """Xoay ảnh quanh điểm giữa đáy rồi dán sao cho điểm đó rơi vào (bx, by). ang > 0 = ngả sang TRÁI."""
    r = im.rotate(ang, resample=Image.NEAREST, expand=True)
    th = math.radians(ang)
    h = im.height
    paste(c, r, round(bx - (h / 2.0) * math.sin(th) - r.width / 2.0), round(by - (h / 2.0) * math.cos(th) - r.height / 2.0))


def rot_paste_center(c, im, cx, cy, ang):
    r = im.rotate(ang, resample=Image.NEAREST, expand=True)
    paste(c, r, round(cx - r.width / 2.0), round(cy - r.height / 2.0))


def shade(im, toward, k):
    """Pha màu ảnh về phía 'toward' với tỉ lệ k (khói bụi, xa hơn) — giữ alpha."""
    a = np.array(im).astype(np.float32)
    for i in range(3):
        a[..., i] = a[..., i] * (1 - k) + toward[i] * k
    return Image.fromarray(a.astype(np.uint8), "RGBA")


def dust_puffs(c, items, rng, col=(120, 96, 120), alpha=70):
    """Bụi mờ: items = [(cx, cy, rx, ry)] — vài elip bán trong suốt chồng nhau."""
    ov = Image.new("RGBA", (c.w, c.h), (0, 0, 0, 0))
    d = ImageDraw.Draw(ov)
    for (cx, cy, rx, ry) in items:
        for _ in range(5):
            ox, oy = rng.uniform(-rx * 0.5, rx * 0.5), rng.uniform(-ry * 0.4, ry * 0.4)
            sx, sy = rx * rng.uniform(0.5, 0.9), ry * rng.uniform(0.5, 0.9)
            d.ellipse([int(cx + ox - sx), int(cy + oy - sy), int(cx + ox + sx), int(cy + oy + sy)], fill=(col[0], col[1], col[2], alpha))
    c.im.alpha_composite(ov)


def place_tower(c, bx, by, w, h, ang, rng, mode, anchors, haze=0.0):
    """Dựng một tòa nhà có chân ở (bx, by) ngả ang độ. mode 'broken' = gãy ngang giữa thân, nửa trên đổ nghiêng quanh điểm gãy."""
    raw = tower_raw(w, h, rng.randint(0, 10 ** 6))
    if mode == "broken":
        cut = rng.uniform(0.3, 0.62) * h
        bottom, top = split_tower(raw, rng, cut, rng.uniform(-0.5, 0.5))
        bottom, top = pad_outline(bottom), pad_outline(top)
        if haze:
            bottom, top = shade(bottom, (70, 40, 84), haze), shade(top, (70, 40, 84), haze)
        rot_paste_base(c, bottom, bx, by, ang)
        bh = h - cut
        th = math.radians(ang)
        hx, hy = bx - bh * math.sin(th), by - bh * math.cos(th)    # bản lề: chỗ gãy
        d = 1 if rng.random() < 0.5 else -1
        a1 = ang + d * rng.uniform(32, 88)
        rot_paste_base(c, top, hx - d * rng.uniform(0, 7), hy + rng.uniform(-1, 7), a1)
        anchors.append((hx, hy))
    else:
        im = pad_outline(raw)
        if haze:
            im = shade(im, (70, 40, 84), haze)
        rot_paste_base(c, im, bx, by, ang)


def wreck_layer(c, rng, near, anchors, width=None, gap=None):
    """Một lớp nhà đổ nát: đứng thẳng / nghiêng / nghiêng nặng / gãy đôi, tựa vào nhau (chân đặt dưới mặt sàn để sàn che đi).
    width: bề ngang cần phủ (mặc định ảnh chiến tranh); gap: (nhỏ nhất, lớn nhất) khoảng cách giữa 2 tòa — nhỏ = chen chúc."""
    d = rng.choice([-1, 1])
    x = -12
    while x < (width or WAR_W) + 20:
        w = rng.randint(28, 46) if near else rng.randint(20, 34)
        h = rng.randint(86, 132) if near else rng.randint(56, 100)
        mode = rng.choice(["stand", "lean", "lean", "heavy", "broken", "broken", "bl"])
        if rng.random() < 0.55:
            d = -d
        if mode == "stand":
            ang, m = rng.uniform(-4, 4), "plain"
        elif mode == "lean":
            ang, m = d * rng.uniform(9, 26), "plain"
        elif mode == "heavy":
            ang, m, h = d * rng.uniform(32, 56), "plain", int(h * 0.8)
        elif mode == "broken":
            ang, m = rng.uniform(-8, 8), "broken"
        else:                                                        # bl: vừa nghiêng vừa gãy
            ang, m = d * rng.uniform(14, 30), "broken"
        by = FLOOR_Y + (4 if near else 2)
        place_tower(c, x, by, w, h, ang, rng, m, anchors, haze=0.0 if near else 0.42)
        if gap:
            x += rng.randint(gap[0], gap[1])
        else:
            x += rng.randint(20, 36) if near else rng.randint(24, 40)


def domino_run(c, x0, d, rng, anchors, n=6):
    """Dãy nhà đổ như quân domino (đổ về phía d: +1 = trái): mỗi tòa nghiêng hơn tòa trước, tòa cuối gần nằm ngang."""
    for i in range(n):
        w, h = rng.randint(30, 40), rng.randint(96, 124) - i * 6
        place_tower(c, x0 - d * i * 24, FLOOR_Y + 5, w, h, d * (4 + i * 14), rng, "plain", anchors)


def bridge_tower(c, cx, cy, length, thick, d, rng):
    """Tòa nhà đổ vắt ngang như cây cầu, gần như nằm ngang, gác lên các tòa khác (ở trên đầu đám đánh nhau); đỉnh bị bẻ rơi lệch bên cạnh."""
    raw = tower_raw(thick, length, rng.randint(0, 10 ** 6))
    bottom, top = split_tower(raw, rng, length * rng.uniform(0.62, 0.8), rng.uniform(-0.4, 0.4))
    rot_paste_center(c, pad_outline(bottom), cx, cy, d * rng.uniform(68, 82))
    rot_paste_center(c, pad_outline(top), cx - d * length * 0.5 - 6 * d, cy + rng.randint(8, 20), d * rng.uniform(52, 100))


def lying_tower(c, cx, length, thick, d, rng, anchors):
    """Tòa nhà đổ nằm ngang trên mặt đất, gãy thành 2-3 khúc lệch nhau."""
    raw = tower_raw(thick, length, rng.randint(0, 10 ** 6))
    pieces = []
    rest = raw
    cuts = rng.choice([1, 2])
    for k in range(cuts):
        keep, top = split_tower(rest, rng, rest.height * 0.5, rng.uniform(-0.5, 0.5))
        pieces.append(top)                                          # phần đỉnh
        box = keep.getbbox()
        rest = keep.crop(box) if box else keep
    pieces.append(rest)                                             # phần chân
    # đỉnh nằm phía d: xếp từ đỉnh (xa) -> chân (gần)
    x = cx - length * 0.5 * d
    for i, p in enumerate(pieces):
        r = pad_outline(p).rotate(d * 90 + rng.uniform(-10, 10), resample=Image.NEAREST, expand=True)
        gap = rng.randint(2, 7)
        left = x if d > 0 else x - r.width
        top_y = FLOOR_Y + rng.randint(3, 11) - r.height
        paste(c, r, round(left), round(top_y))
        anchors.append((left + r.width * 0.5, top_y + 4))
        x = x + (r.width + gap) * d


def rubble_heap(c, x, w, h, rng):
    """Đống đổ nát: gò gạch + tấm sàn dựng xiên + thép chìa."""
    base = FLOOR_Y + 8
    pts = [(x, base + 4)]
    n = max(4, w // 7)
    for k in range(n + 1):
        t = k / float(n)
        pts.append((x + w * t, base - h * math.sin(math.pi * t) * rng.uniform(0.6, 1.0)))
    pts.append((x + w, base + 4))
    c.poly(pts, rng.choice([(26, 22, 38), (32, 26, 44)]))
    for k in range(1, len(pts) - 2):
        c.line(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], (100, 84, 120), 1)
    for _ in range(rng.randint(2, 4)):                              # tấm sàn / mảnh tường dựng xiên
        sw, sh = rng.randint(10, 22), rng.randint(3, 6)
        slab = Cv(sw, sh)
        slab.rect(0, 0, sw - 1, sh - 1, rng.choice([(58, 50, 82), (46, 40, 70), (70, 56, 90)]))
        slab.rect(0, 0, sw - 1, 0, (140, 120, 170))
        slab = pad_outline(slab.im)
        rot_paste_center(c, slab, x + rng.uniform(0.15, 0.85) * w, base - h * rng.uniform(0.35, 0.9), rng.uniform(-60, 60))
    for _ in range(rng.randint(3, 6)):
        px_ = x + rng.uniform(0.1, 0.9) * w
        py_ = base - h * rng.uniform(0.4, 0.95)
        c.line(px_, py_, px_ + rng.randint(-5, 5), py_ - rng.randint(4, 11), REBAR, 1)


def flying_chunks(c, rng, cx, spread, n):
    """Mảnh nhà bị cuốn lên trời (quanh lốc xoáy)."""
    for _ in range(n):
        w, h = rng.randint(8, 18), rng.randint(6, 14)
        raw = pad_outline(tower_raw(w, h, rng.randint(0, 10 ** 6)))
        rot_paste_center(c, shade(raw, (60, 40, 80), 0.3), cx + rng.uniform(-spread, spread), rng.uniform(14, 135), rng.uniform(0, 360))


def war_bg():
    c = Cv(WAR_W, FH)
    rng = noise_rng(81)
    anchors = []
    sky_gradient(c, FH, (10, 8, 22), (52, 26, 56), (190, 70, 50), 130)
    # mây bão: các mảng xám tối
    for _ in range(46):
        cx_, cy_ = rng.randint(0, WAR_W), rng.randint(4, 70)
        for k in range(14):
            ox, oy = rng.randint(-26, 26), rng.randint(-7, 7)
            c.ell(cx_ + ox - 14, cy_ + oy - 6, cx_ + ox + 14, cy_ + oy + 6, lerp((26, 20, 38), (60, 46, 70), rng.random()))
    # thành phố phía xa (mờ hơn, nhà gãy + cháy) — lớp nhà đổ nát ở gần che lên trên
    far = Cv(WAR_W, FH)
    city_far(far, 168, 101, True)
    far.im = shade(far.im, (74, 38, 84), 0.4)
    c.im.alpha_composite(far.im)
    smoke_columns(c, [120, 330, 560, 760, 1010, 1190], 24, 4)
    # lốc xoáy khổng lồ ở x ~ 990: các elip chồng nhỏ dần xuống mặt đất, kèm mảnh vỡ
    for k in range(34):
        t = k / 33.0
        w = 10 + 62 * (1 - t) ** 1.2
        y = 20 + t * 150
        x = 990 + math.sin(t * 5) * 8
        c.ell(x - w, y - 4, x + w, y + 4, lerp((56, 50, 66), (28, 24, 38), (k % 3) / 3.0))
    for _ in range(60):
        a = rng.uniform(0, math.pi * 2)
        r = rng.uniform(16, 70)
        c.px(990 + math.cos(a) * r, 60 + rng.uniform(-40, 90), rng.choice([(110, 100, 120), (60, 50, 70), (160, 120, 90)]))
    flying_chunks(c, rng, 990, 85, 22)                                          # mảnh nhà bị lốc cuốn lên
    # sét đánh xuống đất (tĩnh) ở vài chỗ
    for x0 in (300, 700, 1130):
        pts = bolt_points(x0, 0, x0 + rng.randint(-24, 24), 170, rng, 10)
        draw_bolt(c, pts, WHITE, (140, 110, 240), 1)
    # ---- nhà cửa đổ nát: lớp xa (mờ) -> lớp gần -> dãy domino -> nhà vắt ngang như cầu
    wreck_layer(c, rng, False, anchors)
    wreck_layer(c, rng, True, anchors)
    domino_run(c, 232, 1, rng, anchors)                                          # đổ về bên trái
    domino_run(c, 598, -1, rng, anchors)                                         # đổ về bên phải
    domino_run(c, 1066, 1, rng, anchors, 5)
    for (bx_, by_, ln, th, dd) in ((300, 112, 150, 30, 1), (560, 106, 132, 28, -1), (880, 116, 120, 28, 1), (1130, 108, 136, 30, -1), (60, 118, 110, 26, -1)):
        bridge_tower(c, bx_, by_, ln, th, dd, rng)
    dust_puffs(c, [(x_, FLOOR_Y - 8, 46, 16) for x_ in range(20, WAR_W, 64)], rng)
    rubble_floor(c, 105)
    # đống gạch + nhà đổ nằm ngang trên mặt đất (xen vào chỗ trống giữa các cụm giao tranh, phía sau lính)
    for (hx, hw, hh) in ((200, 70, 20), (330, 60, 16), (480, 70, 22), (610, 56, 18), (740, 36, 14), (985, 70, 22), (1100, 60, 18), (1240, 64, 20), (10, 60, 18)):
        rubble_heap(c, hx, hw, hh, rng)
    for (lx, ln, th, dd) in ((290, 132, 30, 1), (562, 104, 28, -1), (1046, 92, 26, 1), (24, 96, 28, 1), (1256, 96, 28, -1), (800, 70, 22, -1)):
        lying_tower(c, lx, ln, th, dd, rng, anchors)
    for (hx, hw, hh) in ((260, 54, 14), (540, 50, 12), (1010, 44, 12)):
        rubble_heap(c, hx, hw, hh, rng)
    dust_puffs(c, [(x_, FLOOR_Y + 2, 40, 12) for x_ in range(40, WAR_W, 90)], rng, alpha=56)
    # lửa trên mặt đất + trên chỗ nhà gãy
    fires = [Image.open(os.path.join(G.OUT_DIR, "op_fire_%d.png" % k)).convert("RGBA") for k in range(3)] if os.path.exists(os.path.join(G.OUT_DIR, "op_fire_0.png")) else []
    for x in range(30, WAR_W - 30, rng.randint(150, 210)):
        if fires:
            paste(c, fires[rng.randint(0, 2)], x, FLOOR_Y - 20)
    for (ax_, ay_) in anchors:
        if fires and rng.random() < 0.38:
            paste(c, fires[rng.randint(0, 2)], int(ax_) - 8, int(ay_) - 22)
    # các cụm giao tranh: trái hướng phải ↔ phải hướng trái, xen kẽ thời đại
    kinds = ["rome", "knight", "soldier", "neon"]
    clusters = [150, 430, 690, 930, 1160]
    for cxm in clusters:
        for k in range(4):
            kl, kr = rng.choice(kinds), rng.choice(kinds)
            fl = fighter(kl, rng.choice(["stand", "strike"])).im
            fr = fighter(kr, rng.choice(["stand", "strike"])).im
            cl = Cv(32, 32); cl.im = fl; cl.outline()
            cr = Cv(32, 32); cr.im = fr; cr.outline()
            paste(c, cl.im, cxm - 44 + k * 13 - rng.randint(0, 6), FLOOR_Y - 31 + rng.randint(-1, 5))
            paste(c, cr.im, cxm + 10 + k * 13 + rng.randint(0, 6), FLOOR_Y - 31 + rng.randint(-1, 5), flip=True)
        for k in range(3):
            body = fallen(rng.choice(kinds))
            paste(c, body, cxm - 70 + k * 56 + rng.randint(-8, 8), FLOOR_Y - 2 + rng.randint(0, 10), flip=rng.random() < 0.5)
        # tia súng / tên bay giữa hai bên
        for _ in range(5):
            y = FLOOR_Y - rng.randint(8, 24)
            x = cxm - 20 + rng.randint(-8, 8)
            c.line(x, y, x + rng.randint(14, 26), y + rng.randint(-2, 2), rng.choice([YEL, CYAN, WHITE]), 1)
    # em bé đói khóc (x ~ 805): ngồi bó gối cạnh cái bát rỗng, nước mắt
    kid = Cv(22, 24)
    kid.ell(6, 1, 15, 10, SKIN_B)
    kid.rect(5, 0, 16, 3, HAIR_BK)
    kid.px(8, 6, INK); kid.px(12, 6, INK)
    kid.rect(9, 8, 11, 9, (150, 50, 50))                                            # miệng mếu
    kid.rect(7, 10, 14, 19, (112, 90, 70))                                          # áo rách
    kid.rect(4, 12, 6, 20, SKIN_B); kid.rect(15, 12, 17, 20, SKIN_B)
    kid.rect(6, 20, 15, 22, SKIN_B)
    kid.rect(8, 21, 13, 23, (60, 44, 34))
    kid.px(7, 7, (120, 190, 255)); kid.px(7, 8, (120, 190, 255)); kid.px(13, 7, (120, 190, 255)); kid.px(14, 9, (120, 190, 255))
    kid.outline()
    paste(c, kid.im, 800, FLOOR_Y - 21)
    bowl = Cv(12, 5)
    bowl.poly([(0, 0), (11, 0), (9, 4), (2, 4)], (150, 150, 160))
    bowl.rect(1, 0, 10, 0, (210, 210, 224))
    bowl.outline()
    paste(c, bowl.im, 828, FLOOR_Y - 5)
    # tiền cảnh: đống đổ nát + gai thép
    for _ in range(30):
        x = rng.randint(0, WAR_W - 40)
        w, h = rng.randint(24, 80), rng.randint(6, 20)
        c.poly([(x, FH), (x + 6, FH - h * 0.6), (x + w * 0.4, FH - h), (x + w * 0.8, FH - h * 0.5), (x + w, FH)], rng.choice([(20, 16, 28), (30, 24, 40)]))
    save_img("op_s8_war_bg", c.im)
    # hạt mưa
    r = Cv(3, 12)
    r.line(1, 0, 1, 11, (160, 180, 230, 200))
    r.save("op_rain", pivot=(0.5, 0.5), outline=False)


# ================================================================ TIẾN HÓA (cảnh 9) — 6 hình + 5 sợi dây
EVO_POS = [(56, 164), (131, 112), (206, 150), (281, 90), (356, 124), (425, 50)]       # tâm từng hình (px trong khung 480x210)
FUR = (112, 76, 46)
FUR_D = (70, 46, 30)
FUR_L = (196, 150, 104)


APE = (88, 62, 44)
APE_D = (52, 36, 28)
APE_L = (136, 100, 70)
APE_FACE = (214, 170, 130)
APE_FACE_D = (156, 108, 82)


def _fur_texture(c, rng, n, region=None):
    """Rắc những nét lông sáng / tối ngắn lên phần thân đã vẽ (chỉ ở pixel có màu)."""
    for _ in range(n):
        x, y = rng.randint(0, c.w - 1), rng.randint(0, c.h - 1)
        if region and not (region[0] <= x <= region[2] and region[1] <= y <= region[3]):
            continue
        px_ = c.im.getpixel((x, y))
        if px_[3] == 0 or px_[:3] in (APE_FACE, APE_FACE_D, (10, 10, 22)):
            continue
        col = APE_L if rng.random() < 0.45 else APE_D
        c.px(x, y, col)
        if rng.random() < 0.5 and c.im.getpixel((x, min(c.h - 1, y + 1)))[3] > 0:
            c.px(x, y + 1, col)


def _ape_head(c, hx, hy):
    """Đầu vượn nhìn nghiêng sang phải (hộp ~20 x 15, góc trên trái (hx, hy)): sọ tròn phủ lông, gờ mày nhô, mắt sâu,
    mặt trần sáng màu với mõm ngắn + miệng rộng, tai tròn to bên hông."""
    c.ell(hx, hy, hx + 13, hy + 13, APE)                                 # sọ
    c.poly([(hx + 2, hy + 3), (hx + 5, hy - 2), (hx + 8, hy + 1), (hx + 11, hy - 1), (hx + 11, hy + 4)], APE_D)       # chỏm lông
    c.ell(hx + 5, hy + 3, hx + 16, hy + 14, APE_FACE)                    # mặt trần
    c.ell(hx + 11, hy + 7, hx + 19, hy + 14, APE_FACE)                   # mõm ngắn, môi dày
    c.rect(hx + 5, hy + 3, hx + 15, hy + 4, APE_D)                       # gờ mày dày
    c.rect(hx + 7, hy + 5, hx + 8, hy + 6, INK)                          # mắt sâu dưới gờ mày
    c.rect(hx + 12, hy + 5, hx + 13, hy + 6, INK)
    c.px(hx + 6, hy + 5, (240, 230, 220)); c.px(hx + 11, hy + 5, (240, 230, 220))
    c.px(hx + 17, hy + 9, APE_FACE_D)                                    # lỗ mũi
    c.rect(hx + 12, hy + 12, hx + 18, hy + 12, APE_FACE_D)               # khe miệng rộng
    c.ell(hx - 3, hy + 3, hx + 3, hy + 10, APE_FACE_D)                   # tai tròn to
    c.ell(hx - 2, hy + 4, hx + 2, hy + 9, APE_FACE)


def evo_ape4():
    """Vượn lớn (tinh tinh) đi bằng 4 chi, chống đốt ngón tay: vai cao, lưng dốc xuống hông, tay RẤT dài chạm đất, chân sau ngắn cong, đầu nhỏ chúi giữa hai vai."""
    rng = noise_rng(404)
    c = Cv(64, 64)
    # chi phía xa (tối hơn)
    c.line(15, 42, 12, 54, APE_D, 5)
    c.rect(5, 54, 16, 57, APE_D)
    c.line(38, 26, 41, 54, APE_D, 5)
    c.rect(37, 53, 47, 57, APE_D)
    # thân: vai cao, lưng dốc xuống hông
    c.poly([(9, 38), (17, 28), (29, 19), (40, 17), (46, 24), (44, 34), (36, 40), (22, 44), (12, 44)], APE)
    c.ell(26, 15, 47, 33, APE)                                           # khối vai
    c.poly([(10, 37), (18, 29), (29, 20), (24, 30), (16, 38)], APE_L)    # lưng sáng
    c.ell(18, 36, 36, 46, APE_L)                                         # bụng
    c.poly([(9, 38), (12, 33), (7, 33), (11, 37)], APE_D)                # chỏm lông ở mông (không đuôi)
    # chân sau gần: đùi to, cẳng ngắn cong, bàn chân bè
    c.ell(7, 33, 24, 50, APE_D)
    c.ell(8, 34, 23, 49, APE)
    c.line(13, 46, 14, 54, APE_D, 7)
    c.line(13, 46, 14, 54, APE, 5)
    c.rect(7, 54, 20, 57, APE_D)
    c.rect(8, 54, 19, 54, APE_L)
    # đầu nhỏ chúi giữa hai vai
    _ape_head(c, 41, 8)
    # cánh tay gần: rất dài, viền tối để tách khỏi thân, chống đốt ngón tay
    c.line(40, 24, 44, 38, APE_D, 8)
    c.line(44, 38, 47, 53, APE_D, 7)
    c.line(40, 24, 44, 38, APE, 6)
    c.line(44, 38, 47, 53, APE, 5)
    c.ell(44, 51, 55, 58, APE_D)
    for k in range(3):
        c.px(46 + k * 3, 53, APE_L)                                      # đốt ngón tay
    _fur_texture(c, rng, 140, (6, 16, 52, 56))
    return c


def evo_ape_hunch():
    c = Cv(64, 64)
    c.line(28, 38, 24, 56, FUR_D, 6)                                 # chân gập
    c.line(34, 38, 40, 56, FUR, 6)
    c.rect(18, 55, 28, 58, FUR_D); c.rect(36, 55, 46, 58, FUR_D)
    c.poly([(24, 20), (40, 16), (46, 38), (26, 42)], FUR)           # thân khom
    c.poly([(24, 20), (30, 19), (32, 40), (26, 42)], FUR_D)
    c.line(42, 22, 52, 40, FUR, 5)                                   # tay buông thõng gần đầu gối
    c.line(52, 40, 50, 52, FUR, 4)
    c.rect(48, 51, 52, 54, FUR_D)
    c.ell(38, 6, 52, 22, FUR)                                        # đầu chúi về trước
    c.ell(44, 11, 52, 21, FUR_L)
    c.px(47, 12, INK); c.px(49, 12, INK)
    c.rect(47, 18, 51, 18, FUR_D)
    return c


def evo_early_human():
    c = Cv(64, 64)
    c.line(30, 40, 28, 58, SKIN_C, 5)
    c.line(36, 40, 38, 58, SKIN_B, 5)
    c.rect(24, 57, 31, 59, FUR_D); c.rect(34, 57, 42, 59, FUR_D)
    c.poly([(26, 24), (40, 24), (42, 42), (26, 42)], SKIN_B)       # thân trần + khố lông thú
    c.poly([(26, 38), (42, 38), (44, 46), (24, 46)], FUR)
    c.line(26, 26, 22, 40, SKIN_C, 4)                                # tay buông
    c.line(40, 26, 46, 38, SKIN_B, 4)
    c.rect(45, 37, 48, 40, SKIN_B)
    c.line(48, 36, 54, 16, (130, 100, 70), 2)                        # cây gậy
    c.ell(29, 8, 42, 24, SKIN_B)
    c.rect(28, 6, 43, 11, HAIR_BK)
    c.px(37, 15, INK); c.px(34, 15, INK)
    c.rect(34, 19, 38, 19, (120, 60, 50))
    return c


def evo_cold():
    c = Cv(64, 64)
    c.line(30, 40, 28, 58, (80, 60, 40), 5)
    c.line(36, 40, 38, 58, (60, 44, 30), 5)
    c.rect(24, 57, 31, 59, SHOE); c.rect(34, 57, 42, 59, SHOE)
    c.rect(26, 24, 42, 42, (176, 128, 60))                           # áo giáp đồng kiểu La Mã
    c.rect(26, 24, 42, 25, (230, 180, 90))
    c.rect(26, 40, 42, 46, (170, 46, 46))
    c.line(42, 28, 48, 24, (176, 128, 60), 4)
    c.line(48, 24, 56, 4, STEEL_L, 3)                                # kiếm giơ cao
    c.rect(46, 24, 50, 26, (180, 140, 50))
    c.ell(14, 26, 28, 46, (190, 44, 44)); c.ell(17, 29, 25, 43, (232, 190, 70))     # khiên
    c.ell(29, 8, 42, 24, SKIN_A)
    c.rect(28, 6, 43, 13, (176, 128, 60))
    c.rect(32, 2, 40, 6, (200, 40, 40))
    c.px(37, 16, INK); c.px(34, 16, INK)
    return c


def evo_gun():
    c = Cv(64, 64)
    c.line(30, 40, 26, 58, (70, 84, 56), 5)
    c.line(36, 40, 40, 58, (56, 70, 44), 5)
    c.rect(20, 57, 31, 59, SHOE); c.rect(34, 57, 45, 59, SHOE)
    c.rect(26, 24, 42, 42, (84, 100, 66))
    c.rect(28, 28, 40, 36, (60, 72, 46))
    c.line(40, 30, 50, 30, (84, 100, 66), 4)
    c.line(24, 34, 58, 28, (24, 24, 30), 3)                          # súng trường
    c.rect(56, 25, 60, 30, (24, 24, 30))
    c.rect(61, 25, 63, 29, YEL)
    c.ell(29, 8, 42, 24, SKIN_B)
    c.rect(28, 5, 43, 13, (70, 82, 54))
    c.px(37, 16, INK); c.px(34, 16, INK)
    return c


def evo_neon():
    c = Cv(64, 64)
    c.line(30, 40, 28, 58, (24, 22, 54), 5)
    c.line(36, 40, 38, 58, (24, 22, 54), 5)
    c.line(27, 42, 27, 58, CYAN, 1); c.line(39, 42, 39, 58, MAG, 1)
    c.rect(22, 57, 31, 59, CYAN); c.rect(34, 57, 44, 59, MAG)
    c.rect(26, 22, 42, 42, (24, 22, 54))
    c.line(26, 22, 26, 42, CYAN); c.line(42, 22, 42, 42, MAG); c.line(26, 32, 42, 32, CYAN)
    c.rect(32, 26, 36, 29, LIME)
    c.line(26, 24, 22, 40, (24, 22, 54), 4); c.line(42, 24, 46, 40, (24, 22, 54), 4)
    c.line(22, 40, 22, 42, MAG, 2); c.line(46, 40, 46, 42, CYAN, 2)
    c.rect(20, 40, 24, 43, (200, 210, 230)); c.rect(44, 40, 48, 43, (200, 210, 230))
    c.ell(29, 7, 42, 23, (186, 196, 216))
    c.rect(28, 5, 43, 11, MAG)
    c.rect(33, 13, 42, 15, CYAN)
    c.px(30, 3, CYAN); c.px(41, 3, CYAN)
    return c


def evolution():
    figs = [evo_ape4(), evo_ape_hunch(), evo_early_human(), evo_cold(), evo_gun(), evo_neon()]
    for i, f in enumerate(figs):
        f.save("op_evo_%d" % i, pivot=(0.5, 0.5))
    # sợi dây trắng nối hình i -> i+1 (lớp phủ 480x210) theo đường sóng
    for i in range(5):
        c = Cv(FW, FH)
        (x0, y0), (x1, y1) = EVO_POS[i], EVO_POS[i + 1]
        x0 += 22; x1 -= 22
        pts = []
        steps = int((x1 - x0) * 1.5)
        for k in range(steps + 1):
            t = k / float(steps)
            sm = t * t * (3 - 2 * t)
            y = y0 + (y1 - y0) * sm + 6 * math.sin(t * math.pi * 3)
            pts.append((x0 + (x1 - x0) * t, y))
        for k in range(len(pts) - 1):
            c.line(pts[k][0], pts[k][1], pts[k + 1][0], pts[k + 1][1], (255, 255, 255, 255), 1)
        save_img("op_evo_line_%d" % i, c.im)


# ================================================================ MAIN
def main():
    scientists()
    nancelas_sprites()
    machine_sprites()
    bunker_bg()
    small_props()
    s3_assets()
    mechs()
    dead_scientists()
    fire_smoke_debris()
    lightning()
    hall_bg("a")
    hall_bg("b")
    ruins_mix_bg()
    flying_cars()
    crowd_bg()
    war_bg()
    evolution()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (cot truyen mo dau) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
