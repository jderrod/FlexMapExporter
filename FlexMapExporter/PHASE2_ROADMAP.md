# Phase 2 Roadmap: Browser-Based Configurator

Now that Phase 1 (Revit Add-In) is complete, Phase 2 will build the three.js-based configurator.

## 🎯 Objective

Create a browser-based 3D configurator that:
- Loads the exported JSON + geometry
- Displays the family in real-time 3D
- Provides UI controls for all parameters
- Procedurally rebuilds geometry based on parameter changes
- **No Revit round-trip required**

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│  Three.js Scene                         │
│  ┌─────────────────────────────────┐   │
│  │  Family Geometry                │   │
│  │  (GLB meshes loaded)            │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │  Transform Controller           │   │
│  │  • Scale handlers               │   │
│  │  • Translation handlers         │   │
│  │  • Mirror handlers              │   │
│  │  • Visibility handlers          │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
          ↑
          │ Parameter changes
          │
┌─────────────────────────────────────────┐
│  UI Panel (React/Vue/Vanilla)           │
│  • Sliders for numeric params          │
│  • Dropdowns for string params         │
│  • Formula evaluation engine           │
│  • Dependency resolver                 │
└─────────────────────────────────────────┘
```

## 📋 Core Components

### 1. Geometry Loader
```javascript
class FamilyLoader {
  async loadFamily(configPath) {
    // Load JSON config
    // Load all GLB meshes
    // Build scene hierarchy
  }
}
```

### 2. Parameter Manager
```javascript
class ParameterManager {
  constructor(config) {
    this.parameters = new Map();
    this.dependencies = buildDependencyGraph(config);
  }
  
  setValue(paramName, value) {
    // Update parameter
    // Resolve dependent formulas
    // Trigger geometry updates
  }
  
  evaluateFormula(formula, context) {
    // Parse and evaluate formula
    // Handle parameter references
  }
}
```

### 3. Transform Controller
```javascript
class TransformController {
  applyInfluence(mesh, influence, paramValue) {
    switch(influence.effect) {
      case 'scaleX':
        mesh.scale.x = paramValue / baseValue;
        break;
      case 'mirrorX':
        mesh.scale.x *= -1;
        break;
      case 'visibilityToggle':
        mesh.visible = (paramValue === 'Yes');
        break;
      // ... etc
    }
  }
}
```

### 4. UI Builder
```javascript
class ConfiguratorUI {
  buildControls(parameters) {
    parameters.forEach(param => {
      if (param.storageType === 'Double') {
        this.addSlider(param);
      } else if (param.options) {
        this.addDropdown(param);
      }
    });
  }
}
```

## 🛠️ Technology Stack

### Core
- **Three.js** - 3D rendering engine
- **GLTFLoader** - Load exported geometry
- **OrbitControls** - Camera navigation

### UI Framework (Choose One)
- **Option A: Vanilla JS** - Lightweight, no dependencies
- **Option B: React** - Component-based, great for complex UIs
- **Option C: Vue** - Simple, reactive

### Additional Libraries
- **math.js** - Formula evaluation
- **dat.GUI** - Quick parameter controls (dev mode)
- **lil-gui** - Modern alternative to dat.GUI

## 📁 Project Structure

```
configurator/
├── index.html
├── src/
│   ├── main.js                 # Entry point
│   ├── FamilyLoader.js         # Loads JSON + meshes
│   ├── ParameterManager.js     # Manages parameter state
│   ├── TransformController.js  # Applies transformations
│   ├── FormulaEngine.js        # Evaluates parameter formulas
│   ├── UI/
│   │   ├── ConfiguratorPanel.js
│   │   ├── ParameterControl.js
│   │   └── ExportButton.js
│   └── utils/
│       ├── DependencyGraph.js
│       └── MathHelpers.js
├── assets/
│   └── families/
│       └── Door_Panel/
│           ├── Door_Panel_config.json
│           └── geometry/
│               ├── Door_Panel_12345.glb
│               └── Door_Panel_12346.glb
└── package.json
```

## 🔧 Implementation Steps

### Step 1: Basic Three.js Setup
```javascript
// Initialize scene
const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(75, width/height, 0.1, 1000);
const renderer = new THREE.WebGLRenderer();

// Add lights, controls
const controls = new OrbitControls(camera, renderer.domElement);
```

### Step 2: Load Family Data
```javascript
const loader = new FamilyLoader();
const family = await loader.loadFamily('./assets/Door_Panel_config.json');
```

### Step 3: Build Parameter UI
```javascript
const ui = new ConfiguratorUI(family.parameters);
ui.mount('#controls');

ui.on('parameter-change', (name, value) => {
  paramManager.setValue(name, value);
});
```

### Step 4: Apply Transformations
```javascript
paramManager.on('update', () => {
  family.geometry.forEach(geom => {
    geom.influences.forEach(influence => {
      const paramValue = paramManager.getValue(influence.parameter);
      transformController.apply(geom.mesh, influence, paramValue);
    });
  });
});
```

### Step 5: Formula Evaluation
```javascript
class FormulaEngine {
  evaluate(formula, parameters) {
    // Replace parameter names with values
    let expr = formula;
    for (let [name, value] of parameters) {
      expr = expr.replace(new RegExp(name, 'g'), value);
    }
    // Evaluate using math.js
    return math.evaluate(expr);
  }
}
```

## 🎨 UI Design Mockup

```
┌───────────────────────────────────────────────────┐
│  Family Configurator - Door Panel                │
├─────────────────────┬─────────────────────────────┤
│                     │  Parameters                 │
│                     │  ┌────────────────────────┐ │
│   3D Viewport       │  │ Height: [====•===] mm  │ │
│                     │  │ 2100                   │ │
│   [Door rotating    │  └────────────────────────┘ │
│    in 3D space]     │  ┌────────────────────────┐ │
│                     │  │ Width: [====•====] mm  │ │
│                     │  │ 900                    │ │
│                     │  └────────────────────────┘ │
│                     │  ┌────────────────────────┐ │
│                     │  │ Hinge Side:            │ │
│                     │  │ ◉ Left  ○ Right        │ │
│                     │  └────────────────────────┘ │
│                     │  ┌────────────────────────┐ │
│                     │  │ Drill Holes:           │ │
│                     │  │ ☑ Yes  ☐ No            │ │
│                     │  └────────────────────────┘ │
│                     │                             │
│                     │  [Export STL] [Export GLB] │
└─────────────────────┴─────────────────────────────┘
```

## 🚀 Advanced Features

### V1 (MVP)
- ✅ Load and display family
- ✅ Basic parameter controls
- ✅ Scale/translate effects
- ✅ Visibility toggles

### V2 (Enhanced)
- 🔄 Formula evaluation
- 🔄 Dependency resolution
- 🔄 Parameter validation
- 🔄 Undo/redo

### V3 (Pro)
- 🌟 Export to STL/OBJ
- 🌟 Material customization
- 🌟 Save/load configurations
- 🌟 URL parameter sharing
- 🌟 Screenshot generation

## 🔍 Key Challenges

### 1. Formula Evaluation
**Challenge**: Revit formulas use specific syntax
**Solution**: Build parser/adapter for math.js

### 2. Coordinate Systems
**Challenge**: Revit vs Three.js coordinate differences
**Solution**: Apply transformation matrix on import

### 3. Complex Effects
**Challenge**: Topology changes can't be procedurally rebuilt
**Solution**: Pre-render variants, swap meshes

### 4. Performance
**Challenge**: Many meshes + many parameters = slow updates
**Solution**: Batch updates, use instancing, LOD

## 📊 Metrics & Goals

- **Load Time**: < 2 seconds for typical family
- **Update Time**: < 16ms (60 FPS) for parameter changes
- **File Size**: < 5MB for complete family package
- **Browser Support**: Chrome, Firefox, Safari, Edge (latest 2 versions)

## 🧪 Testing Strategy

### Unit Tests
- Formula evaluation
- Dependency resolution
- Transform calculations

### Integration Tests
- Full family loading
- Parameter changes → geometry updates
- Export functionality

### Visual Tests
- Screenshot comparison
- Known configurations
- Edge cases

## 📖 Example Usage

```javascript
import { Configurator } from './src/main.js';

const config = new Configurator({
  container: '#app',
  familyUrl: './assets/Door_Panel_config.json',
  theme: 'light',
  controls: {
    orbit: true,
    zoom: true,
    pan: true
  }
});

await config.load();

// Set parameter programmatically
config.setParameter('Height', 2100);
config.setParameter('HingeSide', 'Right');

// Export current configuration
const stl = await config.exportSTL();
```

## 🎓 Learning Resources

- [Three.js Journey](https://threejs-journey.com/)
- [GLTF Specification](https://github.com/KhronosGroup/glTF)
- [Math.js Docs](https://mathjs.org/)
- [Web Components](https://developer.mozilla.org/en-US/docs/Web/Web_Components)

## 🤝 Integration with Phase 1

Phase 1 output becomes Phase 2 input:
```
Revit Add-in → JSON + Meshes → Web Configurator
```

The JSON schema is designed to be directly consumed by the configurator with minimal transformation.

## 🗓️ Timeline Estimate

- **Week 1-2**: Basic three.js setup + loader
- **Week 3-4**: Parameter controls + simple effects
- **Week 5-6**: Formula engine + dependencies
- **Week 7-8**: Advanced effects + polish
- **Week 9**: Testing + documentation
- **Week 10**: Deployment + optimization

## 🎯 Success Criteria

Phase 2 is complete when:
1. ✅ Any Phase 1 export can be loaded
2. ✅ All parameter types are controllable
3. ✅ All effect types work correctly
4. ✅ Formulas evaluate accurately
5. ✅ 60 FPS on mid-range hardware
6. ✅ Works in all modern browsers

---

**Ready to build?** Start with the basic three.js scene and work your way up! 🚀
