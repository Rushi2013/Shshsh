# Car Menu UI Kit — Version 2

## Contents

- `Main_Buttons/`: 11 main menu buttons — Play, Start, Continue, Garage, Cars, Customize, Settings, Store, Coins, Exit, Credits.
- `Sub_Buttons/`: 28 garage, purchase, navigation and settings buttons, including Select, Purchase, Buy, Equip, Selected, Locked, Upgrade, Paint, Wheels, Next, Previous, Back, Close, Confirm, Cancel, Yes, No, Music, Sound, Graphics, Controls, On, Off, Low, Medium, High, Reset and Save.
- `Icons/Icon_Coins.png`: coin icon.
- `Background/MainMenu_Background.png`: the previously selected AI-generated car-game menu background.
- `Button_Preview.jpg`: overview of the buttons; not a Unity sprite.
- `IMPLEMENTATION_PROMPT.md`: ready-to-copy prompt for a Unity coding agent.

Each individual button is a transparent PNG, 3840 × 960 pixels (4K **width**, not 4K UHD 3840 × 2160). The coin icon is 3840 × 3840. The selected background is **1672 × 941**, not 4K. It is supplied at its actual resolution without upscaling.

Buttons are grouped by role: gold Play, cyan Garage, gold Store, grey-blue Settings, green confirmation, red cancellation and arrow-shaped navigation. They are original generated graphical designs made for this kit; background is AI-generated. Text is baked into the PNGs, so use without a separate text child. PNGs do not provide button logic.

## Unity setup

1. Copy the folders under your project's `Assets/UI/CarMenu/`. Select each PNG and choose Texture Type **Sprite (2D and UI)**, Sprite Mode **Single**, then Apply. Preserve aspect ratio when placing each Image.
2. Assign the images to UI Buttons, hook up real OnClick handlers and set active/interactable/disabled states through Unity. The labelled `SELECTED` and `LOCKED` images are examples, not functional state logic.
3. Add the background to a full-screen UI Image. Use an aspect-preserving crop/fit strategy for different devices and keep the car clear of overlaid buttons.
4. These source textures are large. On mobile, set platform Max Size (e.g. 1024 or 2048 after testing), compression and atlases; avoid shipping dozens of full-resolution textures without memory profiling.
5. This is visual art only, not a full menu, car garage, purchase system or Unity package. See `IMPLEMENTATION_PROMPT.md`.

Button text is rasterized using DejaVu Sans Bold; font licence information: https://dejavu-fonts.github.io/License.html.
