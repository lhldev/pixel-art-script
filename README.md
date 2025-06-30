# 🎨  Pixel Art Script

A script designed to create pixel art in donation-based painting game.

![giffy](https://media3.giphy.com/media/v1.Y2lkPTc5MGI3NjExb3Z2Z2Y4cWcydnl3ZDlvYmNpbDRxcDE4bTMzcXdvZXhtam0yc3M4dyZlcD12MV9pbnRlcm5hbF9naWZfYnlfaWQmY3Q9Zw/DI3T85eGnr4O7Bwd73/giphy.gif)

---

### How to compile
Requires .Net 9.0.
```bash
dotnet run
```

---

### How to use
1. Compile from source and run.
2. Follow the on-screen instructions.
3. Use the following keyboard shortcuts when painting:
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
*If the script is painting too quick, increas the WaitTime for smoother performance.*

---

### Discalmer
This script is provided for **educational and experimental purposes only**.  
I do **not** endorse or promote cheating, exploitation, or any activity that violates the terms of service of any game.  
Use at your own discretion and risk.

---

### License
This project is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
