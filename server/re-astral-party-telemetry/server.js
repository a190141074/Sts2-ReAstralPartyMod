const http = require("node:http");
const fs = require("node:fs");
const path = require("node:path");
const crypto = require("node:crypto");

const HOST = process.env.HOST || "127.0.0.1";
const PORT = Number.parseInt(process.env.PORT || "3000", 10);
const DATA_DIR = process.env.DATA_DIR || path.join(__dirname, "data");
const DERIVED_DIR = path.join(DATA_DIR, "derived");
const LABELS_FILE = path.join(__dirname, "labels.json");
const RESULTS_FILE = path.join(DATA_DIR, "run_results.jsonl");
const MAX_BODY_BYTES = 256 * 1024;
const MIN_RUN_TIME_FOR_DEFAULT_STATS = 180;

fs.mkdirSync(DATA_DIR, { recursive: true });
fs.mkdirSync(DERIVED_DIR, { recursive: true });

const LABELS = loadLabels();
const knownRunIds = loadKnownRunIds();

function loadLabels() {
  try {
    if (fs.existsSync(LABELS_FILE)) {
      return JSON.parse(fs.readFileSync(LABELS_FILE, "utf8"));
    }
  } catch {
  }
  return {};
}

function getLabel(category, id) {
  if (!id) {
    return "";
  }
  return LABELS?.[category]?.[id] || id;
}

function sha256(value) {
  return crypto.createHash("sha256").update(value).digest("hex");
}

function loadKnownRunIds() {
  const ids = new Set();
  if (!fs.existsSync(RESULTS_FILE)) {
    return ids;
  }
  for (const line of fs.readFileSync(RESULTS_FILE, "utf8").split(/\r?\n/)) {
    if (!line.trim()) continue;
    try {
      const record = JSON.parse(line);
      const runId = record?.payload?.run?.runId;
      if (typeof runId === "string" && runId.length > 0) {
        ids.add(runId);
      }
    } catch {
    }
  }
  return ids;
}

function sendJson(res, status, value) {
  const body = JSON.stringify(value);
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "cache-control": "no-store",
    "access-control-allow-origin": "*",
    "content-length": Buffer.byteLength(body)
  });
  res.end(body);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", (chunk) => {
      size += chunk.length;
      if (size > MAX_BODY_BYTES) {
        reject(Object.assign(new Error("payload too large"), { statusCode: 413 }));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
    req.on("error", reject);
  });
}

function validatePayload(payload) {
  if (!payload || typeof payload !== "object") return "payload must be an object";
  if (payload.schemaVersion !== 1) return "unsupported schemaVersion";
  if (payload.modId !== "ReAstralPartyMod") return "modId must be ReAstralPartyMod";
  if (!payload.run || typeof payload.run !== "object") return "run is required";
  if (typeof payload.run.runId !== "string" || payload.run.runId.length < 16) return "run.runId is required";
  if (typeof payload.run.seedHash !== "string" || payload.run.seedHash.length < 16) return "run.seedHash is required";
  if (typeof payload.run.isVictory !== "boolean") return "run.isVictory must be boolean";
  if (!Array.isArray(payload.players) || !Array.isArray(payload.personaChoices) || !Array.isArray(payload.tokenChoices)) {
    return "players, personaChoices, and tokenChoices must be arrays";
  }
  return null;
}

async function handleIngest(req, res) {
  let payload;
  try {
    payload = JSON.parse(await readBody(req));
  } catch (error) {
    return sendJson(res, error.statusCode || 400, { ok: false, error: error.message || "invalid json" });
  }

  const validationError = validatePayload(payload);
  if (validationError) {
    return sendJson(res, 400, { ok: false, error: validationError });
  }

  const runId = payload.run.runId;
  if (knownRunIds.has(runId)) {
    return sendJson(res, 202, { ok: true, duplicate: true, runId });
  }

  const record = {
    receivedAtUtc: new Date().toISOString(),
    payloadHash: sha256(JSON.stringify(payload)),
    payload
  };
  fs.appendFileSync(RESULTS_FILE, `${JSON.stringify(record)}\n`, "utf8");
  knownRunIds.add(runId);
  rebuildDerivedNow();
  return sendJson(res, 202, { ok: true, duplicate: false, runId });
}

function readRecords() {
  if (!fs.existsSync(RESULTS_FILE)) {
    return [];
  }
  const byRunId = new Map();
  for (const line of fs.readFileSync(RESULTS_FILE, "utf8").split(/\r?\n/)) {
    if (!line.trim()) continue;
    try {
      const record = JSON.parse(line);
      const runId = record?.payload?.run?.runId;
      if (typeof runId === "string" && runId.length > 0) {
        byRunId.set(runId, record);
      }
    } catch {
    }
  }
  return [...byRunId.values()];
}

function pct(part, total) {
  return total > 0 ? Number(((part / total) * 100).toFixed(1)) : 0;
}

function writeCsv(fileName, rows, headers) {
  const lines = [headers.join(",")];
  for (const row of rows) {
    lines.push(headers.map((header) => csvCell(row[header])).join(","));
  }
  fs.writeFileSync(path.join(DERIVED_DIR, fileName), `${lines.join("\n")}\n`, "utf8");
}

function csvCell(value) {
  const raw = value == null ? "" : String(value);
  if (/[",\r\n]/.test(raw)) {
    return `"${raw.replaceAll('"', '""')}"`;
  }
  return raw;
}

function rebuildDerivedNow() {
  const records = readRecords();
  const eligibleRecords = records.filter((record) => Number(record?.payload?.run?.runTime || 0) > MIN_RUN_TIME_FOR_DEFAULT_STATS);

  const summary = {
    generatedAtUtc: new Date().toISOString(),
    filters: {
      minRunTimeForDefaultStats: MIN_RUN_TIME_FOR_DEFAULT_STATS
    },
    runCount: eligibleRecords.length,
    winCount: 0,
    winRate: 0,
    personas: [],
    tokenChoices: [],
    tokenObtained: [],
    personaSkillUsage: []
  };

  const personaChoiceStats = new Map();
  const personaRunStats = new Map();
  const tokenChoiceStats = new Map();
  const tokenObtainedStats = new Map();
  const personaSkillUsageStats = new Map();

  const personaChoiceRows = [];
  const personaRunRows = [];
  const tokenChoiceRows = [];
  const tokenObtainedRows = [];
  const personaSkillRows = [];

  for (const record of eligibleRecords) {
    const payload = record.payload;
    const run = payload.run;
    const isVictory = run.isVictory === true;
    if (isVictory) {
      summary.winCount += 1;
    }

    for (const choice of payload.personaChoices || []) {
      const options = Array.isArray(choice.options) ? choice.options : [];
      const selected = typeof choice.selected === "string" ? choice.selected : "";
      for (const option of options) {
        if (!personaChoiceStats.has(option)) {
          personaChoiceStats.set(option, { offered: 0, selected: 0, selectedWins: 0 });
        }
        const stat = personaChoiceStats.get(option);
        stat.offered += 1;
        if (option === selected) {
          stat.selected += 1;
          if (isVictory) stat.selectedWins += 1;
        }
        personaChoiceRows.push({
          runId: run.runId,
          playerSlot: choice.playerSlot,
          option,
          optionName: option,
          selected,
          isSelected: option === selected ? 1 : 0,
          isVictory: isVictory ? 1 : 0
        });
      }
    }

    for (const player of payload.players || []) {
      const personaSelected = player.personaSelected || "";
      if (personaSelected) {
        if (!personaRunStats.has(personaSelected)) {
          personaRunStats.set(personaSelected, { runs: 0, wins: 0 });
        }
        const stat = personaRunStats.get(personaSelected);
        stat.runs += 1;
        if (isVictory) stat.wins += 1;
        personaRunRows.push({
          runId: run.runId,
          slot: player.slot,
          character: player.character,
          personaSelected,
          isVictory: isVictory ? 1 : 0
        });
      }

      const skillCardId = player.personaSkillCardId || "";
      if (skillCardId) {
        if (!personaSkillUsageStats.has(skillCardId)) {
          personaSkillUsageStats.set(skillCardId, { samples: 0, totalUses: 0 });
        }
        const stat = personaSkillUsageStats.get(skillCardId);
        stat.samples += 1;
        stat.totalUses += Number(player.personaSkillUseCount || 0);
        personaSkillRows.push({
          runId: run.runId,
          slot: player.slot,
          personaSelected,
          personaSkillCardId: skillCardId,
          personaSkillUseCount: Number(player.personaSkillUseCount || 0),
          isVictory: isVictory ? 1 : 0
        });
      }

      for (const tokenId of player.obtainedTokens || []) {
        if (!tokenObtainedStats.has(tokenId)) {
          tokenObtainedStats.set(tokenId, { runs: 0, wins: 0 });
        }
        const stat = tokenObtainedStats.get(tokenId);
        stat.runs += 1;
        if (isVictory) stat.wins += 1;
        tokenObtainedRows.push({
          runId: run.runId,
          slot: player.slot,
          tokenId,
          tokenName: tokenId,
          isVictory: isVictory ? 1 : 0
        });
      }
    }

    for (const choice of payload.tokenChoices || []) {
      const options = Array.isArray(choice.options) ? choice.options : [];
      const selected = typeof choice.selected === "string" ? choice.selected : "";
      for (const option of options) {
        if (!tokenChoiceStats.has(option)) {
          tokenChoiceStats.set(option, { offered: 0, selected: 0, selectedWins: 0 });
        }
        const stat = tokenChoiceStats.get(option);
        stat.offered += 1;
        if (option === selected) {
          stat.selected += 1;
          if (isVictory) stat.selectedWins += 1;
        }
        tokenChoiceRows.push({
          runId: run.runId,
          playerSlot: choice.playerSlot,
          source: choice.source,
          option,
          optionName: option,
          selected,
          rerollCount: choice.rerollCount ?? 0,
          isSelected: option === selected ? 1 : 0,
          isVictory: isVictory ? 1 : 0
        });
      }
    }
  }

  summary.winRate = pct(summary.winCount, summary.runCount);
  summary.personas = [...personaChoiceStats.entries()].map(([id, stat]) => ({
    id,
    name: id,
    offered: stat.offered,
    selected: stat.selected,
    pickRate: pct(stat.selected, stat.offered),
    selectedWins: stat.selectedWins,
    selectedWinRate: pct(stat.selectedWins, stat.selected),
    heldRuns: personaRunStats.get(id)?.runs || 0,
    heldWins: personaRunStats.get(id)?.wins || 0,
    heldWinRate: pct(personaRunStats.get(id)?.wins || 0, personaRunStats.get(id)?.runs || 0)
  })).sort((a, b) => b.selected - a.selected || a.id.localeCompare(b.id));

  summary.tokenChoices = [...tokenChoiceStats.entries()].map(([id, stat]) => ({
    id,
    name: id,
    offered: stat.offered,
    selected: stat.selected,
    pickRate: pct(stat.selected, stat.offered),
    selectedWins: stat.selectedWins,
    selectedWinRate: pct(stat.selectedWins, stat.selected)
  })).sort((a, b) => b.selected - a.selected || a.id.localeCompare(b.id));

  summary.tokenObtained = [...tokenObtainedStats.entries()].map(([id, stat]) => ({
    id,
    name: id,
    runs: stat.runs,
    wins: stat.wins,
    winRate: pct(stat.wins, stat.runs)
  })).sort((a, b) => b.runs - a.runs || a.id.localeCompare(b.id));

  summary.personaSkillUsage = [...personaSkillUsageStats.entries()].map(([id, stat]) => ({
    id,
    name: id,
    samples: stat.samples,
    totalUses: stat.totalUses,
    averageUses: stat.samples > 0 ? Number((stat.totalUses / stat.samples).toFixed(2)) : 0
  })).sort((a, b) => b.samples - a.samples || a.id.localeCompare(b.id));

  fs.writeFileSync(path.join(DERIVED_DIR, "summary.json"), `${JSON.stringify(summary, null, 2)}\n`, "utf8");
  writeCsv("persona_choices.csv", personaChoiceRows, ["runId", "playerSlot", "option", "optionName", "selected", "isSelected", "isVictory"]);
  writeCsv("persona_runs.csv", personaRunRows, ["runId", "slot", "character", "personaSelected", "isVictory"]);
  writeCsv("token_choices.csv", tokenChoiceRows, ["runId", "playerSlot", "source", "option", "optionName", "selected", "rerollCount", "isSelected", "isVictory"]);
  writeCsv("token_obtained.csv", tokenObtainedRows, ["runId", "slot", "tokenId", "tokenName", "isVictory"]);
  writeCsv("persona_skill_usage.csv", personaSkillRows, ["runId", "slot", "personaSelected", "personaSkillCardId", "personaSkillUseCount", "isVictory"]);
}

function sendDerived(res, fileName) {
  const filePath = path.join(DERIVED_DIR, fileName);
  if (!fs.existsSync(filePath)) {
    rebuildDerivedNow();
  }
  if (!fs.existsSync(filePath)) {
    return sendJson(res, 404, { ok: false, error: "not found" });
  }
  const body = fs.readFileSync(filePath);
  res.writeHead(200, {
    "content-type": fileName.endsWith(".json") ? "application/json; charset=utf-8" : "text/csv; charset=utf-8",
    "cache-control": "no-store"
  });
  res.end(body);
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, "http://localhost");
  if (req.method === "GET" && url.pathname === "/health") {
    return sendJson(res, 200, { ok: true, service: "re-astral-party-telemetry", runs: knownRunIds.size });
  }
  if (req.method === "GET" && url.pathname === "/api/re-astral-party/summary") {
    rebuildDerivedNow();
    return sendDerived(res, "summary.json");
  }
  if (req.method === "GET" && url.pathname.startsWith("/api/re-astral-party/derived/")) {
    return sendDerived(res, path.basename(url.pathname));
  }
  if (req.method === "POST" && url.pathname === "/api/re-astral-party/run-result") {
    return handleIngest(req, res);
  }
  return sendJson(res, 404, { ok: false, error: "not found" });
});

server.listen(PORT, HOST, () => {
  console.log(`re-astral-party-telemetry listening on ${HOST}:${PORT}`);
});
