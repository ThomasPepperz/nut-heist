# Connecting Claude (AI) to this Unity project

There are two complementary ways to let an AI assistant work on **Nut Heist**.
Use either or both.

## TL;DR

- **Code loop — works today, no install, no keys.** A Claude session writes/edits
  C# and opens a PR; you `git pull` and press **Play**. This is how a *cloud*
  Claude (claude.ai/code) contributes — it cannot touch your local Editor
  directly.
- **Live Editor control — local only.** Run a *local* AI client (Claude Desktop,
  Claude Code CLI, or Cursor) on the **same machine** as Unity, plus a Unity MCP
  bridge. The local AI can then create objects, edit scenes, enter Play mode, and
  (with the custom server) generate 3D assets.

> ⚠️ A Unity MCP is a **localhost** bridge: the AI client and the Unity Editor
> must be on the **same computer**. A cloud session can't reach your local
> Editor — it contributes through git PRs.

## Requirements

- **Unity `6000.4.7f1`** — this project's pinned version (`ProjectSettings/ProjectVersion.txt`).
  Unity Hub will offer to install a matching editor when you open the project.
- A local desktop AI client (Claude Desktop / Claude Code CLI / Cursor) for live
  Editor control.
- **Node 20+** only if you use the custom server (Option B).

## Option A — Public CoplayDev MCP (fast, no keys, Editor control)

1. Unity → **Window → Package Manager → ＋ → Add package from git URL**, paste:
   `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#main`
   (package `com.coplaydev.unity-mcp`, v9.7.1)
2. **Window → MCP for Unity → Configure All Detected Clients** — auto-writes the
   MCP config for Claude Desktop / Claude Code / Cursor.
3. Start your local AI client; approve the pending connection in Unity if asked.

No API keys. Controls scenes / GameObjects / scripts / assets. Does **not**
generate 3D models.

## Option B — Custom unity-mcp (Editor control **+** AI 3D-asset generation)

Lives in the `utilities` repo at `unity-mcp/` (recovered in **utilities PR #322**;
verified to compile and boot). Adds Tripo / Meshy / free-local 3D generation with
auto-import into `Assets/Generated/`.

1. **Build the server:** `cd unity-mcp && npm install && npm run build`
2. **Install the bridge:** Unity → Package Manager → ＋ → **Install package from
   disk** → pick `unity-mcp/unity-package/package.json`.
3. Unity → **Window → Pepperz → Unity MCP** → set a port + a **token you invent**
   → Save → Start. Console shows `[UnityMCP] listening on http://127.0.0.1:17653/`.
4. Point your local client at it, e.g. `claude_desktop_config.json`:
   ```json
   {
     "mcpServers": {
       "unity": {
         "command": "node",
         "args": ["/ABSOLUTE/PATH/to/utilities/unity-mcp/dist/index.js"],
         "env": {
           "UNITY_BRIDGE_HOST": "127.0.0.1",
           "UNITY_BRIDGE_PORT": "17653",
           "UNITY_BRIDGE_TOKEN": "the-token-you-set-in-the-window",
           "TRIPO_API_KEY": "optional",
           "MESHY_API_KEY": "optional"
         }
       }
     }
   }
   ```
   `UNITY_BRIDGE_TOKEN` must match the Unity settings window.

## Keys — there is **no "Unity API key"**

- **Unity itself:** no key. Sign into Unity Hub with a free Unity ID (a Personal
  license has no serial — it activates on sign-in).
- **3D generation:** the `local_*` tools are **free, no key** (bundled TripoSR).
  Optional paid upgrades for higher quality:
  - Tripo → <https://platform.tripo3d.ai/api-keys> → `TRIPO_API_KEY`
  - Meshy → <https://www.meshy.ai/settings/api> (**Meshy Pro plan required**) → `MESHY_API_KEY`
  - A tool whose key is missing simply doesn't load — nothing breaks.
- **`UNITY_BRIDGE_TOKEN`:** a random string *you* invent; it only has to match
  between the server env and the Unity window.

## The code loop with a cloud Claude

1. Tell the cloud session what to build or fix.
2. It commits to a `claude/…` branch and opens a PR.
3. `git pull` (or merge) → Unity recompiles on focus → press **Play** to test.
4. Report back what you saw; repeat.
