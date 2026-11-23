"""
Convertit des assets du projet Unity de Ninou's Farm en ressources Godot.

Ce que fait le script (relançable autant de fois qu'on veut, il écrase sa sortie) :
  1. copie les textures utilisées dans Godot/assets/ ;
  2. lit les RuleTiles Unity (.asset) et les découpages de sprites (.png.meta) pour produire
     - resources/tiles/world_tileset.tres : le TileSet Godot (une source d'atlas par texture,
       avec les polygones de collision repris des « Physics Shape » des sprites Unity) ;
     - resources/tiles/rule_tiles.json : les règles des RuleTiles (voisins This / NotThis, tuiles),
       relues à l'exécution par RuleTile.cs ;
     ainsi que les feuilles labourées / arrosées de SO_FarmTileSet (« tuiles miroir ») ;
  3. reconstruit les animations du personnage (SpriteFrames) en lisant directement les clips .anim ;
  4. convertit les ScriptableObjects d'objets (outils, récoltes, graines, cultures) en ressources .tres ;
  5. extrait les sprites d'interface (case, cadre 9-slice, sélecteur, widget d'heure), la police pixel,
     et les animations du coffre et de l'arbre ;
  6. monde procédural : RuleTiles et pentes des 5 biomes, config de génération, catalogue du décor.

Usage (depuis la racine du dépôt) :
    python Godot/tools/convert_unity_assets.py
"""
import json
import re
import shutil
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
UNITY = REPO / "Assets" / "_Project" / "Assets"
GODOT = REPO / "Godot"
PACK = UNITY / "Packs" / "Sprout Lands - Sprites - premium pack" / "Sprout Lands - Sprites - premium pack"

TILE = 16  # pixels par case (16 PPU dans Unity)

# RuleTiles converties : nom côté Godot -> asset Unity
RULE_TILES = {
    "ground": UNITY / "World" / "RuleTiles" / "RuleTile_Grass.asset",
    "floor": UNITY / "World" / "RuleTiles" / "RuleTile_Grass_High.asset",
    "water": UNITY / "World" / "RuleTiles" / "RuleTile_Water.asset",
}
# Monde procédural (GridWorld._tileMappings de Test_1) : une RuleTile de sol et une de falaise par biome.
# Nom Godot : <biome>_ground / <biome>_floor ; les collines (HILLS) gardent « ground » / « floor ».
BIOME_RULE_TILES = {
    "FOREST": ("RuleTile_Forest", "RuleTile_Forest_High"),
    "DESERT": ("RuleTile_Desert", "RuleTile_Desert_High"),
    "DEADZONE": ("RuleTile_Deadzone", "RuleTile_Deadzone_High"),
    "MOUNTAINS": ("RuleTile_Mountains", "RuleTile_Mountains_High"),
}
for _biome, (_ground, _floor) in BIOME_RULE_TILES.items():
    RULE_TILES[f"{_biome.lower()}_ground"] = UNITY / "World" / "RuleTiles" / f"{_ground}.asset"
    RULE_TILES[f"{_biome.lower()}_floor"] = UNITY / "World" / "RuleTiles" / f"{_floor}.asset"
# Pentes (GridWorld._slopes) : 4 tuiles par biome, [haut-gauche, haut-droite, bas-gauche, bas-droite]
SLOPE_PREFIX = {"HILLS": "Slope_Hill", "FOREST": "Slope_Forest", "DESERT": "Slope_Desert",
                "DEADZONE": "Slope_Deadzone", "MOUNTAINS": "Slope_Mountains"}
for _biome, _prefix in SLOPE_PREFIX.items():
    for _i in range(4):
        RULE_TILES[f"slope_{_biome.lower()}_{_i}"] = \
            UNITY / "World" / "Slopes" / (f"{_prefix}.asset" if _i == 0 else f"{_prefix} {_i}.asset")

WORLD_CONFIG = UNITY / "World" / "WorldConfig" / "SO_WorldGenConfig.asset"
WORLD_ITEMS = {
    "basic": [UNITY / "World" / "Items" / "Commons" / f for f in (
        "CommonItem _Leaf 1.asset", "CommonItem_Leaf.asset", "CommonItem_Leaf 2.asset",
        "CommonItem_Purple_Mushroom.asset", "CommonItem_Red_Mushroom.asset", "CommonItem_Rock.asset",
        "CommonItem_Rock 1.asset", "CommonItem_Rock 2.asset", "CommonItem_Rock 3.asset", "CommonItem_Rock 4.asset")],
    "biomes": [UNITY / "World" / "Items" / "Biomes" / f for f in (
        "WATER/WATER_Items.asset", "HILLS/HILLS_Items.asset", "FOREST/FOREST_Items.asset",
        "DESERT_Items.asset", "MOUNTAINS/MOUNTAINS_Items.asset", "DEADZONE_Items.asset")],
    "interactive": [UNITY / "World" / "Items" / "Biomes" / "FOREST" / "FOREST_InteractiveItems.asset"],
}
# Prefabs Unity des objets interactifs -> scènes Godot
PREFAB_SCENES = {"Tree.prefab": "res://scenes/objects/tree.tscn"}
BIOME_NAMES = ["WATER", "HILLS", "FOREST", "DESERT", "DEADZONE", "MOUNTAINS"]

# Feuille du personnage (découpée en grille 48x48) et dossier de ses clips
CHARACTER_SHEET = PACK / "Characters" / "Premium Charakter Spritesheet.png"
CHARACTER_CLIPS = UNITY / "Characters" / "Pneem"

# Catalogue des tuiles de ferme (sol -> labouré -> arrosé)
FARM_TILESET = UNITY / "World" / "FarmindData" / "SO_FarmTileSet.asset"

# ScriptableObjects d'objets convertis en .tres (dossier Unity -> dossier Godot)
ITEM_FOLDERS = {
    UNITY / "World" / "ToolData": "resources/items/tools",
    UNITY / "World" / "Items" / "ItemData_Dropable": "resources/items/loot",
    UNITY / "World" / "FarmindData" / "Crops": "resources/items/crops",
    UNITY / "World" / "Fishing": "resources/items/fishing",
}
# Classes Unity converties -> script Godot
ITEM_SCRIPTS = {
    "ItemData": "res://scripts/Items/ItemData.cs",
    "HoeData": "res://scripts/Tools/HoeData.cs",
    "AxeData": "res://scripts/Tools/AxeData.cs",
    "FishingRodData": "res://scripts/Tools/FishingRodData.cs",
    "FishData": "res://scripts/Fishing/FishData.cs",
    "FishLootTable": "res://scripts/Fishing/FishLootTable.cs",
    "WateringCanData": "res://scripts/Tools/WateringCanData.cs",
    "SeedData": "res://scripts/World/Farming/SeedData.cs",
    "CropData": "res://scripts/World/Farming/CropData.cs",
}
SPLASH_SCRIPT = "res://scripts/Tools/SplashSet.cs"
FISH_ENTRY_SCRIPT = "res://scripts/Fishing/FishLootEntry.cs"
CROP_SCENE = "res://scenes/crop_instance.tscn"
# Trigger de l'Animator Unity -> préfixe d'animation Godot
ANIMATION_NAMES = {"HOE": "hoe", "WATER": "watering", "CHOP": "chop"}

# Interface : feuille de sprites d'UI, police, et sprites utilisés (nom Godot -> nom du sprite Unity)
UI_PACK = UNITY / "Packs" / "Sprout Lands - UI Pack - Premium pack" / "Sprout Lands - UI Pack - Premium pack"
UI_SHEET = UI_PACK / "UI Sprites" / "ALL UI ASSETS on one sheet.png"
UI_FONT = UI_PACK / "fonts" / "pixelFont-7-8x14-sproutLands.ttf"
UI_SPRITES = {
    "slot": "ALL UI ASSETS on one sheet_30",           # case d'inventaire (UISlot.prefab)
    "panel": "ALL UI ASSETS on one sheet_59",          # cadre 9-slice des panneaux
    "selector_top_left": "ALL UI ASSETS on one sheet_120",
    "selector_top_right": "ALL UI ASSETS on one sheet_121",
    "selector_bottom_left": "ALL UI ASSETS on one sheet_127",
    "selector_bottom_right": "ALL UI ASSETS on one sheet_128",
}

# Widget d'heure (TimeUI) : sprites pris dans d'autres feuilles que celle de l'UI
WEATHER_DIR = UI_PACK / "emojis" / "emoji style ui" / "weather"
PORTRAIT_SHEET = UNITY / "UI" / "UserProfileAnimations" / "Teemo premium emote animations sprite sheet-export.png"
EXTRA_UI_SPRITES = {
    "time_frame": (WEATHER_DIR / "Weather_UI.png", "Weather_UI_2"),
    "time_weather": (WEATHER_DIR / "Weather_UI.png", "Weather_UI_12"),
    "time_arrow": (WEATHER_DIR / "Weather_UI.png", "Weather_UI_6"),
    "sky_day": (WEATHER_DIR / "Weather_Icons_Big.png", "Weather_Icons_Big_58"),
    "sky_sunset": (WEATHER_DIR / "Weather_Icons_Big.png", "Weather_Icons_Big_62"),
    "sky_night": (WEATHER_DIR / "Weather_Icons_Big.png", "Weather_Icons_Big_65"),
}

# Portrait animé du widget d'heure (Animator UserCharacter)
PORTRAIT_CLIPS = UNITY / "UI" / "UserProfileAnimations"

# Arbre : clips de l'Animator (nom Godot -> (clip, boucle)) et hitbox de la hache
TREE_CLIPS = {
    "idle": (UNITY / "Prefabs" / "Trees" / "Idle.anim", True),
    "hit": (UNITY / "Prefabs" / "Trees" / "Hit.anim", False),
    "cut": (UNITY / "Prefabs" / "Trees" / "Cut.anim", False),
}
AXE_HITBOX_SCENE = "res://scenes/objects/axe_hitbox.tscn"
# Layers Unity -> masque de calque Godot (9 = arbres / objets destructibles -> calque 6 « damageable »)
UNITY_TO_GODOT_LAYER = {9: 1 << 5}

# Coffre : clips d'ouverture / fermeture (feuille en cases de 48x48)
CHEST_CLIPS = {"open": UNITY / "Animations" / "OpenChest.anim", "close": UNITY / "Animations" / "CloseChest.anim"}
CHEST_CELL = 48

# RuleTiles purement visuelles : l'eau est peinte partout sous le sol, sa collision est portée
# par le calque COLLISION (tuile "blocker") posé uniquement hors des terres.
NO_COLLISION = {"water"}

# Collisions : RuleTile.m_ColliderType (0 None, 1 Sprite, 2 Grid)
COLLIDER_SPRITE = 1
COLLIDER_GRID = 2


# --------------------------------------------------------------------------- utilitaires YAML Unity
def guid_index():
    """guid -> chemin de l'asset, pour tous les .meta du dossier Assets/_Project/Assets."""
    index = {}
    for meta in UNITY.rglob("*.meta"):
        with meta.open(encoding="utf-8", errors="ignore") as f:
            for line in f:
                if line.startswith("guid:"):
                    index[line.split()[1]] = meta.with_suffix("")
                    break
    return index


def read_texture_meta(png: Path):
    """
    Lit le .meta d'une texture : taille des sprites, rectangles, physics shapes, par fileID.
    Renvoie (spriteMode, {fileID: sprite}) où sprite = {name, rect:(x,y,w,h), shapes:[[(x,y),...]]}.
    """
    text = (png.parent / (png.name + ".meta")).read_text(encoding="utf-8", errors="ignore")
    mode = int(re.search(r"^\s*spriteMode: (\d+)", text, re.M).group(1))
    sprites = {}

    # Anciens identifiants 32 bits (table fileIDToRecycleName / internalIDToNameTable) -> nom
    names_by_id = {}
    for m in re.finditer(r"- first:\s*\n\s*213: (-?\d+)\s*\n\s*second: (.+)", text):
        names_by_id[int(m.group(1))] = m.group(2).strip()

    if mode == 1:
        # Sprite unique : fileID 21300000, toute la texture.
        w, h = png_size(png)
        sprites[21300000] = {"name": png.stem, "rect": (0, 0, w, h), "shapes": parse_shapes(text_block_top_physics(text))}
        return mode, sprites

    by_name = {}
    for block in re.split(r"\n    - serializedVersion: 2\n", text)[1:]:
        name = re.search(r"^      name: (.+)$", block, re.M)
        if not name:
            continue
        rect = re.search(r"rect:\s*\n\s*serializedVersion: 2\s*\n\s*x: ([\d.]+)\s*\n\s*y: ([\d.]+)\s*\n\s*width: ([\d.]+)\s*\n\s*height: ([\d.]+)", block)
        internal = re.search(r"^      internalID: (-?\d+)", block, re.M)
        shapes_text = re.search(r"^      physicsShape:(.*?)^      tessellationDetail", block, re.M | re.S)
        alignment = re.search(r"^      alignment: (\d+)", block, re.M)
        pivot = re.search(r"^      pivot: \{x: (-?[\d.e-]+), y: (-?[\d.e-]+)\}", block, re.M)
        sprite = {
            "name": name.group(1).strip(),
            "rect": tuple(int(float(v)) for v in rect.groups()),
            "shapes": parse_shapes(shapes_text.group(1) if shapes_text else ""),
            "pivot": sprite_pivot(int(alignment.group(1)) if alignment else 0,
                                  (float(pivot.group(1)), float(pivot.group(2))) if pivot else (0.5, 0.5)),
        }
        by_name[sprite["name"]] = sprite
        if internal:
            sprites[int(internal.group(1))] = sprite

    for file_id, name in names_by_id.items():
        if name in by_name:
            sprites.setdefault(file_id, by_name[name])
    return mode, sprites


# SpriteAlignment Unity -> pivot normalisé (x vers la droite, y vers le HAUT) ; 9 = Custom (pivot du .meta)
ALIGNMENT_PIVOTS = {0: (0.5, 0.5), 1: (0, 1), 2: (0.5, 1), 3: (1, 1), 4: (0, 0.5), 5: (1, 0.5),
                    6: (0, 0), 7: (0.5, 0), 8: (1, 0)}


def sprite_pivot(alignment, custom):
    return custom if alignment == 9 else ALIGNMENT_PIVOTS.get(alignment, (0.5, 0.5))


def text_block_top_physics(text):
    m = re.search(r"^    physicsShape:(.*?)^    \w", text, re.M | re.S)
    return m.group(1) if m else ""


def parse_shapes(text):
    """physicsShape Unity : liste de polygones, points en pixels depuis le centre du sprite, y vers le haut."""
    shapes = []
    for poly in re.split(r"\n\s*- - ", "\n" + text.strip())[1:]:
        pts = [(float(x), float(y)) for x, y in re.findall(r"\{x: (-?[\d.]+), y: (-?[\d.]+)\}", poly)]
        if len(pts) >= 3:
            shapes.append(pts)
    return shapes


def png_size(png: Path):
    with png.open("rb") as f:
        head = f.read(24)
    return int.from_bytes(head[16:20], "big"), int.from_bytes(head[20:24], "big")


def read_rule_tile(asset: Path):
    """Règles d'une RuleTile : liste de {sprites:[(guid, fileID)], output, collider, neighbors:[(dx,dy,v)]}."""
    text = asset.read_text(encoding="utf-8", errors="ignore")
    default_collider = int(re.search(r"m_DefaultColliderType: (\d+)", text).group(1))
    rules = []
    for block in re.split(r"\n  - m_Id: ", text)[1:]:
        sprites = [(g, int(fid)) for fid, g in re.findall(r"- \{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}", block.split("m_GameObject")[0])]
        neighbors_hex = re.search(r"m_Neighbors: ([0-9a-f]*)", block).group(1)
        values = [int.from_bytes(bytes.fromhex(neighbors_hex[i:i + 8]), "little") for i in range(0, len(neighbors_hex), 8)]
        positions = [(int(x), int(y)) for x, y in re.findall(r"- \{x: (-?\d+), y: (-?\d+), z: 0\}", block)]
        rules.append({
            "sprites": sprites,
            "output": int(re.search(r"m_Output: (\d+)", block).group(1)),
            "collider": int(re.search(r"m_ColliderType: (\d+)", block).group(1)),
            "neighbors": [(x, y, v) for (x, y), v in zip(positions, values)],
            "transform": int(re.search(r"m_RuleTransform: (\d+)", block).group(1)),
        })
    # Aucune règle ne correspond : Unity affiche m_DefaultSprite. On l'ajoute comme dernière règle, sans condition.
    default = re.search(r"m_DefaultSprite: \{fileID: (-?\d+), guid: ([0-9a-f]+)", text)
    if default and default.group(1) != "0":
        rules.append({"sprites": [(default.group(2), int(default.group(1)))], "output": 0,
                      "collider": default_collider, "neighbors": [], "transform": 0})
    return default_collider, rules


# --------------------------------------------------------------------------- TileSet
class AtlasSource:
    def __init__(self, source_id, res_path, size):
        self.id = source_id
        self.res_path = res_path
        self.size = size
        self.tiles = {}  # (ax, ay) -> liste de polygones Godot (ou None si pas de collision)

    def add_tile(self, coords, polygons):
        current = self.tiles.get(coords)
        if current is None or (polygons and not current):
            self.tiles[coords] = polygons


def convert_tiles():
    guids = guid_index()
    sources = {}  # chemin Unity -> AtlasSource
    texture_cache = {}
    rule_json = {}
    warnings = []

    def source_for(png: Path):
        if png not in sources:
            dest = GODOT / "assets" / "tiles" / png.name.replace(" ", "_")
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(png, dest)
            sources[png] = AtlasSource(len(sources), "res://" + dest.relative_to(GODOT).as_posix(), png_size(png))
            texture_cache[png] = read_texture_meta(png)
        return sources[png]

    for name, asset in RULE_TILES.items():
        _, rules = read_rule_tile(asset)
        out_rules = []
        for rule in rules:
            if rule["transform"] != 0:
                warnings.append(f"{asset.name}: m_RuleTransform={rule['transform']} non géré (règle ignorée)")
                continue
            tiles = []
            for guid, file_id in rule["sprites"]:
                png = guids.get(guid)
                if png is None:
                    warnings.append(f"{asset.name}: texture introuvable (guid {guid})")
                    continue
                src = source_for(png)
                _, sprites = texture_cache[png]
                sprite = sprites.get(file_id)
                if sprite is None:
                    warnings.append(f"{asset.name}: sprite {file_id} introuvable dans {png.name}")
                    continue
                x, y, w, h = sprite["rect"]
                if (w, h) != (TILE, TILE):
                    warnings.append(f"{asset.name}: {sprite['name']} fait {w}x{h} (attendu {TILE}x{TILE})")
                # Unity : origine en bas à gauche de la texture ; Godot : en haut à gauche.
                coords = (x // TILE, (src.size[1] - y - h) // TILE)
                polygons = None
                if name in NO_COLLISION:
                    pass
                elif rule["collider"] == COLLIDER_SPRITE:
                    polygons = [[(px, -py) for px, py in shape] for shape in sprite["shapes"]]
                elif rule["collider"] == COLLIDER_GRID:
                    polygons = [[(-8, -8), (8, -8), (8, 8), (-8, 8)]]
                src.add_tile(coords, polygons)
                tiles.append([src.id, coords[0], coords[1]])
            if not tiles:
                continue
            out_rules.append({
                # Unity : y vers le haut ; Godot : y vers le bas.
                "neighbors": [[dx, -dy, v] for dx, dy, v in rule["neighbors"]],
                "random": rule["output"] == 1,
                "tiles": tiles,
            })
        rule_json[name] = {"rules": out_rules}

        # SlopeConnectedRuleTile.SlopesToConnect : tuiles de pente traitées comme « moi » par la falaise.
        text = asset.read_text(encoding="utf-8", errors="ignore")
        m = re.search(r"SlopesToConnect:\s*\n((?:  - \{.*\}\s*\n)+)", text)
        if m:
            asset_names = {p: n for n, p in RULE_TILES.items()}
            connects = [asset_names.get(guids.get(g)) for g in re.findall(r"guid: ([0-9a-f]+)", m.group(1))]
            rule_json[name]["connects"] = [c for c in connects if c]

    # Feuilles de ferme (SO_FarmTileSet) : la feuille labourée / arrosée a la même disposition que le sol,
    # la case labourée reprend donc les mêmes coordonnées d'atlas dans une autre source (« tuile miroir »).
    farm_sheets = []
    for ground_guid, hoed_guid, watered_guid in read_farm_sheets():
        ground_png = guids.get(ground_guid)
        if ground_png not in sources:
            continue  # feuille de sol pas (encore) utilisée par une RuleTile convertie
        ground_src = sources[ground_png]
        entry = {"ground": ground_src.id}
        for key, guid in (("hoed", hoed_guid), ("watered", watered_guid)):
            if guid and guids.get(guid):
                src = source_for(guids[guid])
                for coords in ground_src.tiles:
                    src.add_tile(coords, None)
                entry[key] = src.id
        farm_sheets.append(entry)
    rule_json["farm_sheets"] = farm_sheets

    # Tuile de collision pleine (calque COLLISION invisible) : 1re tuile d'eau, alternative 1.
    water_first = rule_json["water"]["rules"][0]["tiles"][0]
    rule_json["blocker"] = {"source": water_first[0], "atlas": water_first[1:], "alternative": 1}

    out_dir = GODOT / "resources" / "tiles"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "rule_tiles.json").write_text(json.dumps(rule_json, indent=1), encoding="utf-8")
    write_tileset(out_dir / "world_tileset.tres", sources.values(), water_first)
    return warnings


def read_farm_sheets():
    """SO_FarmTileSet._sheets : (guid sol, guid labouré, guid arrosé) de chaque feuille."""
    text = FARM_TILESET.read_text(encoding="utf-8", errors="ignore")
    sheets = []
    for block in re.split(r"\n  - Ground:", text)[1:]:
        parts = re.split(r"\n    (Hoed|Watered):", block)
        sections = {"Ground": parts[0]}
        for i in range(1, len(parts) - 1, 2):
            sections[parts[i]] = parts[i + 1]

        def first_guid(key):
            m = re.search(r"guid: ([0-9a-f]+)", sections.get(key, ""))
            return m.group(1) if m else None
        sheets.append((first_guid("Ground"), first_guid("Hoed"), first_guid("Watered")))
    return sheets


def fmt(v):
    return ("%g" % v)


def write_tileset(path: Path, sources, blocker):
    sources = sorted(sources, key=lambda s: s.id)
    lines = [f'[gd_resource type="TileSet" load_steps={len(sources) * 2 + 1} format=3]', ""]
    for s in sources:
        lines.append(f'[ext_resource type="Texture2D" path="{s.res_path}" id="tex_{s.id}"]')
    lines.append("")
    for s in sources:
        lines.append(f'[sub_resource type="TileSetAtlasSource" id="atlas_{s.id}"]')
        lines.append(f'texture = ExtResource("tex_{s.id}")')
        lines.append(f"texture_region_size = Vector2i({TILE}, {TILE})")
        for (ax, ay), polygons in sorted(s.tiles.items()):
            key = f"{ax}:{ay}/0"
            lines.append(f"{key} = 0")
            for i, poly in enumerate(polygons or []):
                pts = ", ".join(f"{fmt(x)}, {fmt(y)}" for x, y in poly)
                lines.append(f"{key}/physics_layer_0/polygon_{i}/points = PackedVector2Array({pts})")
        if s.id == blocker[0]:
            key = f"{blocker[1]}:{blocker[2]}"
            lines.append(f"{key}/next_alternative_id = 2")
            lines.append(f"{key}/1 = 1")
            lines.append(f"{key}/1/physics_layer_0/polygon_0/points = PackedVector2Array(-8, -8, 8, -8, 8, 8, -8, 8)")
        lines.append("")
    lines.append("[resource]")
    lines.append(f"tile_size = Vector2i({TILE}, {TILE})")
    lines.append("physics_layer_0/collision_layer = 1")
    for s in sources:
        lines.append(f'sources/{s.id} = SubResource("atlas_{s.id}")')
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------- Sprites isolés
class SpriteResolver:
    """
    Résout une référence de sprite Unity (guid + fileID) en (texture copiée dans Godot, région en pixels).
    Les textures sont copiées dans assets/sprites/ en gardant le chemin relatif au pack.
    """

    def __init__(self, guids):
        self.guids = guids
        self.metas = {}

    def resolve(self, guid, file_id):
        png = self.guids.get(guid)
        if png is None or png.suffix.lower() != ".png":
            return None
        if png not in self.metas:
            self.metas[png] = read_texture_meta(png)
        sprite = self.metas[png][1].get(file_id)
        if sprite is None:
            return None
        try:
            rel = png.relative_to(PACK)
        except ValueError:
            rel = Path(png.name)
        dest = GODOT / "assets" / "sprites" / Path(*[p.replace(" ", "_") for p in rel.parts])
        if not dest.exists():
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(png, dest)
        x, y, w, h = sprite["rect"]
        _, tex_h = png_size(png)
        return "res://" + dest.relative_to(GODOT).as_posix(), (x, tex_h - y - h, w, h)


class Tres:
    """Petit écrivain de fichier .tres (ressource Godot texte)."""

    def __init__(self, script_path=None):
        self.ext = {}   # chemin -> (type, id)
        self.subs = []  # (id, type, [lignes])
        self.script = self.ext_id(script_path, "Script") if script_path else None

    def ext_id(self, path, kind):
        if path not in self.ext:
            self.ext[path] = (kind, f"{len(self.ext) + 1}_{kind.lower()}")
        return self.ext[path][1]

    def atlas(self, resolved):
        tex_path, (x, y, w, h) = resolved
        sid = f"atlas_{len(self.subs) + 1}"
        tex = self.ext_id(tex_path, "Texture2D")
        self.subs.append((sid, "AtlasTexture", [f'atlas = ExtResource("{tex}")', f"region = Rect2({x}, {y}, {w}, {h})"]))
        return f'SubResource("{sid}")'

    def sub(self, kind, lines, script_path=None):
        sid = f"{kind.lower()}_{len(self.subs) + 1}"
        if script_path:
            lines = [f'script = ExtResource("{self.ext_id(script_path, "Script")}")'] + lines
        self.subs.append((sid, kind, lines))
        return f'SubResource("{sid}")'

    def write(self, path: Path, props, class_name="Resource"):
        head = f'[gd_resource type="Resource" script_class="{class_name}" load_steps={len(self.ext) + len(self.subs) + 1} format=3]'
        lines = [head, ""]
        for p, (kind, rid) in self.ext.items():
            lines.append(f'[ext_resource type="{kind}" path="{p}" id="{rid}"]')
        lines.append("")
        for sid, kind, body in self.subs:
            lines += [f'[sub_resource type="{kind}" id="{sid}"]'] + body + [""]
        lines.append("[resource]")
        if self.script:
            lines.append(f'script = ExtResource("{self.script}")')
        lines += [f"{k} = {v}" for k, v in props]
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------- Objets (ScriptableObjects)
def gd_string(s):
    return '"' + s.replace("\\", "\\\\").replace('"', '\\"') + '"'


def yaml_field(text, name):
    m = re.search(rf"^  {name}: ?(.*)$", text, re.M)
    return m.group(1).strip() if m else None


def ref(value):
    """{fileID: X, guid: Y, type: Z} -> (guid, fileID) ou None."""
    m = re.match(r"\{fileID: (-?\d+), guid: ([0-9a-f]+)", value or "")
    return (m.group(2), int(m.group(1))) if m else None


def yaml_list(text, name, indent="  "):
    """Liste de références sous un champ (ex : _growStages)."""
    m = re.search(rf"^{indent}{name}:\s*\n((?:{indent}- .*\n)+)", text, re.M)
    return [ref(line.split("- ", 1)[1]) for line in m.group(1).splitlines()] if m else []


def convert_items(guids):
    """Convertit outils, objets, graines et cultures en .tres. Renvoie les avertissements."""
    resolver = SpriteResolver(guids)
    warnings = []
    assets = {}  # guid de l'asset Unity -> (chemin Unity, classe, chemin res:// Godot)
    for folder, dest in ITEM_FOLDERS.items():
        for asset in sorted(folder.glob("*.asset")):
            text = asset.read_text(encoding="utf-8", errors="ignore")
            cls = re.search(r"m_EditorClassIdentifier: Assembly-CSharp::(\w+)", text)
            if not cls or cls.group(1) not in ITEM_SCRIPTS:
                warnings.append(f"{asset.name}: classe {cls.group(1) if cls else '?'} non convertie")
                continue
            guid = (asset.parent / (asset.name + ".meta")).read_text().split("guid: ")[1].split()[0]
            assets[guid] = (asset, cls.group(1), f"res://{dest}/{asset.stem}.tres")

    def res_ref(tres, value):
        r = ref(value)
        if not r or r[0] not in assets:
            return None
        return f'ExtResource("{tres.ext_id(assets[r[0]][2], "Resource")}")'

    def sprite(tres, value, what):
        r = ref(value)
        if not r or r[1] == 0:
            return None
        resolved = resolver.resolve(*r)
        if resolved is None:
            warnings.append(f"{what}: sprite {r} introuvable")
            return None
        return tres.atlas(resolved)

    for guid, (asset, cls, res_path) in assets.items():
        text = asset.read_text(encoding="utf-8", errors="ignore")
        tres = Tres(ITEM_SCRIPTS[cls])
        props = []

        if cls == "CropData":
            # Pas de _seed : il ferait une référence circulaire graine <-> culture, que Godot ne sait pas charger.
            # La graine se transmet elle-même à la culture au moment de planter (SeedData.Use).
            props.append(("_cropName", gd_string(yaml_field(text, "_cropName") or "")))
            props.append(("_scene", f'ExtResource("{tres.ext_id(CROP_SCENE, "PackedScene")}")'))
            icon = sprite(tres, yaml_field(text, "_icon"), asset.name)
            if icon:
                props.append(("_icon", icon))
            props.append(("_growTime", yaml_field(text, "_growTime") or "0"))
            stages = [sprite(tres, f"{{fileID: {r[1]}, guid: {r[0]}", asset.name) for r in yaml_list(text, "_growStages") if r]
            props.append(("_growStages", "Array[Texture2D]([" + ", ".join(s for s in stages if s) + "])"))
            loot = res_ref(tres, yaml_field(text, "_loot"))
            if loot:
                props.append(("_loot", loot))
        elif cls == "FishLootTable":
            # Liste de structs Entry { Fish, Weight } -> sous-ressources FishLootEntry.
            entries = []
            for fish_ref, weight in re.findall(r"- Fish: (\{[^}]*\})\s*\n\s*Weight: ([\d.]+)", text):
                fish = res_ref(tres, fish_ref)
                if fish:
                    entries.append(tres.sub("Resource", [f"Fish = {fish}", f"Weight = {weight}"], FISH_ENTRY_SCRIPT))
            props.append(("_entries", "Array[Resource]([" + ", ".join(entries) + "])"))
        else:
            for field in ("WorldSprite", "Icon"):
                s = sprite(tres, yaml_field(text, field), asset.name)
                if s:
                    props.append((field, s))
            props.append(("Name", gd_string(yaml_field(text, "Name") or asset.stem)))
            desc = yaml_field(text, "Description") or ""
            if desc:
                props.append(("Description", gd_string(desc)))
            props.append(("Amount", yaml_field(text, "Amount") or "1"))
            if yaml_field(text, "_maxStack"):
                props.append(("_maxStack", yaml_field(text, "_maxStack")))

        if cls == "FishData":
            for field in ("_zoneWidth", "_cursorSpeed"):
                if yaml_field(text, field):
                    props.append((field, yaml_field(text, field)))

        if cls == "FishingRodData":
            for field in ("StaminaCost", "Cooldown"):
                props.append((field, yaml_field(text, field) or "0"))
            table = res_ref(tres, yaml_field(text, "_lootTable"))
            if table:
                props.append(("_lootTable", table))
            delay = re.search(r"_biteDelay: \{x: ([\d.]+), y: ([\d.]+)\}", text)
            if delay:
                props.append(("_biteDelay", f"Vector2({delay.group(1)}, {delay.group(2)})"))
            if yaml_field(text, "_hitWindow"):
                props.append(("_hitWindow", yaml_field(text, "_hitWindow")))

        if cls in ("HoeData", "WateringCanData", "AxeData"):
            for field in ("StaminaCost", "Cooldown"):
                props.append((field, yaml_field(text, field) or "0"))
            trigger = (yaml_field(text, "_animationTriggerName") or "").upper()
            props.append(("_animationName", gd_string(ANIMATION_NAMES.get(trigger, trigger.lower()))))

        if cls == "AxeData":
            # Prefab Axe_HITBOX -> scène Godot ; layer Unity 9 (arbres...) -> calque Godot « damageable ».
            props.append(("_actionScene", f'ExtResource("{tres.ext_id(AXE_HITBOX_SCENE, "PackedScene")}")'))
            bits = int(re.search(r"_targetLayers:\s*\n\s*serializedVersion: \d+\s*\n\s*m_Bits: (\d+)", text).group(1))
            props.append(("_targetLayers", str(sum(UNITY_TO_GODOT_LAYER.get(i, 0) for i in range(32) if bits >> i & 1))))
            for field in ("_damage", "_actionDuration", "_spawnOffset"):
                if yaml_field(text, field):
                    props.append((field, yaml_field(text, field)))

        if cls == "WateringCanData":
            for field in ("_splashFront", "_splashBack", "_splashLeft", "_splashRight"):
                m = re.search(rf"^  {field}:\s*\n(.*?)(?=^  \w)", text, re.M | re.S)
                block = m.group(1) if m else ""
                frames = [sprite(tres, f"{{fileID: {r[1]}, guid: {r[0]}", asset.name)
                          for r in yaml_list(block, "Frames", "    ") if r]
                off = re.search(r"PixelOffset: \{x: (-?\d+), y: (-?\d+)\}", block)
                behind = re.search(r"Behind: (\d)", block)
                props.append((field, tres.sub("Resource", [
                    "Frames = Array[Texture2D]([" + ", ".join(f for f in frames if f) + "])",
                    f"PixelOffset = Vector2i({off.group(1) if off else 0}, {off.group(2) if off else 0})",
                    f"Behind = {'true' if behind and behind.group(1) == '1' else 'false'}",
                ], SPLASH_SCRIPT)))
            for field in ("_splashFrameRate", "_splashDelay"):
                if yaml_field(text, field):
                    props.append((field, yaml_field(text, field)))

        if cls == "SeedData":
            crop = res_ref(tres, yaml_field(text, "_cropData"))
            if crop:
                props.append(("_cropData", crop))

        tres.write(GODOT / res_path.replace("res://", ""), props, cls)
    return warnings, len(assets)


# --------------------------------------------------------------------------- Personnage
# Direction Unity -> Godot (Front/Back = face/dos)
DIRECTIONS = {"Front": "down", "Back": "up", "Left": "left", "Right": "right", "Down": "down", "Up": "up"}
# Clips d'action : joués une fois (« Loop Time » activé par erreur sur certains clips Unity)
ONE_SHOT = ("Chop", "Hoe", "Watering", "Show")


def read_clip(anim: Path):
    text = anim.read_text(encoding="utf-8", errors="ignore")
    keys = re.findall(r"- time: ([\d.e-]+)\s*\n\s*value: \{fileID: (-?\d+), guid: ([0-9a-f]+)", text)
    stop = float(re.search(r"m_StopTime: ([\d.e-]+)", text).group(1))
    loop = re.search(r"m_LoopTime: (\d)", text).group(1) == "1"
    return [(float(t), guid, int(fid)) for t, fid, guid in keys], stop, loop


def convert_character(guids):
    """
    Animations du personnage. La feuille principale (Premium Charakter Spritesheet) et les feuilles de pêche
    (fishing animation front / back / side) sont toutes découpées en cases de 48x48 : chaque clip peut venir
    de n'importe laquelle. Les clips « Right » de la pêche réutilisent la feuille de côté avec SpriteRenderer.flipX :
    leurs noms sont rangés dans les métadonnées « flip_h » de la ressource, que PlayerMovements applique.
    """
    dest_dir = GODOT / "assets" / "characters"
    dest_dir.mkdir(parents=True, exist_ok=True)
    sheets = {}  # chemin Unity -> (id ext_resource, chemin res://, sprites, hauteur)

    def sheet_for(png):
        if png not in sheets:
            name = "player_spritesheet.png" if png == CHARACTER_SHEET else png.name.replace(" ", "_")
            shutil.copyfile(png, dest_dir / name)
            sheets[png] = (f"sheet_{len(sheets)}", f"res://assets/characters/{name}",
                           read_texture_meta(png)[1], png_size(png)[1])
        return sheets[png]

    sheet_for(CHARACTER_SHEET)
    fps = 60.0  # vitesse de base ; la durée de chaque image est un multiple de 1/60 s
    regions, anims, skipped, flipped = {}, [], [], []
    for anim in sorted(CHARACTER_CLIPS.rglob("*.anim")):
        parts = anim.stem.split("_")
        if parts[-1] not in DIRECTIONS:
            skipped.append(anim.stem)  # diagonales (feuille « teemo 8 directions », sprites non découpés en grille)
            continue
        keys, stop, loop = read_clip(anim)
        sources = [guids.get(g) for _, g, _ in keys]
        if not keys or any(p is None or p.suffix.lower() != ".png" for p in sources):
            skipped.append(anim.stem)
            continue
        name = "_".join(p.lower() for p in parts[:-1]) + "_" + DIRECTIONS[parts[-1]]
        # Courbe m_FlipX à 1 : clé « value: 1 » juste avant « attribute: m_FlipX ».
        if re.search(r"value: 1\s*\n(?:.*\n){0,12}?\s*attribute: m_FlipX",
                     anim.read_text(encoding="utf-8", errors="ignore")):
            flipped.append(name)
        frames, ok = [], True
        for i, ((t, _, fid), png) in enumerate(zip(keys, sources)):
            end = keys[i + 1][0] if i + 1 < len(keys) else stop
            if end <= t and i + 1 < len(keys):
                continue  # deux clés au même instant : la seconde l'emporte
            sid, _, sprites, tex_h = sheet_for(png)
            if fid not in sprites or sprites[fid]["rect"][2:] != (48, 48):
                ok = False
                break
            x, y, w, h = sprites[fid]["rect"]
            key = (sid, x, tex_h - y - h)
            regions.setdefault(key, f"frame_{len(regions)}")
            frames.append((regions[key], max(1.0, round((end - t) * fps, 3))))
        if not ok:
            skipped.append(anim.stem)
            continue
        anims.append((name, loop and not anim.stem.startswith(ONE_SHOT), frames))

    lines = [f'[gd_resource type="SpriteFrames" load_steps={len(regions) + len(sheets) + 1} format=3]', ""]
    lines += [f'[ext_resource type="Texture2D" path="{path}" id="{sid}"]' for sid, path, _, _ in sheets.values()] + [""]
    for (sid, x, y), rid in regions.items():
        lines += [f'[sub_resource type="AtlasTexture" id="{rid}"]', f'atlas = ExtResource("{sid}")',
                  f"region = Rect2({x}, {y}, 48, 48)", ""]
    lines.append("[resource]")
    entries = []
    for name, loop, frames in sorted(anims):
        frame_str = ", ".join('{"duration": %s, "texture": SubResource("%s")}' % (fmt(d), rid) for rid, d in frames)
        entries.append('{"frames": [%s], "loop": %s, "name": &"%s", "speed": %s}'
                       % (frame_str, "true" if loop else "false", name, fmt(fps)))
    lines.append("animations = [" + ", ".join(entries) + "]")
    lines.append("metadata/flip_h = PackedStringArray(" + ", ".join(f'"{n}"' for n in sorted(flipped)) + ")")
    out = GODOT / "resources" / "characters" / "player_frames.tres"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    return [a[0] for a in anims], skipped


# --------------------------------------------------------------------------- Interface et coffre
def convert_ui(guids):
    """Sprites d'UI en AtlasTexture (.tres), police, et marges 9-slice dans ui_sprites.json."""
    _, sprites = read_texture_meta(UI_SHEET)
    by_name = {sp["name"]: sp for sp in sprites.values()}
    meta = (UI_SHEET.parent / (UI_SHEET.name + ".meta")).read_text(encoding="utf-8", errors="ignore")
    tex_h = png_size(UI_SHEET)[1]

    sheet_dest = GODOT / "assets" / "ui" / "ui_sheet.png"
    sheet_dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(UI_SHEET, sheet_dest)
    shutil.copyfile(UI_FONT, GODOT / "assets" / "ui" / "pixel_font.ttf")

    out = GODOT / "resources" / "ui"
    out.mkdir(parents=True, exist_ok=True)
    margins = {}
    for name, unity_name in UI_SPRITES.items():
        x, y, w, h = by_name[unity_name]["rect"]
        (out / f"{name}.tres").write_text("\n".join([
            '[gd_resource type="AtlasTexture" load_steps=2 format=3]', "",
            '[ext_resource type="Texture2D" path="res://assets/ui/ui_sheet.png" id="sheet"]', "",
            "[resource]", 'atlas = ExtResource("sheet")', f"region = Rect2({x}, {tex_h - y - h}, {w}, {h})", ""]),
            encoding="utf-8")
        # Bordure 9-slice Unity : x gauche, y bas, z droite, w haut
        m = re.search(rf"name: {re.escape(unity_name)}\s*\n(?:.*\n)*?\s*border: \{{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}}", meta)
        if m and any(int(v) for v in m.groups()):
            left, bottom, right, top = (int(v) for v in m.groups())
            margins[name] = {"left": left, "top": top, "right": right, "bottom": bottom}
    (out / "ui_sprites.json").write_text(json.dumps(margins, indent=1), encoding="utf-8")

    for name, (png, unity_name) in EXTRA_UI_SPRITES.items():
        sprite = next(sp for sp in read_texture_meta(png)[1].values() if sp["name"] == unity_name)
        dest = GODOT / "assets" / "ui" / png.name.replace(" ", "_")
        shutil.copyfile(png, dest)
        x, y, w, h = sprite["rect"]
        (out / f"{name}.tres").write_text("\n".join([
            '[gd_resource type="AtlasTexture" load_steps=2 format=3]', "",
            f'[ext_resource type="Texture2D" path="res://{dest.relative_to(GODOT).as_posix()}" id="sheet"]', "",
            "[resource]", 'atlas = ExtResource("sheet")', f"region = Rect2({x}, {png_size(png)[1] - y - h}, {w}, {h})", ""]),
            encoding="utf-8")
    return list(UI_SPRITES) + list(EXTRA_UI_SPRITES)


def convert_pivot_frames(guids, clips, out_path):
    """
    SpriteFrames dont toutes les images gardent le pivot Unity au même endroit.
    Les sprites Unity sont rognés (tailles différentes) avec un pivot propre à chacun (en bas au centre pour l'arbre) ;
    une AnimatedSprite2D, elle, centre chaque image. On entoure donc chaque région d'une marge transparente
    (AtlasTexture.margin) : toutes les images ont la même taille et le pivot tombe au CENTRE, donc à l'origine du nœud.
    """
    frames_by_anim, metas = {}, {}
    for name, (clip, loop) in clips.items():
        keys, stop, _ = read_clip(clip)
        frames = []
        for i, (t, guid, fid) in enumerate(keys):
            png = guids[guid]
            if png not in metas:
                metas[png] = read_texture_meta(png)[1]
            sp = metas[png][fid]
            x, y, w, h = sp["rect"]
            px, py = sp["pivot"]
            frames.append({"png": png, "region": (x, png_size(png)[1] - y - h, w, h),
                           "pivot": (round(w * px), round(h * (1 - py))),  # depuis le haut à gauche, en pixels
                           "duration": max(1.0, round(((keys[i + 1][0] if i + 1 < len(keys) else stop) - t) * 60, 3))})
        frames_by_anim[name] = (loop, frames)

    every = [f for _, fr in frames_by_anim.values() for f in fr]
    half_w = max(max(f["pivot"][0], f["region"][2] - f["pivot"][0]) for f in every)
    half_h = max(max(f["pivot"][1], f["region"][3] - f["pivot"][1]) for f in every)

    textures, lines_sub, entries = {}, [], []
    for name, (loop, frames) in frames_by_anim.items():
        parts = []
        for f in frames:
            png = f["png"]
            if png not in textures:
                dest = GODOT / "assets" / "sprites" / "Objects" / png.name.replace(" ", "_")
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(png, dest)
                textures[png] = (f"tex_{len(textures)}", "res://" + dest.relative_to(GODOT).as_posix())
            sid = f"frame_{len(lines_sub)}"
            x, y, w, h = f["region"]
            left, top = half_w - f["pivot"][0], half_h - f["pivot"][1]
            lines_sub.append("\n".join([f'[sub_resource type="AtlasTexture" id="{sid}"]',
                                        f'atlas = ExtResource("{textures[png][0]}")',
                                        f"region = Rect2({x}, {y}, {w}, {h})",
                                        f"margin = Rect2({left}, {top}, {2 * half_w - w}, {2 * half_h - h})", ""]))
            parts.append('{"duration": %s, "texture": SubResource("%s")}' % (fmt(f["duration"]), sid))
        entries.append('{"frames": [%s], "loop": %s, "name": &"%s", "speed": 60.0}'
                       % (", ".join(parts), "true" if loop else "false", name))

    lines = [f'[gd_resource type="SpriteFrames" load_steps={len(textures) + len(lines_sub) + 1} format=3]', ""]
    lines += [f'[ext_resource type="Texture2D" path="{p}" id="{i}"]' for i, p in textures.values()] + [""]
    lines += lines_sub + ["[resource]", "animations = [" + ", ".join(entries) + "]"]
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


# --------------------------------------------------------------------------- Monde procédural
def convert_world(guids):
    """
    - resources/world/world_gen_config.tres : SO_WorldGenConfig (tous des nombres, recopiés tels quels) ;
    - resources/world/world_items.json : catalogue du décor (CommonItem, BiomeCommonItem, BiomeInteractiveItem).
    Chaque objet de décor : empreinte, images (texture + région + décalage du pivot) et vitesse d'animation.
    """
    out = GODOT / "resources" / "world"
    out.mkdir(parents=True, exist_ok=True)

    # Config : chaque champ « Nom: valeur » du ScriptableObject devient une propriété du même nom.
    text = WORLD_CONFIG.read_text(encoding="utf-8", errors="ignore")
    body = text[text.index("m_EditorClassIdentifier"):].splitlines()[1:]
    props = []
    for line in body:
        m = re.match(r"^  (\w+): (-?[\d.]+)$", line)
        if m:
            value = m.group(2)
            props.append((m.group(1), "true" if (m.group(1) == "UseRandomSeed" and value == "1") else
                          "false" if m.group(1) == "UseRandomSeed" else value))
    Tres("res://scripts/World/WorldGenConfig.cs").write(out / "world_gen_config.tres", props, "WorldGenConfig")

    resolver = SpriteResolver(guids)

    def read(asset):
        return asset.read_text(encoding="utf-8", errors="ignore")

    def tile_frames(tile_asset):
        """Images d'une tuile Unity : AnimatedTile (m_AnimatedSprites), Tile (m_Sprite) ou RuleTile (défaut)."""
        t = read(tile_asset)
        refs = re.findall(r"- \{fileID: (-?\d+), guid: ([0-9a-f]+), type: 3\}",
                          t.split("m_AnimatedSprites:")[1].split("m_MinSpeed")[0]) if "m_AnimatedSprites:" in t else []
        if not refs:
            m = re.search(r"m_(?:Default)?Sprite: \{fileID: (-?\d+), guid: ([0-9a-f]+)", t)
            refs = [m.groups()] if m else []
        frames = []
        for fid, guid in refs:
            resolved = resolver.resolve(guid, int(fid))
            if not resolved:
                continue
            png = guids[guid]
            sp = resolver.metas[png][1][int(fid)]
            px, py = sp["pivot"]
            w, h = sp["rect"][2], sp["rect"][3]
            ppu = re.search(r"spritePixelsToUnits: ([\d.]+)",
                            (png.parent / (png.name + ".meta")).read_text(encoding="utf-8", errors="ignore"))
            frames.append({"texture": resolved[0], "region": list(resolved[1]),
                           # Unity affiche le sprite à 1 unité = PPU pixels ; une case fait 16 px dans Godot.
                           "scale": round(TILE / float(ppu.group(1)), 4) if ppu else 1.0,
                           # décalage du centre de l'image par rapport au pivot (Godot : y vers le bas)
                           "offset": [round((0.5 - px) * w, 2), round((py - 0.5) * h, 2)]})
        speed = re.search(r"m_MinSpeed: ([\d.]+)", t)
        return frames, float(speed.group(1)) if speed else 1.0

    def common_item(asset):
        t = read(asset)
        fp = re.search(r"FootPrint: \{x: (\d+), y: (\d+)\}", t)
        tile = ref(yaml_field(t, "ItemTile"))
        frames, speed = tile_frames(guids[tile[0]]) if tile and tile[0] in guids else ([], 1.0)
        return {"name": asset.stem, "footprint": [int(fp.group(1)), int(fp.group(2))] if fp else [1, 1],
                "frames": frames, "speed": speed}

    catalog = {"basic": [common_item(a) for a in WORLD_ITEMS["basic"]], "biomes": {}, "interactive": {}}
    for asset in WORLD_ITEMS["biomes"]:
        t = read(asset)
        biome = BIOME_NAMES[int(yaml_field(t, "Biome"))]
        items = [guids[r[0]] for r in yaml_list(t, "ItemTiles") if r and r[0] in guids]
        catalog["biomes"][biome] = [common_item(a) for a in items]
    for asset in WORLD_ITEMS["interactive"]:
        t = read(asset)
        biome = BIOME_NAMES[int(yaml_field(t, "Biome"))]
        entries = []
        for r in yaml_list(t, "Items"):
            it = read(guids[r[0]])
            prefab = guids.get(ref(yaml_field(it, "Prefab"))[0])
            fp = re.search(r"FootPrint: \{x: (\d+), y: (\d+)\}", it)
            if prefab and prefab.name in PREFAB_SCENES:
                entries.append({"scene": PREFAB_SCENES[prefab.name],
                                "footprint": [int(fp.group(1)), int(fp.group(2))] if fp else [1, 1]})
        catalog["interactive"][biome] = entries
    (out / "world_items.json").write_text(json.dumps(catalog, indent=1), encoding="utf-8")
    return sum(len(v) for v in catalog["biomes"].values()) + len(catalog["basic"])


def convert_portrait(guids):
    """
    Les 37 clips du portrait (UserCharacter.controller) -> un SpriteFrames, une animation par clip
    (nom = nom du clip en minuscules : « angry_start », « verysleep_loop »...). Cases de 32x32 d'une même feuille.
    Les transitions de l'Animator, elles, sont écrites à la main dans PortraitStateMachine.cs.
    """
    regions, anims, sheet = {}, [], None
    for clip in sorted(PORTRAIT_CLIPS.rglob("*.anim")):
        keys, stop, loop = read_clip(clip)
        frames = []
        for i, (t, guid, fid) in enumerate(keys):
            end = keys[i + 1][0] if i + 1 < len(keys) else stop
            if end <= t and i + 1 < len(keys):
                continue  # deux clés au même instant : la seconde l'emporte
            png = guids[guid]
            sheet = sheet or png
            x, y, w, h = read_texture_meta(png)[1][fid]["rect"]
            key = (x, png_size(png)[1] - y - h, w, h)
            regions.setdefault(key, f"frame_{len(regions)}")
            frames.append((regions[key], max(1.0, round((end - t) * 60, 3))))
        anims.append((clip.stem.lower(), loop, frames))

    dest = GODOT / "assets" / "ui" / "portrait_sheet.png"
    shutil.copyfile(sheet, dest)
    lines = [f'[gd_resource type="SpriteFrames" load_steps={len(regions) + 2} format=3]', "",
             '[ext_resource type="Texture2D" path="res://assets/ui/portrait_sheet.png" id="sheet"]', ""]
    for (x, y, w, h), rid in regions.items():
        lines += [f'[sub_resource type="AtlasTexture" id="{rid}"]', 'atlas = ExtResource("sheet")',
                  f"region = Rect2({x}, {y}, {w}, {h})", ""]
    lines.append("[resource]")
    entries = ['{"frames": [%s], "loop": %s, "name": &"%s", "speed": 60.0}'
               % (", ".join('{"duration": %s, "texture": SubResource("%s")}' % (fmt(d), r) for r, d in frames),
                  "true" if loop else "false", name) for name, loop, frames in sorted(anims)]
    lines.append("animations = [" + ", ".join(entries) + "]")
    (GODOT / "resources" / "ui" / "portrait_frames.tres").write_text("\n".join(lines) + "\n", encoding="utf-8")
    return len(anims)


def convert_chest(guids):
    """SpriteFrames du coffre : cases entières de 48x48 (les sprites Unity sont rognés, la case évite les sauts)."""
    regions, anims = {}, []
    sheet = None
    for name, clip in CHEST_CLIPS.items():
        keys, stop, _ = read_clip(clip)
        frames = []
        for i, (t, guid, fid) in enumerate(keys):
            png = guids[guid]
            sheet = png
            x, y, w, h = read_texture_meta(png)[1][fid]["rect"]
            tex_h = png_size(png)[1]
            cell = (x // CHEST_CELL * CHEST_CELL, (tex_h - y - h) // CHEST_CELL * CHEST_CELL)
            regions.setdefault(cell, f"frame_{len(regions)}")
            end = keys[i + 1][0] if i + 1 < len(keys) else stop
            frames.append((regions[cell], max(1.0, round((end - t) * 60, 3))))
        anims.append((name, frames))

    dest = GODOT / "assets" / "sprites" / "Objects" / "chest.png"
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(sheet, dest)
    lines = [f'[gd_resource type="SpriteFrames" load_steps={len(regions) + 2} format=3]', "",
             '[ext_resource type="Texture2D" path="res://assets/sprites/Objects/chest.png" id="sheet"]', ""]
    for (x, y), rid in regions.items():
        lines += [f'[sub_resource type="AtlasTexture" id="{rid}"]', 'atlas = ExtResource("sheet")',
                  f"region = Rect2({x}, {y}, {CHEST_CELL}, {CHEST_CELL})", ""]
    lines.append("[resource]")
    entries = ['{"frames": [%s], "loop": false, "name": &"%s", "speed": 60.0}'
               % (", ".join('{"duration": %s, "texture": SubResource("%s")}' % (fmt(d), r) for r, d in frames), name)
               for name, frames in anims]
    lines.append("animations = [" + ", ".join(entries) + "]")
    out = GODOT / "resources" / "objects" / "chest_frames.tres"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")


if __name__ == "__main__":
    tile_warnings = convert_tiles()
    guids = guid_index()
    anims, skipped = convert_character(guids)
    item_warnings, item_count = convert_items(guids)
    ui_sprites = convert_ui(guids)
    convert_chest(guids)
    print(f"Portrait : {convert_portrait(guids)} animations dans portrait_frames.tres")
    convert_pivot_frames(guids, TREE_CLIPS, GODOT / "resources" / "objects" / "tree_frames.tres")
    print(f"Monde : world_gen_config.tres + {convert_world(guids)} objets de décor dans world_items.json")
    print(f"UI : {', '.join(ui_sprites)} + police ; coffre : chest_frames.tres")
    print(f"TileSet + rule_tiles.json écrits ; {item_count} objets convertis")
    print(f"Animations ({len(anims)}) : {', '.join(sorted(anims))}")
    if skipped:
        print(f"Clips ignorés (autre feuille) : {', '.join(skipped)}")
    for w in tile_warnings + item_warnings:
        print("ATTENTION :", w, file=sys.stderr)
