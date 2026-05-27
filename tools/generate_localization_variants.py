from __future__ import annotations

import json
import re
import subprocess
import time
from pathlib import Path

from deep_translator import GoogleTranslator


ROOT = Path(__file__).resolve().parents[1]
LOC_DIR = ROOT / "ReAstralPartyMod" / "localization"
ZHS_DIR = LOC_DIR / "zhs"
ENG_DIR = LOC_DIR / "eng"
JPN_DIR = LOC_DIR / "jpn"

MASK_PATTERN = re.compile(r"\[[^\]]+\]|\{[^{}]+\}")
SPACE_BEFORE_PUNCT = re.compile(r"\s+([,.;:!?])")
MULTISPACE = re.compile(r"[ \t]{2,}")

EN_COMPACT_REPLACEMENTS = [
    ("Show 3 random other event cards. Choose 1 to play immediately.", "Show 3 random event cards. Choose 1 to play."),
    ("Show 3 random anomaly event cards. Choose 1 to play immediately.", "Show 3 random anomaly events. Choose 1 to play."),
    ("All players gain [blue]2[/blue] [power]Half Life Heal[/power], and their next Attack costs [blue]0[/blue].", "All players gain [blue]2[/blue] [power]Half Life Heal[/power]. Their next Attack costs [blue]0[/blue]."),
    ("All units gain [blue]1[/blue] [weak]Weak[/weak]. All players gain [blue]3[/blue] [gold]StarLight[/gold].", "All units gain [blue]1[/blue] [weak]Weak[/weak]. All players gain [blue]3[/blue] [gold]StarLight[/gold]."),
    ("Gain [blue]1[/blue] [weak]Weak[/weak]. All players gain {energyPrefix:energyIcons(1)}.", "Gain [blue]1[/blue] [weak]Weak[/weak]. All players gain {energyPrefix:energyIcons(1)}."),
    ("Every other player loses [blue]3[/blue] Gold. You gain [gold]StarLight[/gold] equal to [blue]5[/blue] times the number of other players.", "Every other player loses [blue]3[/blue] Gold. Gain [gold]StarLight[/gold] equal to [blue]5[/blue] times the number of other players."),
    ("All players gain [blue]3[/blue] temporary [red]Strength[/red].", "All players gain [blue]3[/blue] temporary [red]Strength[/red]."),
    ("This turn, all players temporarily lose [blue]1[/blue] [red]Strength[/red] and [green]Dexterity[/green]. All enemies lose [blue]2[/blue] [red]Strength[/red].", "This turn, all players lose [blue]1[/blue] temporary [red]Strength[/red] and [green]Dexterity[/green]. All enemies lose [blue]2[/blue] [red]Strength[/red]."),
    ("All units take [blue]2[/blue] damage. All players gain [blue]1[/blue] [card]Giant Rock[/card] next turn.", "All units take [blue]2[/blue] damage. All players gain [blue]1[/blue] [card]Giant Rock[/card] next turn."),
    ("All enemies gain [blue]3[/blue] [power]Mark[/power], [blue]2[/blue] [vulnerable]Vulnerable[/vulnerable], and [blue]2[/blue] [weak]Weak[/weak]. All players gain [blue]5[/blue] [gold]StarLight[/gold].", "All enemies gain [blue]3[/blue] [power]Mark[/power], [blue]2[/blue] [vulnerable]Vulnerable[/vulnerable], and [blue]2[/blue] [weak]Weak[/weak]. All players gain [blue]5[/blue] [gold]StarLight[/gold]."),
    ("Choose [blue]1[/blue] normal event card other than [card]Deus Ex Machina[/card] and play it once. All allied persona relics advance their cooldown by [blue]1[/blue] turn.", "Choose [blue]1[/blue] normal event card other than [card]Deus Ex Machina[/card] and play it once. All allied persona relics reduce cooldown by [blue]1[/blue] turn."),
    ("Give each ally a [card]Giant Rock[/card] card ([exhaust]Exhaust[/exhaust]+[ethereal]Ethereal[/ethereal]).", "Give each ally [blue]1[/blue] [card]Giant Rock[/card]."),
    ("Draw {Cards:diff()} card(s) for all allies and gain {StarLight:diff()} [gold]StarLight[/gold].", "All allies draw {Cards:diff()} card(s). Gain {StarLight:diff()} [gold]StarLight[/gold]."),
    ("Only you make a judgment. On 1-3, you take {SelfDamage:diff()} damage and teammates take {TeamDamage:diff()} damage; on 4-6, you gain {SelfStarLight:diff()} [gold]StarLight[/gold] and teammates gain {TeamStarLight:diff()} [gold]StarLight[/gold].", "Only you judge. On 1-3, you take {SelfDamage:diff()} damage and teammates take {TeamDamage:diff()} damage. On 4-6, you gain {SelfStarLight:diff()} [gold]StarLight[/gold] and teammates gain {TeamStarLight:diff()} [gold]StarLight[/gold]."),
    ("At the end of this combat, gain Gold equal to this power's amount.", "After this combat, gain Gold equal to this power's amount."),
    ("At the start of your turn, heal for the amount shown. The healing is halved each turn.", "At turn start, heal this amount. Halve it each turn."),
    ("Your next Attack costs [blue]0[/blue]. Remove this after it is played.", "Your next Attack costs [blue]0[/blue]. Remove after use."),
    ("At the start of your next turn, gain [blue]1[/blue] [card]Giant Rock[/card].", "At your next turn start, gain [blue]1[/blue] [card]Giant Rock[/card]."),
    ("This power can stack, but regardless of its amount, all hits only deal [blue]1[/blue] extra damage, including hits fully absorbed by Block. Loses [blue]1[/blue] stack at the end of each enemy turn.", "Can stack, but hits deal only [blue]1[/blue] extra damage. Lose [blue]1[/blue] stack at the end of each enemy turn."),
    ("At the end of combat, [relic]Dragon Gate[/relic] reduces its Thunder counter by this amount.", "After combat, [relic]Dragon Gate[/relic] reduces its Thunder counter by this amount."),
    ("Until the enemy turn starts, ignore damage from enemy sources.", "Until the enemy turn starts, ignore enemy-source damage."),
    ("Until your next turn starts, ignore damage from enemy sources.\nDuring that time, the first time you deal damage to an enemy, gain [power]Ambush[/power] equal to that enemy's [power]Mark[/power], then remove this power.", "Until your next turn starts, ignore enemy-source damage.\nThe first time you damage an enemy, gain [power]Ambush[/power] equal to its [power]Mark[/power], then remove this power."),
]

JP_TERM_MAP = {
    "[power]再生[/power]": "[power]再生[/power]",
    "[power]治愈[/power]": "[power]治愈[/power]",
    "[power]标记[/power]": "[power]标记[/power]",
    "[power]突袭[/power]": "[power]突袭[/power]",
    "[power]缓冲[/power]": "[power]缓冲[/power]",
    "[red]力量[/red]": "[red]筋力[/red]",
    "[green]敏捷[/green]": "[green]敏捷[/green]",
    "[gold]星光[/gold]": "[gold]星光[/gold]",
    "[gold]活力[/gold]": "[gold]活力[/gold]",
    "[gold]中毒[/gold]": "[gold]中毒[/gold]",
    "[gold]灾厄[/gold]": "[gold]灾厄[/gold]",
    "[gold]缓冲[/gold]": "[gold]缓冲[/gold]",
    "[gold]易伤[/gold]": "[gold]易伤[/gold]",
    "[gold]虚弱[/gold]": "[gold]虚弱[/gold]",
    "[buffer]缓冲[/buffer]": "[buffer]缓冲[/buffer]",
    "[weak]虚弱[/weak]": "[weak]虚弱[/weak]",
    "[vulnerable]易伤[/vulnerable]": "[vulnerable]易伤[/vulnerable]",
    "[poison]中毒[/poison]": "[poison]中毒[/poison]",
    "[card]天降神兵[/card]": "[card]天降神兵[/card]",
    "[card]巨石[/card]": "[card]巨石[/card]",
    "自己以外的玩家": "自分以外のプレイヤー",
    "所有友方角色": "すべての味方キャラクター",
    "所有友方": "すべての味方",
    "所有玩家": "すべてのプレイヤー",
    "所有敌人": "すべての敵",
    "所有单位": "すべてのユニット",
    "其他玩家数量": "他のプレイヤー数",
    "常规事件牌": "通常イベントカード",
    "异常事件牌": "異常イベントカード",
    "事件牌": "イベントカード",
    "异常事件": "異常イベント",
    "人格遗物": "人格遺物",
    "金币": "ゴールド",
    "冷却": "クールダウン",
}


def load_json(path: Path) -> dict[str, str]:
    with path.open("r", encoding="utf-8") as handle:
        return json.load(handle)


def write_json(path: Path, payload: dict[str, str]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="\n") as handle:
        json.dump(payload, handle, ensure_ascii=False, indent=2)
        handle.write("\n")


def load_head_json(rel_path: str) -> dict[str, str]:
    text = subprocess.run(
        ["git", "show", f"HEAD:{rel_path}"],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
    ).stdout
    return json.loads(text)


def compact_english(text: str) -> str:
    compacted = text
    for old, new in EN_COMPACT_REPLACEMENTS:
        compacted = compacted.replace(old, new)

    compacted = compacted.replace("At the start of your turn", "At turn start")
    compacted = compacted.replace("At the start of every [blue]", "Every [blue]")
    compacted = compacted.replace("At the start of your next turn", "At your next turn start")
    compacted = compacted.replace("At the end of this combat", "After this combat")
    compacted = compacted.replace("At the end of combat", "After combat")
    compacted = compacted.replace("At combat start", "At combat start")
    compacted = compacted.replace(" does not stack or expire on its own.", ".")
    compacted = compacted.replace("Maximum 3 stacks.", "Max 3 stacks.")
    compacted = compacted.replace("Does not stack.", "Does not stack.")
    compacted = compacted.replace("Refreshes to 2 turns when applied again.", "Reapplying refreshes it to 2 turns.")
    compacted = compacted.replace("including hits fully absorbed by Block", "including Blocked hits")
    compacted = MULTISPACE.sub(" ", compacted)
    compacted = SPACE_BEFORE_PUNCT.sub(r"\1", compacted)
    return compacted.strip()


def mask_terms(text: str, term_map: dict[str, str]) -> tuple[str, dict[str, str]]:
    mapping: dict[str, str] = {}
    counter = 0

    def add_token(value: str) -> str:
        nonlocal counter
        token = f"<T{counter}>"
        counter += 1
        mapping[token] = value
        return token

    masked = text
    for source, target in sorted(term_map.items(), key=lambda item: len(item[0]), reverse=True):
        if source in masked:
            masked = masked.replace(source, add_token(target))

    def replace_markup(match: re.Match[str]) -> str:
        return add_token(match.group(0))

    masked = MASK_PATTERN.sub(replace_markup, masked)
    return masked, mapping


def restore_terms(text: str, mapping: dict[str, str]) -> str:
    restored = text
    for token, value in mapping.items():
        restored = restored.replace(token, value)
    restored = restored.replace(" [/", "[/").replace("[ ", "[").replace(" ]", "]")
    restored = SPACE_BEFORE_PUNCT.sub(r"\1", restored)
    restored = MULTISPACE.sub(" ", restored)
    return restored.strip()


def translate_with_retry(translator: GoogleTranslator, text: str) -> str:
    for attempt in range(4):
        try:
            return translator.translate(text)
        except Exception:
            if attempt == 3:
                raise
            time.sleep(1 + attempt)
    raise RuntimeError("unreachable")


def translate_zh_to_ja(text: str, translator: GoogleTranslator) -> str:
    masked, mapping = mask_terms(text, JP_TERM_MAP)
    translated = translate_with_retry(translator, masked)
    return restore_terms(translated, mapping)


def build_english_files() -> None:
    for zhs_path in sorted(ZHS_DIR.glob("*.json")):
        rel = f"ReAstralPartyMod/localization/eng/{zhs_path.name}"
        head_eng = load_head_json(rel)
        updated = dict(head_eng)
        for key, value in head_eng.items():
            if any(suffix in key for suffix in (".description", ".smartDescription", ".flavor", ".select_prompt", ".neow_description")):
                updated[key] = compact_english(value)
        write_json(ENG_DIR / zhs_path.name, updated)

    modifiers_block = load_head_json("ReAstralPartyMod/localization/eng/modifiers.json.block")
    write_json(ENG_DIR / "modifiers.json.block", modifiers_block)


def build_japanese_files() -> None:
    translator = GoogleTranslator(source="zh-CN", target="ja")
    for zhs_path in sorted(ZHS_DIR.glob("*.json")):
        zhs = load_json(zhs_path)
        out_path = JPN_DIR / zhs_path.name
        output: dict[str, str] = load_json(out_path) if out_path.exists() else {}
        for key, value in zhs.items():
            if key in output and output[key]:
                continue
            output[key] = translate_zh_to_ja(value, translator)
            write_json(out_path, output)
        write_json(out_path, output)

    if (ENG_DIR / "modifiers.json.block").exists():
        eng_block = load_json(ENG_DIR / "modifiers.json.block")
        output = {}
        block_translator = GoogleTranslator(source="en", target="ja")
        for key, value in eng_block.items():
            output[key] = translate_with_retry(block_translator, value)
        write_json(JPN_DIR / "modifiers.json.block", output)


def main() -> None:
    build_english_files()
    build_japanese_files()


if __name__ == "__main__":
    main()
