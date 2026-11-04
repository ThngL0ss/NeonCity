#!/usr/bin/env python3
"""
Vẽ sprite cho hệ thống Skill (dùng chung công cụ vẽ + bảng màu của gen_art.py):

  part_0..3        linh kiện rơi ra khi hạ quái (chip xoay, màu trắng-xám để game nhuộm theo loại quái)
  skill_<id>       icon 16x16 cho 9 kỹ năng mẫu:
                   parts, scavenger, efficient (nhánh bị động) · minibot, squad, slots, tactics, growth, repair (nhánh chủ động)

Chạy:  python3 Tools/PixelArt/gen_skill.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import math
import os
import sys

import gen_art as G
from gen_art import (Cv, CYAN, CYAN_D, MAG, MAG_D, PURP, PURP_D, LIME, ORG, ORG_D, RED, RED_D, YEL, WHITE,
                     NAVY, NAVY2, NAVYL)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

STEEL = (170, 185, 220)
STEEL_D = (90, 104, 150)
LIGHT = (236, 240, 250)
GREY = (150, 160, 185)
GREY_D = (84, 92, 120)


# ================================================================ linh kiện (xoay 4 khung, màu trung tính để nhuộm)
def parts():
    for i, w in enumerate((10, 7, 3, 7)):
        c = Cv(12, 12)
        x0 = 6 - w / 2.0
        x1 = 6 + w / 2.0 - 1
        c.rect(x0, 1, x1, 10, GREY_D)
        if w >= 6:
            c.rect(x0 + 1, 2, x1 - 1, 9, LIGHT)
            c.rect(x0 + 2, 4, x1 - 2, 7, GREY)
            c.px(x0 + 1, 2, WHITE)
            # chân chip hai bên
            for yy in (3, 5, 7):
                c.px(x0 - 1, yy, STEEL_D)
                c.px(x1 + 1, yy, STEEL_D)
        else:
            c.rect(x0, 1, x1, 10, LIGHT)
        c.save("part_%d" % i, pivot=(0.5, 0.5))


# ================================================================ icon kỹ năng
def icon(name, fn):
    c = Cv(16, 16)
    fn(c)
    c.save("skill_" + name, pivot=(0.5, 0.5))


def gear(c, cx, cy, r_out, r_in, col, col_d, teeth=8):
    pts = []
    for i in range(teeth * 2):
        a = math.pi * 2 * i / (teeth * 2)
        r = r_out if i % 2 == 0 else r_in
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    c.poly(pts, col_d)
    c.ell(cx - r_in + 1, cy - r_in + 1, cx + r_in - 1, cy + r_in - 1, col)
    c.ell(cx - 1.5, cy - 1.5, cx + 1.5, cy + 1.5, NAVY2)


def robot_head(c, x, y, col, col_d, eye):
    c.rect(x + 1, y, x + 8, y + 1, col_d)          # ăng-ten / mũ
    c.rect(x, y + 2, x + 9, y + 8, col_d)
    c.rect(x + 1, y + 3, x + 8, y + 7, col)
    c.rect(x + 2, y + 4, x + 3, y + 5, eye)
    c.rect(x + 6, y + 4, x + 7, y + 5, eye)
    c.rect(x + 3, y + 7, x + 6, y + 7, col_d)
    c.px(x + 4, y - 1, ORG)


def icons():
    def parts_i(c):
        gear(c, 7, 8, 7, 5, CYAN, CYAN_D)
        c.px(12, 2, WHITE)
        c.px(13, 3, YEL)
        c.px(12, 4, WHITE)
        c.px(11, 3, YEL)
    icon("parts", parts_i)

    def scavenger(c):
        gear(c, 6, 9, 5, 3.6, LIME, (30, 120, 40), 7)
        c.line(11, 5, 11, 1, WHITE, 1)             # mũi tên lên = rơi nhiều hơn
        c.poly([(9, 3), (11, 0), (13, 3)], WHITE)
        c.px(14, 7, YEL)
        c.px(2, 3, YEL)
    icon("scavenger", scavenger)

    def efficient(c):
        # cờ lê
        c.line(3, 13, 11, 5, STEEL, 2)
        c.ell(9, 1, 14, 6, STEEL_D)
        c.ell(10, 2, 13, 5, STEEL)
        c.rect(11, 1, 12, 3, NAVY2)
        c.px(3, 13, ORG)
        c.px(2, 14, ORG)
        c.px(13, 12, YEL)
        c.px(12, 13, YEL)
    icon("efficient", efficient)

    def minibot(c):
        robot_head(c, 3, 3, CYAN, CYAN_D, WHITE)
        c.rect(2, 11, 3, 13, STEEL_D)
        c.rect(12, 11, 13, 13, STEEL_D)
        c.rect(5, 11, 10, 13, STEEL_D)
        c.px(7, 12, LIME)
    icon("minibot", minibot)

    def squad(c):
        robot_head(c, 0, 5, MAG, MAG_D, WHITE)
        robot_head(c, 6, 3, CYAN, CYAN_D, WHITE)
        c.px(11, 13, YEL)
    icon("squad", squad)

    def slots(c):
        for (x, y, col) in ((1, 1, CYAN), (9, 1, CYAN), (1, 9, CYAN), (9, 9, LIME)):
            c.rect(x, y, x + 5, y + 5, CYAN_D)
            c.rect(x + 1, y + 1, x + 4, y + 4, NAVY2)
            c.rect(x + 2, y + 2, x + 3, y + 3, col)
        c.rect(7, 11, 8, 12, WHITE)
        c.rect(6, 12, 9, 12, WHITE)
    icon("slots", slots)

    def tactics(c):
        c.ell(1, 1, 14, 14, STEEL_D)
        c.ell(2, 2, 13, 13, NAVY2)
        c.line(8, 8, 8, 3, WHITE, 1)
        c.line(8, 8, 11, 10, ORG, 1)
        c.px(8, 8, YEL)
        for (x, y) in ((8, 2), (13, 8), (8, 13), (2, 8)):
            c.px(x, y, CYAN)
    icon("tactics", tactics)

    def growth(c):
        c.rect(1, 12, 14, 14, STEEL_D)
        c.rect(1, 12, 9, 13, LIME)
        c.poly([(8, 1), (13, 7), (10, 7), (10, 10), (6, 10), (6, 7), (3, 7)], LIME)
        c.poly([(8, 3), (11, 7), (5, 7)], (170, 255, 150))
        c.px(8, 2, WHITE)
    icon("growth", growth)

    def repair(c):
        c.rect(6, 1, 9, 14, RED_D)
        c.rect(1, 6, 14, 9, RED_D)
        c.rect(7, 2, 8, 13, RED)
        c.rect(2, 7, 13, 8, RED)
        c.px(7, 3, WHITE)
        c.px(3, 7, WHITE)
        c.px(12, 3, LIME)
        c.px(13, 2, LIME)
        c.px(12, 1, LIME)
    icon("repair", repair)


def main():
    parts()
    icons()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (linh kien, icon skill) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
