# Testing

Edit-mode tests cover the server-side collision decision logic and the movement checks. The rules are plain C# types in the `Limekicker.DamageDecision` and `Limekicker.MovementAuthority` assemblies with no networking or scene dependencies, so every case runs as a fast NUnit test in `Limekicker.DamageDecision.Tests` and `Limekicker.MovementAuthority.Tests` (`Assets/Limekicker/Tests/EditMode`).

## What the tests cover

| Type | Covered behavior |
|------|------------------|
| `CollisionDamageCalculator` | Head-on hits split damage by opposing speed (even split when both cars are stationary). A front bumper hit damages only the victim; the attacker takes nothing. An idle attacker deals no damage, and a slow attacker's damage scales with speed. Flank hits damage the slower car only with majority velocity, otherwise damage splits evenly. Damage to the other car at or below the minimum threshold is zeroed while damage to this car is kept. The damage multiplier scales the result. Swapping the viewpoint of a contact swaps who takes damage, so the wrong car never takes the hit. |
| `CollisionDeduplicator` | The pair key is order independent and distinct per pair. A collision is recent inside the cooldown and expired at or after it, and a restarted clock counts as expired. One physical hit observed by both cars and the owner report is processed once; hits separated by more than the cooldown are processed separately. |
| `ScoreLedger` | Participants register once without resetting points. Awards accumulate and unknown ids are rejected. Ranking is highest first. Dealt damage rounds to points; non-positive damage and attacker-less damage (`ulong.MaxValue`) score nothing. The dedup and ledger tests together show a single collision awarding points once. |
| `MovementRules` | Horizontal speed at the cap is accepted and above it is too fast, including a teleport across the arena. Vertical movement is not speed limited. A position outside the play volume, horizontally or vertically, is out of bounds, and out of bounds wins over speed. Without a play volume only speed is checked, and a non-positive time step is accepted. |
| `MovementViolationLog` | Violations trigger only when the limit is reached inside the time window; entries older than the window or from before a clock restart are dropped. |
