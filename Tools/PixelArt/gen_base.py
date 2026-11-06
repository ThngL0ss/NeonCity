#!/usr/bin/env python3
"""
Vẽ sprite cho căn cứ ngầm của Albert (hầm công nghệ cũ kỹ pha đá / đất), dùng chung công cụ vẽ + bảng màu của gen_art.py:

  base_*               gạch nền: tường đá, ốp kim loại, sàn, trần, ống, cột
  base_lamp / cone     đèn trần + chùm sáng
  albert_*             Albert (ông già khoa học áo choàng trắng): idle / walk / admire
  capsule_*            lồng kính hồi sinh
  station_*            bàn chế tác, máy ghép linh kiện, cửa ra
  prop_*               phát minh / đồ vật của Albert (tháp Tesla, màn hình, máy phát, giá đồ, cánh tay robot...)

Chạy:  python3 Tools/PixelArt/gen_base.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import math
import os
import random
import sys

import numpy as np
from PIL import Image

import gen_art as G
from gen_art import (Cv, CYAN, CYAN_D, MAG, MAG_D, PURP, PURP_D, LIME, ORG, ORG_D, RED, RED_D, YEL, WHITE,
                     NAVY, NAVY2, NAVYL, OUT, lerp, save_array)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

# ---------------------------------------------------------------- bảng màu của căn cứ
ROCK_DD = (20, 16, 26)
ROCK_D = (33, 27, 40)
ROCK = (50, 41, 56)
ROCK_L = (76, 63, 82)
ROCK_LL = (104, 86, 106)
DIRT_D = (48, 33, 31)
DIRT = (74, 51, 43)
DIRT_L = (104, 74, 58)
RUST_D = (110, 52, 26)
RUST = (160, 82, 38)
RUST_L = (212, 128, 60)
MET_DD = (28, 31, 50)
MET_D = (48, 54, 82)
MET = (80, 90, 124)
MET_L = (132, 146, 186)
MET_LL = (186, 198, 228)
AMB = (255, 190, 80)
AMB_L = (255, 232, 160)
AMB_D = (180, 110, 40)
COAT = (238, 242, 252)
COAT_S = (190, 200, 226)
COAT_D = (138, 150, 190)
HAIR = (214, 218, 228)
HAIR_D = (150, 156, 174)
SKIN = (238, 190, 154)
SKIN_D = (200, 146, 116)
BOOT = (104, 64, 42)
BOOT_D = (66, 40, 28)
PANT = (84, 90, 110)
PANT_D = (56, 60, 80)
GLASS = (110, 220, 240)

rng = random.Random(7)


def noise_tile(w, h, base, speckles, seed, density=0.08):
    r = random.Random(seed)
    a = np.zeros((h, w, 4), np.uint8)
    a[:, :] = (*base, 255)
    for _ in range(int(w * h * density)):
        x, y = r.randrange(w), r.randrange(h)
        a[y, x] = (*r.choice(speckles), 255)
    return a


def stone_tile(w, h, seed, tones, mortar, hi, dirt):
    """Tường xây bằng đá khối không đều. Mỗi hàng cao 6-9 pixel, khối rộng 8-15 pixel; liền mạch khi lặp."""
    r = random.Random(seed)
    a = np.zeros((h, w, 4), np.uint8)
    a[:, :] = (*mortar, 255)
    y = 0
    rows = []
    while y < h:
        rh = r.randint(6, 9) if y + 9 <= h else h - y
        if h - y - rh < 5:
            rh = h - y
        rows.append((y, rh))
        y += rh
    for (y0, rh) in rows:
        widths = []
        total = 0
        while total < w:
            bw = r.randint(8, 15)
            if w - total - bw < 6:
                bw = w - total
            widths.append(bw)
            total += bw
        x = r.randint(0, 5)
        # dịch hàng nhưng vẫn liền mạch: vẽ theo modulo w
        for bw in widths:
            tone = r.choice(tones)
            for yy in range(y0 + 1, y0 + rh):
                for xx in range(x + 1, x + bw):
                    c = tone
                    if yy == y0 + 1:
                        c = lerp(tone, hi, 0.45)          # sáng ở mép trên
                    elif xx == x + 1:
                        c = lerp(tone, hi, 0.2)
                    elif yy == y0 + rh - 1:
                        c = lerp(tone, mortar, 0.5)
                    a[yy % h, xx % w] = (*c, 255)
            # vài đốm đất / nứt
            for _ in range(2):
                px_, py_ = x + r.randrange(2, max(3, bw - 1)), y0 + r.randrange(2, max(3, rh - 1))
                a[py_ % h, px_ % w] = (*(dirt if r.random() < 0.6 else lerp(tone, mortar, 0.6)), 255)
            x += bw
    return a


# ================================================================ TILES
def tiles():
    # tường đá nền (tile 32x32, liền mạch): các khối đá xây không đều
    save_array("base_wall", stone_tile(32, 32, 11, (ROCK_D, ROCK, (42, 34, 50), (58, 47, 64)), ROCK_DD, ROCK_L, DIRT_D), tile=True)

    # tường phía sau tối hơn, có dầm sắt (tile 32x32)
    a = stone_tile(32, 32, 21, (ROCK_DD, (26, 21, 32), ROCK_D), (14, 11, 20), ROCK, (24, 18, 22))
    a[0, :] = (*MET_DD, 255)
    a[1, :] = (*MET_D, 255)
    for (x, y) in ((3, 1), (12, 1), (19, 1), (28, 1)):
        a[y, x] = (*MET, 255)
    save_array("base_wall_back", a, tile=True)

    # ốp kim loại gỉ ở chân tường (tile 16x16)
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            c = MET_D if y < 14 else MET_DD
            if x == 0 or x == 15:
                c = MET_DD
            a[y, x] = (*c, 255)
    a[0, :] = (*MET_L, 255)
    a[1, :] = (*MET, 255)
    for (x, y) in ((2, 3), (13, 3), (2, 12), (13, 12)):
        a[y, x] = (*MET_LL, 255)
        a[y + 1, x] = (*MET_DD, 255)
    for (x, y, n) in ((4, 6, 5), (6, 9, 4), (10, 5, 3), (9, 11, 4)):
        a[y, x:x + n] = (*RUST_D, 255)
        a[y + 1, x + 1:x + n] = (*RUST, 255)
    a[7, 5:12] = (*MET_DD, 255)
    save_array("base_wainscot", a, tile=True)

    # sàn: tấm kim loại có đường vàng cam, đinh tán
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            c = lerp((64, 70, 100), (34, 36, 58), y / 15.0)
            a[y, x] = (*c, 255)
    a[0, :] = (*AMB, 255)
    a[1, :] = (*AMB_D, 255)
    a[2, :] = (*MET_L, 255)
    a[3, :] = (*MET, 255)
    for x in (0, 8):
        a[4:16, x] = (*MET_DD, 255)
    for (x, y) in ((3, 5), (12, 5)):
        a[y, x] = (*MET_LL, 255)
    for x in range(3, 13):
        a[10, x] = (*MET_DD, 255)
    a[8, 10:14] = (*RUST, 255)
    a[12, 2:5] = (*RUST_D, 255)
    save_array("base_floor_top", a, tile=True)

    # lớp đất đá bên dưới sàn
    a = noise_tile(16, 16, DIRT_D, [DIRT, DIRT, ROCK_D, ROCK, DIRT_L], 31, 0.28)
    a[0, :] = (*MET_DD, 255)
    for (x, y) in ((3, 6), (4, 6), (10, 11), (11, 11), (7, 3)):
        a[y, x] = (*ROCK_L, 255)
    save_array("base_floor_fill", a, tile=True)

    # trần: đá + dầm thép dưới cùng
    a = noise_tile(16, 16, ROCK_DD, [ROCK_D, ROCK_D, ROCK, (14, 11, 20)], 41, 0.2)
    a[11, :] = (*MET_DD, 255)
    a[12, :] = (*MET_L, 255)
    a[13, :] = (*MET, 255)
    a[14, :] = (*MET_D, 255)
    a[15, :] = (*MET_DD, 255)
    for x in (0, 8):
        a[11:16, x] = (*MET_DD, 255)
    for x in (3, 12):
        a[13, x] = (*MET_LL, 255)
    save_array("base_ceiling", a, tile=True)

    # ống ngang (tile 16x8)
    a = np.zeros((8, 16, 4), np.uint8)
    cols = [MET_DD, MET_D, MET, MET_L, MET, MET_D, MET_DD, MET_DD]
    for y in range(8):
        for x in range(16):
            a[y, x] = (*cols[y], 255)
    for x in (0, 1):
        a[:, x] = (*MET_DD, 255)
    a[2:6, 8] = (*MET_DD, 255)
    a[3, 3] = (*MET_LL, 255)
    a[2, 12] = (*RUST, 255)
    save_array("base_pipe_h", a, tile=True)

    # cột dẫn sét dọc (tile 6x16): dây đồng + sứ cách điện
    c = Cv(6, 16)
    c.rect(1, 0, 4, 15, MET_D)
    c.rect(2, 0, 3, 15, MET)
    c.line(2, 0, 2, 15, MET_L, 1)
    for y in (3, 11):
        c.rect(0, y, 5, y + 1, AMB_D)
        c.px(1, y, AMB_L)
    c.save("base_conductor", pivot=(0.5, 0.5), outline=False, tile=True)

    # cột chống ngang tường (tile 16x16 nhưng rộng 16, dùng làm trụ dọc)
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            if 3 <= x <= 12:
                c = MET_D
                if x <= 4:
                    c = MET_L
                elif x <= 6:
                    c = MET
                elif x >= 11:
                    c = MET_DD
                a[y, x] = (*c, 255)
    for y in (2, 13):
        a[y, 3:13] = (*MET_DD, 255)
    a[7:9, 3:13] = (*MET_D, 255)
    for (x, y) in ((5, 4), (10, 4), (5, 11), (10, 11)):
        a[y, x] = (*MET_LL, 255)
    save_array("base_column", a, tile=True)


# ================================================================ LAMP
def lamp():
    c = Cv(14, 22)
    c.line(7, 0, 7, 8, MET_D, 1)                            # dây treo
    c.px(6, 4, MET)
    c.poly([(2, 18), (4, 9), (10, 9), (12, 18)], MET_D)     # chụp đèn
    c.poly([(3, 17), (4, 10), (7, 10), (7, 17)], MET)
    c.rect(2, 17, 12, 18, MET_DD)
    c.rect(4, 18, 10, 20, AMB)                              # bóng
    c.rect(5, 19, 9, 20, AMB_L)
    c.px(6, 19, WHITE)
    c.rect(6, 8, 8, 9, RUST)
    c.save("base_lamp", pivot=(0.5, 1.0))

    # chùm sáng (cộng sáng): hình thang mờ dần xuống dưới
    w, h = 64, 96
    a = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        t = y / (h - 1.0)
        half = 5 + (w * 0.5 - 5) * t
        for x in range(w):
            d = abs(x - (w - 1) / 2.0) / max(half, 1.0)
            if d <= 1.0:
                al = (1.0 - d) ** 1.4 * (1.0 - t) ** 0.8 * 0.55
                a[y, x] = (255, 200, 110, int(al * 255))
    save_array("base_lamp_cone", a, ppu=16, pivot=(0.5, 1.0))


# ================================================================ ALBERT
def albert(name, foot_f=(14, 31), foot_b=(19, 31), bob=0, arm_f=(0, 0), arm_b=(0, 0), chin=False, lean=0, head_dy=0, sway=0):
    """Albert nhìn sang PHẢI. Canvas 32x32, chân ở y=31, tâm x=16."""
    c = Cv(32, 32)
    cx = 16 + lean
    top = 3 + bob + head_dy

    # --- chân (quần xám + giày nâu)
    for (fx, fy), shade in ((foot_b, PANT_D), (foot_f, PANT)):
        hip_y = 25 + bob
        c.line(cx + (-1 if shade == PANT else 2), hip_y, fx, fy - 2, shade, 3)
        c.rect(fx - 2, fy - 1, fx + 2, fy, BOOT)
        c.rect(fx - 2, fy, fx + 2, fy, BOOT_D)
        c.px(fx + 2, fy - 1, BOOT_D)

    # --- tay sau (bên trái canvas) ----
    abx, aby = arm_b
    c.line(cx - 4, 14 + bob, cx - 5 + abx, 21 + bob + aby, COAT_D, 3)
    c.rect(cx - 6 + abx, 21 + bob + aby, cx - 4 + abx, 22 + bob + aby, SKIN_D)

    # --- áo choàng trắng: thân + vạt xoè
    c.poly([(cx - 5, 12 + bob), (cx + 5, 12 + bob), (cx + 6, 26 + bob), (cx - 7 + sway, 26 + bob)], COAT)
    c.poly([(cx - 5, 12 + bob), (cx - 1, 12 + bob), (cx - 2, 26 + bob), (cx - 7 + sway, 26 + bob)], COAT_S)
    c.line(cx + 1, 14 + bob, cx + 2, 26 + bob, COAT_D, 1)           # đường mở áo
    c.rect(cx + 2, 13 + bob, cx + 4, 14 + bob, (60, 70, 120))        # cổ áo sơ mi / cà vạt
    c.px(cx + 3, 14 + bob, MAG)
    for y in (16, 19, 22):
        c.px(cx + 3, y + bob, COAT_D)                                # khuy
    c.rect(cx - 4, 19 + bob, cx - 1, 21 + bob, COAT_S)               # túi áo
    c.rect(cx - 4, 19 + bob, cx - 1, 19 + bob, COAT_D)
    c.px(cx - 3, 18 + bob, RED)                                      # bút trong túi
    c.px(cx - 2, 18 + bob, CYAN)
    c.rect(cx - 7 + sway, 25 + bob, cx + 6, 26 + bob, COAT_S)       # gấu áo

    # --- đầu
    c.ell(cx - 3, top, cx + 5, top + 8, SKIN)
    c.rect(cx - 3, top + 4, cx - 2, top + 7, SKIN_D)
    c.px(cx + 6, top + 5, SKIN)                                      # mũi to
    c.px(cx + 6, top + 6, SKIN_D)
    c.px(cx + 5, top + 5, SKIN)
    # tóc bạc rối hai bên + chỏm
    c.rect(cx - 4, top + 1, cx - 2, top + 5, HAIR)
    c.px(cx - 5, top + 2, HAIR)
    c.px(cx - 5, top + 4, HAIR_D)
    c.px(cx - 4, top, HAIR)
    c.px(cx - 3, top - 1, HAIR)
    c.px(cx - 2, top - 1, HAIR_D)
    c.px(cx - 1, top, HAIR)
    c.px(cx + 1, top, HAIR)
    c.px(cx + 2, top - 1, HAIR)
    c.px(cx + 3, top, HAIR_D)
    c.line(cx - 2, top + 1, cx + 1, top, HAIR, 1)
    # kính tròn
    c.rect(cx + 2, top + 3, cx + 5, top + 6, MET_L)
    c.rect(cx + 3, top + 4, cx + 4, top + 5, GLASS)
    c.px(cx + 3, top + 4, WHITE)
    c.line(cx - 1, top + 4, cx + 1, top + 4, MET_L, 1)               # gọng
    c.px(cx + 4, top + 4, NAVY2)
    # lông mày + ria mép + râu
    c.rect(cx + 2, top + 2, cx + 5, top + 2, HAIR)
    c.rect(cx + 1, top + 7, cx + 6, top + 8, HAIR)
    c.rect(cx - 2, top + 8, cx + 4, top + 10, HAIR)
    c.px(cx - 1, top + 11, HAIR_D)
    c.px(cx + 1, top + 11, HAIR)
    c.px(cx + 3, top + 10, HAIR_D)

    # --- tay trước
    afx, afy = arm_f
    if chin:
        c.line(cx + 1, 15 + bob, cx + 4, 15 + bob + afy - 3, COAT_S, 3)
        c.line(cx + 4, 12 + bob, cx + 4, 15 + bob, COAT_S, 3)
        c.rect(cx + 3, top + 7, cx + 5, top + 9, SKIN)               # bàn tay vuốt cằm
        c.px(cx + 4, top + 9, SKIN_D)
    else:
        c.line(cx - 1, 14 + bob, cx + afx, 21 + bob + afy, COAT_S, 3)
        c.rect(cx - 2 + afx, 21 + bob + afy, cx + afx, 22 + bob + afy, SKIN)
    c.save(name, pivot=(0.5, 0.0))


def albert_all():
    albert("albert_idle_0")
    albert("albert_idle_1", bob=1, head_dy=-1)
    albert("albert_walk_0", foot_f=(12, 31), foot_b=(21, 31), bob=0, arm_f=(2, 0), arm_b=(-2, 0), sway=-1)
    albert("albert_walk_1", foot_f=(15, 31), foot_b=(18, 30), bob=-1, arm_f=(0, -1), arm_b=(0, -1))
    albert("albert_walk_2", foot_f=(21, 31), foot_b=(12, 31), bob=0, arm_f=(-2, 0), arm_b=(2, 0), sway=1)
    albert("albert_walk_3", foot_f=(18, 30), foot_b=(15, 31), bob=-1, arm_f=(0, -1), arm_b=(0, -1))
    albert("albert_admire_0", chin=True, lean=1, head_dy=1, arm_b=(1, -2))
    albert("albert_admire_1", chin=True, lean=1, head_dy=2, arm_b=(1, -2))


# ================================================================ CAPSULE
def capsule():
    W, H = 48, 80
    # ---- lớp sau: khung + đế + nội thất tối
    c = Cv(W, H)
    # nắp trên + cột thu lôi
    c.rect(18, 0, 29, 3, MET_D)
    c.rect(21, 0, 26, 8, MET)
    c.rect(22, 0, 23, 8, MET_L)
    c.poly([(8, 12), (14, 4), (34, 4), (40, 12)], MET_D)
    c.poly([(11, 11), (15, 6), (33, 6), (37, 11)], MET)
    c.rect(8, 10, 40, 13, MET_DD)
    for x in (12, 20, 28, 36):
        c.px(x, 11, AMB)
    # kính (nền tối trong suốt)
    for y in range(14, 70):
        t = (y - 14) / 56.0
        col = lerp((20, 52, 70), (10, 28, 44), t)
        c.rect(9, y, 38, y, (*col, 150))
    for x in range(10, 38, 5):                      # lưới dọc mờ
        c.rect(x, 14, x, 69, (60, 150, 180, 40))
    # hai cột khung
    for (x0, x1) in ((5, 8), (39, 42)):
        c.rect(x0, 12, x1, 70, MET_D)
        c.rect(x0, 12, x0, 70, MET_L)
        c.rect(x1, 12, x1, 70, MET_DD)
        for y in (20, 34, 48, 62):
            c.rect(x0, y, x1, y + 1, MET_DD)
            c.px(x0 + 1, y, MET_LL)
    # dây điện lên đỉnh
    c.line(24, 8, 24, 13, AMB, 1)
    # đế
    c.rect(2, 70, 45, 73, MET)
    c.rect(2, 70, 45, 70, MET_L)
    c.rect(0, 73, 47, 79, MET_D)
    c.rect(0, 73, 47, 73, MET_L)
    c.rect(0, 77, 47, 79, MET_DD)
    for x in range(2, 46, 6):                        # sọc cảnh báo cam
        c.rect(x, 75, x + 2, 76, AMB)
    for x in (6, 20, 34):
        c.px(x, 71, CYAN)
        c.px(x + 1, 71, CYAN)
    c.save("capsule_back", pivot=(0.5, 0.0))

    # ---- lớp trước: kính phản chiếu (không viền)
    c = Cv(W, H)
    for y in range(14, 70):
        c.rect(9, y, 38, y, (120, 220, 240, 26))
    # vệt sáng chéo
    for i in range(18):
        x = 12 + i // 2
        c.line(x, 20 + i * 2, x + 3, 16 + i * 2, (255, 255, 255, 95), 1)
    for i in range(10):
        c.line(30 + i // 3, 40 + i * 2, 33 + i // 3, 37 + i * 2, (255, 255, 255, 60), 1)
    c.rect(9, 14, 38, 15, (255, 255, 255, 70))
    c.rect(9, 68, 38, 69, (120, 220, 240, 80))
    c.save("capsule_glass", pivot=(0.5, 0.0), outline=False)


# ================================================================ STATIONS
def craft_bench():
    W, H = 64, 48
    c = Cv(W, H)
    # bảng đục lỗ treo dụng cụ
    c.rect(4, 2, 59, 25, MET_DD)
    c.rect(4, 2, 59, 3, MET_D)
    for x in range(8, 58, 6):
        for y in (8, 14, 20):
            c.px(x, y, (22, 24, 40))
    # cờ-lê, búa, kìm
    c.line(10, 6, 10, 18, MET_L, 2)
    c.ell(8, 4, 12, 8, MET_L)
    c.px(10, 5, MET_DD)
    c.line(20, 6, 20, 18, RUST_L, 1)
    c.rect(17, 5, 23, 8, MET)
    c.rect(17, 5, 23, 5, MET_L)
    c.line(30, 5, 28, 17, MET_L, 1)
    c.line(32, 5, 34, 17, MET_L, 1)
    c.rect(29, 17, 33, 18, ORG)
    c.rect(42, 6, 52, 9, ORG_D)                          # hộp công cụ nhỏ
    c.rect(42, 6, 52, 6, ORG)
    c.rect(46, 4, 48, 6, MET)
    # màn hình nhỏ
    c.rect(44, 12, 57, 21, MET_D)
    c.rect(45, 13, 56, 20, NAVY2)
    c.line(46, 18, 49, 15, LIME, 1)
    c.line(49, 15, 52, 19, LIME, 1)
    c.line(52, 19, 55, 14, LIME, 1)
    # mặt bàn
    c.rect(2, 26, 61, 30, MET)
    c.rect(2, 26, 61, 27, MET_L)
    c.rect(2, 30, 61, 31, MET_DD)
    for x in (6, 22, 40, 56):
        c.px(x, 28, MET_LL)
    # chân bàn + ngăn kéo
    c.rect(4, 32, 8, 47, MET_D)
    c.rect(55, 32, 59, 47, MET_D)
    c.rect(4, 32, 4, 47, MET_L)
    c.rect(55, 32, 55, 47, MET_L)
    c.rect(10, 32, 53, 44, MET_DD)
    c.rect(11, 33, 32, 43, MET_D)
    c.rect(34, 33, 52, 43, MET_D)
    c.rect(18, 37, 25, 38, AMB_D)
    c.rect(38, 37, 46, 38, AMB_D)
    c.rect(4, 44, 59, 47, MET_DD)
    # trên bàn: đe nhỏ, vài chai phát sáng, bánh răng
    c.poly([(8, 25), (10, 21), (22, 21), (24, 25)], MET_D)
    c.rect(9, 21, 21, 22, MET_L)
    c.rect(12, 25, 20, 25, MET_DD)
    for (x, col) in ((30, LIME), (35, CYAN), (40, MAG)):
        c.rect(x, 20, x + 2, 25, lerp(col, NAVY2, 0.5))
        c.rect(x + 1, 21, x + 1, 24, col)
        c.rect(x, 19, x + 2, 19, MET_L)
    c.ell(48, 22, 54, 25, MET_L)
    c.px(51, 23, MET_DD)
    c.save("station_craft", pivot=(0.5, 0.0))


def fusion_machine():
    W, H = 72, 72
    c = Cv(W, H)
    # ống dẫn bên hông
    c.rect(2, 36, 10, 40, MET_D)
    c.rect(2, 36, 10, 36, MET_L)
    c.rect(61, 36, 69, 40, MET_D)
    c.rect(61, 36, 69, 36, MET_L)
    c.rect(2, 40, 5, 62, MET_D)
    c.rect(66, 40, 69, 62, MET_D)
    c.rect(2, 40, 2, 62, MET_L)
    # đế
    c.rect(6, 60, 65, 71, MET_D)
    c.rect(6, 60, 65, 61, MET_L)
    c.rect(6, 69, 65, 71, MET_DD)
    for x in range(8, 64, 8):
        c.poly([(x, 71), (x + 3, 63), (x + 6, 63), (x + 3, 71)], AMB_D)
    # thân hình trụ
    c.rect(10, 22, 61, 60, MET)
    c.rect(10, 22, 61, 24, MET_L)
    c.rect(10, 22, 12, 60, MET_L)
    c.rect(59, 22, 61, 60, MET_DD)
    c.rect(10, 58, 61, 60, MET_DD)
    # nắp vòm
    c.poly([(12, 22), (18, 8), (53, 8), (59, 22)], MET_D)
    c.poly([(15, 21), (20, 10), (51, 10), (56, 21)], MET)
    c.rect(18, 8, 53, 9, MET_L)
    # ăng-ten + đèn báo
    c.rect(35, 0, 36, 8, MET_L)
    c.rect(34, 0, 37, 1, RED)
    c.px(35, 0, WHITE)
    # mặt kính (cửa sổ buồng ghép)
    c.rect(15, 14, 56, 56, MET_DD)
    c.rect(17, 16, 54, 54, NAVY2)
    for y in range(17, 54):
        c.rect(18, y, 53, y, lerp((16, 30, 58), (10, 18, 40), (y - 17) / 36.0))
    # tam giác nối 3 ổ + lõi giữa
    top, left, right, ctr = (35, 24), (24, 44), (47, 44), (35, 36)
    for (a, b) in ((top, left), (left, right), (right, top)):
        c.line(a[0], a[1], b[0], b[1], CYAN_D, 1)
    for p in (top, left, right):
        c.line(p[0], p[1], ctr[0], ctr[1], PURP_D, 1)
    for p in (top, left, right):
        c.ell(p[0] - 5, p[1] - 5, p[0] + 5, p[1] + 5, MET_D)
        c.ell(p[0] - 4, p[1] - 4, p[0] + 4, p[1] + 4, NAVY2)
        c.ell(p[0] - 2, p[1] - 2, p[0] + 2, p[1] + 2, CYAN_D)
        c.px(p[0] - 1, p[1] - 1, CYAN)
    c.ell(ctr[0] - 6, ctr[1] - 6, ctr[0] + 6, ctr[1] + 6, MET_D)
    c.ell(ctr[0] - 5, ctr[1] - 5, ctr[0] + 5, ctr[1] + 5, PURP_D)
    c.ell(ctr[0] - 3, ctr[1] - 3, ctr[0] + 3, ctr[1] + 3, MAG)
    c.ell(ctr[0] - 1, ctr[1] - 1, ctr[0] + 1, ctr[1] + 1, WHITE)
    # phản chiếu kính
    for i in range(10):
        c.line(20 + i // 2, 18 + i * 3, 22 + i // 2, 16 + i * 3, (255, 255, 255, 40), 1)
    # bảng điều khiển dưới cửa sổ
    c.rect(15, 56, 56, 58, MET_DD)
    for x, col in ((19, LIME), (23, AMB), (27, RED), (44, CYAN), (48, MAG), (52, YEL)):
        c.px(x, 57, col)
        c.px(x + 1, 57, col)
    # rãnh hơi
    for x in (14, 55):
        c.rect(x, 60, x + 1, 62, MET_DD)
    c.save("station_fusion", pivot=(0.5, 0.0))


def exit_door():
    W, H = 48, 72
    c = Cv(W, H)
    # khung cửa
    c.rect(0, 0, 47, 71, MET_D)
    c.rect(0, 0, 47, 3, MET_L)
    c.rect(0, 0, 3, 71, MET)
    c.rect(44, 0, 47, 71, MET_DD)
    c.rect(0, 0, 47, 7, MET_D)
    # sọc cảnh báo
    for x in range(2, 46, 6):
        c.poly([(x, 7), (x + 3, 2), (x + 6, 2), (x + 3, 7)], AMB)
    c.rect(2, 7, 45, 8, MET_DD)
    # tấm cửa
    c.rect(6, 9, 41, 71, MET_DD)
    c.rect(8, 10, 39, 71, MET_D)
    for y in (18, 30, 42, 54, 66):
        c.rect(8, y, 39, y + 1, MET_DD)
        c.rect(8, y + 2, 39, y + 2, MET)
    c.rect(23, 10, 24, 71, MET_DD)                        # khe giữa
    # tay quay trung tâm
    c.ell(14, 32, 33, 51, MET_DD)
    c.ell(16, 34, 31, 49, MET)
    c.ell(19, 37, 28, 46, MET_D)
    c.line(23, 34, 23, 49, MET_L, 1)
    c.line(16, 41, 31, 41, MET_L, 1)
    c.ell(21, 39, 26, 44, ORG)
    c.px(22, 40, AMB_L)
    # đèn báo xanh phía trên
    c.rect(18, 3, 29, 5, (10, 40, 22))
    c.rect(19, 3, 28, 4, LIME)
    c.px(20, 3, WHITE)
    # bu-lông
    for (x, y) in ((10, 12), (37, 12), (10, 68), (37, 68)):
        c.px(x, y, MET_LL)
    c.save("station_door", pivot=(0.5, 0.0))

    # biển chỉ lối ra: mũi tên neon xanh trên nền tối (16x8)
    c = Cv(20, 8)
    c.rect(0, 0, 19, 7, MET_DD)
    c.rect(1, 1, 18, 6, (10, 36, 24))
    c.line(4, 3, 13, 3, LIME, 1)
    c.line(4, 4, 13, 4, LIME, 1)
    c.poly([(12, 1), (17, 3), (17, 4), (12, 6)], LIME)
    c.save("base_sign_exit", pivot=(0.5, 0.5))


# ================================================================ PROPS (phát minh của Albert)
def tesla():
    for f in range(2):
        c = Cv(32, 64)
        c.rect(6, 56, 25, 63, MET_D)
        c.rect(6, 56, 25, 57, MET_L)
        c.rect(6, 61, 25, 63, MET_DD)
        c.rect(12, 58, 19, 60, AMB_D)
        for y, w in ((50, 9), (44, 8), (38, 7), (32, 6), (26, 5)):
            c.rect(16 - w, y, 15 + w, y + 4, RUST)
            c.rect(16 - w, y, 15 + w, y, RUST_L)
            c.rect(16 - w, y + 3, 15 + w, y + 4, RUST_D)
            for x in range(16 - w + 1, 16 + w, 3):
                c.px(x, y + 2, AMB_L if (x + f) % 2 else AMB_D)
        c.rect(14, 18, 17, 26, MET)
        c.ell(7, 4, 24, 21, MET_D)
        c.ell(9, 6, 22, 19, MET_L)
        c.ell(12, 8, 19, 14, MET_LL)
        # hồ quang
        pts = [(8, 12), (3, 8), (5, 3), (0, 0)] if f == 0 else [(23, 12), (28, 7), (26, 3), (31, 0)]
        c.line(pts[0][0], pts[0][1], pts[1][0], pts[1][1], CYAN, 1)
        c.line(pts[1][0], pts[1][1], pts[2][0], pts[2][1], WHITE, 1)
        c.line(pts[2][0], pts[2][1], pts[3][0], pts[3][1], CYAN, 1)
        c.px(16, 13, CYAN if f else WHITE)
        c.save("prop_tesla_%d" % f, pivot=(0.5, 0.0))


def monitors():
    for f in range(2):
        c = Cv(56, 36)
        c.rect(0, 26, 55, 35, MET_D)
        c.rect(0, 26, 55, 27, MET_L)
        c.rect(0, 33, 55, 35, MET_DD)
        for x in range(4, 52, 7):
            c.px(x, 30, (CYAN, AMB, LIME, MAG)[(x // 7 + f) % 4])
        c.rect(6, 28, 18, 29, MET_DD)
        # ba màn hình
        for (x0, y0, w, h) in ((2, 4, 16, 14), (20, 0, 18, 18), (40, 4, 14, 14)):
            c.rect(x0, y0, x0 + w, y0 + h, MET_D)
            c.rect(x0 + 1, y0 + 1, x0 + w - 1, y0 + h - 1, (8, 20, 28))
            c.rect(x0 + w // 2 - 1, y0 + h + 1, x0 + w // 2 + 1, 25, MET_D)
        # nội dung màn hình
        for i in range(7):
            y = 6 + i * 2
            c.line(4, y, 4 + ((i * 5 + f * 3) % 11), y, LIME, 1)
        pts = [(22, 14), (25, 6 + f * 3), (28, 11), (31, 3 + f * 2), (34, 9), (36, 6 + f)]
        for a, b in zip(pts, pts[1:]):
            c.line(a[0], a[1], b[0], b[1], CYAN, 1)
        c.rect(22, 15 + f, 36, 15 + f, CYAN_D)
        for i in range(4):
            c.rect(42, 6 + i * 3, 42 + ((i * 3 + f * 4) % 9) + 1, 7 + i * 3, AMB if i % 2 else RED)
        c.save("prop_monitors_%d" % f, pivot=(0.5, 0.0))


def generator():
    for f in range(2):
        c = Cv(44, 36)
        c.rect(0, 30, 43, 35, MET_DD)
        c.rect(2, 6, 41, 30, MET_D)
        c.rect(2, 6, 41, 7, MET_L)
        c.rect(2, 6, 4, 30, MET)
        c.rect(38, 6, 41, 30, MET_DD)
        # quạt
        c.ell(8, 11, 24, 27, MET_DD)
        c.ell(10, 13, 22, 25, NAVY2)
        if f == 0:
            c.line(16, 14, 16, 24, MET_L, 1)
            c.line(11, 19, 21, 19, MET_L, 1)
        else:
            c.line(12, 14, 20, 24, MET_L, 1)
            c.line(20, 14, 12, 24, MET_L, 1)
        c.px(16, 19, AMB)
        # bảng đo
        c.rect(27, 9, 37, 15, MET_DD)
        c.rect(28, 10, 36, 14, NAVY2)
        c.line(30, 13, 33 + f, 11, LIME, 1)
        c.px(27, 19, RED if f else RED_D)
        c.px(30, 19, LIME)
        c.px(33, 19, AMB)
        c.rect(27, 22, 37, 27, RUST_D)
        c.rect(27, 22, 37, 22, RUST)
        # ống xả
        c.rect(34, 0, 37, 6, MET)
        c.rect(34, 0, 37, 1, MET_L)
        c.rect(33, 6, 38, 7, MET_DD)
        c.save("prop_generator_%d" % f, pivot=(0.5, 0.0))


def shelf():
    c = Cv(44, 56)
    c.rect(0, 0, 43, 55, MET_DD)
    c.rect(2, 2, 41, 55, ROCK_DD)
    for y in (18, 36, 54):
        c.rect(0, y, 43, y + 1, MET)
        c.rect(0, y, 43, y, MET_L)
    c.rect(0, 0, 1, 55, MET_D)
    c.rect(42, 0, 43, 55, MET_D)
    # tầng 1: lọ phát sáng
    for (x, col) in ((5, LIME), (13, CYAN), (22, MAG), (31, AMB)):
        c.rect(x, 8, x + 5, 17, lerp(col, NAVY2, 0.6))
        c.rect(x + 1, 11, x + 4, 16, col)
        c.rect(x, 6, x + 5, 7, MET_L)
        c.px(x + 1, 12, WHITE)
    # tầng 2: hộp + bánh răng
    c.rect(4, 26, 14, 35, DIRT)
    c.rect(4, 26, 14, 27, DIRT_L)
    c.line(4, 31, 14, 31, DIRT_D, 1)
    c.ell(18, 24, 29, 35, MET)
    c.ell(21, 27, 26, 32, ROCK_DD)
    c.ell(18, 24, 29, 35, MET)
    c.ell(21, 27, 26, 32, ROCK_DD)
    for (x, y) in ((23, 23), (23, 36), (17, 29), (30, 29)):
        c.px(x, y, MET_L)
    c.rect(32, 28, 40, 35, MET_D)
    c.rect(33, 29, 39, 31, CYAN_D)
    c.px(34, 30, CYAN)
    # tầng 3: sọ robot + dây cuộn + chip
    c.ell(4, 42, 15, 53, MET_L)
    c.rect(6, 46, 8, 48, NAVY2)
    c.rect(11, 46, 13, 48, NAVY2)
    c.px(7, 47, CYAN)
    c.px(12, 47, CYAN)
    c.rect(7, 51, 12, 53, MET_D)
    c.ell(20, 44, 31, 53, AMB_D)
    c.ell(23, 47, 28, 50, ROCK_DD)
    for y in range(44, 53, 3):
        c.line(20, y, 31, y, RUST_L, 1)
    c.rect(34, 48, 40, 53, NAVY2)
    c.rect(35, 49, 39, 52, CYAN)
    c.save("prop_shelf", pivot=(0.5, 0.0))


def robot_arm():
    c = Cv(40, 56)
    c.rect(4, 50, 35, 55, MET_D)
    c.rect(4, 50, 35, 50, MET_L)
    c.rect(4, 54, 35, 55, MET_DD)
    c.ell(8, 42, 22, 53, MET)
    c.ell(10, 44, 20, 51, MET_D)
    c.px(15, 47, AMB)
    c.line(15, 44, 15, 24, MET, 4)                      # cánh tay dưới
    c.line(13, 44, 13, 26, MET_L, 1)
    c.ell(10, 18, 21, 29, MET_D)
    c.ell(12, 20, 19, 27, MET)
    c.px(15, 23, RED)
    c.line(15, 22, 28, 12, MET, 3)                      # cánh tay trên
    c.line(15, 20, 28, 10, MET_L, 1)
    c.ell(25, 7, 33, 15, MET_D)
    c.px(29, 11, CYAN)
    # kìm kẹp bu-lông
    c.rect(30, 14, 31, 22, MET_L)
    c.rect(35, 14, 36, 22, MET_L)
    c.rect(31, 22, 35, 24, MET_D)
    c.rect(31, 25, 35, 28, MET_LL)
    c.px(33, 26, MET_DD)
    c.line(5, 52, 12, 52, AMB, 1)
    c.save("prop_robot_arm", pivot=(0.5, 0.0))


def prototype_case():
    c = Cv(40, 64)
    c.rect(0, 58, 39, 63, MET_D)
    c.rect(0, 58, 39, 58, MET_L)
    c.rect(0, 62, 39, 63, MET_DD)
    c.rect(2, 2, 37, 58, (24, 56, 76, 150))
    c.rect(0, 0, 39, 3, MET_D)
    c.rect(0, 0, 39, 0, MET_L)
    c.rect(0, 0, 1, 58, MET_D)
    c.rect(38, 0, 39, 58, MET_D)
    # robot dở dang bên trong: thân, cánh tay thiếu, đầu lệch
    c.rect(14, 22, 26, 42, MET)
    c.rect(14, 22, 26, 23, MET_L)
    c.rect(17, 26, 23, 33, NAVY2)
    c.rect(19, 28, 21, 31, CYAN_D)
    c.px(20, 29, CYAN)
    c.rect(15, 11, 25, 20, MET_L)
    c.rect(17, 14, 19, 16, NAVY2)
    c.px(18, 15, RED_D)
    c.rect(22, 14, 23, 16, NAVY2)
    c.line(20, 8, 20, 11, MET, 1)
    c.px(20, 7, RED)
    c.line(13, 24, 8, 36, MET, 2)
    c.line(27, 24, 31, 30, MET, 2)
    c.line(31, 30, 33, 32, AMB, 1)
    for y in range(43, 58, 4):
        c.px(17, y, MET_D)
        c.px(22, y + 1, MET_D)
    c.line(16, 42, 13, 56, MET_D, 2)
    c.line(24, 42, 27, 50, MET_D, 2)
    c.rect(12, 54, 28, 57, ROCK_D)                        # bộ phận chưa lắp dưới đáy
    # phản chiếu kính
    for i in range(14):
        c.px(5 + i // 3, 6 + i * 3, (255, 255, 255, 90))
        c.px(6 + i // 3, 6 + i * 3, (255, 255, 255, 90))
    # đèn cảnh báo
    c.rect(34, 59, 36, 60, AMB)
    c.save("prop_prototype", pivot=(0.5, 0.0))


def server_rack():
    for f in range(2):
        c = Cv(24, 48)
        c.rect(0, 0, 23, 47, MET_DD)
        c.rect(1, 1, 22, 47, MET_D)
        c.rect(1, 1, 1, 47, MET)
        for i in range(6):
            y = 3 + i * 7
            c.rect(3, y, 20, y + 5, MET_DD)
            c.rect(4, y + 1, 19, y + 4, (22, 26, 48))
            for j in range(4):
                lit = (i * 3 + j * 2 + f) % 4 < 2
                col = (LIME, AMB, CYAN, MAG)[(i + j) % 4]
                c.px(5 + j * 2, y + 2, col if lit else lerp(col, NAVY2, 0.7))
            c.line(13, y + 3, 18, y + 3, MET_D, 1)
        c.rect(0, 44, 23, 47, MET_DD)
        c.save("prop_rack_%d" % f, pivot=(0.5, 0.0))


def hologram_table():
    for f in range(2):
        c = Cv(48, 56)
        # bàn
        c.rect(4, 40, 43, 44, MET)
        c.rect(4, 40, 43, 40, MET_L)
        c.rect(4, 44, 43, 45, MET_DD)
        c.rect(20, 45, 27, 53, MET_D)
        c.rect(12, 53, 35, 55, MET_DD)
        c.rect(12, 53, 35, 53, MET_L)
        c.rect(10, 38, 37, 39, CYAN_D)
        # hình chiếu: thành phố neon dạng tháp, 2 khung dịch chuyển
        base = 37
        heights = [8, 14, 10, 20, 12, 16, 9]
        for i, h in enumerate(heights):
            hh = h + (2 if (i + f) % 2 else 0)
            x = 11 + i * 4
            c.rect(x, base - hh, x + 2, base, (*CYAN_D, 160))
            c.rect(x, base - hh, x + 2, base - hh, CYAN)
        for y in range(10 + f, 37, 3):
            c.rect(10, y, 37, y, (*CYAN, 40))
        c.px(24, 6 + f, WHITE)
        c.line(24, 8, 24, 12, (*CYAN, 120), 1)
        c.save("prop_holo_%d" % f, pivot=(0.5, 0.0), outline=False)


def crate_barrel_rocks():
    c = Cv(20, 18)
    c.rect(0, 0, 19, 17, DIRT)
    c.rect(0, 0, 19, 1, DIRT_L)
    c.rect(0, 16, 19, 17, DIRT_D)
    for x in (0, 17):
        c.rect(x, 0, x + 2, 17, DIRT_D)
    c.line(2, 2, 17, 15, DIRT_D, 2)
    c.line(2, 15, 17, 2, DIRT_D, 2)
    c.rect(8, 6, 11, 11, AMB_D)
    c.px(9, 8, AMB_L)
    c.save("prop_crate", pivot=(0.5, 0.0))

    c = Cv(14, 20)
    c.rect(1, 1, 12, 18, RUST_D)
    c.rect(1, 1, 3, 18, RUST)
    c.rect(1, 1, 12, 2, RUST_L)
    c.rect(11, 3, 12, 18, (70, 30, 14))
    c.rect(0, 5, 13, 6, MET_D)
    c.rect(0, 13, 13, 14, MET_D)
    c.rect(4, 8, 9, 11, AMB)
    c.rect(5, 9, 8, 10, RED_D)
    c.px(6, 9, WHITE)
    c.save("prop_barrel", pivot=(0.5, 0.0))

    c = Cv(40, 16)
    c.poly([(0, 15), (4, 8), (10, 4), (17, 7), (22, 2), (30, 6), (36, 9), (39, 15)], ROCK)
    c.poly([(4, 15), (7, 10), (11, 7), (16, 10), (22, 6), (28, 9), (33, 11), (36, 15)], ROCK_L)
    c.line(10, 4, 17, 7, ROCK_LL, 1)
    c.line(22, 2, 30, 6, ROCK_LL, 1)
    c.rect(0, 14, 39, 15, ROCK_DD)
    c.px(15, 9, DIRT_L)
    c.px(25, 8, DIRT_L)
    c.save("prop_rocks", pivot=(0.5, 0.0))

    for i, (w, h) in enumerate(((12, 20), (9, 14))):
        c = Cv(w, h)
        c.poly([(0, 0), (w - 1, 0), (w // 2 + 1, h - 1)], ROCK_D)
        c.poly([(1, 0), (w // 2, 0), (w // 2, h - 3)], ROCK)
        c.line(w // 2, 0, w // 2, h - 4, ROCK_L, 1)
        c.px(w // 2, h - 1, DIRT_L)
        c.save("prop_stalactite_%d" % i, pivot=(0.5, 1.0))


def cables():
    for i, (w, h, sag) in enumerate(((36, 20, 9), (28, 14, 6))):
        c = Cv(w, h)
        pts = []
        for x in range(w):
            t = x / (w - 1.0)
            y = int(4 * math.sin(math.pi * t) * 0 + sag * math.sin(math.pi * t))
            pts.append((x, y))
        for k, col in enumerate((AMB_D, MET_D, RUST_D)):
            for (x, y) in pts:
                c.px(x, min(h - 1, y + k), col)
        for x in (0, w - 1):
            c.rect(x, 0, x, 1, MET_L)
        c.save("prop_cable_%d" % i, pivot=(0.5, 1.0))


def main():
    tiles()
    lamp()
    albert_all()
    capsule()
    craft_bench()
    fusion_machine()
    exit_door()
    tesla()
    monitors()
    generator()
    shelf()
    robot_arm()
    prototype_case()
    server_rack()
    hologram_table()
    crate_barrel_rocks()
    cables()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (can cu cua Albert) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
