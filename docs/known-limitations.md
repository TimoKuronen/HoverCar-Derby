# Known limitations

Intentional prototype gaps and tech debt. Not a full production multiplayer stack.

## Networking and sessions

| Limitation | Notes |
|------------|-------|
| No host migration | If host quits, session ends for clients |
| Client-authoritative movement | No server speed/position validation; cheat surface exists |
| Matchmaking / Multiplay | Stubs / optional path; may be flaky — Relay + join code is the stable demo |
| Dedicated server path | Partially implemented; not the primary portfolio demo |
| Late join mid-match | Not a polished feature; prefer join before countdown |

## Gameplay

| Limitation | Notes |
|------------|-------|
| Single arena / mode | One map, fixed round length |
| Respawn, not elimination | Destruction does not remove players from the round |
| NetworkList leaderboard | Early `Leaderboard.cs` path incomplete; HUD scoreboard (`ScoreDisplayView`) is the active UI |
| Prediction / lag compensation | Not implemented; acknowledge latency in demos |

## Platform and polish

| Limitation | Notes |
|------------|-------|
| Android release polish | Touch works in editor/dev; release build + FPS notes may still be pending |
| Dev toggles | Spawn-bot / skip-countdown are editor/dev only; hide in release builds |
| Assets | Placeholders / AI-generated; non-commercial |

## What we would change first for a soft launch

1. Server-side movement sanity checks (speed, teleport bounds).
2. Host disconnect messaging and graceful return to menu on all clients.
3. Harden Relay + join code as the only supported session path (or invest in dedicated servers).
4. Automated tests for damage qualification and score dedup.