import { SceneManager } from './SceneManager.js';
import { FamilyLoader } from './FamilyLoader.js';
import { ParameterManager } from './ParameterManager.js';
import { TransformController } from './TransformController.js';
import { UIManager } from './UIManager.js';

class DoorConfigurator {
    constructor() {
        this.canvas = document.getElementById('canvas');
        this.loading = document.getElementById('loading');
        
        // Initialize managers
        this.sceneManager = new SceneManager(this.canvas);
        this.familyLoader = new FamilyLoader();
        this.parameterManager = null;
        this.transformController = null;
        this.uiManager = null;
        
        // Configuration - Alcove Recessed stall
        this.configPath = 'AlcoveData/Privada-Alcove_Recessed-v1_2025_10_29_config.json';
        this.geometryBasePath = 'AlcoveData/geometry/';
        
        this.init();
    }
    
    async init() {
        try {
            // Load family configuration
            this.showLoading('Loading configuration...');
            const config = await this.familyLoader.loadConfig(this.configPath);
            
            // Load all geometry
            this.showLoading(`Loading geometry (${config.geometry.length} elements)...`);
            const meshes = await this.familyLoader.loadAllGeometry(config, this.geometryBasePath);
            
            // Add meshes to scene
            this.showLoading('Building 3D scene...');
            meshes.forEach(mesh => {
                this.sceneManager.addMesh(mesh);
            });
            
            // Initialize parameter management
            this.parameterManager = new ParameterManager(config);
            this.transformController = new TransformController(
                this.sceneManager.meshes, 
                config,
                this.parameterManager,
                this.sceneManager.scene  // Pass scene for CSG
            );
            
            // Build UI
            this.uiManager = new UIManager(
                config,
                this.parameterManager,
                (paramName, value) => this.onParameterChange(paramName, value)
            );
            this.uiManager.buildUI();
            
            // Apply initial transforms
            this.transformController.applyAllTransforms();
            
            // Update stats
            this.updateStats(config, meshes);
            
            // Setup reset button
            document.getElementById('reset-btn').addEventListener('click', () => {
                this.resetParameters();
            });
            
            // Start animation
            this.sceneManager.animate();
            
            // Hide loading
            this.hideLoading();
            
            console.log('✓ Configurator initialized successfully!');
            console.log('Config:', config);
            console.log('Meshes:', meshes);
            
        } catch (error) {
            console.error('Failed to initialize configurator:', error);
            this.showError(error.message);
        }
    }
    
    onParameterChange(paramName, value) {
        // Update parameter value
        this.parameterManager.setValue(paramName, value);
        
        // Apply transforms to geometry
        this.transformController.applyAllTransforms();
    }
    
    resetParameters() {
        this.parameterManager.resetAll();
        this.uiManager.updateAllControls();
        this.transformController.applyAllTransforms();
    }
    
    updateStats(config, meshes) {
        document.getElementById('stat-elements').textContent = config.geometry.length;
        const totalVertices = meshes.reduce((sum, mesh) => 
            sum + (mesh.geometry.attributes.position?.count || 0), 0);
        document.getElementById('stat-vertices').textContent = totalVertices.toLocaleString();
        
        const paramsWithInfluences = new Set(
            config.geometry.flatMap(g => g.influences.map(i => i.parameter))
        ).size;
        document.getElementById('stat-params').textContent = paramsWithInfluences;
    }
    
    showLoading(message) {
        this.loading.classList.remove('hidden');
        const text = this.loading.querySelector('div:last-child');
        if (text) text.textContent = message;
    }
    
    hideLoading() {
        this.loading.classList.add('hidden');
    }
    
    showError(message) {
        this.loading.querySelector('.spinner').style.display = 'none';
        this.loading.querySelector('div:last-child').innerHTML = 
            `<strong style="color: #f44336;">Error:</strong><br>${message}`;
    }
}

// Start the configurator when page loads
window.addEventListener('DOMContentLoaded', () => {
    new DoorConfigurator();
});
