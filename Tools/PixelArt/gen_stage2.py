#!/usr/bin/env python3
"""
Vẽ sprite cho giai đoạn 2 (cấp bậc + dầu + nguyên liệu), dùng chung công cụ vẽ + bảng màu của gen_art.py:

  oil_drop_0..3          giọt dầu rơi ra khi hạ quái / mở rương (4 khung nhấp nháy)
  oil_can                thùng dầu nằm trên đường + biểu tượng dầu trong giao diện
  item_scrap_metal       nguyên liệu: sắt vụn
  item_copper_wire       nguyên liệu: cuộn dây đồng
  item_circuit_board     nguyên liệu: bo mạch
  item_energy_gel        nguyên liệu: gel năng lượng

Chạy:  python3 Tools/PixelArt/gen_stage2.py [thư_mục_Art]
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
OIL = (20, 18, 30)
OIL_M = (52, 40, 74)
OIL_HI = (120, 90, 170)
AMBER = (255, 190, 50)
AMBER_D = (190, 110, 20)
COPPER = (226, 128, 70)
COPPER_D = (150, 70, 34)


# ================================================================ dầu
def oil_drop():
    # giọt dầu nhấp nháy: bóng sáng chạy quanh giọt
    shine = [(5, 5), (5, 4), (6, 4), (5, 5)]
    for i in range(4):
        c = Cv(12, 14)
        c.poly([(6, 0), (10, 6), (10, 9), (8, 12), (4, 12), (2, 9), (2, 6)], OIL_M)
        c.poly([(6, 2), (9, 7), (9, 9), (7, 11), (5, 11), (3, 9), (3, 7)], OIL)
        c.px(shine[i][0], shine[i][1] + 3, OIL_HI)
        c.px(shine[i][0], shine[i][1] + 4, WHITE if i % 2 == 0 else OIL_HI)
        c.px(8, 9, AMBER_D)
        c.save("oil_drop_%d" % i, pivot=(0.5, 0.5))


def oil_can():
    c = Cv(16, 16)
    c.rect(5, 1, 10, 2, STEEL_D)                 # nắp + vòi
    c.rect(9, 0, 12, 1, STEEL)
    c.rect(3, 3, 12, 14, STEEL_D)                # thân thùng
    c.rect(4, 4, 11, 13, (70, 74, 110))
    c.rect(4, 4, 6, 13, STEEL)                   # mép sáng
    c.rect(4, 7, 11, 10, AMBER_D)                # nhãn
    c.rect(5, 8, 10, 9, AMBER)
    c.px(7, 5, WHITE)
    c.px(9, 12, OIL)
    c.px(8, 12, OIL)
    c.save("oil_can", pivot=(0.5, 0.5))


# ================================================================ nguyên liệu
def icon(name, fn):
    c = Cv(16, 16)
    fn(c)
    c.save("item_" + name, pivot=(0.5, 0.5))


def materials():
    def scrap_metal(c):
        c.poly([(2, 12), (3, 6), (8, 3), (13, 5), (14, 11), (9, 14)], STEEL_D)
        c.poly([(3, 11), (4, 7), (8, 4), (12, 6), (13, 10), (9, 13)], STEEL)
        c.poly([(4, 10), (5, 8), (8, 5), (10, 6), (7, 10)], (210, 220, 245))
        c.px(10, 9, ORG_D)
        c.px(11, 10, ORG)
        c.px(6, 11, NAVY2)
        c.px(7, 7, WHITE)
    icon("scrap_metal", scrap_metal)

    def copper_wire(c):
        c.ell(2, 3, 13, 14, COPPER_D)                # cuộn dây
        c.ell(3, 4, 12, 13, COPPER)
        c.ell(5, 6, 10, 11, COPPER_D)
        c.ell(6, 7, 9, 10, NAVY2)
        c.line(3, 7, 12, 7, COPPER_D, 1)
        c.line(3, 10, 12, 10, COPPER_D, 1)
        c.rect(11, 2, 14, 3, COPPER)                 # đầu dây thò ra
        c.px(14, 2, YEL)
        c.px(5, 5, WHITE)
    icon("copper_wire", copper_wire)

    def circuit_board(c):
        c.rect(2, 3, 13, 13, (20, 90, 70))
        c.rect(3, 4, 12, 12, (30, 140, 100))
        c.line(4, 6, 9, 6, LIME, 1)
        c.line(9, 6, 9, 10, LIME, 1)
        c.line(5, 10, 9, 10, LIME, 1)
        c.rect(10, 5, 12, 7, NAVY2)                  # chip
        c.px(11, 6, CYAN)
        c.px(4, 6, YEL)
        c.px(5, 10, YEL)
        c.px(4, 4, WHITE)
    icon("circuit_board", circuit_board)

    def energy_gel(c):
        c.rect(5, 1, 10, 3, STEEL_D)                 # nắp ống
        c.rect(4, 4, 11, 14, (30, 44, 90))           # ống trong suốt
        c.rect(5, 5, 10, 13, (20, 70, 80))
        c.rect(5, 7, 10, 13, CYAN_D)
        c.rect(5, 7, 10, 8, CYAN)
        c.px(6, 5, WHITE)
        c.px(6, 6, WHITE)
        c.px(8, 10, WHITE)
    icon("energy_gel", energy_gel)


def main():
    oil_drop()
    oil_can()
    materials()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (dau, nguyen lieu) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
