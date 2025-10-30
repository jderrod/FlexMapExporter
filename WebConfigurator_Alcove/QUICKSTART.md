# 🚀 Quick Start - Door Configurator

## The Fastest Way to See It Working

### Step 1: Open PowerShell in This Folder

Right-click in the `WebConfigurator` folder and select:
- "Open in Terminal" or
- "Open PowerShell window here"

### Step 2: Run the Start Script

```powershell
.\start.ps1
```

**That's it!** The configurator will:
1. Start a local web server on port 8000
2. Automatically open in your browser
3. Load the 3D door model from FlexMapTest4

---

## Alternative Methods

### If Python isn't installed:

**Option A: VS Code**
1. Open this folder in VS Code
2. Install "Live Server" extension
3. Right-click `index.html` → "Open with Live Server"

**Option B: Node.js**
```bash
npm install -g http-server
http-server -p 8000
```

Then open: `http://localhost:8000`

---

## What You'll See

🎨 **Beautiful 3D viewer** with orbit controls  
🎛️ **5 parameter sliders** for dimensions and options  
📊 **Real-time updates** - changes apply instantly  
📈 **Stats panel** showing geometry info  

---

## Try It Out!

1. **Drag with left mouse** to rotate the door
2. **Scroll** to zoom in/out
3. **Move the sliders** to change:
   - Door height
   - Floor clearance
   - Wall mounting options
   - Swing direction

4. **Click "Reset"** to go back to defaults

---

## Troubleshooting

**"Cannot access file" error?**
- You need a web server (can't open `index.html` directly)
- Run `start.ps1` or use one of the alternative methods

**Blank screen?**
- Check browser console (F12) for errors
- Verify JSON files exist in `../Desktop/FlexMapTest4/`
- Make sure paths in `js/main.js` are correct

**Poor performance?**
- Try a different browser (Chrome recommended)
- Close other tabs/applications
- Check stats panel - should be 60 FPS

---

**Ready?** Run `.\start.ps1` and enjoy! 🎉
