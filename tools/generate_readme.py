import json
import re
from pathlib import Path
from xml.etree import ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
README_PATH = ROOT / "README.md"
MANIFEST_PATH = ROOT / "ReAstralPartyMod.json"
PROJECT_PATH = ROOT / "ReAstralPartyMod.csproj"
EXTRACT_PATH = ROOT / "tools" / "readme_extract.json"
ZHS_DIR = ROOT / "ReAstralPartyMod" / "localization" / "zhs"


CARD_TYPE_MAP = {
    "Attack": "攻击",
    "Skill": "技能",
    "Power": "能力",
}

CARD_RARITY_MAP = {
    "Basic": "基础",
    "Common": "普通",
    "Uncommon": "非普通",
    "Rare": "稀有",
    "Ancient": "古代",
}

TARGET_MAP = {
    "Self": "自己",
    "AnyEnemy": "任意敌人",
    "RandomEnemy": "随机敌人",
    "AllEnemies": "所有敌人",
    "AnyAlly": "任意友方",
    "AllAllies": "所有友方",
    "AnyPlayer": "任意玩家",
}

POWER_TYPE_MAP = {
    "Buff": "增益",
    "Debuff": "减益",
}

POWER_STACK_MAP = {
    "Counter": "计数",
    "Single": "单层",
    "None": "不叠层",
}

RELIC_RARITY_MAP = {
    "Starter": "起始",
    "Common": "普通",
    "Uncommon": "非普通",
    "Rare": "稀有",
    "Ancient": "古代",
    "?": "未标注",
}


def load_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def snake_upper(name: str) -> str:
    return re.sub(r"(?<!^)([A-Z])", r"_\1", name).upper()


def strip_tags(text: str) -> str:
    text = text.replace("\r\n", "\n")
    text = re.sub(r"\[[^\[\]]+\]", "", text)
    text = re.sub(r"[ \t]+\n", "\n", text)
    text = re.sub(r"\n{3,}", "\n\n", text)
    return text.strip()


def md_cell(text: str) -> str:
    text = strip_tags(text)
    text = text.replace("|", "\\|")
    text = text.replace("\n", "<br>")
    return text


def load_manifest() -> dict:
    return load_json(MANIFEST_PATH)


def format_dependency(dep: object) -> str:
    if isinstance(dep, str):
        return dep
    if isinstance(dep, dict):
        dep_id = dep.get("id", "")
        min_version = dep.get("min_version")
        if min_version:
            return f"{dep_id} (>= {min_version})"
        return str(dep_id)
    return str(dep)


def load_project_properties() -> dict:
    root = ET.fromstring(PROJECT_PATH.read_text(encoding="utf-8"))
    properties = {}
    for group in root.findall(".//PropertyGroup"):
        for child in group:
            if child.text and child.tag not in properties:
                properties[child.tag] = child.text.strip()
    return properties


def load_extract() -> dict:
    return load_json(EXTRACT_PATH)


def load_localizations() -> dict:
    return {
        "cards": load_json(ZHS_DIR / "cards.json"),
        "powers": load_json(ZHS_DIR / "powers.json"),
        "relics": load_json(ZHS_DIR / "relics.json"),
        "potions": load_json(ZHS_DIR / "potions.json"),
        "modifiers": load_json(ZHS_DIR / "modifiers.json"),
        "enchantments": load_json(ZHS_DIR / "enchantments.json"),
        "card_keywords": load_json(ZHS_DIR / "card_keywords.json"),
        "relic_collection": load_json(ZHS_DIR / "relic_collection.json"),
        "rest_site_ui": load_json(ZHS_DIR / "rest_site_ui.json"),
    }


def build_card_records(extract: dict, loc: dict) -> list[dict]:
    result = []
    code_classes = {path.stem for path in (ROOT / "ReAstralPartyCardCode" / "Cards").glob("*.cs")}
    for item in extract["cards"]:
        cls = item["class"]
        if cls not in code_classes:
            continue
        key_base = f"RE_ASTRAL_PARTY_MOD_CARD_{snake_upper(cls)}"
        title = loc.get(f"{key_base}.title")
        description = loc.get(f"{key_base}.description")
        if not title or not description:
            continue
        result.append(
            {
                "class": cls,
                "title": title,
                "cost": item.get("cost", ""),
                "type": CARD_TYPE_MAP.get(item.get("type"), item.get("type", "")),
                "rarity": CARD_RARITY_MAP.get(item.get("rarity"), item.get("rarity", "")),
                "target": TARGET_MAP.get(item.get("target"), item.get("target", "")),
                "description": description,
            }
        )
    return result


def build_power_records(extract: dict, loc: dict) -> list[dict]:
    result = []
    code_classes = {path.stem for path in (ROOT / "ReAstralPartyCardCode" / "Powers").glob("*.cs")}
    for item in extract["powers"]:
        cls = item["class"]
        if cls not in code_classes:
            continue
        key_base = f"RE_ASTRAL_PARTY_MOD_POWER_{snake_upper(cls)}"
        title = loc.get(f"{key_base}.title")
        description = loc.get(f"{key_base}.description")
        if not title or not description:
            continue
        result.append(
            {
                "class": cls,
                "title": title,
                "type": POWER_TYPE_MAP.get(item.get("type"), item.get("type", "")),
                "stack": POWER_STACK_MAP.get(item.get("stack"), item.get("stack", "")),
                "description": description,
            }
        )
    return result


def build_relic_records(extract: dict, loc: dict) -> list[dict]:
    result = []
    code_classes = {path.stem for path in (ROOT / "ReAstralPartyCardCode" / "Relics").glob("*.cs")}
    for item in extract["relics"]:
        cls = item["class"]
        if cls not in code_classes:
            continue
        key_base = f"RE_ASTRAL_PARTY_MOD_RELIC_{snake_upper(cls)}"
        title = loc.get(f"{key_base}.title")
        description = loc.get(f"{key_base}.description")
        if not title or not description:
            continue
        result.append(
            {
                "class": cls,
                "title": title,
                "rarity": RELIC_RARITY_MAP.get(item.get("rarity"), item.get("rarity", "")),
                "description": description,
            }
        )
    return result


def group_cards(cards: list[dict]) -> list[tuple[str, list[dict]]]:
    groups = [
        ("基础能力牌", [c for c in cards if c["class"].startswith("BaseAbility")]),
        ("收集器与特殊牌", [c for c in cards if c["class"].startswith("CollectorsCard") or c["class"] == "SkillTokenCuriousCandyMachine"]),
        ("事件卡", [c for c in cards if c["class"].startswith("Event") or c["class"].startswith("Events")]),
        ("技能牌", [c for c in cards if c["class"].startswith("Skill")]),
    ]
    return [(name, items) for name, items in groups if items]


def group_relics(relics: list[dict]) -> list[tuple[str, list[dict]]]:
    groups = [
        ("人格遗物", [r for r in relics if r["class"].startswith("Person") and not r["class"].startswith("PersonalityDerivative")]),
        ("人格衍生遗物", [r for r in relics if r["class"].startswith("PersonalityDerivative")]),
        ("蓝色筹码", [r for r in relics if r["class"].startswith("TokenBlue")]),
        ("紫色筹码", [r for r in relics if r["class"].startswith("TokenPurple")]),
        ("金色筹码", [r for r in relics if r["class"].startswith("TokenGold")]),
        ("专属筹码", [r for r in relics if r["class"].startswith("TokenExclusive")]),
        ("其他筹码", [r for r in relics if r["class"].startswith("Token") and not any(r["class"].startswith(prefix) for prefix in ("TokenBlue", "TokenPurple", "TokenGold", "TokenExclusive"))]),
    ]
    return [(name, items) for name, items in groups if items]


def render_card_tables(cards: list[dict]) -> list[str]:
    lines: list[str] = []
    for group_name, items in group_cards(cards):
        lines.append(f"### {group_name}")
        lines.append("")
        lines.append("| 类名 | 中文名 | 费用 | 类型 | 稀有度 | 目标 | 效果 |")
        lines.append("|------|------|------|------|------|------|------|")
        for item in items:
            lines.append(
                f"| `{item['class']}` | {md_cell(item['title'])} | `{item['cost']}` | `{item['type']}` | `{item['rarity']}` | `{item['target']}` | {md_cell(item['description'])} |"
            )
        lines.append("")
    return lines


def render_power_table(powers: list[dict]) -> list[str]:
    lines = [
        "| 类名 | 中文名 | 类型 | 叠层方式 | 效果 |",
        "|------|------|------|------|------|",
    ]
    for item in powers:
        lines.append(
            f"| `{item['class']}` | {md_cell(item['title'])} | `{item['type']}` | `{item['stack']}` | {md_cell(item['description'])} |"
        )
    return lines


def render_relic_tables(relics: list[dict]) -> list[str]:
    lines: list[str] = []
    for group_name, items in group_relics(relics):
        lines.append(f"### {group_name}")
        lines.append("")
        lines.append("| 类名 | 中文名 | 稀有度 | 效果 |")
        lines.append("|------|------|------|------|")
        for item in items:
            lines.append(
                f"| `{item['class']}` | {md_cell(item['title'])} | `{item['rarity']}` | {md_cell(item['description'])} |"
            )
        lines.append("")
    return lines


def build_other_content_summary(localizations: dict) -> list[str]:
    def count_titles(data: dict) -> int:
        return sum(1 for key in data if key.endswith(".title"))

    return [
        "| 内容类型 | 数量 | 说明 |",
        "|------|------|------|",
        f"| 药水 | {count_titles(localizations['potions'])} | 包含起始人格宝箱选择药水、糖果系列药水和随机筹码包。 |",
        f"| Modifier | {count_titles(localizations['modifiers'])} | 当前主要包含星引擎相关的自定义 modifier。 |",
        f"| 附魔 | {count_titles(localizations['enchantments'])} | 当前主要是人格技能牌使用的冷却附魔。 |",
        f"| 卡牌关键词 | {count_titles(localizations['card_keywords'])} | 统一通过 `AstralKeywords` 注册并走 owned keyword 本地化。 |",
        f"| 遗物图鉴分类文本 | {len(localizations['relic_collection'])} | 用于人格遗物与筹码遗物的图鉴分组和展示文本。 |",
        f"| 休息处文本 | {len(localizations['rest_site_ui'])} | 当前主要服务于【初始点】等自定义休息处选项。 |",
    ]


def build_project_tree() -> str:
    return """```text
ReAstralPartyMod/
├─ ReAstralPartyCardCode/
│  ├─ Cards/
│  │  ├─ AstralPartyCardModel.cs               # 本模组卡牌模板，统一资源路径、唯一约束和冷却附魔处理
│  │  ├─ BaseAbilityOrbitalRailgun.cs          # 基础能力牌
│  │  ├─ CollectorsCard*.cs                    # 收集器与特殊牌
│  │  ├─ Event*.cs / Events*.cs                # 事件卡与事件阶段卡
│  │  └─ Skill*.cs                             # 人格技能牌与战斗技能牌
│  ├─ Enchantments/
│  │  └─ AstralCooldownEnchantment.cs          # 人格技能牌冷却附魔
│  ├─ Events/                                  # 自定义事件与事件辅助逻辑
│  ├─ Keywords/
│  │  └─ AstralKeywords.cs                     # 关键词门面、owned keyword 注册入口
│  ├─ Modifiers/                               # 自定义 modifier
│  ├─ Patches/
│  │  ├─ GameplayPatchRegistry.cs              # RitsuLib patcher 统一注册入口
│  │  ├─ StartingPersonaRelicSelectionPatch.cs # 起始人格遗物选择同步与流程补丁
│  │  ├─ StarEngine*.cs                        # 星引擎奖励、事件与战斗相关补丁
│  │  ├─ TemporaryCardHighlightPatch.cs        # 临时卡牌 UI 表现补丁
│  │  └─ TreasureRoomRelicSessionHelper.cs     # 宝箱/遗物选择多人同步辅助
│  ├─ Potions/
│  │  └─ PersonChestChoose.cs                  # 起始人格宝箱药水
│  ├─ Powers/
│  │  ├─ AstralPartyPowerModel.cs              # 本模组 Power 模板与图标路径逻辑
│  │  ├─ AstralTemporaryStatPowers.cs          # 临时力量/敏捷等通用能力实现
│  │  └─ TokenRelicBridgePower.cs              # 将筹码遗物效果桥接为战斗内能力的工具基类
│  ├─ Relics/
│  │  ├─ AstralPartyRelicModel.cs              # 本模组遗物模板
│  │  ├─ CooldownPersonaRelicBase.cs           # 冷却型人格遗物公共基类
│  │  ├─ LegacyCooldownPersonaRelicBase.cs     # 旧式冷却人格遗物兼容基类
│  │  ├─ Person*.cs                            # 人格遗物
│  │  ├─ PersonalityDerivative*.cs             # 人格衍生遗物
│  │  └─ Token*.cs                             # 筹码遗物与系列遗物
│  ├─ RestSite/
│  │  └─ InitialPointRestSiteOption.cs         # 初始点休息处选项
│  └─ Utils/
│     ├─ AstralEventCardPool.cs                # 事件卡池、调查阶段卡池和确定性事件抽取
│     ├─ BaseAbilityCardRegistry.cs            # BaseAbility* 卡牌收集、缓存和确定性抽取
│     ├─ BoxingGlovesRelicHelper.cs            # 拳击手套套装计数与共享套装判定
│     ├─ CandyMachineHelper.cs                 # 怪奇糖果机在手检测、补牌与糖果药水发放
│     ├─ CardGainAttribution.cs                # 抽牌/得牌来源归因上下文，供联机和人格效果追踪
│     ├─ CombatTargetOrdering.cs               # 战斗目标稳定排序，避免联机随机目标分歧
│     ├─ CommonActions.cs                      # 常用攻击命令构造器封装
│     ├─ ConcealingInvestigationHelper.cs      # 邦尼调查事件阶段推进、触发者/控制者上下文和结算逻辑
│     ├─ CyberKittyCombatHelper.cs             # 猫猫人格的攻击牌抽取、弃牌升级和战斗内辅助
│     ├─ DeterministicMultiplayerChoiceHelper.cs # 联机确定性选择、远端同步和稳定随机排序
│     ├─ ExtraBatteryRelicHelper.cs            # 额外电池对人格冷却与步数阈值的统一修正
│     ├─ FlashlightRelicHelper.cs              # 手电筒套装计数、共享结算和额外效果辅助
│     ├─ GeneratedCardObserver.cs              # 生成卡加入手牌/牌堆后的通知与观察器分发
│     ├─ GoldModificationGuard.cs              # 金币改动保护，避免重复或递归触发
│     ├─ LowHpStateHelper.cs                   # 当前生命百分比、低血量阈值和相关判定工具
│     ├─ MascotGirlMimiTokenMemoryHelper.cs    # 看板娘筹码记忆的抽牌计数、记录与奖励投放辅助
│     ├─ MidnightFlashHelper.cs                # 午夜闪光专属牌【泥头车，创死死！】识别与生成
│     ├─ PersonaMultiplayerEffectHelper.cs     # 人格效果的多人同步、发牌、给金、授予遗物等封装
│     ├─ PersonaRelicHelper.cs                 # 人格技能牌判定与全体人格冷却推进工具
│     ├─ PersonaRelicRegistry.cs               # 人格遗物总表、可选人格筛选与图鉴相关筛选
│     ├─ PowerSafetyUtils.cs                   # Power 安全性分析，过滤会破坏奖励或战斗结束路径的能力
│     ├─ RecursiveCallGuard.cs                 # 异步递归/重入保护的小型通用门闩
│     ├─ RewardSyncHelper.cs                   # 联机遗物奖励获得流程的同步封装
│     ├─ TokenRelicBridgeHelper.cs             # 将筹码遗物效果桥接成战斗内能力的核心工具
│     ├─ TokenRelicRegistry.cs                 # 筹码遗物总表、随机池与排除规则
│     ├─ VampirePersonaHelper.cs               # 吸血鬼低血量阈值与【可爱即正义】同步逻辑
│     └─ XiaoLeiAwakeningHelper.cs             # 小雷为其他玩家发牌时的觉醒层数发放辅助
├─ ReAstralPartyMod/
│  ├─ images/
│  │  ├─ card_portraits/                       # 卡牌插画与动态卡图资源
│  │  ├─ relic/                                # 遗物图标
│  │  ├─ powers/                               # Power 图标
│  │  ├─ potion/                               # 药水图标
│  │  ├─ enchantments/                         # 附魔图标
│  │  ├─ events/                               # 事件立绘与界面图
│  │  ├─ ui/                                   # 自定义卡框、休息处和图鉴 UI 资源
│  │  └─ background/                           # 起始人格选择等背景图
│  └─ localization/
│     ├─ zhs/                                  # 简体中文本地化
│     └─ eng/                                  # 英文本地化
├─ Scripts/
│  └─ MainFile.cs                              # ModInitializer 入口、RitsuLib 注册、补丁和产物日志
├─ doc/                                        # 迁移文档、玩法设计稿、外部参考资料
├─ logs/                                       # 运行日志与联机问题排查日志
├─ tools/
│  ├─ ValidateLocalization.ps1                 # 中文本地化 UTF-8 校验
│  ├─ readme_extract.json                      # README 生成的提取数据
│  └─ generate_readme.py                       # README 自动生成脚本
├─ ReAstralPartyMod.csproj                     # 构建、PCK 导出和产物复制配置
└─ ReAstralPartyMod.json                       # Mod manifest
```"""


def build_readme() -> str:
    manifest = load_manifest()
    project = load_project_properties()
    extract = load_extract()
    localizations = load_localizations()

    cards = build_card_records(extract, localizations["cards"])
    powers = build_power_records(extract, localizations["powers"])
    relics = build_relic_records(extract, localizations["relics"])

    persona_count = sum(1 for item in relics if item["class"].startswith("Person") and not item["class"].startswith("PersonalityDerivative"))
    derivative_count = sum(1 for item in relics if item["class"].startswith("PersonalityDerivative"))
    token_count = sum(1 for item in relics if item["class"].startswith("Token"))

    lines: list[str] = [
        '<div align="center">',
        '<img style="width: 256px; height: auto; border-radius: 12px;" src="icon.svg" alt="ReAstralPartyMod"/>',
        "<h1>ReAstralPartyMod</h1>",
        '<div style="display: flex; flex-direction: row; justify-content: center; flex-wrap: wrap; gap: 6px;">',
        '<img src="https://img.shields.io/badge/-C%23-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#"/>',
        '<img src="https://img.shields.io/badge/-.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET"/>',
        '<img src="https://img.shields.io/badge/-Godot-478CBF?style=for-the-badge&logo=godotengine&logoColor=white" alt="Godot"/>',
        '<img src="https://img.shields.io/badge/-Slay%20the%20Spire%202-8B0000?style=for-the-badge&logoColor=white" alt="Slay the Spire 2"/>',
        '<a href="https://github.com/BAKAOLC/STS2-RitsuLib"><img src="https://img.shields.io/badge/-STS2--RitsuLib-5538DD?style=for-the-badge&logo=github&logoColor=white" alt="STS2-RitsuLib"/></a>',
        f'<img src="https://img.shields.io/badge/version-{manifest["version"]}-ffaf50?style=for-the-badge" alt="Version"/>',
        "</div>",
        "<div><b>简体中文</b></div>",
        "</div>",
        "",
        "`ReAstralPartyMod` 是一个以 **STS2-RitsuLib** 为前置的《杀戮尖塔 2》模组。当前版本围绕“人格遗物 + 筹码遗物 + 事件卡”三条主线构建玩法，把原先散落在旧工程里的角色和系统整合进同一个可运行项目中。",
        "",
        "这个 README 以当前仓库代码和 `zhs` 本地化为准生成，面向中文版玩家阅读。文档中的颜色标签、关键字标签和游戏内富文本标记已被移除，避免 Markdown 中混入游戏内专用标签。",
        "",
        "---",
        "",
        "## 模组概览",
        "",
        "| 项目 | 内容 |",
        "|------|------|",
        f"| Mod Id | `{manifest['id']}` |",
        f"| 模组名称 | {manifest['name']} |",
        f"| 作者 | {manifest['author']} |",
        f"| 版本 | `{manifest['version']}` |",
        f"| 前置 | `{', '.join(format_dependency(dep) for dep in manifest.get('dependencies', []))}` |",
        f"| 目标框架 | `{project.get('TargetFramework', '')}` |",
        '| Godot SDK | `Godot.NET.Sdk 4.5.1` |',
        "| 入口文件 | `Scripts/MainFile.cs` |",
        "| 初始化方式 | `ModInitializer` + `EnsureGodotScriptsRegistered` + `AstralKeywords.RegisterAll()` + `RegisterModAssembly` |",
        "",
        "### 内容规模",
        "",
        "| 类型 | 数量 |",
        "|------|------|",
        f"| 卡牌 | {len(cards)} |",
        f"| Power | {len(powers)} |",
        f"| 遗物 | {len(relics)} |",
        f"| 人格遗物 | {persona_count} |",
        f"| 人格衍生遗物 | {derivative_count} |",
        f"| 筹码与特殊遗物 | {token_count} |",
        "| 本地化文件 | `cards / powers / relics / potions / modifiers / enchantments / card_keywords / relic_collection / rest_site_ui` |",
        "",
        "## 主要玩法系统",
        "",
        "### 人格遗物",
        "- 人格遗物是当前模组的核心入口。大多数人格会在战斗中按冷却发放一张专属技能牌，并围绕单独的能力、计数器或衍生遗物展开。",
        "- 项目里已经形成了一套统一的人格模板：冷却类人格主要复用 `CooldownPersonaRelicBase`，展示、发牌、战斗起始和回合推进逻辑都集中在公共基类里。",
        "- 多个人格会在获得时自动附送对应的人格衍生遗物，用来承接整局成长、战斗计数或特殊结算。",
        "",
        "### 筹码遗物",
        "- 筹码遗物分成蓝色、紫色、金色和专属四大类，既包含单体增益，也包含套装、系列和联机辅助效果。",
        "- 当前工程保留了“筹码效果桥接为战斗内能力”的路线，便于在不直接动态增删遗物的前提下，把筹码效果临时注入战斗。",
        "- 一部分筹码依赖低血量、标记、星光、治愈、人格技能牌或联机分发事件触发，因此项目里也配套了低血量判定、生成卡归因和奖励同步辅助类。",
        "",
        "### 事件卡与战斗临时牌",
        "- `Event*` 和 `Events*` 卡牌主要服务于事件池、人格技能触发和特殊阶段推进。",
        "- `Skill*` 卡牌承担人格技能牌、战斗衍生牌和玩法中枢卡的职责，例如【调制饮料】、【无可阻挡】、【忍术连击】等。",
        "",
        "### 多人联机适配",
        "- 当前工程保留了大量联机同步辅助：包括生成卡通知、多人选项确定性处理、起始人格选择同步、奖励同步和人格效果归因。",
        "- README 只描述当前实际启用的内容，不承诺旧存档兼容，也不保留旧 BaseLib 时期的内容 ID 兼容层。",
        "",
        "## 卡牌列表",
        "",
    ]

    lines.extend(render_card_tables(cards))
    lines.append("## Power 列表")
    lines.append("")
    lines.extend(render_power_table(powers))
    lines.append("")
    lines.append("## 遗物列表")
    lines.append("")
    lines.extend(render_relic_tables(relics))
    lines.append("## 其他内容")
    lines.append("")
    lines.extend(build_other_content_summary(localizations))
    lines.extend(
        [
            "",
            "## 项目结构",
            "",
            build_project_tree(),
            "",
            "## 开发说明",
            "",
            "### 新增卡牌",
            "",
            "| 步骤 | 位置 | 说明 |",
            "|------|------|------|",
            "| 1 | `ReAstralPartyCardCode/Cards/Xxx.cs` | 新建卡牌类并继承 `AstralPartyCardModel`。 |",
            "| 2 | 类声明 | 使用 `[RegisterCard(typeof(...Pool))]` 走 RitsuLib 自动注册。 |",
            "| 3 | `ReAstralPartyMod/localization/zhs/cards.json` | 添加中文 `title / description` 和必要的运行时文本。 |",
            "| 4 | `ReAstralPartyMod/localization/eng/cards.json` | 添加英文对应条目。 |",
            "| 5 | `ReAstralPartyMod/images/card_portraits/` | 按自动路径规则补齐卡图文件。 |",
            "",
            "### 新增遗物",
            "",
            "| 步骤 | 位置 | 说明 |",
            "|------|------|------|",
            "| 1 | `ReAstralPartyCardCode/Relics/Xxx.cs` | 新建遗物类并继承 `AstralPartyRelicModel` 或现有公共基类。 |",
            "| 2 | 类声明 | 使用 `[RegisterRelic(typeof(...Pool))]` 走 RitsuLib 自动注册。 |",
            "| 3 | `ReAstralPartyMod/localization/zhs/relics.json` | 添加中文 `title / description`。 |",
            "| 4 | `ReAstralPartyMod/localization/eng/relics.json` | 添加英文对应条目。 |",
            "| 5 | `ReAstralPartyMod/images/relic/` | 补齐遗物图标文件，命名遵循类名转蛇形路径。 |",
            "",
            "### 新增 Power",
            "",
            "| 步骤 | 位置 | 说明 |",
            "|------|------|------|",
            "| 1 | `ReAstralPartyCardCode/Powers/XxxPower.cs` | 新建 Power 类并继承 `AstralPartyPowerModel`。 |",
            "| 2 | `ReAstralPartyMod/localization/zhs/powers.json` | 添加中文 `title / description / smartDescription`。 |",
            "| 3 | `ReAstralPartyMod/localization/eng/powers.json` | 添加英文对应条目。 |",
            "| 4 | `ReAstralPartyMod/images/powers/` | 补齐图标文件，路径按 `AstralPartyPowerModel` 的默认解析规则放置。 |",
            "",
            "### 本地化约定",
            "- 卡牌、Power 和遗物都按 RitsuLib 固定 public entry 规则命名，例如 `RE_ASTRAL_PARTY_MOD_CARD_*`、`RE_ASTRAL_PARTY_MOD_POWER_*`、`RE_ASTRAL_PARTY_MOD_RELIC_*`。",
            "- 关键词通过 `AstralKeywords` 集中注册，业务代码不要散落硬编码 keyword id。",
            "- 中文本地化文件统一使用显式 UTF-8 编码，避免 Rider、PowerShell 或脚本链路写出乱码。",
            "",
            "## 构建与输出",
            "",
            "| 项目 | 配置 |",
            "|------|------|",
            f"| Slay the Spire 2 目录 | `{project.get('Sts2Dir', '')}` |",
            f"| 备用游戏目录 | `{project.get('SecondarySts2Dir', '')}` |",
            "| RitsuLib 引用 | `$(Sts2Dir)\\mods\\RitsuLib\\STS2-RitsuLib.dll` |",
            f"| Godot 编辑器 | `{project.get('GodotEditorPath', '')}` |",
            f"| 主输出目录 | `{project.get('ModOutputDir', '')}` |",
            f"| 备用输出目录 | `{project.get('SecondaryModOutputDir', '')}` |",
            "",
            "常用构建命令：",
            "",
            "```powershell",
            "dotnet build /p:RunPckExport=false",
            "```",
            "",
            "构建完成后会自动处理以下产物：",
            "- `dll` 复制到游戏 `mods/ReAstralPartyMod/` 目录。",
            "- 如果启用导出，则由 Godot 生成 `.pck` 并复制到相同目录。",
            "- 入口初始化时会记录 `dll / pck / manifest` 的 SHA256，便于排查联机和部署问题。",
            "",
            "## 致谢",
            "",
            "- [STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib)",
            "- 《杀戮尖塔 2》mod 社区中的案例工程、迁移文档和联机调试经验",
        ]
    )

    return "\n".join(lines).rstrip() + "\n"


def main() -> None:
    README_PATH.write_text(build_readme(), encoding="utf-8-sig")


if __name__ == "__main__":
    main()
