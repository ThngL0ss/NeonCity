#!/usr/bin/env python3
"""
Vẽ sprite cho hai địa điểm mới + màn hướng dẫn (dùng chung công cụ vẽ + bảng màu của gen_art.py):

  PHỐ NEON (thành phố công nghệ trung tâm, ban đêm, neon rực rỡ)
    city_sky, city_far, city_mid        bầu trời + 2 lớp nhà cao tầng xa (thị sai)
    city_floor_top, city_floor_fill     mặt đường (tile 16x16)
    city_bld_a..d                       toà nhà cao tầng ở sát đường (không va chạm, vẽ phía sau người chơi)
    city_sign_a/b/c_0..1                biển neon dọc nhấp nháy
    city_billboard_0..1                 bảng quảng cáo hologram
    city_lamp, city_vending             đèn đường, máy bán hàng tự động
  RÌA PHỐ NEON (vùng trũng bên cạnh, rác + khí thải của thành phố trên cao)
    rim_sky, rim_far, rim_mid           trời khói bẩn + bóng cống thải / lều trại xa
    rim_floor_top, rim_floor_fill       nền đất bùn lẫn rác (tile 16x16)
    rim_junk_a..c                       đống rác công nghệ (màn hình vỡ, ống, lốp, bo mạch)
    rim_barrel_toxic, rim_shack, rim_fire_0..1, rim_drip_0..1, rim_fly_0..1
  NPC (nhìn sang PHẢI, canvas 32x32, chân ở y=31)
    npc_h1/h2/h3_walk_0..3, _idle_0..1  người dân (3 kiểu trang phục)
    npc_raider_walk_0..3, _idle_0..1    NPC đeo mặt nạ, mang vũ khí (Rìa Phố Neon)
    npc_bot_a_roll_0..1                 robot bánh xe
    npc_bot_b_walk_0..3, _idle_0..1     robot hai chân
  MÀN HƯỚNG DẪN
    tut_door_0..1                       cánh cửa dẫn vào cảnh mở đầu (nhấp nháy)
    tut_sign_0..1                       bảng chỉ dẫn phát sáng

Chạy:  python3 Tools/PixelArt/gen_places.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import math
import os
import random
import sys

import numpy as np

import gen_art as G
from gen_art import (Cv, lerp, skyline, save_array, OUT, NAVY, NAVY2, NAVYL, CYAN, CYAN_D, MAG, MAG_D,
                     PURP, PURP_D, LIME, ORG, ORG_D, RED, RED_D, YEL, WHITE)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

DARK = (8, 8, 22)
STEEL = (70, 78, 108)
STEEL_L = (128, 140, 176)
STEEL_D = (36, 40, 64)
RUST = (150, 82, 40)
RUST_D = (92, 46, 26)
MUD = (44, 36, 28)
MUD_D = (28, 22, 18)
MUD_L = (72, 60, 44)
SLUDGE = (92, 150, 40)
SLUDGE_D = (50, 86, 26)
SKIN_A = (236, 190, 150)
SKIN_B = (190, 132, 96)
SKIN_C = (120, 78, 56)


# ================================================================ PHỐ NEON
def city_backgrounds():
    W, H = 480, 270
    a = np.zeros((H, W, 4), np.uint8)
    top, mid, hor = (3, 3, 18), (20, 8, 52), (78, 16, 84)
    for y in range(H):
        t = min(1.0, (int(y / 12) * 12 + 6) / 200.0)
        col = lerp(top, mid, min(1.0, t * 1.2)) if t < 0.7 else lerp(mid, hor, (t - 0.7) / 0.3)
        a[y, :] = (*col, 255)
    rng = random.Random(41)
    for _ in range(90):
        x, y = rng.randint(0, W - 1), rng.randint(0, 150)
        a[y, x] = (*rng.choice([WHITE, CYAN, MAG, WHITE]), 255)
    # vệt sáng ngang xa xa (đèn của xe bay)
    for k in range(9):
        y = rng.randint(30, 170)
        x0 = rng.randint(0, W - 40)
        ln = rng.randint(10, 28)
        c = rng.choice([CYAN, MAG, YEL])
        for x in range(x0, x0 + ln):
            a[y, x] = (*lerp(c, (0, 0, 0), 0.55), 255)
    save_array("city_sky", a, ppu=16, pivot=(0.5, 0.5))

    far = skyline(256, 128, 61, (12, 10, 36), (190, 70, 255), [PURP, CYAN, MAG], 60, 124, 10, 22, 0.28)
    save_array("city_far", far, ppu=16, pivot=(0.5, 0.0), tile=True)
    mid = skyline(256, 128, 77, (8, 8, 28), (26, 220, 240), [CYAN, YEL, MAG, CYAN, ORG], 70, 126, 14, 28, 0.42)
    save_array("city_mid", mid, ppu=16, pivot=(0.5, 0.0), tile=True)


def city_tiles():
    # mặt đường: dải neon mảnh trên cùng, nhựa đường, khe lát mỗi 8 px
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            a[y, x] = (*lerp((52, 56, 84), (26, 28, 50), y / 15.0), 255)
    a[0, :] = (*CYAN, 255)
    a[1, :] = (*CYAN_D, 255)
    a[2, :] = (*STEEL_L, 255)
    a[3, :] = (*STEEL, 255)
    for x in (0, 8):
        a[4:16, x] = (*STEEL_D, 255)
    for x in range(3, 13):
        a[10, x] = (*STEEL_D, 255)
    for (x, y) in ((3, 6), (12, 6), (4, 13)):
        a[y, x] = (*STEEL_L, 255)
    a[6, 9:12] = (*MAG_D, 255)
    save_array("city_floor_top", a, tile=True)

    b = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            b[y, x] = (*lerp((24, 26, 46), (12, 12, 26), y / 15.0), 255)
    for x in (0, 8):
        b[:, x] = (*STEEL_D, 255)
    b[7, :] = (*STEEL_D, 255)
    b[3, 4] = (*STEEL, 255)
    b[11, 12] = (*STEEL, 255)
    save_array("city_floor_fill", b, tile=True)


def building(name, w, h, seed, body, trim, accents, roof):
    """Toà nhà cao tầng nhìn thẳng: thân + lưới cửa sổ sáng / tối + dải neon hai bên + mái + cửa tiệm ở chân. Pivot dưới giữa."""
    rng = random.Random(seed)
    c = Cv(w, h)
    for y in range(h):
        c.rect(0, y, w - 1, y, lerp(body, lerp(body, (0, 0, 0), 0.5), y / float(h)))
    c.rect(0, 0, 1, h - 1, lerp(body, trim, 0.25))                  # mép sáng trái
    c.rect(w - 2, 0, w - 1, h - 1, lerp(body, (0, 0, 0), 0.4))      # mép tối phải
    c.rect(w // 2 - 1, 6, w // 2, h - 14, lerp(body, (0, 0, 0), 0.25))   # rãnh giữa
    # cửa sổ
    for wy in range(14, h - 20, 9):
        lit_row = rng.random()
        for wx in range(5, w - 8, 8):
            if rng.random() < 0.58 + 0.2 * (lit_row > 0.5):
                col = rng.choice(accents)
                c.rect(wx, wy, wx + 4, wy + 5, lerp(col, (0, 0, 0), 0.25))
                c.rect(wx + 1, wy + 1, wx + 3, wy + 3, col)
                if rng.random() < 0.3:
                    c.px(wx + 2, wy + 2, WHITE)
            else:
                c.rect(wx, wy, wx + 4, wy + 5, (10, 10, 24))
                c.rect(wx, wy, wx + 4, wy, lerp(body, trim, 0.2))
        c.rect(2, wy + 7, w - 3, wy + 7, lerp(body, (0, 0, 0), 0.35))       # sàn tầng
    # mái: gờ neon + ăng-ten + thiết bị
    c.rect(0, 0, w - 1, 3, lerp(body, trim, 0.35))
    c.rect(0, 0, w - 1, 0, trim)
    ax = rng.randint(6, w - 7)
    c.rect(ax, 0, ax, 0, WHITE)
    if roof == 0:
        pass
    if roof == 1:
        c.rect(w // 2 - 6, 0, w // 2 + 5, 1, trim)
    # cửa tiệm ở chân: khung neon + cửa kính sáng
    sx0, sx1 = 6, w - 7
    c.rect(sx0, h - 16, sx1, h - 1, (6, 6, 18))
    c.rect(sx0, h - 16, sx1, h - 16, trim)
    c.rect(sx0 + 1, h - 14, sx1 - 1, h - 2, lerp(accents[0], (0, 0, 0), 0.55))
    c.rect(sx0 + 3, h - 12, sx0 + 8, h - 2, lerp(accents[-1], (0, 0, 0), 0.2))
    c.rect(sx1 - 8, h - 12, sx1 - 3, h - 2, lerp(accents[0], (255, 255, 255), 0.15))
    c.rect(w // 2 - 2, h - 12, w // 2 + 1, h - 1, (4, 4, 14))
    c.save(name, pivot=(0.5, 0.0))


def city_buildings():
    building("city_bld_a", 96, 240, 3, (22, 24, 60), CYAN, [CYAN, YEL, MAG, WHITE], 1)
    building("city_bld_b", 72, 200, 5, (30, 16, 56), MAG, [MAG, PURP, ORG, YEL], 0)
    building("city_bld_c", 112, 176, 8, (14, 28, 54), LIME, [LIME, CYAN, YEL], 1)
    building("city_bld_d", 80, 256, 13, (36, 22, 44), ORG, [ORG, YEL, CYAN, MAG], 0)


def glyph(c, x, y, w, h, col, rng):
    """Ký hiệu giống chữ tượng hình (không phải chữ thật): vài nét ngang / dọc ngẫu nhiên."""
    for _ in range(3):
        if rng.random() < 0.5:
            yy = rng.randint(y, y + h - 1)
            c.rect(x, yy, x + w - 1, yy, col)
        else:
            xx = rng.randint(x, x + w - 1)
            c.rect(xx, y, xx, y + h - 1, col)
    c.px(x + rng.randint(0, w - 1), y + rng.randint(0, h - 1), WHITE)


def city_signs():
    specs = (("a", CYAN, MAG, 11), ("b", MAG, YEL, 22), ("c", ORG, CYAN, 33))
    for key, col, col2, seed in specs:
        for f in range(2):
            rng = random.Random(seed)
            c = Cv(14, 44)
            c.rect(0, 0, 13, 43, (10, 10, 26))
            c.rect(0, 0, 13, 0, col)
            c.rect(0, 43, 13, 43, col)
            c.rect(0, 0, 0, 43, col)
            c.rect(13, 0, 13, 43, col2)
            for k in range(4):
                gx, gy = 3, 3 + k * 10
                lit = (k + f) % 2 == 0
                gc = col if lit else lerp(col, (0, 0, 0), 0.65)
                glyph(c, gx, gy, 8, 8, gc, rng)
            c.save("city_sign_%s_%d" % (key, f), pivot=(0.5, 0.5), outline=False)


def city_billboard():
    for f in range(2):
        c = Cv(56, 32)
        c.rect(0, 0, 55, 31, (6, 6, 20))
        for y in range(2, 30):
            c.rect(2, y, 53, y, lerp((40, 10, 90), (120, 20, 110), y / 30.0))
        c.ell(30 + f, 7, 44 + f, 21, lerp(YEL, MAG, 0.4))
        for yy in range(15, 22, 3):
            c.rect(30, yy, 46, yy, (60, 12, 80))
        for k in range(4):
            c.rect(6, 7 + k * 5, 16 + (k * 3 + f * 2) % 8, 8 + k * 5, CYAN if k % 2 == f else CYAN_D)
        for y in range(2, 30, 2):
            c.rect(2, y, 53, y, (0, 0, 0, 40))
        c.rect(0, 0, 55, 1, CYAN)
        c.rect(0, 30, 55, 31, MAG)
        c.rect(0, 0, 1, 31, CYAN)
        c.rect(54, 0, 55, 31, MAG)
        c.save("city_billboard_%d" % f, pivot=(0.5, 0.0), outline=False)


def city_props():
    # đèn đường
    c = Cv(16, 60)
    c.rect(7, 8, 8, 59, STEEL)
    c.rect(7, 8, 7, 59, STEEL_L)
    c.rect(5, 56, 10, 59, STEEL_D)
    c.rect(7, 4, 14, 5, STEEL)
    c.rect(11, 5, 15, 8, (255, 240, 190))
    c.rect(12, 6, 14, 7, WHITE)
    c.save("city_lamp", pivot=(0.5, 0.0))

    # máy bán hàng tự động
    c = Cv(22, 32)
    c.rect(1, 1, 20, 31, (30, 34, 70))
    c.rect(1, 1, 20, 2, CYAN)
    c.rect(3, 4, 15, 22, (6, 8, 24))
    rng = random.Random(5)
    for r in range(4):
        for k in range(3):
            c.rect(4 + k * 4, 5 + r * 4, 6 + k * 4, 7 + r * 4, rng.choice([MAG, YEL, LIME, ORG, CYAN]))
    c.rect(17, 5, 19, 12, (10, 10, 30))
    c.px(18, 7, LIME)
    c.px(18, 9, RED)
    c.rect(3, 25, 15, 28, (4, 4, 14))
    c.rect(17, 15, 19, 21, STEEL)
    c.save("city_vending", pivot=(0.5, 0.0))


# ================================================================ RÌA PHỐ NEON
def rim_backgrounds():
    W, H = 480, 270
    a = np.zeros((H, W, 4), np.uint8)
    top, mid, hor = (6, 8, 8), (24, 24, 14), (74, 64, 24)
    for y in range(H):
        t = min(1.0, (int(y / 14) * 14 + 7) / 200.0)
        col = lerp(top, mid, min(1.0, t * 1.3)) if t < 0.7 else lerp(mid, hor, (t - 0.7) / 0.3)
        a[y, :] = (*col, 255)
    rng = random.Random(97)
    for _ in range(260):                                  # vệt khói bẩn
        x, y = rng.randint(0, W - 1), rng.randint(0, 180)
        ln = rng.randint(6, 22)
        col = lerp((a[y, x][0], a[y, x][1], a[y, x][2]), (60, 52, 30), 0.35)
        for xx in range(x, min(W, x + ln)):
            a[y, xx] = (*col, 255)
    for _ in range(40):
        x, y = rng.randint(0, W - 1), rng.randint(0, 120)
        a[y, x] = (*rng.choice([(150, 150, 120), (110, 150, 80)]), 255)
    save_array("rim_sky", a, ppu=16, pivot=(0.5, 0.5))

    far = skyline(256, 128, 83, (14, 14, 10), (120, 100, 30), [SLUDGE, ORG, (150, 130, 40)], 70, 126, 12, 26, 0.10)
    save_array("rim_far", far, ppu=16, pivot=(0.5, 0.0), tile=True)
    mid = skyline(256, 96, 59, (18, 15, 12), (96, 78, 30), [ORG, SLUDGE, (170, 80, 30)], 22, 70, 6, 16, 0.16)
    save_array("rim_mid", mid, ppu=16, pivot=(0.5, 0.0), tile=True)


def rim_tiles():
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            a[y, x] = (*lerp(MUD_L, MUD, y / 15.0), 255)
    a[0, :] = (*MUD_D, 255)
    a[1, :] = (*MUD_L, 255)
    for (x, y, n) in ((1, 5, 4), (9, 3, 3), (6, 9, 5), (12, 12, 3)):
        a[y, x:x + n] = (*MUD_D, 255)
    for (x, y) in ((3, 2), (11, 6)):
        a[y, x] = (*RUST, 255)
    for (x, y) in ((7, 2), (14, 10)):
        a[y, x] = (*SLUDGE_D, 255)
        a[y + 1, x] = (*SLUDGE, 255)
    a[5, 12:15] = (*STEEL, 255)
    save_array("rim_floor_top", a, tile=True)

    b = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            b[y, x] = (*lerp(MUD, MUD_D, y / 15.0), 255)
    for (x, y) in ((2, 3), (10, 8), (5, 12), (13, 2)):
        b[y, x] = (*MUD_L, 255)
    for (x, y) in ((8, 5), (3, 10)):
        b[y, x] = (*RUST_D, 255)
    save_array("rim_floor_fill", b, tile=True)


def junk(name, w, h, seed):
    """Đống rác công nghệ: màn hình vỡ, ống, lốp, bo mạch. Pivot dưới giữa."""
    rng = random.Random(seed)
    c = Cv(w, h)
    # gò rác (đa giác thấp)
    pts = [(0, h - 1), (w * 0.15, h * 0.55), (w * 0.4, h * 0.25), (w * 0.62, h * 0.4), (w * 0.85, h * 0.6), (w - 1, h - 1)]
    c.poly(pts, MUD_D)
    for _ in range(int(w * h / 38)):
        x = rng.randint(2, w - 8)
        y = rng.randint(int(h * 0.3), h - 5)
        kind = rng.random()
        if kind < 0.25:       # màn hình vỡ
            c.rect(x, y, x + 6, y + 4, STEEL_D)
            c.rect(x + 1, y + 1, x + 5, y + 3, (10, 22, 26))
            c.px(x + 2, y + 2, CYAN_D)
            c.line(x + 1, y + 1, x + 4, y + 3, (0, 0, 0), 1)
        elif kind < 0.5:      # ống / thanh gỉ
            c.line(x, y, x + rng.randint(4, 9), y + rng.randint(-3, 3), rng.choice([RUST, RUST_D, STEEL]), 2)
        elif kind < 0.7:      # lốp
            c.ell(x, y, x + 6, y + 6, (14, 14, 18))
            c.ell(x + 2, y + 2, x + 4, y + 4, MUD_D)
        elif kind < 0.85:     # bo mạch
            c.rect(x, y, x + 5, y + 3, (20, 70, 40))
            c.px(x + 1, y + 1, YEL)
            c.px(x + 4, y + 2, ORG)
        else:                 # vỏ thùng
            c.rect(x, y, x + 4, y + 5, STEEL)
            c.rect(x, y, x + 4, y, STEEL_L)
    for _ in range(max(2, w // 12)):                  # vũng sệt phát sáng
        x, y = rng.randint(2, w - 6), rng.randint(h - 6, h - 2)
        c.rect(x, y, x + rng.randint(2, 4), y, SLUDGE)
    c.rim_top(MUD_L)
    c.save(name, pivot=(0.5, 0.0))


def rim_props():
    junk("rim_junk_a", 64, 36, 2)
    junk("rim_junk_b", 48, 28, 7)
    junk("rim_junk_c", 88, 46, 12)

    # thùng độc hại
    c = Cv(16, 20)
    c.rect(2, 2, 13, 19, (60, 110, 30))
    c.rect(2, 2, 13, 3, SLUDGE)
    c.rect(2, 9, 13, 10, (30, 60, 18))
    c.rect(2, 17, 13, 18, (30, 60, 18))
    c.rect(6, 12, 9, 15, YEL)
    c.px(7, 13, DARK)
    c.px(8, 14, DARK)
    c.rect(11, 4, 12, 8, SLUDGE)                      # vệt rỉ
    c.px(12, 9, SLUDGE)
    c.save("rim_barrel_toxic", pivot=(0.5, 0.0))

    # lều trại tạm bợ: khung gỉ + tấm bạt + đèn cầy
    c = Cv(72, 52)
    c.poly([(4, 51), (10, 14), (60, 10), (68, 51)], (54, 44, 36))
    c.poly([(4, 51), (10, 14), (30, 12), (24, 51)], (70, 58, 44))
    for x in range(14, 60, 11):
        c.line(x, 14 - (x // 20), x + 3, 51, MUD_D, 1)
    c.rect(34, 30, 50, 51, (6, 4, 4))                 # cửa lều tối
    c.rect(36, 36, 38, 38, ORG)                       # đốm sáng trong lều
    c.rect(2, 8, 5, 51, STEEL)                        # cột
    c.rect(2, 8, 2, 51, STEEL_L)
    c.rect(62, 6, 65, 51, STEEL)
    c.rect(58, 3, 70, 7, RUST)
    c.rect(12, 20, 20, 26, MAG_D)                     # miếng vá
    c.rect(13, 21, 19, 25, (110, 30, 90))
    c.save("rim_shack", pivot=(0.5, 0.0))

    # thùng lửa (2 khung)
    for f in range(2):
        c = Cv(16, 28)
        c.rect(3, 14, 12, 27, RUST_D)
        c.rect(3, 14, 12, 15, RUST)
        c.rect(3, 20, 12, 20, MUD_D)
        flame = [(4, 14), (6, 7 + f), (8, 11), (9, 4 - f), (11, 14)]
        c.poly(flame, ORG)
        c.poly([(6, 14), (8, 8 + f), (10, 14)], YEL)
        c.px(8, 12, WHITE)
        c.save("rim_fire_%d" % f, pivot=(0.5, 0.0))

    # ống xả nhỏ giọt (2 khung giọt rơi)
    for f in range(2):
        c = Cv(16, 40)
        c.rect(5, 0, 10, 8, STEEL)
        c.rect(5, 0, 10, 1, STEEL_L)
        c.rect(4, 8, 11, 11, STEEL_D)
        c.rect(6, 11, 9, 12, SLUDGE_D)
        c.px(7, 14 + f * 10, SLUDGE)
        c.px(7, 15 + f * 10, SLUDGE)
        c.px(8, 26 + f * 8, SLUDGE_D)
        c.save("rim_drip_%d" % f, pivot=(0.5, 1.0), outline=False)

    # ruồi (2 khung vỗ cánh)
    for f in range(2):
        c = Cv(5, 4)
        c.px(2, 2, (10, 10, 10))
        c.px(2, 1, (10, 10, 10))
        c.px(1, 0 + f, (150, 170, 150))
        c.px(3, 0 + f, (150, 170, 150))
        c.save("rim_fly_%d" % f, pivot=(0.5, 0.5), outline=False)


# ================================================================ NPC
def human(name, p, phase, bob=0, acc="none"):
    """Người / robot nhìn sang PHẢI. phase: 0 đứng, 1-3 các nhịp đi. p = bảng màu (dict)."""
    c = Cv(32, 32)
    cx = 16
    hip = 22 + bob
    feet_f, feet_b = {0: ((18, 31), (14, 31)), 1: ((21, 31), (11, 30)), 2: ((17, 31), (15, 29)), 3: ((12, 31), (20, 30))}[phase]
    arm_f, arm_b = {0: (1, 8), 1: (-3, 8), 2: (0, 8), 3: (3, 8)}[phase], {0: (-1, 8), 1: (3, 8), 2: (0, 8), 3: (-3, 8)}[phase]
    sy = 14 + bob

    def leg(foot, col, bx):
        c.line(cx + bx, hip, foot[0], foot[1] - 2, col, 3)
        c.rect(foot[0] - 2, foot[1] - 1, foot[0] + 2, foot[1], p["boots"])

    def arm(hand, col, glove, sx):
        c.line(cx + sx, sy, cx + sx + hand[0], sy + hand[1], col, 2)
        c.rect(cx + sx + hand[0] - 1, sy + hand[1] - 1, cx + sx + hand[0] + 1, sy + hand[1] + 1, glove)

    arm(arm_b, p["jacket_d"], p["skin_d"], -1)
    leg(feet_b, p["pants_d"], -1)
    # thân
    c.poly([(cx - 4, 13 + bob), (cx + 4, 13 + bob), (cx + 4, hip + 2), (cx - 4, hip + 2)], p["jacket"])
    c.rect(cx - 4, 13 + bob, cx - 3, hip + 1, p["jacket_d"])
    c.rect(cx - 4, hip, cx + 4, hip + 1, p["pants_d"])
    if "stripe" in p:
        c.rect(cx - 4, 17 + bob, cx + 4, 17 + bob, p["stripe"])
        c.px(cx + 2, 15 + bob, p["stripe"])
    leg(feet_f, p["pants"], 1)
    # đầu
    top = 3 + bob
    if acc == "bot":
        c.rect(cx - 3, top + 1, cx + 4, top + 9, p["skin"])
        c.rect(cx - 3, top + 1, cx + 4, top + 1, STEEL_L)
        c.rect(cx + 1, top + 4, cx + 4, top + 6, DARK)
        c.rect(cx + 2, top + 5, cx + 4, top + 5, p["accent"])
        c.line(cx - 1, top + 1, cx - 1, top - 3, STEEL_L, 1)
        c.px(cx - 1, top - 4, p["accent"])
    else:
        c.ell(cx - 3, top + 1, cx + 4, top + 9, p["skin"])
        c.px(cx + 3, top + 5, DARK)                       # mắt
        c.rect(cx - 4, top + 1, cx, top + 4, p["hair"])
        c.px(cx + 1, top, p["hair"])
        c.px(cx - 2, top, p["hair"])
        c.px(cx + 4, top + 7, p["skin_d"])
    if acc == "cap":
        c.rect(cx - 4, top, cx + 3, top + 2, p["accent"])
        c.rect(cx + 2, top + 2, cx + 7, top + 2, p["accent"])
    elif acc == "helmet":
        c.rect(cx - 4, top - 1, cx + 4, top + 2, p["accent"])
        c.rect(cx - 4, top + 2, cx + 5, top + 2, p["accent"])
        c.px(cx, top - 2, WHITE)
    elif acc == "visor":
        c.rect(cx + 1, top + 4, cx + 5, top + 5, p["accent"])
        c.px(cx + 5, top + 4, WHITE)
    elif acc == "mask":                                    # mặt nạ phòng độc + mũ trùm
        c.rect(cx - 4, top, cx + 4, top + 3, p["jacket_d"])
        c.rect(cx - 4, top + 3, cx - 3, top + 8, p["jacket_d"])
        c.rect(cx + 1, top + 5, cx + 6, top + 9, (60, 64, 70))
        c.rect(cx + 6, top + 7, cx + 7, top + 8, (40, 44, 50))
        c.rect(cx + 1, top + 4, cx + 4, top + 5, p["accent"])      # kính mặt nạ phát sáng
        c.px(cx + 2, top + 4, WHITE)
        c.px(cx + 3, top + 8, STEEL_L)
    # tay trước
    if acc == "mask":                                      # cầm súng chĩa xuống thấp, không nhắm vào ai
        c.line(cx - 4, 21 + bob, cx + 9, 18 + bob, (30, 32, 38), 2)
        c.rect(cx - 6, 20 + bob, cx - 3, 23 + bob, (50, 40, 30))
        c.rect(cx + 7, 17 + bob, cx + 10, 18 + bob, STEEL)
        c.line(cx + 1, sy, cx + 3, 20 + bob, p["jacket"], 2)
        c.rect(cx + 2, 19 + bob, cx + 4, 21 + bob, p["skin_d"])
        c.line(cx - 4, 14 + bob, cx + 4, 21 + bob, RUST_D, 1)             # dây đạn chéo ngực
    else:
        arm(arm_f, p["jacket"], p["skin"], 1)
    c.save(name, pivot=(0.5, 0.0))


def pal(skin, hair, jacket, pants, accent, stripe=None):
    d = {
        "skin": skin, "skin_d": lerp(skin, (0, 0, 0), 0.3), "hair": hair,
        "jacket": jacket, "jacket_d": lerp(jacket, (0, 0, 0), 0.4),
        "pants": pants, "pants_d": lerp(pants, (0, 0, 0), 0.4),
        "boots": (24, 22, 30), "accent": accent,
    }
    if stripe:
        d["stripe"] = stripe
    return d


def npc_set(key, p, acc):
    human("npc_%s_idle_0" % key, p, 0, 0, acc)
    human("npc_%s_idle_1" % key, p, 0, 1, acc)
    for i, ph in enumerate((1, 2, 3, 2)):
        human("npc_%s_walk_%d" % (key, i), p, ph, 1 if i % 2 == 0 else 0, acc)


def npcs():
    npc_set("h1", pal(SKIN_A, (40, 28, 24), (30, 90, 120), (34, 36, 66), CYAN, CYAN), "visor")
    npc_set("h2", pal(SKIN_B, (20, 18, 30), (150, 36, 120), (28, 28, 52), MAG, YEL), "cap")
    npc_set("h3", pal(SKIN_C, (12, 12, 16), (200, 110, 30), (50, 46, 40), YEL, WHITE), "helmet")
    npc_set("raider", pal(SKIN_B, (10, 10, 10), (52, 56, 40), (40, 38, 30), RED, None), "mask")
    npc_set("bot_b", pal(STEEL, STEEL_D, (60, 70, 110), (40, 46, 80), LIME, CYAN), "bot")

    # robot bánh xe (2 khung quay bánh + nháy đèn)
    for f in range(2):
        c = Cv(28, 28)
        c.rect(5, 7, 22, 18, STEEL)
        c.rect(5, 7, 22, 8, STEEL_L)
        c.rect(5, 17, 22, 18, STEEL_D)
        c.rect(14, 10, 21, 15, DARK)
        c.rect(16 + f, 11, 19 + f, 14, CYAN)
        c.px(17 + f, 12, WHITE)
        c.line(9, 7, 9, 2, STEEL_L, 1)
        c.px(9, 1, LIME if f == 0 else (30, 80, 30))
        c.rect(6, 12, 9, 14, ORG)
        for wx in (7, 18):
            c.ell(wx - 3, 18, wx + 3, 24, (16, 16, 24))
            c.ell(wx - 1, 20, wx + 1, 22, STEEL_L)
            c.px(wx - 2 + 3 * f, 20 + f, STEEL_L)
        c.save("npc_bot_a_roll_%d" % f, pivot=(0.5, 0.0))


# ================================================================ MÀN HƯỚNG DẪN
def tutorial():
    for f in range(2):
        c = Cv(40, 64)
        c.rect(2, 2, 37, 63, (22, 26, 56))
        c.rect(2, 2, 37, 4, STEEL_L)
        c.rect(2, 2, 4, 63, STEEL)
        c.rect(35, 2, 37, 63, STEEL_D)
        c.rect(8, 8, 31, 63, (4, 6, 20))                   # ô cửa
        for y in range(10, 62, 4):
            c.rect(10, y, 29, y, lerp((20, 120, 150), (140, 30, 150), y / 62.0) if (y // 4 + f) % 2 == 0 else (10, 40, 70))
        c.rect(8, 8, 31, 9, CYAN)
        c.rect(8, 8, 9, 63, CYAN)
        c.rect(30, 8, 31, 63, MAG)
        c.ell(15, 4, 24, 12, (255, 235, 77) if f == 0 else (255, 150, 30))
        c.save("tut_door_%d" % f, pivot=(0.5, 0.0))
    for f in range(2):
        c = Cv(24, 32)
        c.rect(11, 14, 12, 31, STEEL)
        c.rect(1, 1, 22, 16, (8, 10, 30))
        c.rect(1, 1, 22, 1, CYAN)
        c.rect(1, 16, 22, 16, CYAN_D)
        c.rect(1, 1, 1, 16, CYAN)
        c.rect(22, 1, 22, 16, CYAN)
        c.line(5, 8, 15, 8, YEL if f == 0 else ORG, 2)
        c.poly([(14, 4), (19, 8), (14, 12)], YEL if f == 0 else ORG)
        c.save("tut_sign_%d" % f, pivot=(0.5, 0.0), outline=False)


def main():
    city_backgrounds()
    city_tiles()
    city_buildings()
    city_signs()
    city_billboard()
    city_props()
    rim_backgrounds()
    rim_tiles()
    rim_props()
    npcs()
    tutorial()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (Pho Neon / Ria Pho Neon / NPC / huong dan) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
