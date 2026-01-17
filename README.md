# 🎨  Pixel Art Script

A script designed to create pixel art in donation-based painting game with advanced shape drawing capabilities.

![giffy](https://media3.giphy.com/media/v1.Y2lkPTc5MGI3NjExb3Z2Z2Y4cWcydnl3ZDlvYmNpbDRxcDE4bTMzcXdvZXhtam0yc3M4dyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/DI3T85eGnr4O7Bwd73/giphy.gif)

---

## Features

- **Intelligent Shape Optimization**: Uses a reverse-order greedy algorithm to optimize circle placement for better image reconstruction
- **Advanced Color Handling**: Automatically rounds colors to reduce color-switching overhead
- **Background + Shape Layers**: Paints background colors and overlays optimized circle shapes
- **Interactive Setup**: First-time setup captures UI coordinates for automated interaction

---

### How to compile
Requires .Net 9.0.
```bash
dotnet run
```

---

### How to use
1. Compile from source and run.
2. On first run, follow the on-screen instructions to capture UI coordinates:
   - Double-click on the first square (top left of canvas)
   - Double-click on the last square (bottom right of canvas)
   - Double-click on the color change icon (6th icon at the bottom)
   - Double-click on the color text input field
   - Double-click on the shape tool icon (circle icon)
   - Double-click on the close button (X button)
3. Enter the path to your image file
4. Wait for optimization to complete (the preview will be displayed)
5. Use the following keyboard shortcuts when painting:
   - Press 'p' to pause or resume.
   - Press 'r' to restart the painting process.

---

### Command-Line Arguments
- `-w <WaitTime>`  
  Specifies the delay (in milliseconds) between each action.  
  Default: 50

- `-r <RoundValue>`  
  Rounds each RGB color value to reduce time for changing color.  
  Default: 16

**Example usage:**
```bash
./starving-artist-script.exe -w 50 -r 16
```
*If the script is painting too quick, increase the WaitTime for smoother performance.*

---

### Technical Details

The script implements a sophisticated image reconstruction algorithm based on discrete shape primitives:

1. **Image Preprocessing**: Crops and resizes input images to 2048x2048, then downsamples to 32x32 grid
2. **Shape Optimization**: Uses a three-phase reverse-order greedy strategy:
   - **Phase 1**: Precomputes baseline background colors for each cell
   - **Phase 2**: Evaluates circle shapes (sizes 1-3) in reverse rendering order to handle occlusion
   - **Phase 3**: Refines background colors based on final shape placement
3. **Rendering**: Draws shapes in forward order (0-1023) with proper layering

Each cell can contain:
- A background color
- An optional circle shape (size 1 = 0.5 cell radius, size 2 = 1 cell radius, size 3 = 1.5 cell radius)
- A foreground color for the shape

---

### Discalmer
This script is provided for **educational and experimental purposes only**.  
I do **not** endorse or promote cheating, exploitation, or any activity that violates the terms of service of any game.  
Use at your own discretion and risk.

---

### License
This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
