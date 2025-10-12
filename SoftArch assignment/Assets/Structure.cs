//DungeonCrawler
//├─ Core
//│  ├─ Events              // IEventBus, domain event definitions
//│  ├─ Services            // IGameStateService, ITimeProvider, IAssetLoader, SaveService (if used)
//│  └─ Utils               // small helpers, ID generation, math/extension methods
//│  └─Pooling              // If needed. (for now disconnected)
//├─ Gameplay
//│  ├─ Player
//│  │  ├─ Controller      // First-person controller adapter, view/controller glue
//│  │  └─ Data            // PlayerState, Health, Stamina DTOs
//│  ├─ Enemy
//│  │  ├─ Types           // Enemy archetype data (ScriptableObjects)
//│  │  └─ Logic           // shared enemy logic, common interfaces (IEnemy, IAttackable)
//│  ├─ Boss
//│  │  ├─ StateMachine    // boss states, transitions, state interfaces
//│  │  └─ Data            // boss-specific data, phase configs
//│  ├─ Combat
//│  │  └─ Rules           // damage calculation, resistances, status effects
//│  ├─ Items
//│  │  └─ Data            // item templates (ScriptableObjects)
//│  └─ Inventory
//│     └─ Model           // Inventory model, item stacks, equip logic
//├─ Systems
//│  ├─ Spawning           // SpawnManager, spawn points, spawn tables, spawn rules
//│  ├─ AISystem           // AI tick orchestration for non-boss enemies
//│  ├─ CombatSystem       // subscribes to attack events; resolves outcome via Combat.Rules
//│  └─ GameFlow           // GameStateManager (MainMenu, Playing, Paused, GameOver)
//├─ Levels
//│  ├─ Scenes             // scene-specific data (level descriptor SOs), scene bootstrapper
//│  └─ Runtime            // per-scene runtime helpers (spawn point registries, nav meshes)
//├─ UI
//│  ├─ HUD                // crosshair, health/stamina bars
//│  └─ Screens            // pause, inventory screen, game over
//├─ Input
//│  └─ FirstPerson        // input mapping, abstraction (IInputService) -> actions (Move/Look/Fire)
//├─ Audio
//│  └─ Runtime            // AudioService, SFX/Music events
//├─ Persistence
//│  └─ DTOs               // save DTOs (PlayerStateDTO, LevelStateDTO) — if I save
//├─ Editor                // editor-only tools (in Editor assembly / folder)
//└─ Tools                 // tools that help building the level

