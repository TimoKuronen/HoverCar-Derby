# Authority model

Authority assigns each piece of state to either the **server** (referee) or the **owning client** (player). HoverCar uses a **hybrid** split: responsive driving on the client, trusted combat and scoring on the server.

## Authority table

| Concern | Authority | Notes |
|---------|-----------|-------|
| Movement / transform | Owning client | `HoverCarMover` + `ClientNetworkTransform` |
| Bot movement | Server | No human owner; server drives bot input |
| Collisions / damage | Server | `ServerPhysicsCollisionHandler`; health via NetworkVariable |
| Score | Server | `ScoreManager` from `DamageDealtEvent` / collectibles |
| Match phases / timer | Server | `GameManager` NetworkVariables + server coroutine |
| Collectible consumption | Server | Server marks processed; clients observe |
| VFX / audio | Presentation | EventBus / ClientRpc — not gameplay-authoritative |
| HUD scoreboard | Local UI from server events | `ScoreDisplayView` |

## Why this split

| If movement were fully server-authoritative | If damage were client-authoritative |
|---------------------------------------------|-------------------------------------|
| Steering waits for RTT; mushy mobile feel | Players can fake damage or invulnerability |
| Higher server CPU for physics | Scores and win condition become meaningless |

Derby scoring needs trust. Driving needs responsiveness. This is an acceptable prototype tradeoff.

## Server path (damage)

1. Server runs collision logic.
2. Server applies damage (NetworkVariable health).
3. Server raises `DamageDealtEvent`.
4. `ScoreManager` awards points; HUD updates via presenters.

Clients do not unilaterally decide damage outcomes.

## Client path (movement)

1. Owner reads input and applies hover forces in `FixedUpdate`.
2. `ClientNetworkTransform` replicates transform to peers.
3. Bots use the same mover with server-side input (`isBot`).

## Cheat surface (documented limitation)

Without server validation of speed/position, a modified client could speed-hack or teleport. Health and score remain server-writable NetworkVariable / server events, so permanent fake health is not trivial without compromising the host/server.

Production hardening would add sanity checks on velocity and position. See [known-limitations.md](known-limitations.md).

## Key files

- `Scripts/Networking/Client/ClientNetworkTransform.cs`
- `Scripts/Player/HoverCarMover.cs`
- `Scripts/DamageSystem/ServerPhysicsCollisionHandler.cs`
- `Scripts/DamageSystem/CarDamageManager.cs`
- `Scripts/Managers/ScoreManager.cs`
- `Scripts/Managers/GameManager.cs`