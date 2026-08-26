# Architecture

HoverCar Derby is a Unity 2022.3 LTS demolition-derby prototype focused on **Netcode for GameObjects (NGO)** and **Unity Gaming Services** (Auth, Relay, Lobbies). Networking and session design are the primary concern.

## Folder map

```
Assets/Limekicker/
├── Scenes/                 NetBootstrap, MainMenu, PlayScene, Bootstrap
├── Prefabs/                Cars, NetworkManager, HUD, collectibles (no scripts)
├── Settings/               NGO default prefabs, Input System assets
├── Scripts/
│   ├── GameFlow/           Match orchestration, score, timer, pause
│   ├── Networking/         Connection, host/client/server, UGS services
│   ├── Player/             Movement, controller, bots, spawn, input
│   ├── Car/                Car lifecycle, colors, hover physics, nitro
│   ├── DamageSystem/       Collisions, health, VFX, damage numbers
│   ├── Collectibles/       Pickups and spawner
│   ├── GameStates/         Cinematic, countdown, play, completion, IGameState
│   ├── EventBus/           Decoupled local events
│   ├── Core/               IntVariable, shared helpers, small utilities
│   ├── DI/                 VContainer lifetime scopes
│   ├── UI/
│   │   ├── Bootstrap/      Presenter composition roots (menu + play HUD)
│   │   ├── Input/          Touch driving controls
│   │   ├── Presenters/
│   │   ├── Views/
│   │   ├── Leaderboard/
│   │   └── Lobby/
│   ├── Audio/              Cues, preferences, AudioService
│   ├── Cameras/            Cinemachine, dolly intros
│   ├── Debug/              DevMenuOptions
│   └── Editor/             Custom inspectors and drawers
├── Scriptables/            Timer/countdown assets, audio cues
```

Art and VFX under `Assets/JMO Assets` and `Assets/RPGPP_LT` are third-party packs used as placeholders.

## Scenes

| Scene | Role |
|-------|------|
| `NetBootstrap` | App entry; creates singletons; auth; routes to menu or dedicated server |
| `Bootstrap` | Optional name entry |
| `MainMenu` | Host and join by code |
| `PlayScene` | Arena, GameManager, match loop |

## Runtime flow

```mermaid
flowchart TD
    A[NetBootstrap] --> B{Dedicated server?}
    B -->|Yes| C[ServerSingleton + PlayScene]
    B -->|No| D[HostSingleton + ClientSingleton]
    D --> E[UGS Auth]
    E --> F[MainMenu]
    F --> G[Host / Join by code]
    G --> H[PlayScene]
    H --> I[GameManager phases]
    I --> J[Cinematic to Countdown to Play to Completion]
```

## Naming: session managers vs match GameManager

| Name | Role |
|------|------|
| `GameManager` (PlayScene) | Match/race state machine; server-authoritative phases |
| `ClientGameManager` | Client session: Relay join, matchmaker, disconnect |
| `HostGameManager` | Host session: Relay alloc, lobby create, StartHost |
| `ServerGameManager` | Dedicated server / Multiplay path |
| `NetworkSession` | Facade — UI should call this, not singletons |

## NetworkSession facade

UI and gameplay call `NetworkSession` instead of Host/Client/Server singletons directly.

### Session surface

| Method | Purpose |
|--------|---------|
| `StartHostAsync()` | Relay + lobby metadata + StartHost |
| `StartClientViaJoinCodeAsync(code)` | Join Relay session by code |
| `LeaveGame()` / `ReturnToMainMenu()` | Host shutdown or client disconnect to MainMenu |
| `RestartCurrentMatch()` | Reload PlayScene for rematch (host NGO / client local) |

Additional session APIs (lobby browse, matchmaker join) exist in the facade but are not exposed in the menu UI.

Singletons (`HostSingleton`, `ClientSingleton`, `ServerSingleton`) are DontDestroyOnLoad and created at NetBootstrap.

## PlayScene menu MVP

| Menu | View | Presenter |
|------|------|-----------|
| Pause (resume, leave, open/close) | `PauseMenuView` | `PauseMenuPresenter` |
| Round results | `RoundResultsView` | `RoundResultsPresenter` |
| Live scoreboard | `ScoreDisplayView` | `ScoreDisplayPresenter` |

Composition root: `GameUiPresenterBootstrap` (registered in `GameLifetimeScope`). Pause opens via on-screen pause button; resume via resume button or pause toggle. Leave calls `NetworkSession.ReturnToMainMenu()`. Session actions go through `NetworkSession` only.

## MainMenu MVP

| Concern | View | Presenter |
|---------|------|-----------|
| Host / join by code | `MainMenuView` | `MainMenuPresenter` |

Composition root: `MenuUiPresenterBootstrap` (registered in `MenuLifetimeScope`). Session calls go through `NetworkSession` only.

## Key systems

| Question | Start here |
|----------|------------|
| Host / join | `MainMenuView` / `MainMenuPresenter` → `NetworkSession` → Host/Client game managers |
| Player spawn | `PlayerSpawnManager` |
| Round start/end | `GameManager` + `MatchTimerDisplaySync` + `GameStates/*` |
| Pause | `MatchPauseController` via `IGameManager.TogglePause` |
| Damage | `ServerPhysicsCollisionHandler` |
| Score | `ScoreManager` + `DamageDealtEvent` |
| PlayScene menus | `GameUiPresenterBootstrap`, pause / round-results presenters |
| Session leave / rematch | `NetworkSession.ReturnToMainMenu` / `RestartCurrentMatch` |
| Player notifications | `SessionNotifications` + `NotificationConsoleView` |
| Debug log panel | `RuntimeConsoleView` |

## Prefabs (high level)

| Prefab | Purpose |
|--------|---------|
| `NetworkManager` | NGO + Unity Transport |
| `Networking/HostManager` | Host singleton wiring |
| `Networking/ClientManager` | Client singleton wiring |
| `Hover Car_Player Variant` | Networked player car |
| `GameManager` | Match flow (NetworkBehaviour) |
| `GameUiCanvas` | HUD, pause/settings/results, scoreboard; `GameplayHudPresenter` toggles driving HUD by match state |
| `Prefabs/UI/RuntimeConsolePanel` | Error/exception log UI |
| `Prefabs/UI/NotificationConsolePanel` | Player-facing session messages |

## Demo path

**Host + Relay + join code** (2–4 players).

Related: [authority.md](authority.md)