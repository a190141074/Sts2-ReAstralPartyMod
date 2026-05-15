# ReAstralParty Telemetry Server

这个目录承载 `ReAstralPartyMod` 的数据统计服务端与统计说明。

目录约定：

- `server.js`
  - 遥测接收器与派生汇总服务
- `labels.json`
  - 中文标签映射
- `package.json`
  - Node 启动配置
- `posthog-telemetry-guide.md`
  - PostHog 事件、看板、SQL 统计说明

说明：

- 游戏内客户端上报代码仍保留在 `ReAstralPartyCardCode/Online/AstralTelemetry*.cs`
- 这是运行时逻辑，不能迁到服务端目录，否则 mod 本体无法记录和上传数据
- 服务端、统计口径、SQL、看板说明统一收口在本目录维护

启动示例：

```powershell
cd B:\Documents\re-astral-party-mod\server\re-astral-party-telemetry
npm install
npm start
```
