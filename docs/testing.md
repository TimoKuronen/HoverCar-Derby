# Testing

Edit-mode tests cover the server-side collision decision logic. The rules are plain C# types in the `Limekicker.DamageDecision` assembly with no networking or scene dependencies, so every case runs as a fast NUnit test in `Limekicker.DamageDecision.Tests` (`Assets/Limekicker/Tests/EditMode`).

## What the tests cover

| Type | Covered behavior |
|------|------------------|
| `CollisionDamageCalculator` | Head-on hits split damage by opposing speed (even split when both cars are stationary). A front bumper hit damages only the victim; the attacker takes nothing. An idle attacker deals no damage, and a slow attacker's damage scales with speed. Flank hits damage the slower car only with majority velocity, otherwise damage splits evenly. Damage to the other car at or below the minimum threshold is zeroed while damage to this car is kept. The damage multiplier scales the result. Swapping the viewpoint of a contact swaps who takes damage, so the wrong car never takes the hit. |
| `CollisionDeduplicator` | The pair key is order independent and distinct per pair. A collision is recent inside the cooldown and expired at or after it, and a restarted clock counts as expired. One physical hit observed by both cars and the owner report is processed once; hits separated by more than the cooldown are processed separately. |
| `ScoreLedger` | Participants register once without resetting points. Awards accumulate and unknown ids are rejected. Ranking is highest first. Dealt damage rounds to points; non-positive damage and attacker-less damage (`ulong.MaxValue`) score nothing. The dedup and ledger tests together show a single collision awarding points once. |

## What the tests do not cover

Network transport, RPC ownership and physics callbacks (`ServerPhysicsCollisionHandler` feeding the calculator) are exercised through the multiplayer session checklists rather than automated tests.
