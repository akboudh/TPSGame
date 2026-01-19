# TPS Game - Third-Person Shooter

A third-person shooter game built with Unity 6, featuring player movement, combat mechanics, enemy AI, and a complete UI system.

## 🎮 Features

- **Third-Person Controls**: Smooth character movement and camera rotation
- **Combat System**: Player shooting mechanics with impact effects
- **Enemy AI**: Intelligent enemies with both melee and ranged combat capabilities
- **Health System**: Player and enemy health management
- **UI System**: 
  - Crosshair targeting
  - Hitmarker feedback
  - Damage flash effects
  - Debug UI for development
- **Universal Render Pipeline (URP)**: Modern rendering pipeline for optimal performance

## 🛠️ Technologies

- **Unity Version**: 6000.3.4f1 (Unity 6)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Input System**: Unity's new Input System
- **Platform**: WebGL (published on Unity Play)

## 📋 Requirements

- Unity 6.0.3 or later
- Universal Render Pipeline package
- Input System package

## 🚀 Getting Started

### Prerequisites

1. Install [Unity Hub](https://unity.com/download)
2. Install Unity 6.0.3 or later through Unity Hub

### Setup

1. Clone the repository:
   ```bash
   git clone git@github.com:akboudh/TPSGame.git
   ```

2. Open the project in Unity:
   - Open Unity Hub
   - Click "Add" and select the cloned project folder
   - Unity will import all assets and dependencies

3. Open the main scene:
   - Navigate to `Assets/Scenes/SampleScene.unity`
   - Press Play to test the game

## 🎯 Controls

- **Movement**: WASD or Arrow Keys
- **Camera**: Mouse movement
- **Shoot**: Left Mouse Button
- **Aim**: Mouse look

## 📦 Project Structure

```
Assets/
├── Animations/          # Character animations (Idle, Walk, Run, Shoot)
├── Models/              # 3D models
├── Scripts/             # Game scripts
│   ├── PlayerMovement.cs
│   ├── PlayerShooting.cs
│   ├── PlayerHealth.cs
│   ├── EnemyAI.cs
│   ├── EnemyCombatAI.cs
│   ├── EnemyRangedAI.cs
│   └── ...
├── Scenes/              # Unity scenes
├── Settings/            # URP and render settings
└── Resources/           # Game resources
```

## 🏗️ Building

### WebGL Build

1. Go to `File > Build Settings`
2. Select `WebGL` platform
3. Click `Build` and choose an output folder
4. The build output can be deployed to any web server or Unity Play

### Desktop Build

1. Go to `File > Build Settings`
2. Select your target platform (Windows, macOS, Linux)
3. Click `Build`

## 🌐 Play Online

The game is published on Unity Play. You can play it directly in your browser!

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is open source and available for educational purposes.

## 👤 Author

**Akshat Boudh**
- GitHub: [@akboudh](https://github.com/akboudh)

## 🙏 Acknowledgments

- Unity Technologies for the game engine
- Free assets used in the project

---

**Note**: This repository contains only the source code. Build outputs and Unity-generated files are excluded via `.gitignore`.
