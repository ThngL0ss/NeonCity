#!/usr/bin/env python3
"""
Vẽ thêm sprite cho 2 loại quái mới (dùng chung bộ công cụ vẽ và bảng màu của gen_art.py):
  heavy_*   : quái giáp nặng - to, chậm, máu trâu
  rusher_*  : quái lao nhanh - nhỏ, mỏng, sát thương lớn

Chạy:  python3 Tools/PixelArt/gen_enemies2.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị, quay mặt sang PHẢI.
"""
import json
import os
import sys

import gen_art as G
from gen_art import Cv, CYAN, CYAN_D, MAG, MAG_D, PURP, PURP_D, LIME, ORG, RED, RED_D, YEL, WHITE, NAVYL

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

STEEL = (34, 44, 98)
STEEL_D = (18, 24, 60)
VENOM = (22, 74, 40)
VENOM_D = (12, 44, 26)


# ================================================================ HEAVY (quái giáp nặng)
def heavy(name, phase=0, windup=False, lunge=False):
    c = Cv(44, 34)
    # trạng thái tư thế
    if lunge:
        bx0, by0, bx1, by1 = 6, 12, 42, 26          # thân dài ra phía trước
        lift = 0
    elif windup:
        bx0, by0, bx1, by1 = 4, 15, 39, 27          # thân hạ thấp, gồng
        lift = 3                                      # giáp lưng dựng lên
    else:
        bx0, by0, bx1, by1 = 4, 9 + (phase % 2), 39, 25
        lift = 0
    # chân: 4 chân to
    legx = [8, 15, 25, 32]
    for i, lx in enumerate(legx):
        raised = ((i + phase) % 2 == 0) and not windup and not lunge
        top = by1 - 3
        foot = 31 if raised else 33
        if lunge:
            c.line(lx + 3, top, lx - 5, 33, STEEL_D, 3)
            c.rect(lx - 7, 32, lx - 3, 33, PURP)
        else:
            c.rect(lx, top, lx + 2, foot, STEEL_D)
            c.rect(lx - 1, foot - 1, lx + 4, foot, PURP)       # bàn chân phát sáng
            c.px(lx + 1, top + 1, CYAN_D)
    # thân
    c.ell(bx0, by0, bx1, by1, STEEL)
    c.ell(bx0 + 2, by0 + 5, bx1 - 2, by1 - 1, STEEL_D)
    # giáp lưng: 3 tấm
    for k, px0 in enumerate((bx0 + 5, bx0 + 13, bx0 + 21)):
        y0 = by0 - 2 - lift - (1 if k == 1 else 0)
        c.rect(px0, y0, px0 + 6, by0 + 5, NAVYL)
        c.rect(px0, y0, px0 + 6, y0, CYAN)                       # viền sáng neon
        c.line(px0 + 3, y0 + 1, px0 + 3, by0 + 4, STEEL_D)
        c.px(px0 + 6, y0 + 1, STEEL_D)
    # gai lưng
    for sx in (bx0 + 8, bx0 + 16, bx0 + 24):
        c.poly([(sx - 1, by0 - 2 - lift), (sx, by0 - 6 - lift - (2 if windup else 0)), (sx + 1, by0 - 2 - lift)], PURP)
    # lõi năng lượng ở sườn
    cx, cy = bx0 + 12, by0 + 9
    core = RED if windup else MAG
    c.ell(cx - 3, cy - 3, cx + 3, cy + 3, MAG_D)
    c.ell(cx - 2, cy - 2, cx + 2, cy + 2, core)
    c.px(cx, cy, WHITE)
    # đầu / hàm phía trước (bên phải)
    hx0 = bx1 - 7
    c.rect(hx0, by0 + 1, bx1 + 3, by1 - 2, STEEL_D)
    c.rect(hx0 + 1, by0 + 1, bx1 + 3, by0 + 2, NAVYL)
    # sừng hàm
    c.poly([(bx1 + 3, by0 + 2), (bx1 + 4, by0 + 6), (bx1 - 1, by0 + 5)], PURP)
    c.poly([(bx1 + 3, by1 - 3), (bx1 + 4, by1 - 7), (bx1 - 1, by1 - 6)], PURP)
    # mắt
    ex, ey = hx0 + 2, by0 + 4
    if windup:
        c.rect(ex - 1, ey - 1, ex + 4, ey + 3, RED)
        c.rect(ex, ey, ex + 3, ey + 2, WHITE)
    else:
        c.rect(ex, ey, ex + 3, ey + 1, YEL)
        c.px(ex + 3, ey, WHITE)
    # đường viền đáy
    c.rect(bx0 + 4, by1 - 2, bx1 - 4, by1 - 2, PURP_D)
    c.save(name, pivot=(0.5, 0.0))


def make_heavy():
    for i in range(4):
        heavy("heavy_walk_%d" % i, phase=i)
    heavy("heavy_windup", windup=True)
    heavy("heavy_lunge", lunge=True)


# ================================================================ RUSHER (quái lao nhanh)
def rusher(name, phase=0, windup=False, lunge=False):
    c = Cv(30, 18)
    if lunge:
        ty, by = 6, 12
    elif windup:
        ty, by = 8, 14
    else:
        ty, by = 5 + (phase % 2), 11 + (phase % 2)
    # chân: 3 chân mảnh gập về sau
    for i, lx in enumerate((8, 13, 18)):
        raised = ((i + phase) % 2 == 0) and not windup and not lunge
        if lunge:
            c.line(lx, by, lx - 6, 16, VENOM_D, 1)
            c.px(lx - 6, 16, LIME)
        elif windup:
            c.line(lx, by, lx - 2, 16, VENOM_D, 1)
            c.line(lx - 2, 16, lx + 1, 16, LIME)
        else:
            fx = lx + (3 if raised else -1)
            fy = 14 if raised else 16
            c.line(lx, by, fx, fy, VENOM_D, 1)
            c.px(fx, fy, LIME)
    # thân: hình nêm thuôn về phía trước (phải)
    c.poly([(2, ty + 3), (7, ty), (19, ty - 1), (27, ty + 3), (27, by - 1), (18, by + 1), (6, by)], VENOM)
    c.poly([(5, by - 2), (18, by - 1), (26, by - 2), (18, by + 1), (6, by)], VENOM_D)
    c.rim_top(LIME)
    # sọc vàng báo hiệu tốc độ
    c.line(8, ty + 3, 20, ty + 2, YEL)
    # đuôi gai
    c.poly([(2, ty + 3), (-1, ty + 1), (0, ty + 4)], LIME)
    c.poly([(3, ty), (0, ty - 3), (6, ty + 1)], LIME)
    # sừng lao phía trước
    hornw = 3 if lunge else 2
    c.poly([(26, ty + 1), (29, ty + 3), (26, ty + 3 + hornw)], YEL)
    c.px(29, ty + 3, WHITE)
    # mắt
    ex, ey = 19, ty + 1
    if windup:
        c.rect(ex - 1, ey, ex + 4, ey + 2, RED)
        c.rect(ex, ey + 1, ex + 3, ey + 1, WHITE)
    else:
        c.rect(ex, ey, ex + 3, ey + 1, RED)
        c.px(ex + 3, ey, WHITE)
    c.save(name, pivot=(0.5, 0.0))


def make_rusher():
    for i in range(4):
        rusher("rusher_walk_%d" % i, phase=i)
    rusher("rusher_windup", windup=True)
    rusher("rusher_lunge", lunge=True)


def main():
    make_heavy()
    make_rusher()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite quai moi -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
