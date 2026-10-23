# Generates restaurant.obj/.mtl: a ~24 x 18 m cafe-style restaurant made of named box parts.
# Same format as kitchen_scene.obj: one 'o' per piece of furniture so each can be moved in Unity.
# Layout (top view): X -12..12, Z -9..9, Y up. Kitchen at +Z (back), lobby at -Z (front, entrance at z=-9).
import sys, os

objs = []          # [name, [(mat, x0,x1,y0,y1,z0,z1), ...]]
def O(name):
    objs.append([name, []])
def B(mat, x0, x1, y0, y1, z0, z1):
    x0, x1 = sorted((x0, x1)); y0, y1 = sorted((y0, y1)); z0, z1 = sorted((z0, z1))
    objs[-1][1].append((mat, x0, x1, y0, y1, z0, z1))
def C(mat, cx, cz, sx, sz, y0, y1):  # box by footprint center/size
    B(mat, cx - sx / 2, cx + sx / 2, y0, y1, cz - sz / 2, cz + sz / 2)

H = 3.5  # wall height
W, F = 'Mat_WallsArchitectural', 'Mat_FloorTile'

# ---------------- Room ----------------
O('Floor_Kitchen');  B('Mat_KitchenFloorSlate', -12, 12, -0.1, 0, 1.2, 9)
O('Floor_Lobby');    B(F, -12, 12, -0.1, 0, -9, 1.2)
O('Wall_Back');      B(W, -12.2, 12.2, 0, H, 9, 9.2)
O('Wall_Kitchen_Left');  B(W, -12.2, -12, 0, H, 1.2, 9)
O('Wall_Kitchen_Right'); B(W, 12, 12.2, 0, H, 1.2, 9)

def window_wall_x(x0, x1, z0, z1):  # sill + glass + top band
    B(W, x0, x1, 0, 0.9, z0, z1); B('Mat_GlassWindow', x0, x1, 0.9, 2.6, z0, z1); B(W, x0, x1, 2.6, H, z0, z1)
O('Wall_Lobby_Left_Windows');  window_wall_x(-12.2, -12, -9.2, 1.2)
for z in (-6.6, -4.0, -1.4): B(W, -12.25, -11.95, 0.9, 2.6, z - 0.06, z + 0.06)   # mullions
O('Wall_Lobby_Right_Windows'); window_wall_x(12, 12.2, -9.2, 1.2)
for z in (-6.6, -4.0, -1.4): B(W, 11.95, 12.25, 0.9, 2.6, z - 0.06, z + 0.06)
O('Wall_Front_Windows')
for xa, xb in ((-12, -1.4), (1.4, 12)): window_wall_x(xa, xb, -9.2, -9)
for x in (-9.0, -6.0, -3.0, 3.0, 6.0, 9.0): B(W, x - 0.06, x + 0.06, 0.9, 2.6, -9.25, -8.95)
O('Entrance_Door_Frame')
B('Mat_MetalLegs', -1.4, -1.25, 0, 2.6, -9.2, -9)
B('Mat_MetalLegs', 1.25, 1.4, 0, 2.6, -9.2, -9)
B(W, -1.4, 1.4, 2.6, H, -9.2, -9)
for side, (xa, xb) in (('L', (-1.25, 0)), ('R', (0, 1.25))):
    O(f'Entrance_Door_{side}')  # separate so you can animate / delete them
    B('Mat_MetalLegs', xa, xb, 0, 0.1, -9.13, -9.07); B('Mat_MetalLegs', xa, xb, 2.5, 2.6, -9.13, -9.07)
    B('Mat_GlassWindow', xa, xb, 0.1, 2.5, -9.12, -9.08)
    hx = xb - 0.15 if side == 'L' else xa + 0.15
    B('Mat_StainlessSteel', hx - 0.03, hx + 0.03, 0.8, 1.4, -9.2, -9.0)

# Partition between lobby and kitchen; open above/through the front counter, 1 m crew gap at x -7..-6
O('Partition_Left');   B(W, -12, -7, 0, H, 1.2, 1.4); B('Mat_AccentTeal', -12, -7, 0, 0.9, 1.15, 1.2)
O('Partition_Right');  B(W, 7.8, 12, 0, H, 1.2, 1.4); B('Mat_AccentTeal', 7.8, 12, 0, 0.9, 1.15, 1.2)
O('Counter_Header');   B(W, -7, 7.8, 3.0, H, 1.2, 1.4); B('Mat_AccentWood', -7, 7.8, 3.0, 3.1, 1.15, 1.2)

# ---------------- Front of house ----------------
O('Front_Counter')
B('Mat_AccentTeal', -6, 4, 0, 1.0, 0.45, 1.1)
B('Mat_AccentWood', -6, 4, 0.75, 0.85, 0.42, 0.45)          # accent stripe
B('Mat_CountertopStone', -6.05, 4.05, 1.0, 1.05, 0.35, 1.15)
B('Mat_CabinetBase', -6, 4, 0, 0.1, 0.5, 1.1)              # kick plate
for i, x in enumerate((-3.0, 1.0), 1):
    O(f'POS_Register_{i}')
    C('Mat_POSTerminalDark', x, 0.85, 0.35, 0.3, 1.05, 1.12)       # base
    C('Mat_POSTerminalDark', x, 0.9, 0.06, 0.06, 1.12, 1.35)       # stand
    C('Mat_POSTerminalDark', x, 0.88, 0.42, 0.04, 1.3, 1.6)        # screen housing
    C('Mat_POSScreenWhite', x, 0.855, 0.38, 0.01, 1.32, 1.58)      # crew-facing screen
    C('Mat_POSTerminalDark', x, 0.7, 0.3, 0.12, 1.05, 1.1)         # cash drawer top

O('Menu_Boards')
for x0, x1 in ((-5, -1.8), (-1.6, 1.6), (1.8, 5)):
    B('Mat_POSTerminalDark', x0, x1, 2.2, 3.0, 1.08, 1.15)        # frame
    B('Mat_MenuScreen', x0 + 0.05, x1 - 0.05, 2.25, 2.85, 1.06, 1.08)
    B('Mat_AccentWood', x0 + 0.05, x1 - 0.05, 2.85, 2.95, 1.06, 1.08)

O('Drink_Station')
B('Mat_AccentTeal', 4.3, 7.8, 0, 0.9, 0.3, 1.0)
B('Mat_CountertopStone', 4.25, 7.85, 0.9, 0.95, 0.25, 1.05)
B('Mat_StainlessDark', 4.5, 5.2, 0.95, 1.25, 0.45, 0.95)                   # ice bin
B('Mat_StainlessSteel', 4.55, 5.15, 1.2, 1.26, 0.5, 0.9)
B('Mat_POSTerminalDark', 5.4, 6.6, 0.95, 1.75, 0.65, 0.98)                 # soda tower
B('Mat_AccentWood', 5.4, 6.6, 1.55, 1.75, 0.63, 0.65)                        # tower label strip
for k in range(6):
    nx = 5.5 + k * 0.2
    B('Mat_StainlessSteel', nx, nx + 0.08, 1.3, 1.4, 0.55, 0.65)           # nozzles
B('Mat_StainlessDark', 5.4, 6.6, 0.95, 0.98, 0.4, 0.65)                    # drip tray
for k, cx in enumerate((6.9, 7.2, 7.5)):
    C('Mat_CupWhite', cx, 0.7, 0.18, 0.18, 0.95, 1.25 + 0.1 * k)           # cup stacks
# turned 180 deg (mirrored about its own center z=0.65) so the crew serves from behind the counter
objs[-1][1] = [(m, x0, x1, y0, y1, 1.3 - z1, 1.3 - z0) for m, x0, x1, y0, y1, z0, z1 in objs[-1][1]]

def tray_stand(name, cx, cz):
    O(name)
    C('Mat_POSTerminalDark', cx, cz, 0.9, 0.6, 0, 1.0)            # bin cabinet
    C('Mat_TrashBlack', cx, cz + 0.31, 0.5, 0.02, 0.6, 0.85)      # bin flap (faces +z, into lobby)
    C('Mat_StainlessSteel', cx, cz, 0.95, 0.65, 1.0, 1.04)        # tray shelf
    for dx in (-0.42, 0.42):
        C('Mat_MetalLegs', cx + dx, cz - 0.25, 0.04, 0.04, 1.04, 1.5)
    C('Mat_StainlessSteel', cx, cz - 0.05, 0.95, 0.45, 1.5, 1.54)
    for k in range(4):
        C('Mat_AccentTeal', cx, cz - 0.05, 0.45, 0.35, 1.04 + k * 0.02, 1.055 + k * 0.02)  # tray stack
tray_stand('Tray_Return_Trash_1', -2.6, -8.6)
tray_stand('Tray_Return_Trash_2', 2.6, -8.6)

# ---------------- Dining ----------------
def chair(cx, cz, fx, fz):  # (fx,fz) = direction the sitter faces (toward table)
    C('Mat_AccentWood', cx, cz, 0.45, 0.45, 0.42, 0.47)
    for dx in (-0.19, 0.19):
        for dz in (-0.19, 0.19):
            C('Mat_MetalLegs', cx + dx, cz + dz, 0.035, 0.035, 0, 0.42)
    bx, bz = cx - fx * 0.2, cz - fz * 0.2
    sx, sz = (0.45, 0.05) if fz else (0.05, 0.45)
    C('Mat_AccentTeal', bx, bz, sx, sz, 0.47, 0.95)

def table(cx, cz, sx, sz):
    C('Mat_TableTop', cx, cz, sx, sz, 0.72, 0.76)
    C('Mat_MetalLegs', cx, cz, 0.08, 0.08, 0.04, 0.72)
    C('Mat_MetalLegs', cx, cz, 0.5, 0.5, 0, 0.04)

n4 = n2 = 0
for cx in (-7.5, 7.5):
    for cz in (-6.5, -3.2):
        n4 += 1; O(f'Table4_{n4}')
        table(cx, cz, 0.9, 0.9)
        for fx, fz in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            chair(cx - fx * 0.75, cz - fz * 0.75, fx, fz)
for cx in (-4.2, 4.2):
    for cz in (-6.5, -3.2):
        n2 += 1; O(f'Table2_{n2}')
        table(cx, cz, 0.7, 0.7)
        for fz in (1, -1):
            chair(cx, cz - fz * 0.65, 0, fz)

# Center of the dining room: two long shared tables either side of a straight 2.4 m aisle (door -> cashier)
for i, cx in enumerate((-2.3, 2.3), 1):
    O(f'Shared_Table_{i}')
    C('Mat_TableTop', cx, -4.6, 1.0, 4.0, 0.72, 0.76)
    for cz in (-6.3, -2.9):
        C('Mat_MetalLegs', cx, cz, 0.08, 0.08, 0.04, 0.72)
        C('Mat_MetalLegs', cx, cz, 0.7, 0.5, 0, 0.04)
    for k in range(5):
        cz = -6.2 + k * 0.8
        chair(cx - 0.78, cz, 1, 0)
        chair(cx + 0.78, cz, -1, 0)

nb = 0
for xa, xb in ((-11.95, -10.35), (10.35, 11.95)):
    for s in (-8.8, -6.5, -4.2, -1.9):
        nb += 1; O(f'Booth_{nb}')
        for z0, z1, back in ((s, s + 0.6, (s, s + 0.12)), (s + 1.5, s + 2.1, (s + 1.98, s + 2.1))):
            B('Mat_BoothFabric', xa, xb, 0, 0.45, z0, z1)
            B('Mat_BoothFabric', xa, xb, 0.45, 1.15, *back)
            B('Mat_AccentWood', xa, xb, 1.15, 1.2, *back)
        tx0, tx1 = (xa, xb - 0.2) if xa < 0 else (xa + 0.2, xb)
        B('Mat_TableTop', tx0, tx1, 0.72, 0.76, s + 0.65, s + 1.45)
        C('Mat_MetalLegs', (tx0 + tx1) / 2, s + 1.05, 0.08, 0.08, 0, 0.72)

# ---------------- Kitchen line (back wall) ----------------
O('Grill_Counter')
B('Mat_StainlessSteel', -11.95, -9.05, 0, 0.85, 8.2, 9)
B('Mat_StainlessDark', -11.9, -9.1, 0.3, 0.75, 8.18, 8.2)          # door fronts
B('Mat_CooktopDark', -11.85, -9.15, 0.85, 0.92, 8.25, 8.95)        # griddle plate
B('Mat_StainlessSteel', -11.95, -9.05, 0.85, 1.0, 8.2, 8.25)       # grease trough lip
B('Mat_StainlessSteel', -11.95, -9.05, 0.85, 1.3, 8.95, 9)         # splash guard
B('Mat_StainlessSteel', -11.95, -11.9, 0.85, 1.3, 8.25, 8.95)
B('Mat_StainlessSteel', -9.1, -9.05, 0.85, 1.3, 8.25, 8.95)

O('Stove')
B('Mat_StainlessSteel', -8.95, -7.05, 0, 0.9, 8.2, 9)
B('Mat_CooktopDark', -8.95, -7.05, 0.9, 0.93, 8.2, 9)
for bx in (-8.5, -7.5):
    for bz in (8.4, 8.8):
        C('Mat_BurnerMetal', bx, bz, 0.3, 0.3, 0.93, 0.97)
B('Mat_StainlessDark', -8.85, -7.15, 0.15, 0.7, 8.17, 8.2)         # oven door
B('Mat_StainlessSteel', -8.7, -7.3, 0.65, 0.68, 8.12, 8.17)        # oven handle
for k in range(4):
    C('Mat_POSTerminalDark', -8.6 + k * 0.4, 8.17, 0.06, 0.04, 0.78, 0.84)  # knobs

O('Range_Hood')
B('Mat_StainlessSteel', -11.95, -7.05, 2.0, 2.6, 7.9, 9)
B('Mat_StainlessDark', -11.9, -7.1, 1.98, 2.0, 7.95, 8.95)         # filter face
B('Mat_StainlessSteel', -9.8, -9.2, 2.6, H, 8.4, 9)                # duct

O('Prep_Counter')
B('Mat_StainlessSteel', -6.95, -4.6, 0, 0.9, 8.3, 9)
B('Mat_CountertopStone', -6.95, -4.6, 0.9, 0.95, 8.25, 9)
B('Mat_StainlessDark', -6.9, -4.65, 0.12, 0.15, 8.35, 8.95)        # under shelf
O('Cutting_Board');  B('Mat_LightOakWood', -6.4, -5.6, 0.95, 0.99, 8.45, 8.85)
O('Prep_Sink')
B('Mat_StainlessSteel', -4.5, -3.0, 0, 0.75, 8.3, 9)
B('Mat_StainlessSteel', -4.5, -3.0, 0.75, 0.95, 8.3, 8.4)          # basin rim (hollow basin)
B('Mat_StainlessSteel', -4.5, -3.0, 0.75, 0.95, 8.9, 9)
B('Mat_StainlessSteel', -4.5, -4.4, 0.75, 0.95, 8.4, 8.9)
B('Mat_StainlessSteel', -3.1, -3.0, 0.75, 0.95, 8.4, 8.9)
B('Mat_StainlessDark', -4.4, -3.1, 0.74, 0.76, 8.4, 8.9)           # basin floor
B('Mat_StainlessSteel', -3.8, -3.7, 0.95, 1.3, 8.9, 8.98)          # faucet
B('Mat_StainlessSteel', -3.8, -3.7, 1.25, 1.3, 8.65, 8.9)

O('Kitchen_Trash_1');  C('Mat_TrashBlack', -2.5, 8.6, 0.5, 0.5, 0, 0.85); C('Mat_TrashBlack', -2.5, 8.6, 0.54, 0.54, 0.85, 0.9)
O('Kitchen_Trash_2');  C('Mat_TrashBlack', 4.8, 2.7, 0.5, 0.5, 0, 0.85);  C('Mat_TrashBlack', 4.8, 2.7, 0.54, 0.54, 0.85, 0.9)

O('Dish_Sink_3_Compartment')
B('Mat_StainlessSteel', -1.9, 1.5, 0, 0.75, 8.3, 9)
B('Mat_StainlessSteel', -1.9, 1.5, 0.9, 1.3, 8.95, 9)              # backsplash
B('Mat_StainlessSteel', -1.9, -1.3, 0.75, 0.9, 8.3, 8.95)          # left drainboard
B('Mat_StainlessSteel', 0.9, 1.5, 0.75, 0.9, 8.3, 8.95)            # right drainboard
for k in range(3):
    x0 = -1.3 + k * 0.733
    B('Mat_StainlessSteel', x0, x0 + 0.733, 0.75, 0.95, 8.3, 8.38)
    B('Mat_StainlessSteel', x0, x0 + 0.733, 0.75, 0.95, 8.87, 8.95)
    B('Mat_StainlessSteel', x0, x0 + 0.05, 0.75, 0.95, 8.38, 8.87)
    B('Mat_StainlessDark', x0 + 0.05, x0 + 0.733, 0.74, 0.76, 8.38, 8.87)
    B('Mat_StainlessSteel', x0 + 0.33, x0 + 0.4, 0.95, 1.35, 8.88, 8.95)  # faucet
B('Mat_StainlessSteel', 0.85, 0.9, 0.75, 0.95, 8.38, 8.87)

O('Mop_Sink')
B('Mat_CupWhite', 1.7, 2.4, 0, 0.3, 8.4, 9)
B('Mat_StainlessDark', 1.8, 2.3, 0.29, 0.31, 8.5, 8.9)
B('Mat_StainlessSteel', 2.0, 2.08, 0.3, 1.0, 8.92, 9)

for i, (x0, x1) in enumerate(((2.7, 5.1), (5.3, 7.7)), 1):
    O(f'Dry_Storage_Shelf_{i}')
    for px in (x0, x1 - 0.04):
        for pz in (8.45, 8.96):
            B('Mat_MetalLegs', px, px + 0.04, 0, 2.0, pz, pz + 0.04)
    for k, y in enumerate((0.15, 0.6, 1.05, 1.5, 1.95)):
        B('Mat_StainlessSteel', x0, x1, y, y + 0.03, 8.45, 9)

# ---------------- Walk-in freezer (x 8.2..12, z 4..9) ----------------
O('Walk_In_Freezer')
FW = 'Mat_FreezerWhite'
B(FW, 8.2, 12, 0, 2.8, 4.0, 4.12)           # front (faces the counter)
B(FW, 8.2, 8.32, 0, 2.8, 4.12, 5.0)          # west wall, door gap z 5.0..6.1
B(FW, 8.2, 8.32, 0, 2.8, 6.1, 9)
B(FW, 8.2, 8.32, 2.2, 2.8, 5.0, 6.1)         # above door
B(FW, 8.2, 12, 2.8, 2.9, 4.0, 9)             # roof
B('Mat_StainlessDark', 8.32, 12, 0, 0.02, 4.12, 9)  # floor plate
for z in (4.98, 6.1):
    B('Mat_StainlessSteel', 8.15, 8.37, 0, 2.2, z - 0.04, z + 0.04)  # door jambs
O('Walk_In_Freezer_Door')                    # hinged at z 6.1, left open into the kitchen
B(FW, 7.12, 8.2, 0, 2.18, 6.06, 6.16)
B('Mat_StainlessSteel', 7.25, 7.33, 0.9, 1.3, 5.98, 6.06)

# ---------------- Holding cabinet + heat lamps (pass between kitchen and counter) ----------------
O('Pass_Table')
B('Mat_StainlessSteel', -3.6, 3.6, 0.85, 0.9, 2.4, 3.1)
B('Mat_StainlessSteel', -3.6, 3.6, 0.15, 0.18, 2.45, 3.05)
for px in (-3.55, 3.51):
    for pz in (2.42, 3.04):
        B('Mat_MetalLegs', px, px + 0.04, 0, 0.85, pz, pz + 0.04)
O('Holding_Cabinet')
B('Mat_StainlessSteel', -3.3, -0.5, 0.9, 1.6, 2.5, 3.0)
for k in range(3):
    y = 1.0 + k * 0.2
    B('Mat_POSTerminalDark', -3.2, -0.6, y, y + 0.12, 2.48, 3.02)   # pass-through slots, both sides
B('Mat_HeatLampGlow', -3.3, -0.5, 1.6, 1.64, 2.5, 3.0)              # status strip
O('Heat_Lamps')
for px in (-0.1, 3.3):
    B('Mat_StainlessSteel', px, px + 0.06, 0.9, 1.75, 2.72, 2.78)
B('Mat_StainlessSteel', -0.1, 3.36, 1.75, 1.8, 2.5, 3.0)            # lamp bar
for k in range(4):
    lx = 0.2 + k * 0.8
    B('Mat_StainlessDark', lx, lx + 0.5, 1.68, 1.75, 2.55, 2.95)    # lamp housing
    B('Mat_HeatLampGlow', lx + 0.05, lx + 0.45, 1.665, 1.68, 2.6, 2.9)

# ---------------- Materials ----------------
MATS = {  # name: (Kd rgb, Ks, Ns, d)
    'Mat_StainlessSteel': ((0.77, 0.82, 0.86), 0.6, 75, 1), 'Mat_StainlessDark': ((0.48, 0.55, 0.60), 0.5, 50, 1),
    'Mat_CabinetBase': ((0.47, 0.42, 0.38), 0.1, 12, 1), 'Mat_CountertopStone': ((0.84, 0.86, 0.88), 0.3, 40, 1),
    'Mat_LightOakWood': ((0.82, 0.66, 0.45), 0.1, 15, 1), 'Mat_MetalLegs': ((0.25, 0.25, 0.27), 0.5, 60, 1),
    'Mat_CooktopDark': ((0.08, 0.08, 0.09), 0.4, 60, 1), 'Mat_BurnerMetal': ((0.30, 0.30, 0.32), 0.5, 50, 1),
    'Mat_POSTerminalDark': ((0.12, 0.12, 0.13), 0.3, 40, 1), 'Mat_POSScreenWhite': ((0.92, 0.95, 1.0), 0.5, 80, 1),
    'Mat_FloorTile': ((0.86, 0.84, 0.80), 0.2, 25, 1), 'Mat_WallsArchitectural': ((0.95, 0.92, 0.85), 0.05, 10, 1),
    'Mat_KitchenFloorSlate': ((0.36, 0.39, 0.38), 0.15, 20, 1), 'Mat_AccentTeal': ((0.13, 0.42, 0.42), 0.2, 30, 1),
    'Mat_AccentWood': ((0.78, 0.58, 0.36), 0.15, 20, 1), 'Mat_BoothFabric': ((0.20, 0.48, 0.44), 0.1, 15, 1),
    'Mat_TableTop': ((0.62, 0.45, 0.30), 0.2, 25, 1), 'Mat_GlassWindow': ((0.70, 0.85, 0.92), 0.9, 120, 0.35),
    'Mat_MenuScreen': ((0.15, 0.10, 0.08), 0.5, 80, 1), 'Mat_CupWhite': ((0.97, 0.97, 0.96), 0.2, 20, 1),
    'Mat_TrashBlack': ((0.10, 0.10, 0.10), 0.1, 10, 1),
    'Mat_FreezerWhite': ((0.90, 0.92, 0.93), 0.3, 30, 1), 'Mat_HeatLampGlow': ((1.00, 0.55, 0.15), 0.2, 20, 1),
}

# ---------------- Write OBJ/MTL ----------------
FACES = [  # (normal, corner picks) CCW seen from outside; corners use (xi, yi, zi) index into (lo,hi)
    ((0, 0, 1),  [(0, 0, 1), (1, 0, 1), (1, 1, 1), (0, 1, 1)]),
    ((0, 0, -1), [(1, 0, 0), (0, 0, 0), (0, 1, 0), (1, 1, 0)]),
    ((0, 1, 0),  [(0, 1, 1), (1, 1, 1), (1, 1, 0), (0, 1, 0)]),
    ((0, -1, 0), [(0, 0, 0), (1, 0, 0), (1, 0, 1), (0, 0, 1)]),
    ((1, 0, 0),  [(1, 0, 1), (1, 0, 0), (1, 1, 0), (1, 1, 1)]),
    ((-1, 0, 0), [(0, 0, 0), (0, 0, 1), (0, 1, 1), (0, 1, 0)]),
]
def sub(a, b): return [a[i] - b[i] for i in range(3)]
def cross(a, b): return [a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0]]

def write(out_dir):
    names = [n for n, _ in objs]
    assert len(names) == len(set(names)), 'duplicate object names'
    used = {b[0] for _, bs in objs for b in bs}
    assert used <= MATS.keys(), used - MATS.keys()
    L = ['# Wavefront OBJ - Cafe restaurant (24 x 18 m), generated by gen_restaurant.py',
         '# Every object has its own o group for individual adjustment in Unity.', 'mtllib restaurant.mtl', '']
    vi = 1
    for name, boxes in objs:
        assert boxes, name
        L += [f'o {name}', f'g {name}']
        cur = None
        for mat, x0, x1, y0, y1, z0, z1 in boxes:
            assert x1 > x0 and y1 > y0 and z1 > z0, (name, x0, x1, y0, y1, z0, z1)
            assert -12.3 <= x0 and x1 <= 12.3 and -9.3 <= z0 and z1 <= 9.3 and -0.11 <= y0 and y1 <= H + 0.01, name
            if mat != cur:
                L.append(f'usemtl {mat}'); cur = mat
            X, Y, Z = (x0, x1), (y0, y1), (z0, z1)
            for n, picks in FACES:
                p = [(X[a], Y[b], Z[c]) for a, b, c in picks]
                cn = cross(sub(p[1], p[0]), sub(p[3], p[0]))
                assert all((cn[i] > 0) == (n[i] > 0) and (cn[i] < 0) == (n[i] < 0) for i in range(3)), 'winding'
                su = max(abs(p[1][i] - p[0][i]) for i in range(3)); sv = max(abs(p[3][i] - p[0][i]) for i in range(3))
                for v in p: L.append('v %.4f %.4f %.4f' % v)
                for uv in ((0, 0), (su, 0), (su, sv), (0, sv)): L.append('vt %.4f %.4f' % uv)
                L.append('vn %d %d %d' % n)
                ni = vi // 4 + 1  # one normal per face
                L.append('f ' + ' '.join(f'{vi+k}/{vi+k}/{ni}' for k in range(4)))
                vi += 4
        L.append('')
    open(os.path.join(out_dir, 'restaurant.obj'), 'w', newline='\n').write('\n'.join(L))
    M = ['# Wavefront MTL - Cafe restaurant materials', '']
    for m, (kd, ks, ns, d) in MATS.items():
        M += [f'newmtl {m}', 'Ka 0.200 0.200 0.200', 'Kd %.3f %.3f %.3f' % kd, 'Ks %.3f %.3f %.3f' % (ks, ks, ks),
              f'Ns {ns:.1f}', f'd {d}', 'illum 2', '']
    open(os.path.join(out_dir, 'restaurant.mtl'), 'w', newline='\n').write('\n'.join(M))
    print(len(objs), 'objects,', (vi - 1) // 4, 'faces')

if __name__ == '__main__':
    write(sys.argv[1] if len(sys.argv) > 1 else '.')
