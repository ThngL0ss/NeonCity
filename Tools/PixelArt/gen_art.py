#!/usr/bin/env python3
"""
Sinh toàn bộ sprite pixel-art neon của Neon Breaker thành file PNG thật.

Chạy:  python3 Tools/PixelArt/gen_art.py
Kết quả: Assets/NeonBreaker/Art/*.png  +  manifest.json (PPU, pivot cho từng ảnh - Unity tự đọc khi import)

Bạn có thể mở các PNG này bằng Aseprite / Photoshop / Paint để chỉnh sửa hoặc vẽ đè lên.
Quy ước: 16 pixel = 1 đơn vị thế giới (PPU 16). Nhân vật quay mặt sang PHẢI.
"""
import json
import math
import os
import random

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_DIR = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "NeonBreaker", "Art"))
os.makedirs(OUT_DIR, exist_ok=True)

# ---------------------------------------------------------------- palette
OUT = (6, 8, 24)
NAVY = (24, 34, 78)
NAVY2 = (14, 20, 52)
NAVYL = (44, 62, 122)
CYAN = (26, 242, 255)
CYAN_D = (10, 120, 160)
MAG = (255, 38, 204)
MAG_D = (140, 18, 110)
PURP = (140, 77, 255)
PURP_D = (66, 36, 140)
LIME = (115, 255, 102)
ORG = (255, 153, 26)
ORG_D = (160, 70, 10)
RED = (255, 51, 64)
RED_D = (130, 16, 34)
YEL = (255, 235, 77)
WHITE = (255, 255, 255)

MANIFEST = {}


def lerp(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3))


class Cv:
    """Canvas pixel-art nhỏ: mọi nét vẽ đều không khử răng cưa."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    @staticmethod
    def _c(c):
        return (c[0], c[1], c[2], c[3] if len(c) > 3 else 255)

    def px(self, x, y, c):
        x, y = int(x), int(y)
        if 0 <= x < self.w and 0 <= y < self.h:
            self.im.putpixel((x, y), self._c(c))

    def rect(self, x0, y0, x1, y1, c):
        self.d.rectangle([int(x0), int(y0), int(x1), int(y1)], fill=self._c(c))

    def line(self, x0, y0, x1, y1, c, w=1):
        self.d.line([int(x0), int(y0), int(x1), int(y1)], fill=self._c(c), width=w)

    def poly(self, pts, c):
        self.d.polygon([(int(x), int(y)) for x, y in pts], fill=self._c(c))

    def ell(self, x0, y0, x1, y1, c):
        self.d.ellipse([int(x0), int(y0), int(x1), int(y1)], fill=self._c(c))

    def outline(self, c=OUT):
        a = np.array(self.im)
        alpha = a[:, :, 3] > 0
        p = np.pad(alpha, 1)
        n = p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]
        edge = n & ~alpha
        a[edge] = (c[0], c[1], c[2], 255)
        self.im = Image.fromarray(a, "RGBA")
        self.d = ImageDraw.Draw(self.im)

    def rim_top(self, c):
        """Tô sáng pixel trên cùng của mỗi cột (viền neon)."""
        a = np.array(self.im)
        for x in range(self.w):
            col = np.where(a[:, x, 3] > 0)[0]
            if len(col):
                a[col[0], x] = (c[0], c[1], c[2], 255)
        self.im = Image.fromarray(a, "RGBA")
        self.d = ImageDraw.Draw(self.im)

    def save(self, name, ppu=16, pivot=(0.5, 0.0), outline=True, tile=False):
        if outline:
            self.outline()
        self.im.save(os.path.join(OUT_DIR, name + ".png"))
        MANIFEST[name + ".png"] = {"ppu": ppu, "pivot": [pivot[0], pivot[1]], "tile": tile, "w": self.w, "h": self.h}
        return self.im


def save_array(name, arr, ppu=16, pivot=(0.5, 0.5), tile=False):
    im = Image.fromarray(arr.astype(np.uint8), "RGBA")
    im.save(os.path.join(OUT_DIR, name + ".png"))
    MANIFEST[name + ".png"] = {"ppu": ppu, "pivot": [pivot[0], pivot[1]], "tile": tile, "w": im.width, "h": im.height}


# ================================================================ PLAYER
PCX = 12  # tâm cơ thể trong canvas 32x32


def player(name, foot_f, foot_b, lean=0, bob=0, hand_f=(3, 20), hand_b=(-3, 20), blade=None,
           scarf=0.0, visor=MAG, chest=CYAN, head_dy=0, belt=MAG):
    c = Cv(32, 32)
    cx = PCX + lean
    hipy = 21 + bob

    def leg(foot, col, front):
        fx, fy = foot
        hx = cx + (1 if front else -1)
        kx = (hx + fx) // 2 + (2 if front else 1)
        ky = (hipy + fy) // 2
        c.line(hx, hipy, kx, ky, col, 3)
        c.line(kx, ky, fx, fy - 1, col, 3)
        c.rect(fx - 1, fy - 1, fx + 3, fy, CYAN if front else CYAN_D)
        c.px(fx + 3, fy - 1, WHITE if front else CYAN)

    def arm(hand, col, glove):
        sx, sy = cx + (2 if glove == CYAN else -2), 14 + bob
        hx, hy = cx + hand[0], hand[1]
        c.line(sx, sy, hx, hy, col, 2)
        c.rect(hx - 1, hy - 1, hx + 1, hy + 1, glove)

    # kiếm đeo sau lưng khi không rút
    if blade is None:
        c.line(cx - 3, 11 + bob, cx - 7, 23 + bob, CYAN_D, 1)
        c.px(cx - 3, 11 + bob, MAG)
        c.px(cx - 3, 12 + bob, MAG)

    arm(hand_b, NAVY2, CYAN_D)
    leg(foot_b, NAVY2, False)

    # khăn quàng neon
    for i in range(9):
        sx = cx - 4 - i
        sy = 13 + bob + int(round(math.sin(i * 0.7 + scarf) * 1.5)) + i // 3
        c.px(sx, sy, MAG)
        c.px(sx, sy + 1, MAG_D if i % 2 else MAG)

    # thân
    c.rect(cx - 4, 12 + bob, cx + 4, 21 + bob, NAVY)
    c.rect(cx - 4, 12 + bob, cx - 3, 21 + bob, NAVY2)
    c.rect(cx + 1, 14 + bob, cx + 2, 18 + bob, chest)
    c.px(cx + 1, 14 + bob, WHITE)
    c.rect(cx - 4, 20 + bob, cx + 4, 20 + bob, belt)
    c.rect(cx - 5, 12 + bob, cx - 2, 14 + bob, PURP)

    leg(foot_f, NAVY, True)

    # đầu
    hy0 = 3 + bob + head_dy
    c.rect(cx - 3, hy0, cx + 4, hy0 + 7, NAVY)
    c.rect(cx - 3, hy0 + 2, cx + 4, hy0 + 2, CYAN)
    c.rect(cx + 1, hy0 + 3, cx + 4, hy0 + 4, visor)
    c.px(cx + 4, hy0 + 3, WHITE)
    c.px(cx - 2, hy0 - 1, CYAN)

    arm(hand_f, NAVYL, CYAN)
    if blade is not None:
        hx, hy = cx + hand_f[0], hand_f[1]
        tx, ty = hx + blade[0], hy + blade[1]
        c.line(hx, hy, tx, ty, CYAN, 3)
        c.line(hx, hy, tx, ty, WHITE, 1)
        c.rect(hx - 1, hy - 2, hx, hy + 2, MAG)

    c.save(name, pivot=(PCX / 32.0, 0.0))


def make_player():
    cx = PCX
    player("player_idle_0", (cx + 3, 31), (cx - 2, 31), scarf=0.0)
    player("player_idle_1", (cx + 3, 31), (cx - 2, 31), bob=1, scarf=1.2, hand_f=(3, 21), hand_b=(-3, 21))
    for i in range(6):
        ph = 2 * math.pi * i / 6.0
        s, co = math.sin(ph), math.cos(ph)
        ff = (cx + 1 + int(round(5 * s)), 31 - int(round(4 * max(0, co))))
        fb = (cx + 1 - int(round(5 * s)), 31 - int(round(4 * max(0, -co))))
        bob = 0 if abs(s) > 0.5 else 1
        hand_f = (3 - int(round(3 * s)), 19 + int(round(1 * co)))
        hand_b = (-3 + int(round(3 * s)), 19 - int(round(1 * co)))
        player("player_run_%d" % i, ff, fb, lean=1, bob=bob, hand_f=hand_f, hand_b=hand_b, scarf=i * 0.9)
    player("player_jump", (cx + 4, 27), (cx - 2, 28), hand_f=(5, 15), hand_b=(-4, 16), scarf=2.0)
    player("player_fall", (cx + 5, 30), (cx - 3, 29), hand_f=(6, 16), hand_b=(-5, 15), scarf=3.0)
    player("player_attack_0", (cx + 4, 31), (cx - 3, 31), lean=-1, hand_f=(-2, 9), blade=(-4, -9), scarf=1.0)
    player("player_attack_1", (cx + 6, 31), (cx - 4, 31), lean=2, hand_f=(7, 14), blade=(14, -1), scarf=2.5)
    player("player_attack_2", (cx + 5, 31), (cx - 4, 31), lean=2, hand_f=(6, 19), blade=(11, 6), scarf=3.5)
    player("player_dash", (cx - 2, 30), (cx - 9, 30), lean=4, bob=3, hand_f=(8, 18), hand_b=(-1, 18), scarf=0.0,
           blade=None)
    player("player_hurt", (cx + 2, 31), (cx - 5, 29), lean=-2, hand_f=(-4, 15), hand_b=(-6, 17), visor=RED,
           head_dy=1, scarf=4.0)
    player("player_heal", (cx + 3, 31), (cx - 2, 31), hand_f=(4, 9), hand_b=(-3, 9), chest=LIME, visor=LIME, belt=LIME,
           scarf=1.5)


# ================================================================ WALKER
def walker(name, phase=0, squash=0, lunge=False, windup=False):
    c = Cv(30, 22)
    body = (70, 18, 40)
    body_d = (44, 10, 28)
    if lunge:
        bx0, by0, bx1, by1 = 3, 8, 27, 17
    elif windup:
        bx0, by0, bx1, by1 = 5, 9, 24, 17
    else:
        bx0, by0, bx1, by1 = 5, 5 + squash, 24, 16
    # chân
    legx = [8, 12, 17, 21]
    for i, lx in enumerate(legx):
        raised = ((i + phase) % 2 == 0) and not windup
        fy = 19 if raised else 21
        if lunge:
            c.line(lx + 2, by1 - 1, lx - 4, 20, body_d, 2)
            c.px(lx - 4, 20, ORG)
        else:
            c.line(lx, by1 - 1, lx + (1 if raised else 0), fy, body_d, 2)
            c.px(lx + (1 if raised else 0), fy, ORG)
    c.ell(bx0, by0, bx1, by1, body)
    c.ell(bx0 + 2, by0 + 3, bx1 - 2, by1 - 1, body_d)
    c.rim_top(ORG)
    # gai lưng
    sp = 6 if windup else 4
    for sx in (bx0 + 5, bx0 + 9, bx0 + 13):
        c.poly([(sx - 2, by0 + 1), (sx, by0 - sp), (sx + 2, by0 + 1)], RED)
        c.px(sx, by0 - sp, YEL)
    # mắt
    ex = bx1 - 6
    ey = by0 + 3
    if windup:
        c.rect(ex - 1, ey - 1, ex + 4, ey + 2, RED)
        c.rect(ex, ey, ex + 3, ey + 1, WHITE)
    else:
        c.rect(ex, ey, ex + 3, ey + 1, YEL)
        c.px(ex + 3, ey, WHITE)
    c.rect(bx0 + 3, by1 - 2, bx1 - 3, by1 - 2, ORG_D)
    c.save(name, pivot=(0.5, 0.0))


def make_walker():
    for i in range(4):
        walker("walker_walk_%d" % i, phase=i)
    walker("walker_windup", windup=True)
    walker("walker_lunge", lunge=True)


# ================================================================ FLYER
def flyer(name, flap=0, charge=False):
    c = Cv(32, 26)
    cx, cy = 16, 13
    wing = PURP
    if flap == 0:     # cánh giơ lên
        c.poly([(cx - 4, cy - 1), (cx - 14, cy - 11), (cx - 8, cy + 1)], wing)
        c.poly([(cx + 4, cy - 1), (cx + 14, cy - 11), (cx + 8, cy + 1)], wing)
    else:             # cánh hạ xuống
        c.poly([(cx - 4, cy), (cx - 14, cy + 6), (cx - 7, cy + 4)], wing)
        c.poly([(cx + 4, cy), (cx + 14, cy + 6), (cx + 7, cy + 4)], wing)
    c.poly([(cx, cy - 9), (cx + 9, cy), (cx, cy + 9), (cx - 9, cy)], (62, 12, 72))
    c.poly([(cx, cy - 6), (cx + 6, cy), (cx, cy + 6), (cx - 6, cy)], MAG_D)
    c.line(cx, cy - 9, cx + 9, cy, MAG, 1)
    c.line(cx + 9, cy, cx, cy + 9, MAG, 1)
    c.line(cx, cy - 9, cx - 9, cy, MAG, 1)
    c.line(cx - 9, cy, cx, cy + 9, MAG, 1)
    if charge:
        c.rect(cx + 1, cy - 2, cx + 6, cy + 3, RED)
        c.rect(cx + 2, cy - 1, cx + 5, cy + 2, WHITE)
        for (dx, dy) in ((8, 0), (-1, -4), (-1, 4)):
            c.px(cx + dx, cy + dy, RED)
    else:
        c.rect(cx + 2, cy - 1, cx + 5, cy + 1, YEL)
        c.px(cx + 5, cy - 1, WHITE)
    c.px(cx, cy - 11, CYAN)
    c.line(cx, cy - 9, cx, cy - 11, CYAN_D, 1)
    c.save(name, pivot=(0.5, 0.5))


def make_flyer():
    flyer("flyer_fly_0", 0)
    flyer("flyer_fly_1", 1)
    flyer("flyer_charge", 0, charge=True)


# ================================================================ TOWER
def tower(name, t=0.0, step=0, shard=0):
    c = Cv(40, 76)
    cx = 20
    dark = (32, 18, 70)
    # đế
    c.rect(4, 68, 35, 74, dark)
    c.rect(4, 68, 35, 68, MAG)
    c.rect(7, 73, 32, 73, MAG_D)
    for x in (8, 16, 24, 31):
        c.px(x, 71, CYAN)
    # cánh chống
    c.poly([(4, 68), (12, 52), (15, 68)], PURP_D)
    c.poly([(35, 68), (27, 52), (24, 68)], PURP_D)
    c.line(5, 67, 12, 53, PURP, 1)
    c.line(34, 67, 27, 53, PURP, 1)
    # thân
    c.rect(15, 36, 24, 68, dark)
    c.rect(16, 36, 16, 68, NAVYL)
    for ln in (17, 22):
        for y in range(40, 66):
            if (y + step * 3) % 8 < 4:
                c.px(ln, y, CYAN)
    # đài đỡ pha lê
    c.rect(11, 30, 28, 37, PURP_D)
    c.rect(11, 30, 28, 30, PURP)
    # pha lê
    cr = lerp(MAG, WHITE, t)
    c.poly([(cx, 3), (30, 20), (cx, 37), (10, 20)], MAG_D)
    c.poly([(cx, 7), (27, 20), (cx, 33), (13, 20)], cr)
    c.poly([(cx, 3), (cx + 2, 20), (cx, 37), (cx - 2, 20)], lerp(WHITE, MAG, 0.15))
    c.line(cx - 6, 14, cx - 2, 8, WHITE, 1)
    # mảnh trôi nổi
    for (sx, sy) in ((4, 16 + shard), (35, 26 - shard), (6, 36 - shard)):
        c.poly([(sx, sy - 3), (sx + 2, sy), (sx, sy + 3), (sx - 2, sy)], CYAN)
        c.px(sx, sy - 1, WHITE)
    c.save(name, pivot=(0.5, 0.0))


def make_tower():
    for i, (t, sh) in enumerate(((0.0, 0), (0.3, 1), (0.6, 2), (0.3, 1))):
        tower("tower_idle_%d" % i, t, i, sh)


# ================================================================ BOSS
def boss1(name, bob=0, lf=0, lb=0, crouch=0, core=0.0, air=False):
    c = Cv(76, 90)
    cx = 38
    BDK = (28, 14, 54)
    BMD = (56, 30, 104)
    ground = 88
    cr = crouch
    b = bob
    # chân
    for side, lift in ((-1, lb), (1, lf)):
        x0 = cx + side * 11 - 6
        x1 = cx + side * 11 + 6
        top = 62 + cr + b
        fy = ground - (12 if air else lift)
        c.rect(x0, top, x1, fy, BDK)
        c.rect(x0, top, x0 + 1, fy, BMD)
        c.rect(x0 - 2 + (3 if side > 0 else 0), fy - 5, x1 + 5, fy + 0, PURP_D)
        c.rect(x0 - 2 + (3 if side > 0 else 0), fy - 5, x1 + 5, fy - 5, PURP)
        c.rect(x0 + 2, top + 3, x0 + 2, fy - 7, MAG)
    # thân
    c.rect(cx - 22, 30 + cr + b, cx + 22, 64 + cr + b, BDK)
    c.rect(cx - 19, 33 + cr + b, cx + 19, 61 + cr + b, BMD)
    c.rect(cx - 22, 30 + cr + b, cx + 22, 31 + cr + b, MAG)
    c.rect(cx - 22, 63 + cr + b, cx + 22, 64 + cr + b, MAG_D)
    # lõi năng lượng
    ccy = 46 + cr + b
    rad = 9
    c.poly([(cx + 3, ccy - rad - 2), (cx + 3 + rad, ccy), (cx + 3, ccy + rad + 2), (cx + 3 - rad, ccy)], MAG_D)
    c.poly([(cx + 3, ccy - rad + 2), (cx + 3 + rad - 3, ccy), (cx + 3, ccy + rad - 2), (cx + 3 - rad + 3, ccy)],
           lerp(MAG, WHITE, core))
    c.px(cx + 1, ccy - 3, WHITE)
    if core > 0.5:
        for dx, dy in ((0, -13), (13, 0), (0, 14), (-10, 0)):
            c.px(cx + 3 + dx, ccy + dy, WHITE)
    # vai và gai
    for side in (-1, 1):
        sx0 = cx + side * 21 - (6 if side < 0 else -6)
        c.rect(min(sx0, sx0 + side * 12), 26 + cr + b, max(sx0, sx0 + side * 12), 40 + cr + b, BMD)
        pxx = cx + side * 27
        c.poly([(pxx - 5, 27 + cr + b), (pxx + side * 2, 12 + cr + b), (pxx + 5, 27 + cr + b)], PURP)
        c.px(pxx + side * 2, 13 + cr + b, WHITE)
    # đầu
    hy = 8 + cr + b
    c.rect(cx - 10, hy, cx + 10, hy + 20, BDK)
    c.rect(cx - 10, hy, cx + 10, hy + 1, BMD)
    c.rect(cx - 2, hy + 8, cx + 9, hy + 11, YEL)
    c.rect(cx - 2, hy + 7, cx + 9, hy + 7, MAG)
    c.rect(cx - 2, hy + 12, cx + 9, hy + 12, MAG)
    c.px(cx + 9, hy + 8, WHITE)
    c.poly([(cx - 9, hy + 1), (cx - 17, hy - 11), (cx - 3, hy + 1)], PURP)
    c.poly([(cx + 3, hy + 1), (cx + 12, hy - 12), (cx + 9, hy + 1)], PURP)
    # tay: pháo bên phải, tay trái buông
    c.rect(cx - 34, 40 + cr + b, cx - 26, 62 + cr + b, BDK)
    c.rect(cx - 34, 40 + cr + b, cx - 33, 62 + cr + b, BMD)
    c.rect(cx + 24, 40 + cr + b, cx + 36, 52 + cr + b, PURP_D)
    c.rect(cx + 24, 40 + cr + b, cx + 36, 41 + cr + b, PURP)
    c.rect(cx + 33, 43 + cr + b, cx + 37, 49 + cr + b, MAG)
    c.px(cx + 37, 46 + cr + b, WHITE)
    c.save(name, pivot=(0.5, 0.0))


def make_boss1():
    boss1("boss1_idle_0", 0, core=0.0)
    boss1("boss1_idle_1", 1, core=0.8)
    walk = [(0, 6, 0), (1, 0, 0), (0, 0, 6), (1, 0, 0)]
    for i, (b, lf, lb) in enumerate(walk):
        boss1("boss1_walk_%d" % i, b, lf=lf, lb=lb, core=0.3 * (i % 2))
    boss1("boss1_crouch", 0, crouch=10, core=0.9)
    boss1("boss1_air", 0, core=0.6, air=True)


def boss2(name, bob=0, lf=0, lb=0, pose="idle", core=0.0):
    c = Cv(64, 84)
    cx = 26
    D = (46, 8, 24)
    M = (96, 22, 40)
    ground = 82
    air = pose == "air"
    b = bob
    # áo choàng / gai sau lưng
    c.poly([(cx - 10, 34 + b), (cx - 28, 44 + b), (cx - 12, 56 + b)], RED_D)
    c.poly([(cx - 10, 30 + b), (cx - 22, 28 + b), (cx - 10, 42 + b)], RED_D)
    c.line(cx - 28, 44 + b, cx - 10, 34 + b, ORG, 1)
    # chân
    for side, lift in ((-1, lb), (1, lf)):
        x0 = cx + side * 6 - 4
        x1 = cx + side * 6 + 4
        top = 56 + b
        fy = ground - (10 if air else lift)
        c.rect(x0, top, x1, fy, D)
        c.rect(x0, top, x0 + 1, fy, M)
        c.rect(x0 - 1 + (2 if side > 0 else 0), fy - 4, x1 + 4, fy, ORG_D)
        c.rect(x0 - 1 + (2 if side > 0 else 0), fy - 4, x1 + 4, fy - 4, ORG)
        c.px(x0 + 2, top + 4, ORG)
    # thân
    c.rect(cx - 12, 30 + b, cx + 12, 58 + b, D)
    c.rect(cx - 9, 33 + b, cx + 9, 55 + b, M)
    c.rect(cx - 12, 30 + b, cx + 12, 31 + b, ORG)
    c.rect(cx - 12, 57 + b, cx + 12, 58 + b, RED)
    ccy = 44 + b
    c.poly([(cx + 2, ccy - 8), (cx + 8, ccy), (cx + 2, ccy + 8), (cx - 4, ccy)], RED)
    c.poly([(cx + 2, ccy - 5), (cx + 5, ccy), (cx + 2, ccy + 5), (cx - 1, ccy)], lerp(ORG, WHITE, 0.4 + core * 0.5))
    # đầu + sừng
    hy = 10 + b
    c.rect(cx - 7, hy, cx + 7, hy + 17, D)
    c.rect(cx - 7, hy, cx + 7, hy, M)
    c.rect(cx - 1, hy + 7, cx + 7, hy + 9, YEL)
    c.rect(cx - 1, hy + 6, cx + 7, hy + 6, ORG)
    c.px(cx + 7, hy + 7, WHITE)
    c.poly([(cx - 6, hy), (cx - 12, hy - 14), (cx - 1, hy)], RED)
    c.poly([(cx + 1, hy), (cx + 8, hy - 15), (cx + 6, hy)], RED)
    c.px(cx - 12, hy - 14, WHITE)
    c.px(cx + 8, hy - 15, WHITE)
    # vai
    c.rect(cx - 17, 28 + b, cx - 10, 38 + b, M)
    c.rect(cx + 10, 28 + b, cx + 17, 38 + b, M)
    c.poly([(cx + 11, 28 + b), (cx + 16, 17 + b), (cx + 18, 28 + b)], ORG)
    # tay + lưỡi kiếm năng lượng
    if pose == "slash":
        c.rect(cx + 12, 38 + b, cx + 22, 42 + b, D)
        c.line(cx + 21, 40 + b, 62, 40 + b, ORG, 5)
        c.line(cx + 21, 40 + b, 62, 40 + b, WHITE, 1)
        c.line(cx - 12, 40 + b, cx - 20, 46 + b, D, 4)
    elif pose == "cast":
        c.line(cx + 12, 34 + b, cx + 18, 16 + b, D, 4)
        c.line(cx - 12, 34 + b, cx - 18, 16 + b, D, 4)
        c.ell(cx - 8, 0, cx + 8, 12, ORG)
        c.ell(cx - 5, 2, cx + 5, 10, WHITE)
        c.px(cx - 21, 15 + b, ORG)
        c.px(cx + 21, 15 + b, ORG)
    else:
        c.line(cx + 12, 36 + b, cx + 20, 52 + b, D, 4)
        c.line(cx + 20, 52 + b, cx + 28, 62 + b, ORG, 2)
        c.line(cx + 20, 52 + b, cx + 28, 62 + b, WHITE, 1)
        c.line(cx - 12, 36 + b, cx - 19, 54 + b, D, 4)
        c.line(cx - 19, 54 + b, cx - 25, 64 + b, ORG, 2)
    c.save(name, pivot=(cx / 64.0, 0.0))


def make_boss2():
    boss2("boss2_idle_0", 0)
    boss2("boss2_idle_1", 1, core=0.8)
    walk = [(0, 6, 0), (1, 0, 0), (0, 0, 6), (1, 0, 0)]
    for i, (b, lf, lb) in enumerate(walk):
        boss2("boss2_walk_%d" % i, b, lf=lf, lb=lb)
    boss2("boss2_slash", 0, pose="slash")
    boss2("boss2_cast", 0, pose="cast", core=1.0)
    boss2("boss2_air", 0, pose="air")


# ================================================================ PROJECTILES & PICKUP
def make_projectiles():
    # phi tiêu 4 cánh
    c = Cv(14, 14)
    c.poly([(7, 0), (9, 5), (13, 7), (9, 9), (7, 13), (5, 9), (0, 7), (5, 5)], (225, 245, 255))
    c.poly([(7, 3), (8, 6), (10, 7), (8, 8), (7, 10), (6, 8), (4, 7), (6, 6)], CYAN)
    c.px(7, 7, WHITE)
    c.save("proj_shuriken", pivot=(0.5, 0.5))

    # đạn năng lượng (xám sáng để tô màu bằng SpriteRenderer.color)
    c = Cv(16, 8)
    c.poly([(15, 4), (10, 0), (1, 1), (0, 4), (1, 6), (10, 7)], (210, 210, 210))
    c.poly([(13, 4), (9, 2), (4, 2), (3, 4), (4, 5), (9, 5)], WHITE)
    c.save("proj_bolt", pivot=(0.5, 0.5), outline=False)

    # sóng xung kích 2 khung
    for i in range(2):
        c = Cv(20, 28)
        sh = 2 if i else 0
        c.poly([(10, 0 + sh), (19, 27), (1, 27)], (205, 205, 205))
        c.poly([(10, 8 + sh), (15, 27), (5, 27)], WHITE)
        c.poly([(10, 14), (12, 27), (8, 27)], (235, 235, 235))
        c.save("proj_wave_%d" % i, pivot=(0.5, 0.0), outline=False)

    # vật phẩm hồi máu 2 khung
    for i in range(2):
        c = Cv(16, 16)
        c.ell(1, 1, 14, 14, (6, 40, 24))
        for k in range(16):
            ang = k / 16.0 * 2 * math.pi
            x = 7.5 + 6.6 * math.cos(ang)
            y = 7.5 + 6.6 * math.sin(ang)
            c.px(x, y, LIME if (k + i) % 2 else (60, 170, 70))
        c.rect(6, 3, 9, 12, WHITE)
        c.rect(3, 6, 12, 9, WHITE)
        c.rect(7, 4, 8, 11, LIME)
        c.rect(4, 7, 11, 8, LIME)
        c.save("pickup_%d" % i, pivot=(0.5, 0.5))


# ================================================================ FX
def make_fx():
    def ring_arr(n, fn):
        a = np.zeros((n, n, 4), np.uint8)
        for j in range(n):
            for i in range(n):
                x = ((i + 0.5) / n) * 2 - 1
                y = ((j + 0.5) / n) * 2 - 1
                v = fn(x, y)
                a[j, i] = (255, 255, 255, int(max(0, min(255, v))))
        return a

    save_array("fx_square", np.full((4, 4, 4), 255, np.uint8), ppu=4)

    def circ(x, y):
        return 255 if math.hypot(x, y) <= 0.95 else 0
    save_array("fx_circle", ring_arr(16, circ), ppu=16)

    def glow(x, y):
        d = math.hypot(x, y)
        for lim, al in ((0.22, 235), (0.42, 160), (0.62, 95), (0.82, 45), (1.0, 16)):
            if d < lim:
                return al
        return 0
    save_array("fx_glow", ring_arr(32, glow), ppu=32)

    def ring(x, y):
        d = math.hypot(x, y)
        if 0.82 <= d <= 0.96:
            return 255
        if 0.70 <= d < 0.82:
            return 70
        return 0
    save_array("fx_ring", ring_arr(48, ring), ppu=48)

    def diamond(x, y):
        return 255 if abs(x) + abs(y) <= 0.95 else 0
    save_array("fx_diamond", ring_arr(16, diamond), ppu=16)

    def tri(x, y):
        half = (1 - y) * 0.5
        return 255 if (abs(x) <= half * 0.95 and y > -0.95) else 0
    save_array("fx_triangle", ring_arr(16, tri), ppu=16)

    def star(x, y):
        return 255 if (abs(x) * abs(y) < 0.05 and math.hypot(x, y) < 0.97) or math.hypot(x, y) < 0.22 else 0
    save_array("fx_star", ring_arr(16, star), ppu=16)

    def arc(x, y):
        o = math.hypot(x, y) <= 0.96
        inner = math.hypot(x + 0.45, y) <= 0.90
        return 255 if (o and not inner) else 0
    save_array("fx_arc", ring_arr(32, arc), ppu=32)


# ================================================================ TILES & LEVEL PIECES
def make_tiles():
    # nền đất: hàng trên có đường neon cyan
    a = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            base = lerp((26, 22, 70), (10, 8, 34), y / 15.0)
            a[y, x] = (*base, 255)
    a[0, :] = (*CYAN, 255)
    a[1, :] = (*lerp(CYAN, (26, 22, 70), 0.55), 255)
    a[2, :] = (*lerp(CYAN, (26, 22, 70), 0.8), 255)
    for x in (3, 4, 11):
        a[6, x] = (*CYAN_D, 255)
    a[8, 8:12] = (*PURP, 255)
    a[12, 2:4] = (*MAG_D, 255)
    a[:, 0] = np.where(a[:, 0] > 0, a[:, 0] // 1, a[:, 0])
    save_array("tile_ground_top", a, ppu=16, tile=True)

    b = np.zeros((16, 16, 4), np.uint8)
    for y in range(16):
        for x in range(16):
            base = (10, 8, 34)
            if y == 0 or x == 0:
                base = (22, 18, 58)
            b[y, x] = (*base, 255)
    b[7, 4:9] = (*PURP_D, 255)
    b[11, 10:14] = (*CYAN_D, 255)
    b[3, 12] = (*MAG_D, 255)
    save_array("tile_ground_fill", b, ppu=16, tile=True)

    for nm, col in (("tile_plat", CYAN), ("tile_plat_arena", MAG)):
        p = np.zeros((8, 16, 4), np.uint8)
        for y in range(8):
            for x in range(16):
                base = (22, 18, 58) if y < 6 else (12, 10, 36)
                p[y, x] = (*base, 255)
        p[0, :] = (*col, 255)
        p[1, :] = (*lerp(col, (22, 18, 58), 0.6), 255)
        p[7, :] = (*lerp(col, (12, 10, 36), 0.7), 255)
        for x in (3, 4, 11):
            p[4, x] = (*lerp(col, (22, 18, 58), 0.4), 255)
        save_array("tile_plat" if nm == "tile_plat" else nm, p, ppu=16, tile=True)


def make_gate_arena():
    # trụ cổng
    c = Cv(20, 56)
    c.rect(3, 4, 16, 54, (30, 16, 66))
    c.rect(3, 4, 4, 54, NAVYL)
    c.rect(14, 6, 14, 52, MAG)
    c.rect(9, 8, 9, 50, MAG_D)
    c.poly([(10, 0), (17, 7), (10, 14), (3, 7)], MAG)
    c.poly([(10, 3), (14, 7), (10, 11), (6, 7)], WHITE)
    c.rect(1, 52, 18, 55, PURP_D)
    c.save("gate_post", pivot=(0.5, 0.0))

    # thanh chắn laser (xếp gạch theo chiều dọc)
    a = np.zeros((6, 16, 4), np.uint8)
    a[:, 2:4] = (*MAG, 140)
    a[:, 4:6] = (*WHITE, 230)
    a[:, 6:8] = (*MAG, 140)
    for y in range(6):
        if y % 3 == 0:
            a[y, 4:6] = (*MAG, 230)
    a = np.transpose(a, (1, 0, 2))      # 16 cao x 6 rộng
    a = np.ascontiguousarray(a)
    a[:, 0:1] = 0
    save_array("gate_bar", a, ppu=16, tile=True)

    # vòng rune dưới sân boss (elip dẹt)
    w, h = 256, 24
    arr = np.zeros((h, w, 4), np.uint8)
    for j in range(h):
        for i in range(w):
            x = ((i + 0.5) / w) * 2 - 1
            y = ((j + 0.5) / h) * 2 - 1
            d = math.hypot(x, y)
            for r0, col in ((0.96, MAG), (0.62, PURP), (0.30, CYAN)):
                if abs(d - r0) < 0.035:
                    arr[j, i] = (*col, 255)
                elif abs(d - r0) < 0.07:
                    arr[j, i] = (*col, 70)
    save_array("arena_rune", arr, ppu=16, pivot=(0.5, 0.5))

    c = Cv(20, 100)
    c.rect(4, 8, 15, 98, (24, 12, 56))
    c.rect(4, 8, 5, 98, NAVYL)
    c.rect(13, 10, 13, 96, MAG)
    for y in range(14, 94, 10):
        c.rect(7, y, 11, y + 1, CYAN_D)
    c.poly([(10, 0), (17, 8), (10, 16), (3, 8)], MAG)
    c.poly([(10, 4), (13, 8), (10, 12), (7, 8)], WHITE)
    c.rect(2, 95, 17, 99, PURP_D)
    c.save("arena_pillar", pivot=(0.5, 0.0))


# ================================================================ BACKGROUNDS
def skyline(w, h, seed, body, edge, win, minh, maxh, minw, maxw, win_density):
    rng = random.Random(seed)
    a = np.zeros((h, w, 4), np.uint8)
    blds = []
    x = 0
    while x < w:
        bw = rng.randint(minw, maxw)
        bh = rng.randint(minh, maxh)
        blds.append((x, bw, bh, rng.random()))
        x += bw + rng.randint(0, 3)
    for (bx, bw, bh, r) in blds:
        for ox in (0, -w):
            x0 = bx + ox
            top = h - bh
            for yy in range(top, h):
                for xx in range(x0, x0 + bw):
                    if 0 <= xx < w:
                        a[yy, xx] = (*body, 255)
            for xx in range(x0, x0 + bw):
                if 0 <= xx < w:
                    a[top, xx] = (*edge, 255)
            if 0 <= x0 < w:
                a[top:h, x0] = (*lerp(body, edge, 0.35), 255)
            for yy in range(top + 3, h - 2, 4):
                for xx in range(x0 + 2, x0 + bw - 2, 3):
                    if 0 <= xx < w and rng.random() < win_density:
                        wc = rng.choice(win)
                        a[yy, xx] = (*wc, 255)
                        if xx + 1 < w:
                            a[yy, xx + 1] = (*wc, 255)
            if r > 0.7:   # ăng-ten
                ax = x0 + bw // 2
                if 0 <= ax < w:
                    for yy in range(max(0, top - 6), top):
                        a[yy, ax] = (*edge, 255)
                    a[max(0, top - 7), ax] = (*WHITE, 255)
    return a


def make_backgrounds():
    # bầu trời: dải màu, sao, mặt trời synthwave
    W, H = 480, 270
    a = np.zeros((H, W, 4), np.uint8)
    top = (4, 5, 26)
    mid = (30, 10, 70)
    hor = (110, 24, 110)
    for y in range(H):
        t = y / 200.0
        if t <= 1.0:
            col = lerp(top, mid, min(1.0, t * 1.2)) if t < 0.7 else lerp(mid, hor, (t - 0.7) / 0.3)
        else:
            col = hor
        band = int(y / 14)               # chia dải màu kiểu pixel
        col = lerp(col, (0, 0, 0), 0.0)
        qy = band * 14 + 7
        t2 = min(1.0, qy / 200.0)
        col = lerp(top, mid, min(1.0, t2 * 1.2)) if t2 < 0.7 else lerp(mid, hor, (t2 - 0.7) / 0.3)
        a[y, :] = (*col, 255)
    rng = random.Random(7)
    for _ in range(140):
        x, y = rng.randint(0, W - 1), rng.randint(0, 170)
        c = rng.choice([WHITE, CYAN, MAG, WHITE, WHITE])
        a[y, x] = (*c, 255)
        if rng.random() < 0.15:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if 0 <= x + dx < W and 0 <= y + dy < H:
                    a[y + dy, x + dx] = (*lerp(c, (0, 0, 0), 0.6), 255)
    cx, cy, R = 350, 128, 56
    for y in range(cy - R - 18, cy + R + 18):
        for x in range(cx - R - 18, cx + R + 18):
            if not (0 <= x < W and 0 <= y < H):
                continue
            d = math.hypot(x - cx, y - cy)
            if d <= R:
                t = (y - (cy - R)) / (2.0 * R)
                if y > cy:   # vạch tối cắt ngang nửa dưới
                    k = int((y - cy) / 7)
                    if (y - cy) % 7 < min(5, 1 + k // 2):
                        continue
                tq = round(t * 5) / 5.0
                a[y, x] = (*lerp((255, 214, 80), (255, 50, 150), tq), 255)
            elif d <= R + 14 and (int(d) % 4) == 0:
                prev = a[y, x][:3].astype(float)
                a[y, x] = (*[int(min(255, prev[i] * 0.7 + MAG[i] * 0.3)) for i in range(3)], 255)
    save_array("bg_sky", a, ppu=16, pivot=(0.5, 0.5))

    far = skyline(256, 96, 11, (16, 12, 44), (150, 80, 255), [PURP, CYAN, MAG], 40, 92, 10, 24, 0.22)
    save_array("bg_far", far, ppu=16, pivot=(0.5, 0.0), tile=True)
    mid = skyline(256, 112, 23, (10, 10, 34), (26, 220, 240), [CYAN, YEL, MAG, CYAN], 46, 108, 12, 30, 0.34)
    save_array("bg_mid", mid, ppu=16, pivot=(0.5, 0.0), tile=True)

    # cột điện neon gần
    near = np.zeros((112, 128, 4), np.uint8)
    for k, px_ in enumerate((14, 64, 108)):
        h = [90, 104, 80][k]
        col = [CYAN, MAG, CYAN][k]
        near[112 - h:, px_:px_ + 4] = (8, 8, 26, 255)
        near[112 - h:, px_] = (*col, 255)
        near[112 - h - 3:112 - h, px_ - 1:px_ + 5] = (*col, 255)
        near[112 - h - 1, px_ + 1:px_ + 3] = (*WHITE, 255)
        for y in range(112 - h + 8, 112, 12):
            near[y, px_ + 1:px_ + 3] = (*col, 255)
    save_array("bg_near", near, ppu=16, pivot=(0.5, 0.0), tile=True)


# ================================================================ main
def main():
    make_player()
    make_walker()
    make_flyer()
    make_tower()
    make_boss1()
    make_boss2()
    make_projectiles()
    make_fx()
    make_tiles()
    make_gate_arena()
    make_backgrounds()
    with open(os.path.join(OUT_DIR, "manifest.json"), "w", encoding="utf8") as f:
        json.dump(MANIFEST, f, indent=1, sort_keys=True)
    print("Da tao %d sprite -> %s" % (len(MANIFEST), OUT_DIR))


if __name__ == "__main__":
    main()
