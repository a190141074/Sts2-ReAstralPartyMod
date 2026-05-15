ReAstralPartyMod PostHog 遥测说明

## 概览

当前客户端会在以下条件满足时上报匿名平衡统计：

- 仅统计单人局和联机房主局
- `runTime > 180s`
- 使用匿名 `distinct_id`
- 不创建 PostHog person profile

当前上报到 `US Cloud`：

- Host: `https://us.i.posthog.com`
- Endpoint: `/batch/`

## 本地配置

配置文件会自动创建在 mod 用户数据目录下：

- `telemetry_config.json`
- `telemetry_pending.jsonl`
- `telemetry_active_run.json`
- `telemetry_install_salt.txt`

当前配置格式：

```json
{
  "enabled": true,
  "provider": "posthog",
  "host": "https://us.i.posthog.com",
  "projectToken": "phc_xxx",
  "pendingFile": "telemetry_pending.jsonl"
}
```

## 当前事件

客户端当前会上报 4 类事件：

1. `astral_persona_choice`
2. `astral_token_choice`
3. `astral_token_obtained`
4. `astral_run_finished`

此外还会上报 2 类“候选出现明细”事件，专门服务选取率统计：

5. `astral_persona_option_offered`
6. `astral_token_option_offered`

其中每条事件都带基础字段：

- `run_id`
- `seed_hash`
- `player_slot`
- `mod_id`
- `mod_version`
- `game_version`
- `schema_version`
- `uploaded_at_utc`
- `persona_selected`
- `persona_selected_label`

为方便直接看板，事件里同时带：

- 稳定聚合字段：`*_id`
- 中文展示字段：`*_label`

## 关键字段

### `astral_persona_choice`

- `options`
- `option_labels`
- `selected`
- `selected_label`

### `astral_token_choice`

- `source`
- `options`
- `option_labels`
- `selected`
- `selected_label`
- `reroll_count`

### `astral_token_obtained`

- `token_id`
- `token_label`

### `astral_run_finished`

- `character`
- `persona_selected`
- `persona_selected_label`
- `persona_skill_card_id`
- `persona_skill_card_label`
- `persona_skill_use_count`
- `obtained_tokens`
- `obtained_token_labels`
- `is_victory`
- `run_time`
- `ascension`
- `current_act_index`
- `total_floor`
- `net_mode`
- `player_count`

### `astral_persona_option_offered`

- `option_id`
- `option_label`
- `selected`
- `selected_label`
- `is_selected`

### `astral_token_option_offered`

- `source`
- `option_id`
- `option_label`
- `selected`
- `selected_label`
- `is_selected`
- `reroll_count`

## 推荐看板

建议创建一个 dashboard：

- `ReAstralParty Balance`

建议放这 6 个 insight：

1. 人格选取次数
2. 人格胜率
3. 筹码选取次数
4. 筹码选取率
5. 筹码获得胜率
6. 人格技能牌平均使用次数

## Trends 推荐配置

### 人格选取次数

- Event: `astral_persona_choice`
- Breakdown: `selected_label`
- Metric: `Total count`

### 筹码选取次数

- Event: `astral_token_choice`
- Breakdown: `selected_label`
- Metric: `Total count`

如需区分来源，可加筛选：

- `source`

说明：

- 优先用 `*_label` 做 Breakdown，不要优先用 `*_id`
- 若需要排错，再在 SQL 结果里同时保留 `ID + 中文名称`

## SQL 推荐

以下 SQL 直接按当前事件字段设计。

优化原则：

- 计数类图表优先使用 `Trends`，不要用 SQL 重扫全表
- 胜率 / 选取率这类比率统计再使用 SQL
- SQL 只扫单一事件类型，避免自连接和数组展开
- 若数据量开始变大，优先先缩小日期范围

### 人格胜率

```sql
SELECT
    properties.persona_selected AS "人格ID",
    properties.persona_selected_label AS "人格名称",
    count() AS "样本数",
    sum(if(properties.is_victory = 'true', 1, 0)) AS "胜利次数",
    round(100.0 * "胜利次数" / "样本数", 2) AS "胜率"
FROM events
WHERE event = 'astral_run_finished'
  AND properties.persona_selected IS NOT NULL
  AND properties.persona_selected != ''
  AND {filters}
GROUP BY "人格ID", "人格名称"
ORDER BY "样本数" DESC
```

### 人格技能牌平均使用次数

```sql
SELECT
    properties.persona_selected AS "人格ID",
    properties.persona_selected_label AS "人格名称",
    properties.persona_skill_card_id AS "技能牌ID",
    properties.persona_skill_card_label AS "技能牌名称",
    count() AS "样本数",
    avg(toFloat(properties.persona_skill_use_count)) AS "平均使用次数",
    max(toInt(properties.persona_skill_use_count)) AS "最高使用次数"
FROM events
WHERE event = 'astral_run_finished'
  AND properties.persona_selected IS NOT NULL
  AND properties.persona_selected != ''
  AND properties.persona_skill_card_id IS NOT NULL
  AND properties.persona_skill_card_id != ''
  AND {filters}
GROUP BY "人格ID", "人格名称", "技能牌ID", "技能牌名称"
ORDER BY "样本数" DESC
```

### 筹码获得胜率

```sql
SELECT
    properties.token_id AS "筹码ID",
    properties.token_label AS "筹码名称",
    count() AS "获得次数",
    sum(if(properties.is_victory = 'true', 1, 0)) AS "胜利次数",
    round(100.0 * "胜利次数" / "获得次数", 2) AS "胜率"
FROM events
WHERE event = 'astral_token_obtained'
  AND properties.token_id IS NOT NULL
  AND properties.token_id != ''
  AND {filters}
GROUP BY "筹码ID", "筹码名称"
ORDER BY "获得次数" DESC
```

### 人格选取率

```sql
SELECT
    properties.option_id AS "人格ID",
    properties.option_label AS "人格名称",
    count() AS "出现次数",
    sum(if(properties.is_selected = 'true', 1, 0)) AS "被选次数",
    round(100.0 * "被选次数" / "出现次数", 2) AS "选取率"
FROM events
WHERE event = 'astral_persona_option_offered'
  AND properties.option_id IS NOT NULL
  AND properties.option_id != ''
  AND {filters}
GROUP BY "人格ID", "人格名称"
ORDER BY "出现次数" DESC
```

### 筹码选取率

```sql
SELECT
    properties.option_id AS "筹码ID",
    properties.option_label AS "筹码名称",
    count() AS "出现次数",
    sum(if(properties.is_selected = 'true', 1, 0)) AS "被选次数",
    round(100.0 * "被选次数" / "出现次数", 2) AS "选取率"
FROM events
WHERE event = 'astral_token_option_offered'
  AND properties.option_id IS NOT NULL
  AND properties.option_id != ''
  AND {filters}
GROUP BY "筹码ID", "筹码名称"
ORDER BY "出现次数" DESC
```

## 统计口径

- 胜率统一按整局胜败统计
- 人格统计按开局人格选择口径
- 筹码统计同时保留选择口径与实际获得口径
