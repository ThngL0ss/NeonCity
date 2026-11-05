#!/usr/bin/env python3
"""
Vẽ sprite cho hệ cinematic (cảnh người chơi chết / hồi sinh), dùng chung công cụ vẽ + bảng màu của gen_art.py:

  player_skeleton     bộ khung robot lộ ra khi cơ thể tan (khung 32x32, cùng gốc toạ độ với sprite người chơi)
  robot_bit_0..3      mảnh linh kiện nhỏ: bánh răng, bu-lông, tấm giáp, chip

Chạy:  python3 Tools/PixelArt/gen_cine.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import os
import sys

import gen_art as G
from gen_art import (Cv, CYAN, CYAN_D, MAG, MAG_D, PURP, PURP_D, LIME, ORG, ORG_D, RED, RED_D, YEL, WHITE,
                     NAVY, NAVY2, NAVYL)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

STEEL = (170, 185, 220)
STEEL_L = (214, 224, 245)
STEEL_D = (96, 108, 154)
STEEL_DD = (58, 66, 104)


def skeleton():
    c = Cv(32, 32)
    # sọ
    c.rect(9, 3, 18, 11, STEEL_D)
    c.rect(10, 4, 17, 10, STEEL)
    c.rect(10, 4, 17, 4, STEEL_L)
    c.rect(11, 6, 17, 7, NAVY2)             # khe nhìn
    c.rect(13, 6, 17, 7, CYAN)
    c.px(17, 6, WHITE)
    c.rect(11, 9, 17, 10, STEEL_DD)         # hàm
    for x in (11, 13, 15, 17):
        c.px(x, 10, STEEL_L)
    c.px(11, 2, CYAN)                       # ăng-ten
    c.px(11, 1, CYAN)
    # cổ + cột sống
    c.rect(12, 12, 14, 13, STEEL_D)
    c.rect(13, 13, 14, 20, STEEL)
    c.line(13, 13, 13, 20, STEEL_L, 1)
    # lồng ngực
    for y in (14, 17, 19):
        c.rect(10, y, 17, y, STEEL_D)
        c.px(10, y + 1, STEEL_DD)
        c.px(17, y + 1, STEEL_DD)
    # lõi năng lượng
    c.rect(12, 14, 15, 17, CYAN_D)
    c.rect(13, 15, 14, 16, CYAN)
    c.px(13, 15, WHITE)
    # khớp vai + đinh tán
    for (x, y) in ((9, 14), (18, 14)):
        c.rect(x - 1, y - 1, x + 1, y + 1, STEEL_D)
        c.px(x, y, ORG)
    # tay trước (bên trái) gập xuống, tay sau (bên phải) buông
    c.line(8, 15, 5, 18, STEEL, 1)
    c.line(5, 18, 2, 19, STEEL, 1)
    c.rect(1, 18, 3, 20, STEEL_D)
    c.px(2, 19, MAG)
    c.line(19, 15, 20, 20, STEEL, 1)
    c.rect(19, 20, 21, 22, STEEL_D)
    # xương chậu
    c.rect(10, 21, 17, 23, STEEL_D)
    c.rect(11, 22, 16, 22, STEEL)
    c.px(13, 22, ORG)
    # chân: xương + piston
    c.line(11, 24, 11, 27, STEEL, 1)
    c.line(12, 24, 12, 27, CYAN_D, 1)
    c.rect(10, 27, 13, 28, STEEL_D)         # đầu gối
    c.line(11, 29, 11, 30, STEEL, 1)
    c.rect(9, 31, 14, 31, CYAN)
    c.line(16, 24, 17, 27, STEEL, 1)
    c.line(17, 24, 18, 27, CYAN_D, 1)
    c.rect(16, 27, 19, 28, STEEL_D)
    c.line(17, 29, 18, 30, STEEL, 1)
    c.rect(15, 31, 20, 31, CYAN)
    # dây cáp
    for y in (13, 20, 21):
        c.px(14, y, MAG)
    c.save("player_skeleton", pivot=(0.375, 0.0))


def bits():
    # 0: bánh răng
    c = Cv(7, 7)
    c.ell(0, 0, 6, 6, STEEL_D)
    c.ell(1, 1, 5, 5, STEEL)
    c.px(3, 3, NAVY2)
    for (x, y) in ((3, 0), (3, 6), (0, 3), (6, 3)):
        c.px(x, y, STEEL_L)
    c.save("robot_bit_0", pivot=(0.5, 0.5))
    # 1: bu-lông lục giác
    c = Cv(6, 6)
    c.rect(1, 0, 4, 5, STEEL_D)
    c.rect(0, 1, 5, 4, STEEL_D)
    c.rect(1, 1, 4, 4, STEEL)
    c.px(2, 2, STEEL_L)
    c.px(3, 3, STEEL_DD)
    c.save("robot_bit_1", pivot=(0.5, 0.5))
    # 2: tấm giáp
    c = Cv(8, 5)
    c.rect(0, 0, 7, 4, NAVYL)
    c.rect(1, 1, 6, 3, NAVY)
    c.rect(1, 1, 6, 1, CYAN_D)
    c.px(1, 3, MAG)
    c.px(6, 3, MAG)
    c.save("robot_bit_2", pivot=(0.5, 0.5))
    # 3: chip
    c = Cv(6, 6)
    c.rect(1, 1, 4, 4, NAVY2)
    c.rect(2, 2, 3, 3, CYAN)
    for i in (1, 3):
        c.px(0, i, STEEL)
        c.px(5, i, STEEL)
        c.px(i, 0, STEEL)
        c.px(i, 5, STEEL)
    c.save("robot_bit_3", pivot=(0.5, 0.5))


def main():
    skeleton()
    bits()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (khung robot, manh linh kien) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
