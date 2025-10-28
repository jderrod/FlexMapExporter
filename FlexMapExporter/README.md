# Flex-Map Exporter for Revit 2026

A powerful Revit add-in that automatically analyzes family parameters and exports complete configurator data for browser-based 3D configuration.

## 🎯 Features

- **Automatic Parameter Discovery**: No manual labeling required - the tool automatically discovers which parameters affect which geometry
- **Smart Effect Classification**: Detects scale, translation, mirroring, visibility toggles, and topology changes
- **Formula Parsing**: Extracts parameter dependencies from formulas
- **Complete Export Package**: Generates JSON configuration + geometry meshes ready for three.js
- **Non-Destructive**: Uses transaction rollback to test parameters without permanently modifying the family

## 📋 Requirements

- Autodesk Revit 2026
- .NET 8 SDK
- Windows 10/11

## 🔧 Installation

### Option 1: Build from Source

1. Open the project in Visual Studio 2022 or later
2. Restore NuGet packages:
   ```
   dotnet restore
   ```
3. Build the project (Release or Debug):
   ```
   dotnet build --configuration Release
   ```
4. The build process will automatically copy the DLL and manifest to:
   ```
   %AppData%\Autodesk\Revit\Addins\2026\
   ```

### Option 2: Manual Installation

1. Build the project
2. Copy `FlexMapExporter.dll` to `%AppData%\Autodesk\Revit\Addins\2026\`
3. Copy `FlexMapExporter.addin` to the same folder
4. Restart Revit

## 🚀 Usage

1. **Open a Family Document** in Revit 2026
   - The tool only works on Family Documents (`.rfa` files)
   - It will not work on project files

2. **Run the Command**
   - Go to the Add-Ins tab
   - Click "Flex-Map Exporter"

3. **Select Output Folder**
   - Choose where to save the export package
   - Default location: `Documents\FlexMapExport`

4. **Wait for Analysis**
   - The tool will:
     - Capture baseline geometry
     - Test each parameter by flexing it
     - Compare geometry changes
     - Classify effects
     - Export meshes and JSON

5. **Review Output**
   - `{FamilyName}_config.json` - Complete configuration mapping
   - `geometry/` folder - Individual mesh files for each element

## 📤 Export Schema

The exported JSON follows this structure:

```json
{
  "family": "Door_Panel",
  "revitVersion": "2026",
  "parameters": [
    {
      "name": "Height",
      "storageType": "Double",
      "unit": "SpecTypeId.Length",
      "formula": "TopOffset + BottomOffset + 1800",
      "dependencies": ["TopOffset", "BottomOffset"],
      "range": [1800, 2400]
    }
  ],
  "geometry": [
    {
      "elementId": 12345,
      "category": "GenericModel",
      "meshFile": "DoorPanel_12345.json",
      "influences": [
        { "parameter": "Height", "effect": "scaleY" },
        { "parameter": "HingeSide", "effect": "mirrorX" }
      ]
    }
  ]
}
```

## 🧪 Effect Types

The tool detects and classifies these effect types:

- **Scale Effects**: `scaleX`, `scaleY`, `scaleZ`
- **Translation Effects**: `translateX`, `translateY`, `translateZ`
- **Mirror Effects**: `mirrorX`, `mirrorY`, `mirrorZ`
- **Visibility**: `visibilityToggle`
- **Topology**: `topologyChange` (complex deformations)

## 🏗️ Architecture

### Core Classes

- **FlexMapExporterCommand**: Main IExternalCommand implementation
- **GeometrySnapshot**: Captures and compares element geometry
- **ParameterFlexer**: Performs flex tests and classifies effects
- **JsonExporter**: Generates JSON in the specified schema
- **ExportDialog**: WPF UI for output selection and progress

### How It Works

1. **Capture Baseline**: Snapshot all geometry in initial state
2. **Flex Each Parameter**: 
   - Create TransactionGroup for rollback
   - Change parameter by delta (10% for numbers)
   - Regenerate document
   - Capture new geometry
   - Compare with baseline
   - Rollback changes
3. **Classify Effects**: Analyze bounding box, vertex, and topology changes
4. **Export**: Write JSON + geometry files

## 🔍 Troubleshooting

### "This command only works in Family Documents"
- Make sure you've opened a `.rfa` file, not a `.rvt` project file

### "Failed to create output folder"
- Check that you have write permissions to the selected folder
- Try selecting a different location

### Parameters not being detected
- Ensure parameters are not read-only
- Check that parameters have valid values
- Parameters with formulas are analyzed but not flexed (they're derived)

### Geometry not exporting
- Verify elements have actual 3D geometry
- Check that elements are not in hidden categories
- Try setting DetailLevel to Fine in your view

## 📝 Notes

- **Performance**: Analysis time depends on parameter count and geometry complexity
- **Formula Parsing**: Simple string matching - may not catch all dependencies
- **Mesh Export**: Currently exports as JSON (convert to GLB for production)
- **Revit API Path**: Update `.csproj` if Revit is installed in non-default location

## 🛠️ Customization

### Change Flex Delta

In `ParameterFlexer.cs`, modify:
```csharp
double delta = currentValue.Value * 0.1; // 10% flex
```

### Add Custom Effect Classification

In `ParameterFlexer.ClassifyChanges()`, add your logic:
```csharp
if (/* your condition */)
{
    influences.Add(new ParameterInfluence
    {
        Effect = "customEffect"
    });
}
```

### Modify Revit API Path

In `FlexMapExporter.csproj`:
```xml
<RevitAPIPath>C:\Your\Custom\Path</RevitAPIPath>
```

## 📄 License

This is a development tool. Ensure compliance with Autodesk's developer terms.

## 🤝 Contributing

This is Phase 1 of the Flex-Map system. Phase 2 will include:
- Browser-based configurator (three.js)
- Real-time parameter manipulation
- GLB export with proper materials
- Advanced topology analysis

## 💡 Tips

- Test on simple families first to understand the output
- Parameters with Yes/No types work best as visibility toggles
- Formulas create automatic dependencies
- Use meaningful parameter names for better JSON readability
