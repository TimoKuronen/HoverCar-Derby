# HoverCar

Hover Derby is a small Unity project I built to explore multiplayer gameplay using Netcode for GameObjects (NGO), Relay, and Lobbies etc.
Players control hover cars in a compact arena, colliding with each other in demolition-derby style matches.

The project's main purpose is to learn Unity's modern networking stack, including host/client flow, player spawning, relay allocation, and lobby lifecycle management. Other elements such as gameplay and visuals are intentionally minimal to keep focus on the network architecture and clean code structure.

Built with Unity 2022.3 LTS using C#.
All assets are placeholders or AI-generated for non-commercial use.

## How to run

You need two game instances (two editor clones, or an editor plus a build).

1. **Host:** Open the game, wait for the main menu, click **Start Host**. A short join code appears on screen.
2. **Join:** On the second instance, type that code into the **Join code** field and join.
3. **Play:** When both players are in, a short intro and countdown run, then you can drive and ram. Highest score when the timer ends wins.
4. **Leave:** Use the pause or end-of-round menu to return to the main menu.

Optional: from the menu you can also browse lobbies or use quick join if a host has already started a public session.

## Documentation

Design notes for the project:

- [Architecture](docs/architecture.md)
- [Authority model](docs/authority.md)
- [Known limitations](docs/known-limitations.md)

Full index: [`docs/`](docs/README.md).