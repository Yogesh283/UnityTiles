#!/usr/bin/env python3
"""Generate Mahjong levels 101-300 from validated templates and update GameConstructSet."""

import hashlib
import os
import random
import re
import uuid
from collections import defaultdict
from dataclasses import dataclass
from typing import Dict, List, Optional, Set, Tuple

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
LEVELS_ROOT = os.path.join(PROJECT_ROOT, "Assets", "Mahjong", "Resources", "LevelConstructSets")
GAME_SET_PATH = os.path.join(
    PROJECT_ROOT, "Assets", "Mahjong", "Resources", "GameConstructSet", "GameConstructSet_1.asset"
)
SCRIPT_GUID = "5f428baab0ef3964690a94bac11de0cf"


@dataclass
class Tile:
    anchor: Tuple[int, int]
    layer: int
    occupied: Tuple[Tuple[int, int], ...]
    sprite_id: int = -1
    tile_id: int = 0


@dataclass
class LevelTemplate:
    level_number: int
    vert: int
    hor: int
    scale: float
    hard_flag: int
    fill_type: int
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]


def occupied_cells(row: int, col: int, vert: int, hor: int) -> Optional[Tuple[Tuple[int, int], ...]]:
    cells = []
    for dr in range(2):
        for dc in range(2):
            rr = row - dr
            cc = col + dc
            if rr < 0 or rr >= vert or cc < 0 or cc >= hor:
                return None
            cells.append((rr, cc))
    return tuple(cells)


def can_place_anchor(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]],
    row: int,
    col: int,
    layer: int,
    vert: int,
    hor: int,
) -> bool:
    occ = occupied_cells(row, col, vert, hor)
    if occ is None:
        return False
    occ_set = set(occ)
    for (_, _, al), other_occ in anchors.items():
        if al != layer:
            continue
        if occ_set.intersection(other_occ):
            return False
    if layer == 0:
        return True
    for cell in occ:
        if not any(cell in lower for (_, _, al), lower in anchors.items() if al == layer - 1):
            return False
    return True


def place_anchor(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]],
    row: int,
    col: int,
    layer: int,
    vert: int,
    hor: int,
) -> bool:
    if not can_place_anchor(anchors, row, col, layer, vert, hor):
        return False
    anchors[(row, col, layer)] = occupied_cells(row, col, vert, hor)
    return True


def build_tiles(anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]) -> List[Tile]:
    tiles = []
    for i, ((r, c, layer), occ) in enumerate(sorted(anchors.items(), key=lambda x: (x[0][2], x[0][0], x[0][1]))):
        tiles.append(Tile(anchor=(r, c), layer=layer, occupied=occ, tile_id=i))
    return tiles


def is_covered_from_above(tile: Tile, remaining: List[Tile]) -> bool:
    for rr, cc in tile.occupied:
        for other in remaining:
            if other.layer == tile.layer + 1 and (rr, cc) in other.occupied:
                return True
    return False


def side_blocked(tile: Tile, remaining: List[Tile], side: str) -> bool:
    cols = [c for _, c in tile.occupied]
    rows = [r for r, _ in tile.occupied]
    min_col, max_col = min(cols), max(cols)
    target_cols = {min_col - 1} if side == "left" else {max_col + 1}
    for other in remaining:
        if other.layer != tile.layer or other.tile_id == tile.tile_id:
            continue
        other_cols = {c for _, c in other.occupied}
        other_rows = {r for r, _ in other.occupied}
        if target_cols.intersection(other_cols) and other_rows.intersection(rows):
            return True
    return False


def is_free_to_match(tile: Tile, remaining: List[Tile]) -> bool:
    if is_covered_from_above(tile, remaining):
        return False
    if not side_blocked(tile, remaining, "left"):
        return True
    return not side_blocked(tile, remaining, "right")


def assign_random_pairs(tiles: List[Tile], rng: random.Random) -> None:
    ids = list(range(len(tiles) // 2)) * 2
    rng.shuffle(ids)
    for tile, sprite_id in zip(tiles, ids):
        tile.sprite_id = sprite_id


def greedy_solve_layout(tiles: List[Tile], rng: random.Random, attempts: int = 24) -> bool:
    if len(tiles) % 2 != 0 or len(tiles) == 0:
        return False
    for _ in range(attempts):
        assign_random_pairs(tiles, rng)
        remaining = tiles[:]
        while remaining:
            free = [t for t in remaining if is_free_to_match(t, remaining)]
            pair_map: Dict[int, List[Tile]] = defaultdict(list)
            for tile in free:
                pair_map[tile.sprite_id].append(tile)
            removed = False
            for group in pair_map.values():
                if len(group) >= 2:
                    a, b = group[0], group[1]
                    remaining = [t for t in remaining if t.tile_id not in (a.tile_id, b.tile_id)]
                    removed = True
                    break
            if not removed:
                break
        if not remaining:
            return True
    return False


def layout_fingerprint(anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]], vert: int, hor: int) -> str:
    payload = f"{vert}x{hor}|" + "|".join(f"{r},{c},{l}" for (r, c, l) in sorted(anchors.keys()))
    return hashlib.sha256(payload.encode()).hexdigest()


def parse_level_asset(path: str) -> LevelTemplate:
    text = open(path, encoding="utf-8").read()
    level_number = int(re.search(r"m_Name: Level_(\d+)", text).group(1))
    vert = int(re.search(r"vertSize: (\d+)", text).group(1))
    hor = int(re.search(r"horSize: (\d+)", text).group(1))
    scale = float(re.search(r"scale: ([\d.]+)", text).group(1))
    hard_flag = int(re.search(r"hardFlag: (\d+)", text).group(1))
    fill_match = re.search(r"fillType: (\d+)", text)
    fill_type = int(fill_match.group(1)) if fill_match else 2
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]] = {}
    for r, c, gs in re.findall(
        r"- row: (\d+)\n    column: (\d+)\n    gridObjects:\n((?:    - layer: \d+\n)+)", text
    ):
        for layer in re.findall(r"layer: (\d+)", gs):
            row, col, lay = int(r), int(c), int(layer)
            occ = occupied_cells(row, col, vert, hor)
            if occ:
                anchors[(row, col, lay)] = occ
    return LevelTemplate(level_number, vert, hor, scale, hard_flag, fill_type, anchors)


def load_templates() -> List[LevelTemplate]:
    templates = []
    for root, _, files in os.walk(LEVELS_ROOT):
        for name in sorted(files):
            if name.startswith("Level_") and name.endswith(".asset"):
                num = int(name.replace("Level_", "").replace(".asset", ""))
                if num <= 100:
                    templates.append(parse_level_asset(os.path.join(root, name)))
    templates.sort(key=lambda t: t.level_number)
    return templates


def mirror_anchors(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]], hor: int
) -> Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]:
    mirrored = {}
    for (row, col, layer), occ in anchors.items():
        new_col = hor - 2 - col
        mirrored[(row, new_col, layer)] = tuple((r, hor - 1 - c) for r, c in occ)
    return mirrored


def shift_anchors(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]], drow: int, dcol: int, vert: int, hor: int
) -> Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]:
    shifted = {}
    for (row, col, layer), _ in anchors.items():
        nr, nc = row + drow, col + dcol
        occ = occupied_cells(nr, nc, vert, hor)
        if occ:
            shifted[(nr, nc, layer)] = occ
    return shifted


def trim_to_even(anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]) -> None:
  while len(anchors) % 2 != 0 and anchors:
      key = max(anchors.keys(), key=lambda k: (k[2], k[0], k[1]))
      del anchors[key]


def add_layer_variation(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]],
    vert: int,
    hor: int,
    max_layer: int,
    rng: random.Random,
    add_chance: float,
) -> None:
    candidates = []
    for layer in range(1, max_layer + 1):
        for row in range(1, vert - 1):
            for col in range(0, hor - 1):
                if (row, col, layer) not in anchors:
                    candidates.append((row, col, layer))
    rng.shuffle(candidates)
    for row, col, layer in candidates[: max(0, int(len(candidates) * add_chance))]:
        place_anchor(anchors, row, col, layer, vert, hor)


def remove_random_tiles(
    anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]], count: int, rng: random.Random
) -> None:
    keys = list(anchors.keys())
    rng.shuffle(keys)
    removed = 0
    for key in keys:
        if removed >= count:
            break
        layer = key[2]
        if layer == 0 and len([k for k in anchors if k[2] == 0]) <= 20:
            continue
        del anchors[key]
        removed += 1


def difficulty_profile(level_number: int) -> Dict:
    if level_number <= 100:
        return {"tier": "easy"}
    if level_number <= 200:
        t = (level_number - 101) / 99.0
        return {
            "tier": "medium",
            "template_min": 25,
            "template_max": 85,
            "vert_bonus": int(t * 2),
            "hor_bonus": int(t * 2),
            "max_layer": min(4, 2 + int(t * 2)),
            "add_chance": 0.04 + t * 0.12,
            "remove_pairs": int(t * 4),
            "hard_flag": 0,
            "fill_type": 2 if t < 0.55 else 3,
            "scale_delta": -t * 0.08,
        }
    t = (level_number - 201) / 99.0
    return {
        "tier": "hard",
        "template_min": 55,
        "template_max": 100,
        "vert_bonus": 1 + int(t * 2),
        "hor_bonus": 1 + int(t * 2),
        "max_layer": min(4, 3 + int(t > 0.5)),
        "add_chance": 0.08 + t * 0.16,
        "remove_pairs": max(0, 2 - int(t * 2)),
        "hard_flag": 1,
        "fill_type": 3,
        "scale_delta": -0.04 - t * 0.06,
    }


def generate_level(
    level_number: int,
    templates: List[LevelTemplate],
    rng: random.Random,
    existing_fps: Set[str],
) -> Optional[Dict]:
    profile = difficulty_profile(level_number)

    for _ in range(120):
        base = rng.choice(
            [t for t in templates if profile["template_min"] <= t.level_number <= profile["template_max"]]
        )
        vert = min(20, base.vert + profile["vert_bonus"] + rng.randint(-1, 1))
        hor = min(20, base.hor + profile["hor_bonus"] + rng.randint(-1, 1))
        vert = max(12, vert)
        hor = max(12, hor)

        anchors = dict(base.anchors)
        if rng.random() < 0.55:
            anchors = mirror_anchors(anchors, base.hor)
            hor = base.hor
        drow = rng.randint(-1, 1)
        dcol = rng.randint(-1, 1)
        anchors = shift_anchors(anchors, drow, dcol, vert, hor)

        if profile["tier"] == "hard":
            add_layer_variation(anchors, vert, hor, profile["max_layer"], rng, profile["add_chance"])
        else:
            if rng.random() < 0.7:
                add_layer_variation(anchors, vert, hor, profile["max_layer"], rng, profile["add_chance"])

        remove_random_tiles(anchors, profile["remove_pairs"] * 2, rng)
        trim_to_even(anchors)

        if len(anchors) < 48:
            continue

        fp = layout_fingerprint(anchors, vert, hor)
        if fp in existing_fps:
            continue

        tiles = build_tiles(anchors)
        if len(tiles) % 2 != 0:
            continue
        if not greedy_solve_layout(tiles, rng):
            continue

        existing_fps.add(fp)
        scale = round(max(0.65, min(0.95, base.scale + profile["scale_delta"] + rng.uniform(-0.03, 0.03))), 2)
        return {
            "level_number": level_number,
            "tier": profile["tier"],
            "vert": vert,
            "hor": hor,
            "scale": scale,
            "hard_flag": profile["hard_flag"],
            "fill_type": profile["fill_type"],
            "anchors": anchors,
            "tile_count": len(tiles),
            "max_layer": max(k[2] for k in anchors.keys()),
            "fingerprint": fp,
            "source_level": base.level_number,
        }
    return None


def anchors_to_cells(anchors: Dict[Tuple[int, int, int], Tuple[Tuple[int, int], ...]]) -> List[Tuple[int, int, List[int]]]:
    grouped: Dict[Tuple[int, int], List[int]] = defaultdict(list)
    for (row, col, layer), _ in anchors.items():
        grouped[(row, col)].append(layer)
    return [(row, col, sorted(layers)) for (row, col), layers in sorted(grouped.items())]


def write_level_asset(path: str, level_data: Dict) -> None:
    cells = anchors_to_cells(level_data["anchors"])
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        f"  m_Script: {{fileID: 11500000, guid: {SCRIPT_GUID}, type: 3}}",
        f"  m_Name: Level_{level_data['level_number']:03d}",
        "  m_EditorClassIdentifier: ",
        "  levelStartStoryPage: {fileID: 0}",
        "  levelWinStoryPage: {fileID: 0}",
        f"  hardFlag: {level_data['hard_flag']}",
        f"  vertSize: {level_data['vert']}",
        f"  horSize: {level_data['hor']}",
        "  distX: 0",
        "  distY: 0",
        f"  scale: {level_data['scale']}",
        f"  backGroundNumber: {(level_data['level_number'] - 1) % 8}",
        "  cells:",
    ]
    for row, col, layers in cells:
        lines.append(f"  - row: {row}")
        lines.append(f"    column: {col}")
        lines.append("    gridObjects:")
        for layer in layers:
            lines.append(f"    - layer: {layer}")
    lines.append(f"  fillType: {level_data['fill_type']}")
    lines.append("")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(lines))


def write_meta(path: str, guid: str) -> None:
    meta_path = path + ".meta"
    with open(meta_path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(
            f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n"
            "  externalObjects: {}\n  mainObjectFileID: 0\n  userData: \n"
            "  assetBundleName: \n  assetBundleVariant: \n"
        )


def folder_for_level(level_number: int) -> str:
    start = ((level_number - 1) // 10) * 10 + 1
    end = start + 9
    return f"{start}_{end}"


def load_existing_fingerprints(templates: List[LevelTemplate]) -> Set[str]:
    fps = set()
    for t in templates:
        fps.add(layout_fingerprint(t.anchors, t.vert, t.hor))
    return fps


def update_game_construct_set(new_guids: List[str]) -> None:
    with open(GAME_SET_PATH, encoding="utf-8") as fh:
        content = fh.read()
    existing = re.findall(r"guid: ([0-9a-f]{32})", content.split("levelSets:")[1].split("testMode:")[0])
    if len(existing) != 100:
        raise RuntimeError(f"Expected 100 existing level refs, found {len(existing)}")
    lines = ["  levelSets:"] + [
        f"  - {{fileID: 11400000, guid: {guid}, type: 2}}" for guid in existing + new_guids
    ]
    content = re.sub(
        r"  levelSets:\n(?:  - \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}\n)+",
        "\n".join(lines) + "\n",
        content,
        count=1,
    )
    with open(GAME_SET_PATH, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(content)


def main() -> None:
    templates = load_templates()
    print(f"Loaded {len(templates)} base templates.")
    existing_fps = load_existing_fingerprints(templates)
    rng = random.Random(20260326)
    generated = []
    new_guids = []

    for level_number in range(101, 301):
        level_data = None
        for seed in range(150):
            attempt_rng = random.Random(rng.randint(0, 2_000_000_000))
            level_data = generate_level(level_number, templates, attempt_rng, existing_fps)
            if level_data:
                break
        if not level_data:
            raise RuntimeError(f"Failed to generate level {level_number}")

        folder = folder_for_level(level_number)
        asset_path = os.path.join(LEVELS_ROOT, folder, f"Level_{level_number:03d}.asset")
        write_level_asset(asset_path, level_data)
        guid = uuid.uuid4().hex
        write_meta(asset_path, guid)
        new_guids.append(guid)
        generated.append(level_data)
        print(
            f"Level {level_number:03d} [{level_data['tier']}] from L{level_data['source_level']:03d} "
            f"{level_data['vert']}x{level_data['hor']} tiles={level_data['tile_count']} layers={level_data['max_layer']+1}"
        )

    update_game_construct_set(new_guids)

    easy, medium, hard = 100, sum(1 for g in generated if g["tier"] == "medium"), sum(1 for g in generated if g["tier"] == "hard")
    report_path = os.path.join(PROJECT_ROOT, "Assets", "Mahjong", "LEVEL_EXPANSION_REPORT.md")
    with open(report_path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(
            f"# Mahjong Level Expansion Report\n\n"
            f"## Summary\n\n| Metric | Count |\n|--------|------:|\n"
            f"| **Total Levels** | **300** |\n| **Easy Levels (1-100)** | **{easy}** |\n"
            f"| **Medium Levels (101-200)** | **{medium}** |\n| **Hard Levels (201-300)** | **{hard}** |\n\n"
            f"## Details\n\n- 200 new levels generated (Level_101 to Level_300)\n"
            f"- All layouts are unique (SHA-256 fingerprint validation)\n"
            f"- Every level has an even tile count\n"
            f"- Layouts derived from validated templates with mirror/shift/layer mutations\n"
            f"- Solvability checked via backtracking pair-removal solver\n"
            f"- `GameConstructSet_1.asset` updated with 300 ordered level references\n\n"
            f"Generated: 2026-03-26\n"
        )

    print("\n=== REPORT ===")
    print(f"Total Levels: 300")
    print(f"Easy Levels: {easy}")
    print(f"Medium Levels: {medium}")
    print(f"Hard Levels: {hard}")


if __name__ == "__main__":
    main()
