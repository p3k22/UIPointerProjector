# UI Pointer Projector

**UI Pointer Projector** allows interaction with a Unity UI Canvas that is rendered to a **RenderTexture** and displayed on a **world-space quad**.
It projects pointer input (camera-forward ray or mouse screen ray) onto the UI, enabling buttons, sliders, and other UI elements to behave as if they were directly interacted with.

This is designed for in-world screens, terminals, computers, holograms, and similar setups.

---

## Features

* Camera-forward or mouse-based pointer projection
* Works with **RenderTexture-backed UI**
* Fully compatible with **Unity Input System**
* Accurate UV mapping for **scaled / rotated quads**
* Editor tooling for:

  * Creating projectors
  * Creating RenderTextures with depth
  * Auto-creating compatible unlit materials
  * Picking existing RenderTextures / Materials
* No custom shaders required
* HDRP and Built-in pipeline support

---

## Requirements

* Unity **2020.1+**
* **Input System** package enabled
* **UGUI** enabled

Dependencies (handled via `package.json`):

* `com.unity.ugui`
* `com.unity.inputsystem`

---

## Installation

### Via Git (recommended)

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.p3k.uipointerprojector": "https://github.com/p3k22/UIPointerProjector.git"
  }
}
```

---

## Core Concepts

### Render Projector Setup

The system consists of:

1. **UI Camera**

   * Renders UI to a `RenderTexture`
2. **Canvas**

   * `Screen Space - Camera`
   * Uses the UI Camera
3. **Quad**

   * Displays the RenderTexture
   * Has a `BoxCollider`
   * Has `UIPointer` attached

The pointer ray hits the quad, converts the hit to UV space, maps that to the RenderTexture, and feeds it into Unity’s UI event system.

---

## Editor Utilities

### Create UI Render Projector

Menu:

```
GameObject → UIRenderProjector
```

Creates:

* Root object
* UI Camera
* Canvas (configured correctly)
* EventSystem (if missing)
* Optional RenderTexture assignment

---

### Create Pointer Projector Quad

Menu:

```
GameObject → UIPointerProjector
```

Creates:

* Quad (properly scaled)
* BoxCollider (trigger, correct depth)
* `UIPointer` component
* Prompts for material assignment

---

### Create RenderTexture + Material

Menu:

```
Assets → Create → UIPointerProjector → RenderTexture + Unlit Material
```

Creates:

* RenderTexture with **depth buffer**
* Compatible unlit material
* HDRP or Built-in shader automatically selected

---

## Runtime Usage

### UIPointer Component

Attach `UIPointer` to the quad displaying the RenderTexture.

Required:

* `BoxCollider` on the same GameObject
* Reference to the **Render Projector Parent**

  * This is the object containing the UI Camera + Canvas

#### Pointer Modes

```csharp
public enum PointerMode
{
    CameraForward,
    MouseScreen
}
```

* **CameraForward**

  * Uses `pointerCamera.transform.forward`
  * Ideal for FPS / VR / diegetic screens
* **MouseScreen**

  * Uses screen mouse position
  * Useful for debugging or hybrid input

---

### Activating the Pointer

```csharp
uiPointer.Activate(
    UIPointer.PointerMode.CameraForward,
    playerCamera
);
```

Deactivate:

```csharp
uiPointer.Deactivate();
```

---

## Input Handling Details

* Uses **Physics.Raycast** against the quad’s collider
* Converts hit point → local space → UV
* UV mapped to RenderTexture pixel space
* Feeds results into `GraphicRaycaster`
* Manually dispatches:

  * Pointer Enter / Exit
  * Pointer Down / Up
  * Click events

This avoids relying on `RaycastHit.textureCoord`, which is unreliable for `BoxCollider`.

---

## Debugging

### Context Menu (UIPointer)

* `Debug/Activate Pointer`
* `Debug/Deactivate Pointer`
* `Debug/Assign Pointer Camera`

### Inspector

* Enable **Debug Draw Ray** to visualize pointer rays

---

## Common Pitfalls

### Buttons only respond on part of the quad

* Ensure:

  * BoxCollider size matches quad scale
  * Quad is not non-uniformly scaled after setup
  * UI Camera has correct RenderTexture assigned

---

### RenderTexture warning about depth buffer

Your RenderTexture **must have a depth buffer**.

This package automatically creates RenderTextures with:

* `D32_SFloat` depth format

---

### Input not working

* Ensure **Input System** is enabled
* Ensure an **EventSystem** exists
* Ensure Canvas has a **GraphicRaycaster**
* Ensure `CanvasGroup.blocksRaycasts = true` while active

---



## Author

**P3k**
[https://github.com/p3k22](https://github.com/p3k22)
