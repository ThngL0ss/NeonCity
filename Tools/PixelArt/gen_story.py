#!/usr/bin/env python3
"""
Vẽ sprite cho giai đoạn "nhiệm vụ + lưu game" (dùng chung công cụ vẽ + bảng màu của gen_art.py):

  item_iron_ingot      nguyên liệu: thỏi sắt (nhiệm vụ nguyên liệu hồi sinh)
  item_carbon_fiber    nguyên liệu: sợi cacbon (cuộn sợi đen có vân dệt)
  prop_mailbox         hộp thư nhiệm vụ ở căn cứ (32 x 40 px, cột + hộp neon, cờ báo)
  prop_mailbox_flag    lá cờ báo (nhấp nháy khi có nhiệm vụ mới) — vẽ riêng để Unity xoay / nhấp nháy

Chạy:  python3 Tools/PixelArt/gen_story.py [thư_mục_Art]
Mặc định ghi vào Assets/NeonBreaker/Art và CẬP NHẬT (không ghi đè) manifest.json. Quy ước: 16 pixel = 1 đơn vị.
"""
import json
import os
import sys

import gen_art as G
from gen_art import (Cv, CYAN, CYAN_D, MAG, MAG_D, PURP, PURP_D, LIME, ORG, ORG_D, RED, YEL, WHITE,
                     NAVY, NAVY2, NAVYL)

if len(sys.argv) > 1:
    G.OUT_DIR = os.path.abspath(sys.argv[1])
os.makedirs(G.OUT_DIR, exist_ok=True)

IRON = (150, 160, 180)
IRON_L = (205, 214, 232)
IRON_D = (78, 88, 116)
CARBON = (26, 28, 38)
CARBON_M = (48, 52, 70)
CARBON_L = (86, 94, 124)


def icon(name, fn):
    c = Cv(16, 16)
    fn(c)
    c.save("item_" + name, pivot=(0.5, 0.5))


def items():
    def iron_ingot(c):
        # thỏi sắt hình thang xếp 2 lớp
        c.poly([(1, 12), (3, 7), (13, 7), (15, 12)], IRON_D)          # thỏi dưới
        c.poly([(2, 12), (4, 8), (12, 8), (14, 12)], IRON)
        c.line(4, 8, 12, 8, IRON_L, 1)
        c.poly([(4, 7), (6, 3), (12, 3), (13, 7)], IRON_D)            # thỏi trên
        c.poly([(5, 7), (7, 4), (11, 4), (12, 7)], IRON)
        c.line(7, 4, 11, 4, IRON_L, 1)
        c.px(6, 10, WHITE)
        c.px(8, 5, WHITE)
        c.px(10, 11, IRON_D)
        c.px(13, 11, IRON_D)

    icon("iron_ingot", iron_ingot)

    def carbon_fiber(c):
        # cuộn sợi cacbon: lõi tròn + vân dệt caro
        c.ell(1, 2, 14, 13, CARBON)
        c.ell(2, 3, 13, 12, CARBON_M)
        for y in range(3, 13):
            for x in range(2, 14):
                if (x + y) % 2 == 0 and (x - 7.5) ** 2 / 36 + (y - 7.5) ** 2 / 24 < 1.0:
                    c.px(x, y, CARBON)
        c.ell(5, 5, 10, 10, CARBON)
        c.ell(6, 6, 9, 9, (12, 12, 18))
        c.px(4, 4, CARBON_L)
        c.px(5, 3, CARBON_L)
        c.px(11, 5, CYAN_D)
        c.px(12, 9, CYAN_D)
        c.px(3, 8, CARBON_L)
        c.rect(11, 12, 14, 13, CARBON_M)                              # đầu sợi buông ra
        c.px(14, 13, CARBON_L)

    icon("carbon_fiber", carbon_fiber)


def mailbox():
    # hộp thư nhiệm vụ: cột kim loại + hộp neon có khe thư, đèn báo
    c = Cv(32, 40)
    c.rect(14, 22, 17, 39, IRON_D)                      # cột
    c.rect(15, 22, 16, 39, IRON)
    c.rect(10, 37, 21, 39, IRON_D)                      # đế
    c.rect(11, 37, 20, 38, IRON)
    c.rect(3, 6, 28, 23, NAVY2)                         # thân hộp
    c.rect(4, 7, 27, 22, (24, 30, 64))
    c.rect(4, 7, 27, 9, (40, 52, 104))                  # nắp
    c.rect(4, 7, 27, 7, CYAN)                           # viền neon trên
    c.rect(4, 22, 27, 22, MAG_D)                        # viền neon dưới
    c.rect(8, 13, 23, 15, (4, 6, 20))                   # khe thư
    c.rect(8, 13, 23, 13, CYAN_D)
    c.rect(8, 18, 12, 20, YEL)                          # nhãn
    c.px(9, 19, ORG_D)
    c.px(10, 19, ORG_D)
    c.px(23, 18, LIME)                                  # đèn báo
    c.px(24, 18, LIME)
    c.px(23, 19, LIME)
    c.px(24, 19, LIME)
    c.px(6, 10, WHITE)
    c.save("prop_mailbox", pivot=(0.5, 0.0))

    f = Cv(10, 12)
    f.rect(1, 0, 2, 11, IRON_D)                         # cán cờ
    f.rect(2, 1, 8, 5, RED)
    f.rect(2, 1, 8, 1, (255, 150, 150))
    f.px(7, 4, MAG_D)
    f.save("prop_mailbox_flag", pivot=(0.15, 0.0))


def main():
    items()
    mailbox()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (nhiem vu) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
