# Quick Start Guide

## 🚀 5-Minute Setup

### 1. Prerequisites Check
- [ ] Revit 2026 installed
- [ ] .NET 8 SDK installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- [ ] Visual Studio 2022 or later (optional, for building)

### 2. Build & Install

```powershell
# Navigate to project folder
cd C:\Users\james.derrod\Configurator\FlexMapExporter

# Restore and build
dotnet restore
dotnet build --configuration Release

# Files will auto-copy to: %AppData%\Autodesk\Revit\Addins\2026\
```

### 3. Verify Installation

1. Open Revit 2026
2. Look for "Add-Ins" tab
3. You should see "Flex-Map Exporter" button

### 4. First Export

1. **Open a test family** (e.g., `Door.rfa` or any parametric family)
2. **Click** "Flex-Map Exporter" in Add-Ins tab
3. **Select** output folder (default: `Documents\FlexMapExport`)
4. **Click** Export
5. **Wait** for analysis to complete (progress shows in dialog)

### 5. Review Output

Navigate to your output folder:
```
FlexMapExport/
├── DoorFamily_config.json    ← Parameter mappings
└── geometry/
    ├── DoorFamily_12345.json  ← Element meshes
    ├── DoorFamily_12346.json
    └── ...
```

### 6. Inspect JSON

Open `DoorFamily_config.json`:

```json
{
  "family": "DoorFamily",
  "revitVersion": "2026",
  "parameters": [
    {
      "name": "Height",
      "storageType": "Double",
      "range": [1800, 2400]
    }
  ],
  "geometry": [
    {
      "elementId": 12345,
      "category": "Doors",
      "meshFile": "DoorFamily_12345.json",
      "influences": [
        {
          "parameter": "Height",
          "effect": "scaleZ"
        }
      ]
    }
  ]
}
```

## 🎯 What to Expect

### Parameters Analysis
- **~5-30 seconds** per parameter (depends on geometry complexity)
- Parameters with formulas are cataloged but not flexed
- String parameters test common values (Left/Right, Yes/No, etc.)

### Effects Detection
The tool automatically detects:
- ✅ **Scaling** - dimension changes in X/Y/Z
- ✅ **Translation** - position changes
- ✅ **Mirroring** - flip operations
- ✅ **Visibility** - elements appearing/disappearing
- ✅ **Topology** - shape deformations

## 🧪 Test Families

Good families to test with:
1. **Simple Door** - height, width parameters
2. **Window** - mullion spacing, frame depth
3. **Furniture** - leg height, top width
4. **Structural Column** - height, section dimensions

## ⚠️ Common Issues

### "Command only works in Family Documents"
→ Open a `.rfa` file, not a `.rvt` project

### "No parameters found"
→ Family has no parametric controls (use a different family)

### "Export failed"
→ Check output folder permissions

## 📊 Understanding the Output

### Parameter Object
```json
{
  "name": "Height",           // Parameter name
  "storageType": "Double",    // Data type
  "unit": "SpecTypeId.Length",// Unit type
  "formula": "A + B",         // Formula (if any)
  "dependencies": ["A", "B"], // Used parameters
  "range": [1800, 2400]       // Suggested min/max
}
```

### Influence Object
```json
{
  "parameter": "Height",  // Which parameter
  "effect": "scaleZ"      // What it does
}
```

### Effect Types
- `scaleX`, `scaleY`, `scaleZ` - Stretching
- `translateX`, `translateY`, `translateZ` - Moving
- `mirrorX`, `mirrorY`, `mirrorZ` - Flipping
- `visibilityToggle` - Show/hide
- `topologyChange` - Complex deformation

## 🔄 Workflow Tips

### 1. Start Simple
Test on families with 5-10 parameters first

### 2. Review Influences
Check if detected effects match your expectations

### 3. Iterate
- Add more parameters
- Refine formulas
- Re-export

### 4. Prepare for Three.js
The JSON is designed for browser-based configurators:
- Each geometry element = one mesh
- Influences = transform operations
- Parameters = UI controls

## 📖 Next Steps

1. ✅ Complete first export
2. 📊 Review JSON structure
3. 🎨 Plan your configurator UI
4. 🚀 Build Phase 2 (three.js viewer)

## 💬 Need Help?

Check `README.md` for:
- Detailed architecture
- Troubleshooting
- Customization options
- API documentation

## 🎓 Learning Resources

### Revit API
- [Revit API Docs](https://www.revitapidocs.com/)
- [Building Coder Blog](https://thebuildingcoder.typepad.com/)

### Three.js (for Phase 2)
- [Three.js Docs](https://threejs.org/docs/)
- [GLTF/GLB Format](https://www.khronos.org/gltf/)

---

**Ready for Phase 2?** The next step is building a browser-based configurator that uses this JSON to dynamically rebuild and configure the family in real-time!
