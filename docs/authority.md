# Authority model

Authority assigns each piece of state to either the **server** (referee) or the **owning client** (player). HoverCar Derby uses a **hybrid** split: responsive driving on the client, trusted combat and scoring on the server.

## Authority table

| Concern | Authority | Notes |
|---------|-----------|-------|
| Movement / transform | Owning client, bounded by the server | `HoverCarMover` + `ClientNetworkTransform`; `ServerMovementGuard` checks speed and position |
| Bot movement | Server | No human owner; server drives bot input |
| Collisions / damage | Server, from its own contact or an owner report | `ServerPhysicsCollisionHandler`; health via NetworkVariable |
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

Derby scoring needs trust. Driving needs responsiveness.

## Server path (damage)

1. A car contact reaches the server either from its own physics callback or from the owning client through `ReportCollisionServerRpc`.
2. A pair cooldown (`CollisionDeduplicator`) processes one physical hit once.
3. `CollisionDamageCalculator` computes damage on the server. For an owner report, the impact force, contact data and both speeds are the values the owner measured.
4. Server applies damage (NetworkVariable health).
5. Server raises `DamageDealtEvent`.
6. `ScoreManager` awards points; HUD updates via presenters.

Health and score are written only by the server.

## Client path (movement)

1. Owner reads input and applies hover forces in `FixedUpdate`.
2. `ClientNetworkTransform` replicates transform to peers.
3. Bots use the same mover with server-side input (`isBot`).

## Server movement check

`ServerMovementGuard` runs on the server for every client-owned car. Bots and the host's own car are exempt.

1. The server samples the replicated transform every quarter second and compares it with the last accepted position.
2. Horizontal speed above the car's cap (`HoverCarMover.SpeedCap`, scaled by a tolerance for collision impulses and network smoothing) is rejected. Vertical speed is not limited.
3. A position outside the play volume is rejected. The volume is the `MapData` rectangle plus a horizontal margin, with a vertical band around the map's ground heights.
4. A rejected sample sends the owner back to the last accepted position through the same owner-side teleport used for spawning (`PlayerController.TeleportToSpawnPositionClientRpc`), and the check pauses briefly while the snap-back arrives.
5. Repeated rejections inside a time window disconnect the client, which covers a client that ignores the snap-back.
6. Spawn and respawn teleports are announced to the guard (`ExpectTeleport`), so the owner's jump to the spawn point is accepted when it arrives near the announced target.

The speed, bounds and window rules are plain C# in the `Limekicker.MovementAuthority` assembly (`MovementRules`, `MovementViolationLog`).

## Key files

- `Scripts/Networking/Client/ClientNetworkTransform.cs`
- `Scripts/Player/HoverCarMover.cs`
- `Scripts/Player/ServerMovementGuard.cs`
- `Scripts/Player/Authority/MovementRules.cs`
- `Scripts/DamageSystem/ServerPhysicsCollisionHandler.cs`
- `Scripts/DamageSystem/Decision/CollisionDamageCalculator.cs`
- `Scripts/DamageSystem/CarDamageManager.cs`
- `Scripts/GameFlow/ScoreManager.cs`
- `Scripts/GameFlow/GameManager.cs`