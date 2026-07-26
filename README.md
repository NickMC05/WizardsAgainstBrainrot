# 🧙 Wizards Against Brainrot
### A Unity C# Multiplayer Party Card Game | Networked Gameplay | Humor-First Design

![Unity](https://img.shields.io/badge/Unity-2022.3+-000000?style=flat&logo=unity)
![C#](https://img.shields.io/badge/C%23-111217?style=flat&logo=csharp)
![Netcode](https://img.shields.io/badge/Netcode_for_Unity-Enabled-007ACC?style=flat)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

> A chaotic, humor-driven party card game built in Unity. Players combine absurd "Spell" cards with ridiculous "Situation" prompts to create the funniest (or most brainrot) combos. Designed for 2–8 players online, with quick matches, voice chat integration, and community-driven content.

![Gameplay Preview](https://via.placeholder.com/800x400/1a1a2e/16213e?text=Wizards+Against+Brainrot+%7C+Unity+Gameplay)

---

## ✨ Features

- 🃏 **Dynamic Card System**: ScriptableObject-driven architecture for easy content expansion
- 🌐 **Networked Multiplayer**: Built with Unity Netcode for GameObjects (NGO) for reliable peer-to-peer matches
- 🎨 **Modular UI Framework**: Responsive canvas system with animated card transitions and accessibility toggles
- 🔊 **Voice Chat Integration**: Optional Vivox/Dissonance integration for chaotic group sessions
- 📦 **Content Pipeline**: JSON-based card import/export for community submissions and modding
- 🧪 **Testable Design**: Unit tests for card logic, scoring, and win conditions using NUnit

---

## 🏗️ Technical Architecture

```
Assets/
├── Scripts/
│   ├── Cards/
│   │   ├── CardData.cs           # ScriptableObject base for all cards
│   │   ├── SpellCard.cs          # Logic for "action" cards
│   │   └── SituationCard.cs      # Logic for prompt cards
│   ├── Gameplay/
│   │   ├── MatchManager.cs       # Turn order, scoring, win conditions
│   │   ├── CardDeck.cs           # Shuffle, draw, discard logic
│   │   └── ScoringSystem.cs      # Voting, points, tiebreakers
│   ├── Networking/
│   │   ├── NetworkMatch.cs       # Lobby, join, sync via NGO
│   │   ├── CardSync.cs           # RPCs for card draws/play
│   │   └── VoiceChatManager.cs   # Optional voice integration
│   └── UI/
│       ├── CardView.cs           # Visual representation + animations
│       ├── VotingPanel.cs        # Player voting UI
│       └── ResponsiveCanvas.cs   # Adaptive layout for mobile/desktop
├── Data/
│   ├── Cards/                    # JSON/ScriptableObject card definitions
│   └── Configs/                  # Match settings, balance tuning
└── Tests/
    ├── CardLogicTests.cs
    └── ScoringTests.cs
```

### Key Design Patterns
- **ScriptableObject Data-Driven Design**: Cards are data, not code—enables rapid content iteration without recompiling
- **Event-Driven Gameplay**: `CardPlayedEvent`, `VoteSubmittedEvent` decouple UI, logic, and networking
- **Authority Model**: Host-authoritative scoring to prevent cheating; clients predict locally for responsiveness
- **Localization-Ready**: All text externalized via CSV/JSON for easy translation (7+ language support pattern)

---

## 🚀 Getting Started

### Requirements
- Unity 2022.3 LTS or newer
- .NET Standard 2.1 compatibility
- Basic familiarity with Unity Netcode for GameObjects (optional for solo play)

### Setup
1. Clone this repo into your Unity `Projects/` directory
2. Open the project in Unity Hub → Editor
3. Install required packages via Package Manager:
   - `com.unity.netcode.gameobjects`
   - `com.unity.nuget.newtonsoft-json` (for card import/export)
4. Open `Scenes/MainMenu.unity` and press Play to test locally

### Quick Play Test
1. In the editor, go to `File → Build Settings` and enable `Development Build`
2. Run two instances (Editor + Standalone, or two Editor windows)
3. One player hosts a match; the other joins via LAN/localhost
4. Play a round: draw cards, submit combos, vote, and laugh

---

## 💡 Example: Adding a New Card

```csharp
// Create a new SpellCard asset via Unity Editor or script:
[CreateAssetMenu(menuName = "Cards/SpellCard")]
public class SpellCard : CardData
{
    [TextArea] public string effectDescription;
    public int chaosPoints; // Custom scoring weight

    public override void OnPlay(MatchContext context)
    {
        // Trigger VFX, apply effects, notify networking layer
        context.EventBus.Publish(new CardPlayedEvent(this, context.PlayerId));
    }
}
```

```json
// Or import via JSON (for community content):
{
  "cardId": "SPELL_042",
  "type": "Spell",
  "text": "Summon a pigeon that only speaks in memes",
  "chaosPoints": 3,
  "tags": ["animal", "meme", "chaos"]
}
```

---

## 🙏 Acknowledgements

- Inspired by the chaotic joy of party games and internet culture 🧠✨
- Built with Unity Netcode for GameObjects — thanks to the Unity multiplayer team for the robust foundation
- Card art and SFX created using Blender, Audacity, and free asset packs (see `CREDITS.md`)

> *Note: This is a fan-made, non-commercial project. All original code authored by Nicholas Wilson Kurniawan. No assets copied from proprietary games.*

---

## 🤝 Contributing

This is a passion project and learning reference. Feel free to:
- 🍴 Fork and design your own card packs
- 🐛 Report bugs or suggest balance tweaks via Issues
- 💬 Propose new mechanics in Discussions (e.g., "Team Mode", "Draft Draft")

For community submissions: please follow the `CONTRIBUTING.md` guidelines for card formatting and content policies.

---

> 💡 **Note**: This project is for educational, portfolio, and non-commercial purposes. For commercial deployment, additional licensing, content moderation, and platform compliance would be required.

*Built with clean C#, player-first design, and a healthy dose of internet absurdity.* 🃏🧙✨
