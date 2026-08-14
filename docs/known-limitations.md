# Known limitations

Honest scope notes for HoverCar Derby as a **networking prototype**, not a shippable live game.

## Demo scope

The supported portfolio demo is **Host + Relay + join code** only:

1. Host from the main menu and note the join code in the notification panel.
2. Client enters that code and joins.
3. Both players complete a round and return to the menu via pause or results UI.

Lobby browse, quick join, Find Match, and dedicated-server entry are **not** exposed in the menu UI and are **not** part of the documented demo path. Some related APIs remain in code for future work.

## Session and Unity Gaming Services

| Area | Status |
|------|--------|
| Auth + Relay + join code | Supported demo path |
| Lobby metadata on host | Used internally when hosting; not a separate player-facing flow |
| Lobby browse / list join | Partial implementation; UI hidden; cleanup gaps remain |
| Matchmaking / Multiplay | Stub or partial; requires deployed fleet and queues; not demo-ready |
| Dedicated server boot path | Exists for server builds; not the portfolio demo |
| Host migration | Not implemented; host quit ends the session for clients |

Player-facing session errors use the notification console where wired. Not every edge case (failed auth, reconnect after disconnect) is polished.

## Authority and netcode

Movement uses **client authority** (`HoverCarMover` + `ClientNetworkTransform`) for responsive driving. Damage, scoring, collectibles, and match phases are **server authority**.

Without server-side movement validation, a modified client could speed-hack or teleport. Health and score are server-owned and harder to fake without compromising the host. See [authority.md](authority.md) for the full split and cheat surface.

There is **no** client prediction, reconciliation, or lag compensation. Latency shows up as remote transform updates, which is acceptable for this prototype scope.

## Production gaps (not attempted in this repo)

- Server-side movement sanity checks
- Automated edit-mode or play-mode tests for networking
- Android release build and device FPS notes in public docs
- Polished disconnect handling for every edge case
- Single-map, respawn-based rounds only; no live-ops or matchmaking at scale

## Content and tech debt

- Single arena, placeholder environment art, minimal game modes
- Pause-panel SFX/music toggles are plain UI (not wired to audio prefs in-game)
- `Leaderboard` NetworkList path is stale or unused; active scoring uses `ScoreManager` and HUD presenters
- Dev toggles (spawn bot, skip countdown) appear in editor and development builds only

## Related docs

- [Architecture](architecture.md) — scenes, facade, presenter/view menus
- [Authority model](authority.md) — who owns movement, damage, score, and match state
- [README](../README.md) — how to run the demo
