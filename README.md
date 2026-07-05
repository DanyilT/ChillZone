# 🗑 ChillZone

**Augmented-reality throwing game — throw a ball into the bin, beat your best score, unlock new balls and baskets.**

[![Get it on Google Play](https://img.shields.io/badge/Google%20Play-Download-414141?logo=google-play&logoColor=white)](https://play.google.com/store/apps/details?id=com.DanyT.ChillZone)
![Platform](https://img.shields.io/badge/platform-Android-3DDC84?logo=android&logoColor=white)
![Unity](https://img.shields.io/badge/Unity-2022.3.62f3-000000?logo=unity&logoColor=white)
[![Version](https://img.shields.io/github/v/release/DanyilT/ChillZone?logo=github)](https://github.com/DanyilT/ChillZone/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

> **ChillZone 2** is the ground-up **2.0** remake of ChillZone. The core game is unchanged — place a hoop on a real surface and shoot — but everything under the hood was rebuilt from scratch for real physics, unlockable content, and much cleaner, more scalable code. On the Play Store it ships as the same ChillZone (v2.x.x).

<p align="center"><img alt="ScreenshotUnity" src="https://github.com/user-attachments/assets/f6e46c76-3f83-4cdf-a87d-62c329e4510d" width="70%"> <img alt="Screenshot" src="https://github.com/user-attachments/assets/f64133f7-bd70-4ca4-b840-cd55a320da21" width="22%"></p>

---

## 📲 Download

**[Get it on Google Play →](https://play.google.com/store/apps/details?id=com.DanyT.ChillZone)**

- **Landing page:** _coming soon_ — a small marketing site is planned on a dedicated branch, hosted at **https://danyilt.github.io/ChillZone/** via GitHub Pages.

## ✅ Requirements

- **Android 11 (API 30)** or newer.
- An **[ARCore-supported device](https://developers.google.com/ar/devices)** (Google Play Services for AR).
- A reasonably lit room with a **flat horizontal surface** (floor, desk, table) and a bit of space to aim into.

---

## 🎮 How to Play

**Objective:** throw the ball into the bin. Every successful shot raises your score — chase a new best.

1. **Scan the surface.** Slowly move your device around so it can detect a flat horizontal plane.
2. **Place the basket.** Tap the detected surface to drop the hoop into the real world.
3. **Aim & throw.** The ball sits ready at the bottom of the screen — **drag it and flick to shoot**:
   - how **fast** you flick sets the **power**,
   - how far **up** you drag sets the **arc**,
   - the flick's **left/right** direction leans the shot,
   - a **curved swipe** adds spin (in *Enhanced* mode).
4. **Score.** Sink the ball in the bin to rack up points. Tap the **score** in the header to see your best result.

### Handling the basket

- **Move it** — tap and drag the basket to reposition it.
- **Delete it** — double-tap the basket to remove it.
- **Restart** — reset the scanning phase, or reset the ball. *(Resetting the ball can reset your current score if you'd already thrown it.)*
- **Pause** — freezes the game and disables the camera.

### Balls & baskets

- **Ball Picker** — choose which ball you throw.
- **Basket Picker** — choose a basket skin.
- Balls and baskets have **rarities** and are **unlocked** by hitting score milestones, earning achievements, or entering codes.

### Throw modes

Configurable in **Settings → developer options** (enter "dev" to enable):

| Mode | Behaviour |
|------|-----------|
| **Straight** | Ball always flies straight forward from the camera — no lateral or vertical influence. |
| **DragPath** | Aim follows your swipe — full lateral + vertical control. |
| **Enhanced** | DragPath **plus** curvature analysis — a curved swipe adds spin and a difficulty scoring bonus. |

---

## ✨ What's new in 2.0

A remake from scratch. The mechanics are the same as v1; almost everything else changed.

- **Real physics throws.** In v1 the ball was pinned to the screen and a "throw" just spawned a child copy that flew forward, respawning by repositioning it and toggling its visibility. **v2 actually throws a real rigidbody ball** — flick to launch it with genuine velocity, arc and spin.
- **New, lighter 3D art.** Different, **lower-poly models** and general performance optimization.
- **Unlockable content.** Balls **and** baskets are now **data-driven unlockable content** (with rarities and unlock criteria) instead of hardcoded prefabs.
- **Improved content picker.** A single reusable, code-generated picker sheet powers both the ball and basket selectors.
- **New hand-drawn icons.** A fresh set of hand-drawn UI icons (v1 was hand-drawn too — these are all-new).
- **Code-generated backgrounds.** Button and panel backgrounds (rounded rects, circles) are **generated at runtime** instead of shipping as sprites.
- **New settings UI.** A data-driven settings screen — audio, controls, credits, links, unlock codes and developer options.
- **Cleaner, scalable architecture.** Removed the old god-classes and hardcoded wiring in favor of an event-driven design, decoupled assemblies, and an achievements engine — better code and game-design practices throughout.

---

## 🚽 Built with

- **Unity 2022.3.62f3 (LTS)**
- **AR Foundation** + **ARCore** (XR Origin / AR Session)
- **Universal Render Pipeline (URP)**
- **Input System** · **TextMeshPro** · **DOTween**

### Building from source

```bash
git clone https://github.com/DanyilT/ChillZone.git
```

1. Open the project with **Unity 2022.3.62f3** (Android Build Support + OpenJDK/SDK/NDK modules).
2. Switch the build target to **Android** (`File → Build Settings → Android`).
3. Build and run on an **ARCore-capable** device.

---

## 👀 Credits

- Design, code, art & icons by **[me](https://github.com/DanyilT)**, you can [by me a coffee](https://buymeacoffee.com/danyt).
- **3D models** — from [Poly by Google](https://poly.pizza/u/Poly%20by%20Google) (CC-BY).
- **Tweening** — [DOTween](http://dotween.demigiant.com) by Demigiant.

## 📄 License

Licensed under the **MIT License** — see [LICENSE](LICENSE).
