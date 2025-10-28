export class TransformController {
    constructor(meshes, config, parameterManager) {
        this.meshes = meshes;
        this.config = config;
        this.parameterManager = parameterManager;
        
        // Conversion factor
        this.feetToMeters = 0.3048;
        
        console.log(`✓ Transform controller initialized for ${meshes.length} meshes`);
    }
    
    applyAllTransforms() {
        // Reset all meshes to original state first
        this.meshes.forEach(mesh => {
            this.resetMesh(mesh);
        });
        
        // Apply all parameter influences
        this.meshes.forEach(mesh => {
            const influences = mesh.userData.influences || [];
            
            influences.forEach(influence => {
                this.applyInfluence(mesh, influence);
            });
        });
    }
    
    resetMesh(mesh) {
        // Reset to original transform
        mesh.position.copy(mesh.userData.originalPosition);
        mesh.scale.copy(mesh.userData.originalScale);
        mesh.rotation.copy(mesh.userData.originalRotation);
    }
    
    applyInfluence(mesh, influence) {
        const paramName = influence.parameter;
        const effect = influence.effect;
        
        const scaleFactor = this.parameterManager.getScaleFactor(paramName);
        const delta = this.parameterManager.getDelta(paramName) * this.feetToMeters;
        
        switch(effect) {
            case 'scaleX':
                mesh.scale.x *= scaleFactor;
                break;
                
            case 'scaleY':
                mesh.scale.y *= scaleFactor;
                break;
                
            case 'scaleZ':
                // Z in Revit becomes Y in Three.js
                mesh.scale.y *= scaleFactor;
                break;
                
            case 'translateX':
                mesh.position.x += delta;
                break;
                
            case 'translateY':
                // Y in Revit becomes Z in Three.js
                mesh.position.z += delta;
                break;
                
            case 'translateZ':
                // Z in Revit becomes Y in Three.js
                mesh.position.y += delta;
                break;
                
            case 'mirrorX':
                mesh.scale.x *= -1;
                break;
                
            case 'mirrorY':
                mesh.scale.z *= -1;  // Y→Z
                break;
                
            case 'mirrorZ':
                mesh.scale.y *= -1;  // Z→Y
                break;
                
            case 'topologyChange':
                // Topology changes can't be animated - would need to swap meshes
                // For now, just log it
                console.log(`Topology change for ${mesh.userData.elementName} (param: ${paramName})`);
                break;
                
            default:
                console.warn(`Unknown effect type: ${effect}`);
        }
    }
}
