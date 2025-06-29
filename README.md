# Unity Multiplayer Enemy Spawner (with Time-of-Day Logic)

This is a small multiplayer demo built using Unity’s Netcode for GameObjects. It showcases real-time stat syncing, time-of-day visuals, and dynamic enemy spawning in a networked environment.

Enemies spawn with randomized stats and colors that depend on the time of day (Morning, Afternoon, or Night), and everything stays in sync between clients. It’s simple, but it covers several core multiplayer concepts.

---

## What You Can Do

- Host, join as a client, or start a server — all from in-game UI buttons
- Watch the environment change based on the randomly selected time of day
- See enemies spawn at specific points with different behavior depending on their type and the current time
- View stat labels (HP, ATK, SPD) that update live across the network
- Click the buttons multiple times to restart the session — it shuts down the current network mode and starts it again after a short delay

---

## Project Layout

Assets/
├── Scripts/
│ ├── GameManager.cs # Picks time of day, controls environment & enemy spawning
│ ├── Enemy.cs # Networked enemy logic, stat syncing, label display
│ ├── EnemyType.cs # ScriptableObject blueprint for enemy data
│ └── NetworkManagerUI.cs # Simple UI logic for restarting host/client/server
├── ScriptableObjects/ # Enemy types (customizable in the editor)
├── Prefabs/
│ └── EnemyBase.prefab # Enemy prefab with Netcode components + label
├── Scenes/
│ └── MainScene.unity # Entry scene

---

## How to Try It Out

1. Open the project in Unity (2022.3+ recommended)
2. Press Play in the editor
3. Use the buttons on the screen to:
   - Start as Host (server + client)
   - Start as Client (connect to existing host)
   - Start as Server (starts as a dedicated server with no visuals. This is why you see _"Display 1: No cameras rendering"_ — it's expected because no local client is present. Note: In this mode, UI and input are disabled. You'll need to stop play mode manually from the Unity editor to return to the menu.)

You can click the buttons multiple times. The app will automatically shut down the current network mode and restart it after a small delay. This is helpful for quickly testing different roles without reloading the scene.

---

## Behind the Scenes

### Time of Day Logic

- On spawn, the server randomly selects Morning, Afternoon, or Night.
- Each time of day changes:
  - Skybox color and lighting
  - Ground material gloss and tint
  - Enemy stats and spawn probabilities

### Enemy Spawning

- Enemies are defined as `EnemyType` ScriptableObjects.
- When spawning, the server clones a type and mutates its stats.
- Some enemies get buffs (like Archers in the Morning or Grunts in the Afternoon), and some are excluded (Assassins don’t appear during the day).

### Syncing Across Clients

- Enemy stats (`hp`, `attack`, `speed`, `name`, and `color`) are sent using `NetworkVariable<T>`.
- All clients update their UI labels and visuals based on synced data.
- Even the label color (white or black) is synced depending on time of day for readability.

---

## Ideas for Expanding

- Add enemy movement or basic AI
- Replace color-based visuals with actual materials or models
- Add player-controlled spawning
- Track enemy defeats, score, or player stats using synced variables

---

## License

MIT — feel free to use, remix, and build on this however you'd like.

---

## Author

Built by [Your Name] as a small demo project for multiplayer spawning, environment variation, and ScriptableObject-driven design. Let me know if you want to collaborate or build it out into a full game.
