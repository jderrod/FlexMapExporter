# Flex-Map Exporter - System Architecture

## 🏗️ High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    REVIT 2026                               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │            Family Document (.rfa)                    │   │
│  │  • Parameters (Height, Width, etc.)                  │   │
│  │  • Formulas (Height = Base + Offset)                 │   │
│  │  • Geometry (Solids, Meshes)                         │   │
│  └────────────────────┬─────────────────────────────────┘   │
│                       │                                      │
│                       v                                      │
│  ┌──────────────────────────────────────────────────────┐   │
│  │      FlexMapExporter Add-In (IExternalCommand)       │   │
│  │                                                        │   │
│  │  [User clicks button in Add-Ins tab]                 │   │
│  └────────────────────┬─────────────────────────────────┘   │
└────────────────────────┼──────────────────────────────────────┘
                         │
                         v
        ┌────────────────────────────────┐
        │     ExportDialog (WPF)         │
        │  • Folder selection            │
        │  • Progress display            │
        │  • Status updates              │
        └────────────────┬───────────────┘
                         │
                         v
        ┌────────────────────────────────┐
        │  FlexMapExporterCommand        │
        │  • Orchestrates workflow       │
        │  • Manages transactions        │
        └────────┬───────────────────────┘
                 │
     ┌───────────┼───────────┐
     │           │           │
     v           v           v
┌─────────┐ ┌──────────┐ ┌──────────┐
│Geometry │ │Parameter │ │  JSON    │
│Snapshot │ │ Flexer   │ │Exporter  │
└─────────┘ └──────────┘ └──────────┘

                 │
                 v
        ┌────────────────┐
        │ Output Package │
        │ • JSON config  │
        │ • Mesh files   │
        └────────────────┘
```

## 🔄 Execution Flow

```
START
  │
  ├─> 1. Validate Family Document
  │     ├─> If Project: Error & Exit
  │     └─> If Family: Continue
  │
  ├─> 2. Show Export Dialog
  │     ├─> Select output folder
  │     ├─> User confirms
  │     └─> Continue
  │
  ├─> 3. Collect Parameters
  │     ├─> FamilyManager.Parameters
  │     ├─> Extract formulas
  │     ├─> Parse dependencies
  │     └─> Store in ParameterData[]
  │
  ├─> 4. Capture Baseline Geometry
  │     ├─> FilteredElementCollector
  │     ├─> Extract solids & meshes
  │     ├─> Store bounding boxes
  │     └─> Create GeometrySnapshot
  │
  ├─> 5. For Each Parameter:
  │     │
  │     ├─> Start TransactionGroup
  │     │
  │     ├─> Start Transaction
  │     │     ├─> Change parameter value (+10%)
  │     │     ├─> Document.Regenerate()
  │     │     └─> Commit
  │     │
  │     ├─> Capture Flexed Geometry
  │     │     └─> New GeometrySnapshot
  │     │
  │     ├─> Compare Snapshots
  │     │     ├─> Bounding box deltas
  │     │     ├─> Vertex count changes
  │     │     ├─> Element additions/removals
  │     │     └─> Classify effects
  │     │
  │     └─> Rollback TransactionGroup
  │           └─> Restore original state
  │
  ├─> 6. Export Geometry Meshes
  │     ├─> For each element
  │     ├─> Extract vertices
  │     └─> Write to JSON/GLB
  │
  ├─> 7. Export JSON Configuration
  │     ├─> Build parameter array
  │     ├─> Build geometry array
  │     ├─> Build influence mappings
  │     └─> Write to {Family}_config.json
  │
  └─> 8. Show Success Dialog
        └─> Display statistics
END
```

## 📦 Class Diagram

```
┌─────────────────────────────────────┐
│   FlexMapExporterCommand            │
│   (IExternalCommand)                │
├─────────────────────────────────────┤
│ + Execute(commandData, ...)         │
│ - CollectParameters()               │
│ - ParseFormulaDependencies()        │
└──────────┬──────────────────────────┘
           │ uses
           │
    ┌──────┴──────┬──────────┬──────────┐
    │             │          │          │
    v             v          v          v
┌───────┐   ┌─────────┐ ┌────────┐ ┌────────┐
│Export │   │Geometry │ │Param   │ │JSON    │
│Dialog │   │Snapshot │ │Flexer  │ │Exporter│
└───────┘   └─────────┘ └────────┘ └────────┘
    │            │          │          │
    │            │          │          │
    v            v          v          v
┌───────┐   ┌─────────┐ ┌────────┐ ┌────────┐
│       │   │Element  │ │Param   │ │FlexMap │
│       │   │Geometry │ │Influence│ │Config  │
│       │   │         │ │        │ │        │
│       │   ├─────────┤ ├────────┤ ├────────┤
│       │   │SolidData│ │        │ │Param   │
│       │   │MeshData │ │        │ │Config  │
│       │   │         │ │        │ │        │
│       │   │Comparison│         │ │Geometry│
│       │   │Result   │         │ │Config  │
│       │   │         │         │ │        │
│       │   │Geometry │         │ │Influence│
│       │   │Change   │         │ │Config  │
└───────┘   └─────────┘ └────────┘ └────────┘
```

## 🔍 GeometrySnapshot Detail

```
GeometrySnapshot
├─ CaptureAll()
│  ├─ FilteredElementCollector
│  ├─ For each element:
│  │  ├─ element.get_Geometry(options)
│  │  ├─ ProcessGeometryObject()
│  │  │  ├─ If Solid → CaptureSolid()
│  │  │  ├─ If Mesh → CaptureMesh()
│  │  │  └─ If Instance → Recurse
│  │  └─ Store in _geometryData[elementId]
│  └─ Return
│
├─ CompareTo(otherSnapshot)
│  ├─ Find added elements
│  ├─ Find removed elements
│  ├─ For common elements:
│  │  └─ CompareElementGeometry()
│  │     ├─ Compare bounding boxes
│  │     │  ├─ Dimension deltas → Scale
│  │     │  └─ Position deltas → Translation
│  │     ├─ Compare vertex counts
│  │     │  └─ Changes → Topology
│  │     └─ Return GeometryChange
│  └─ Return ComparisonResult
│
└─ ExportMeshes()
   ├─ For each element:
   │  ├─ Extract all vertices
   │  ├─ Convert to JSON/GLB
   │  └─ Write to file
   └─ Return
```

## 🧪 ParameterFlexer Detail

```
ParameterFlexer
│
├─ AnalyzeAllParameters()
│  │
│  └─ For each parameter:
│     │
│     ├─ FlexParameter()
│     │  │
│     │  ├─ Start TransactionGroup
│     │  │
│     │  ├─ Switch on StorageType:
│     │  │  │
│     │  │  ├─ Double → FlexDoubleParameter()
│     │  │  │  ├─ delta = value * 0.1
│     │  │  │  ├─ Set(param, value + delta)
│     │  │  │  ├─ Regenerate
│     │  │  │  ├─ Capture geometry
│     │  │  │  └─ Compare & classify
│     │  │  │
│     │  │  ├─ Integer → FlexIntegerParameter()
│     │  │  │  ├─ delta = 1
│     │  │  │  └─ (same as Double)
│     │  │  │
│     │  │  └─ String → FlexStringParameter()
│     │  │     ├─ Try: Left/Right/Yes/No...
│     │  │     └─ First valid change detected
│     │  │
│     │  ├─ ClassifyChanges()
│     │  │  │
│     │  │  ├─ Added elements → visibilityToggle
│     │  │  ├─ Removed elements → visibilityToggle
│     │  │  ├─ Dimension changes → scale[X|Y|Z]
│     │  │  ├─ Position changes → translate[X|Y|Z]
│     │  │  └─ Topology changes → mirror or topologyChange
│     │  │
│     │  └─ Rollback TransactionGroup
│     │
│     └─ Return influences[]
│
└─ Return all influences
```

## 📊 Data Flow

```
Family Document
      │
      ├─> FamilyManager
      │      │
      │      └─> Parameters
      │            ├─> Name
      │            ├─> Type
      │            ├─> Formula
      │            └─> CurrentValue
      │
      └─> GeometryElement
             │
             ├─> Solids
             │     ├─> Faces → Triangulate → Vertices
             │     ├─> Volume
             │     └─> BoundingBox
             │
             └─> Meshes
                   └─> Triangles → Vertices

                    ↓ [TRANSFORM]

              JSON Configuration
                    │
                    ├─> parameters[]
                    │     ├─> name
                    │     ├─> type
                    │     ├─> formula
                    │     ├─> dependencies
                    │     └─> range
                    │
                    └─> geometry[]
                          ├─> elementId
                          ├─> meshFile
                          └─> influences[]
                                ├─> parameter
                                └─> effect
```

## 🎛️ Effect Classification Logic

```
Comparison Result
      │
      ├─> Added Elements
      │     └─> Effect: "visibilityToggle"
      │
      ├─> Removed Elements
      │     └─> Effect: "visibilityToggle"
      │
      └─> Modified Elements
            │
            ├─> BBox.Max.X - BBox.Min.X changed?
            │     └─> Effect: "scaleX"
            │
            ├─> BBox.Max.Y - BBox.Min.Y changed?
            │     └─> Effect: "scaleY"
            │
            ├─> BBox.Max.Z - BBox.Min.Z changed?
            │     └─> Effect: "scaleZ"
            │
            ├─> BBox.Min.X changed?
            │     └─> Effect: "translateX"
            │
            ├─> BBox.Min.Y changed?
            │     └─> Effect: "translateY"
            │
            ├─> BBox.Min.Z changed?
            │     └─> Effect: "translateZ"
            │
            └─> Vertex count changed?
                  │
                  ├─> If position changed (no scale)
                  │     └─> Effect: "mirror[X|Y|Z]"
                  │
                  └─> Else
                        └─> Effect: "topologyChange"
```

## 🔐 Transaction Management

```
TransactionGroup: "Flex {Parameter}"
      │
      └─> Start()
            │
            ├─> Transaction: "Flex [Type]"
            │     │
            │     ├─> Start()
            │     ├─> FamilyManager.Set(param, newValue)
            │     ├─> Document.Regenerate()
            │     └─> Commit()
            │
            ├─> [Capture & Compare Geometry]
            │
            └─> RollBack()  ← Restore original state
                  └─> Family unchanged after analysis!
```

## 📤 Export Strategy

```
Output Folder
│
├─── {FamilyName}_config.json
│      {
│        family: "...",
│        revitVersion: "2026",
│        parameters: [...],
│        geometry: [...]
│      }
│
└─── geometry/
       ├─── {FamilyName}_{ElementId_1}.json
       │      { vertices: [[x,y,z], ...] }
       │
       ├─── {FamilyName}_{ElementId_2}.json
       └─── {FamilyName}_{ElementId_N}.json
```

## 🧩 Key Design Patterns

### 1. Snapshot Pattern
**GeometrySnapshot** captures state for later comparison
- Baseline before changes
- Flexed after changes
- Compare to detect deltas

### 2. Command Pattern
**IExternalCommand** encapsulates the export operation
- Single responsibility
- Invoked by Revit UI
- Returns Result enum

### 3. Builder Pattern
**JsonExporter** builds complex JSON structure
- AddParameters()
- AddGeometry()
- WriteToFile()

### 4. Strategy Pattern
**ParameterFlexer** uses different flex strategies
- FlexDoubleParameter()
- FlexIntegerParameter()
- FlexStringParameter()

## ⚙️ Configuration Points

| Component | Configuration | Location |
|-----------|--------------|----------|
| Flex Delta | `0.1` (10%) | ParameterFlexer.cs |
| Tolerance | `0.001` (~0.3mm) | GeometrySnapshot.cs |
| Options.DetailLevel | `Fine` | GeometrySnapshot.cs |
| Revit Path | Project property | .csproj |
| Export Format | JSON (→GLB) | GeometrySnapshot.cs |

## 🎯 Extension Points

Want to customize? Here are the key extension points:

1. **Custom Effect Detection**
   - `ParameterFlexer.ClassifyChanges()`
   - Add your own effect types

2. **Different Flex Strategy**
   - `ParameterFlexer.FlexDoubleParameter()`
   - Change delta calculation

3. **Advanced Geometry Export**
   - `GeometrySnapshot.ExportMeshes()`
   - Add GLB export library

4. **UI Customization**
   - `ExportDialog.xaml`
   - Add more controls/options

5. **JSON Schema Extension**
   - `JsonExporter.cs`
   - Add custom metadata fields

---

**This architecture supports:**
- ✅ Automatic parameter discovery
- ✅ Non-destructive testing (rollback)
- ✅ Multi-effect detection per parameter
- ✅ Formula dependency tracking
- ✅ Extensible export format
- ✅ Ready for web configurator integration
