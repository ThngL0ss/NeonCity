#!/usr/bin/env python3
"""
Vẽ sprite cho: 5 loại bẫy, đồng xu, rương, và icon 17 vật phẩm (dùng chung công cụ vẽ + bảng màu của gen_art.py).

  trap_spike_down / warn / up   bẫy gai sàn (3 trạng thái)
  trap_saw                      lưỡi cưa quay
  trap_emitter                  trụ phát laser
  trap_turret_base / barrel     súng tự động (thân + nòng)
  trap_mine_idle_0/1, trap_mine_armed_0/1   mìn năng lượng
  coin_0..3                     đồng xu xoay
  chest_closed / chest_open     rương
  item_<id>                     icon vật phẩm 16x16

Chạy:  python3 Tools/PixelArt/gen_extras.py [thư_mục_Art]
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
GOLD = (255, 200, 40)
GOLD_D = (190, 120, 10)
CLEAR = (0, 0, 0, 0)


# ================================================================ BẪY
def spikes():
    # --- thụt xuống: tấm sàn có lỗ
    c = Cv(48, 5)
    c.rect(0, 1, 47, 4, NAVYL)
    c.rect(0, 1, 47, 1, RED_D)
    for i in range(6):
        c.rect(2 + i * 8 + 2, 2, 2 + i * 8 + 4, 3, NAVY2)
    c.save("trap_spike_down", pivot=(0.5, 0.0))

    # --- báo hiệu: mũi gai nhú lên
    c = Cv(48, 9)
    c.rect(0, 5, 47, 8, NAVYL)
    c.rect(0, 5, 47, 5, RED_D)
    for i in range(6):
        x0 = 2 + i * 8
        c.poly([(x0 + 1, 5), (x0 + 3, 1), (x0 + 5, 5)], ORG)
        c.px(x0 + 3, 1, WHITE)
    c.save("trap_spike_warn", pivot=(0.5, 0.0))

    # --- nhô lên: gai dài
    c = Cv(48, 17)
    c.rect(0, 14, 47, 16, NAVYL)
    c.rect(0, 14, 47, 14, RED_D)
    for i in range(6):
        x0 = 2 + i * 8
        c.poly([(x0, 14), (x0 + 3, 1), (x0 + 6, 14)], STEEL)
        c.poly([(x0 + 3, 1), (x0 + 6, 14), (x0 + 3, 14)], STEEL_D)
        c.px(x0 + 3, 1, WHITE)
        c.px(x0 + 3, 2, ORG)
        c.px(x0 + 3, 3, ORG)
    c.save("trap_spike_up", pivot=(0.5, 0.0))


def saw():
    c = Cv(32, 32)
    cx = cy = 16
    pts = []
    n = 12
    for i in range(n * 2):
        a = math.pi * 2 * i / (n * 2)
        r = 15 if i % 2 == 0 else 11.5
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    c.poly(pts, STEEL)
    c.ell(7, 7, 24, 24, STEEL_D)
    c.ell(9, 9, 22, 22, NAVYL)
    # lỗ lõi
    for k in range(3):
        a = math.pi * 2 * k / 3
        hx, hy = cx + math.cos(a) * 6, cy + math.sin(a) * 6
        c.ell(hx - 2, hy - 2, hx + 2, hy + 2, NAVY2)
    c.ell(13, 13, 18, 18, RED_D)
    c.ell(14, 14, 17, 17, RED)
    c.px(15, 15, WHITE)
    # đầu răng sáng
    for i in range(0, n * 2, 2):
        a = math.pi * 2 * i / (n * 2)
        c.px(cx + math.cos(a) * 14, cy + math.sin(a) * 14, WHITE)
    c.save("trap_saw", pivot=(0.5, 0.5))


def emitter():
    c = Cv(16, 16)
    c.rect(3, 13, 12, 15, NAVY2)
    c.rect(3, 13, 12, 13, CYAN_D)
    c.poly([(4, 13), (5, 6), (8, 2), (11, 6), (12, 13)], NAVYL)
    c.rect(7, 3, 8, 12, NAVY)
    c.ell(6, 3, 9, 6, MAG)
    c.px(7, 4, WHITE)
    c.px(5, 8, CYAN)
    c.px(10, 8, CYAN)
    c.save("trap_emitter", pivot=(0.5, 0.5))


def turret():
    c = Cv(24, 16)
    c.rect(0, 12, 23, 15, NAVY2)
    c.rect(0, 12, 23, 12, CYAN_D)
    c.rect(3, 4, 20, 12, NAVYL)
    c.rect(3, 4, 20, 4, CYAN)
    c.rect(6, 6, 17, 10, NAVY)
    c.ell(10, 6, 14, 10, RED_D)
    c.px(12, 8, RED)
    c.px(4, 6, MAG)
    c.px(19, 6, MAG)
    c.save("trap_turret_base", pivot=(0.5, 0.0))

    b = Cv(16, 8)
    b.rect(0, 2, 12, 5, NAVY)
    b.rect(0, 2, 12, 2, CYAN)
    b.rect(11, 1, 15, 6, ORG_D)
    b.rect(14, 2, 15, 5, ORG)
    b.px(15, 3, WHITE)
    b.px(15, 4, WHITE)
    b.save("trap_turret_barrel", pivot=(0.2, 0.5))


def mine():
    def one(name, light, ring):
        c = Cv(16, 12)
        c.ell(1, 2, 14, 15, NAVYL)
        c.rect(0, 10, 15, 11, NAVY2)
        c.rect(2, 9, 13, 9, NAVY)
        for sx in (3, 7, 12):
            c.px(sx, 1, STEEL)
            c.px(sx, 2, STEEL_D)
        c.ell(5, 3, 10, 8, ring)
        c.ell(6, 4, 9, 7, light)
        c.px(7, 5, WHITE)
        c.save(name, pivot=(0.5, 0.0))
    one("trap_mine_idle_0", MAG_D, NAVY2)
    one("trap_mine_idle_1", MAG, MAG_D)
    one("trap_mine_armed_0", RED, RED_D)
    one("trap_mine_armed_1", YEL, ORG_D)


# ================================================================ TIỀN & RƯƠNG
def coins():
    for i, w in enumerate((14, 10, 4, 10)):
        c = Cv(16, 16)
        x0 = 8 - w / 2.0
        x1 = 8 + w / 2.0 - 1
        c.ell(x0, 1, x1, 14, GOLD_D)
        if w >= 8:
            c.ell(x0 + 1, 2, x1 - 1, 13, GOLD)
            c.ell(x0 + 3, 4, x1 - 3, 11, GOLD_D)
            c.rect(7, 5, 8, 10, YEL)
            c.px(x0 + 2, 4, WHITE)
        else:
            c.ell(x0, 1, x1, 14, GOLD)
        c.save("coin_%d" % i, pivot=(0.5, 0.5))


def chest():
    # đóng
    c = Cv(32, 24)
    c.rect(2, 10, 29, 22, NAVYL)
    c.rect(2, 21, 29, 22, NAVY2)
    c.ell(2, 3, 29, 17, NAVY)
    c.rect(2, 10, 29, 12, NAVYL)                       # viền giữa nắp và thân
    c.rect(2, 10, 29, 10, CYAN_D)
    for bx in (6, 24):
        c.rect(bx, 4, bx + 1, 22, GOLD_D)
        c.rect(bx, 4, bx, 22, GOLD)
    c.rect(14, 9, 17, 16, GOLD)
    c.rect(15, 11, 16, 13, NAVY2)
    c.px(14, 9, WHITE)
    c.rect(2, 3, 29, 3, CYAN)
    c.save("chest_closed", pivot=(0.5, 0.0))

    # mở
    o = Cv(32, 30)
    o.rect(2, 16, 29, 28, NAVYL)
    o.rect(2, 27, 29, 28, NAVY2)
    o.rect(4, 16, 27, 19, YEL)                         # ánh sáng bên trong
    o.rect(6, 17, 25, 18, WHITE)
    o.rect(2, 16, 29, 16, CYAN_D)
    o.poly([(2, 15), (4, 2), (27, 2), (29, 15)], NAVY)   # nắp mở dựng lên
    o.rect(4, 2, 27, 2, CYAN)
    for bx in (7, 23):
        o.rect(bx, 2, bx + 1, 14, GOLD_D)
    for bx in (6, 24):
        o.rect(bx, 20, bx + 1, 28, GOLD_D)
        o.rect(bx, 20, bx, 28, GOLD)
    o.save("chest_open", pivot=(0.5, 0.0))


# ================================================================ ICON VẬT PHẨM (16x16)
def icon(name, fn):
    c = Cv(16, 16)
    fn(c)
    c.save("item_" + name, pivot=(0.5, 0.5))


def vial(c, liquid, liquid_hi):
    c.rect(6, 1, 9, 2, STEEL_D)
    c.rect(5, 3, 10, 3, STEEL)
    c.rect(4, 4, 11, 14, NAVY2)
    c.rect(5, 5, 10, 13, (30, 44, 90))
    c.rect(5, 8, 10, 13, liquid)
    c.rect(5, 8, 10, 8, liquid_hi)
    c.px(6, 5, WHITE)
    c.px(6, 6, WHITE)


def icons():
    # --- trang bị (Passive)
    def core_cell(c):
        c.ell(2, 2, 13, 13, CYAN_D)
        c.ell(4, 4, 11, 11, CYAN)
        c.ell(6, 6, 9, 9, WHITE)
        c.rect(7, 0, 8, 1, STEEL)
        c.rect(7, 14, 8, 15, STEEL)
    icon("core_cell", core_cell)

    def neon_armor(c):
        c.poly([(2, 2), (13, 2), (13, 9), (8, 14), (2, 9)], NAVYL)
        c.poly([(4, 4), (11, 4), (11, 8), (8, 11), (4, 8)], NAVY)
        c.rect(7, 3, 8, 11, MAG)
        c.rect(2, 2, 13, 2, CYAN)
        c.px(5, 5, WHITE)
    icon("neon_armor", neon_armor)

    def volt_boots(c):
        c.rect(4, 2, 8, 10, NAVYL)
        c.rect(4, 9, 13, 12, NAVYL)
        c.rect(3, 12, 13, 13, CYAN)
        c.rect(4, 2, 8, 2, CYAN)
        c.poly([(7, 4), (5, 7), (7, 7), (6, 10), (10, 6), (8, 6)], YEL)
    icon("volt_boots", volt_boots)

    def freq_blade(c):
        c.line(3, 13, 12, 3, CYAN, 2)
        c.line(4, 12, 11, 4, WHITE, 1)
        c.line(2, 10, 6, 14, MAG, 2)
        c.px(13, 2, WHITE)
        c.px(2, 14, MAG_D)
    icon("freq_blade", freq_blade)

    def shuriken_rail(c):
        c.poly([(8, 0), (10, 6), (15, 8), (10, 10), (8, 15), (6, 10), (1, 8), (6, 6)], CYAN_D)
        c.poly([(8, 2), (9, 6), (13, 8), (9, 9), (8, 13), (7, 9), (3, 8), (7, 6)], CYAN)
        c.ell(7, 7, 8, 8, NAVY2)
        c.px(8, 3, WHITE)
    icon("shuriken_rail", shuriken_rail)

    def phase_coil(c):
        c.ell(1, 1, 14, 14, PURP)
        c.ell(3, 3, 12, 12, CLEAR)
        c.ell(5, 5, 10, 10, PURP_D)
        c.ell(6, 6, 9, 9, MAG)
        c.px(7, 7, WHITE)
        c.px(3, 8, WHITE)
        c.px(12, 7, WHITE)
    icon("phase_coil", phase_coil)

    def coin_magnet(c):
        c.rect(2, 3, 5, 12, RED)
        c.rect(10, 3, 13, 12, RED)
        c.rect(2, 10, 13, 13, RED)
        c.rect(5, 10, 10, 10, CLEAR)
        c.rect(2, 3, 5, 5, WHITE)
        c.rect(10, 3, 13, 5, WHITE)
        c.rect(3, 11, 12, 12, RED_D)
        c.px(7, 2, GOLD)
        c.px(8, 1, YEL)
    icon("coin_magnet", coin_magnet)

    def nano_kit(c):
        c.rect(6, 1, 9, 2, STEEL_D)
        c.rect(1, 3, 14, 13, NAVYL)
        c.rect(2, 4, 13, 12, NAVY)
        c.rect(7, 5, 8, 11, LIME)
        c.rect(4, 7, 11, 8, LIME)
        c.rect(1, 3, 14, 3, CYAN)
    icon("nano_kit", nano_kit)

    # --- tiêu hao (Consumable)
    icon("heal_vial", lambda c: vial(c, LIME, WHITE))
    icon("rage_serum", lambda c: vial(c, RED, ORG))
    icon("speed_serum", lambda c: vial(c, CYAN, WHITE))

    def charge_cell(c):
        c.rect(6, 1, 9, 2, STEEL)
        c.rect(3, 3, 12, 14, NAVYL)
        c.rect(4, 4, 11, 13, NAVY2)
        c.rect(4, 10, 11, 13, LIME)
        c.rect(4, 7, 11, 9, YEL)
        c.rect(4, 4, 11, 6, ORG)
        c.px(5, 5, WHITE)
    icon("charge_cell", charge_cell)

    def vital_shard(c):
        c.poly([(8, 0), (13, 5), (10, 15), (5, 15), (2, 5)], MAG_D)
        c.poly([(8, 1), (12, 5), (9, 14), (6, 14), (3, 5)], MAG)
        c.poly([(8, 1), (8, 14), (6, 14), (3, 5)], (255, 120, 225))
        c.px(7, 3, WHITE)
        c.px(7, 4, WHITE)
    icon("vital_shard", vital_shard)

    # --- vật phẩm giá trị (Valuable)
    def scrap_battery(c):
        c.rect(6, 2, 9, 3, STEEL_D)
        c.rect(4, 4, 11, 14, (70, 74, 100))
        c.rect(4, 4, 11, 4, (110, 114, 140))
        c.rect(4, 11, 11, 14, ORG_D)
        c.line(5, 6, 9, 10, NAVY2, 1)
        c.px(10, 7, ORG)
    icon("scrap_battery", scrap_battery)

    def data_chip(c):
        c.rect(3, 3, 12, 12, NAVYL)
        c.rect(4, 4, 11, 11, NAVY)
        c.rect(6, 6, 9, 9, CYAN)
        c.px(7, 7, WHITE)
        for i in range(4):
            c.px(1, 4 + i * 2, CYAN_D)
            c.px(2, 4 + i * 2, CYAN_D)
            c.px(13, 4 + i * 2, CYAN_D)
            c.px(14, 4 + i * 2, CYAN_D)
    icon("data_chip", data_chip)

    def neon_crystal(c):
        c.poly([(8, 0), (14, 6), (8, 15), (2, 6)], CYAN_D)
        c.poly([(8, 1), (13, 6), (8, 13), (3, 6)], CYAN)
        c.poly([(8, 1), (8, 13), (3, 6)], (150, 250, 255))
        c.line(3, 6, 13, 6, WHITE, 1)
        c.px(6, 3, WHITE)
    icon("neon_crystal", neon_crystal)

    def ancient_core(c):
        c.ell(1, 1, 14, 14, ORG_D)
        c.ell(3, 3, 12, 12, ORG)
        c.ell(5, 5, 10, 10, YEL)
        c.ell(7, 7, 8, 8, WHITE)
        c.rect(0, 7, 15, 8, GOLD_D)
        c.rect(7, 0, 8, 15, GOLD_D)
        c.rect(7, 5, 8, 10, YEL)
        c.rect(5, 7, 10, 8, YEL)
        c.px(7, 7, WHITE)
        c.px(8, 8, WHITE)
    icon("ancient_core", ancient_core)


def main():
    spikes()
    saw()
    emitter()
    turret()
    mine()
    coins()
    chest()
    icons()
    path = os.path.join(G.OUT_DIR, "manifest.json")
    data = {}
    if os.path.exists(path):
        with open(path, encoding="utf8") as f:
            data = json.load(f)
    data.update(G.MANIFEST)
    with open(path, "w", encoding="utf8") as f:
        json.dump(data, f, indent=1, sort_keys=True)
    print("Da them %d sprite (bay, xu, ruong, vat pham) -> %s" % (len(G.MANIFEST), G.OUT_DIR))


if __name__ == "__main__":
    main()
