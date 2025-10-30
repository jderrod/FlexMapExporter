/**
 * TransformController - Manages geometry transforms and void visibility
 * 
 * VOID ELEMENT MAPPING (with color coding):
 * ------------------------------------------
 * Routing Voids (edge grooves) - 🔴 RED:
 *   - 5987: LEFT side routing (visible when door_swing_direction_out = 0/IN)
 *   - 6457: RIGHT side routing (visible when door_swing_direction_out = 1/OUT)
 * 
 * Notch Voids (bottom corner cutouts) - 🟢 GREEN / 🔵 BLUE:
 *   - 6900: LEFT bottom notch - GREEN (visible when floor_clearance = 1" AND NOT (wall_post_hinging))
 *   - 7150: RIGHT bottom notch - BLUE (visible when floor_clearance = 1" AND NOT (wall_keep_latching))
 * 
 * Hinge Hole Voids (mounting holes - always visible) - 🟠 ORANGE:
 *   - 3535: LEFT edge hinge holes
 *   - 3684: RIGHT edge hinge holes  
 *   - 4126: Middle hinge hole
 *   - 4132: Another middle hinge hole
 * 
 * Solid Elements - 🟤 BROWN (wood):
 *   - 3453: Main door extrusion
 *   - 3668: Joined solid geometry (combined with voids)
 */
export class TransformController {
    constructor(meshes, config, parameterManager, scene) {
        this.meshes = meshes;
        this.config = config;
        this.parameterManager = parameterManager;
        this.scene = scene;
        
        // Conversion factor
        this.feetToMeters = 0.3048;
        
        console.log(`✓ Transform controller initialized for ${meshes.length} meshes`);
    }
    
    applyAllTransforms() {
        // Reset all meshes to original state first
        this.meshes.forEach(mesh => {
            this.resetMesh(mesh);
        });
        
        // Apply visibility controls first
        this.applyVisibilityControls();
        
        // Apply all parameter influences
        // Group by parameter to apply cumulative effects properly
        this.meshes.forEach(mesh => {
            const influences = mesh.userData.influences || [];
            
            // Process each influence
            influences.forEach(influence => {
                this.applyInfluence(mesh, influence);
            });
        });
    }
    
    applyVisibilityControls() {
        // Get parameter values
        const swingOut = this.parameterManager.getValue('door_swing_direction_out');
        const wallPostHinging = this.parameterManager.getValue('door_wall_post_hinging');
        const wallKeepLatching = this.parameterManager.getValue('door_wall_keep_latching');
        const floorClearance = this.parameterManager.getValue('door_floor_clearance_desired');
        
        // Convert clearance to inches for comparison
        const clearanceInches = floorClearance * 12; // feet to inches
        
        this.meshes.forEach(mesh => {
            const id = mesh.userData.elementId;
            
            // ROUTING VOIDS (controlled by swing direction)
            if (id === 5987) {
                // Element 5987: LEFT routing - visible when swing is IN (0)
                mesh.visible = (swingOut === 0);
            } 
            else if (id === 6457) {
                // Element 6457: RIGHT routing - visible when swing is OUT (1)
                mesh.visible = (swingOut === 1);
            }
            
            // NOTCH VOIDS (bottom corner cutouts)
            // Formula: notch visible if floor_clearance = 1" AND not disabled by hinging/latching
            else if (id === 6900) {
                // Element 6900: LEFT bottom notch
                // Hidden if: clearance ≠ 1" OR (hinge on left AND wall_post_hinging)
                // For simplicity, assuming hinge is on left side
                const clearanceIs1Inch = Math.abs(clearanceInches - 1.0) < 0.1;
                const hideForHinging = (wallPostHinging === 1);
                mesh.visible = clearanceIs1Inch && !hideForHinging;
            }
            else if (id === 7150) {
                // Element 7150: RIGHT bottom notch  
                // Hidden if: clearance ≠ 1" OR (hinge on right AND wall_post_hinging) OR (latch side AND wall_keep_latching)
                // Assuming latch is on right side
                const clearanceIs1Inch = Math.abs(clearanceInches - 1.0) < 0.1;
                const hideForLatching = (wallKeepLatching === 1);
                mesh.visible = clearanceIs1Inch && !hideForLatching;
            }
            
            // HINGE HOLE VOIDS (always visible - 3535, 3684, 4126, 4132)
            else if (id === 3535 || id === 3684 || id === 4126 || id === 4132) {
                // Hinge holes always visible
                mesh.visible = true;
            }
        });
    }
    
    resetMesh(mesh) {
        // Reset to original transform
        mesh.position.copy(mesh.userData.originalPosition);
        mesh.scale.copy(mesh.userData.originalScale);
        mesh.rotation.copy(mesh.userData.originalRotation);
        
        // Reset visibility for non-void elements
        if (!mesh.userData.isVoid) {
            mesh.visible = true;
        }
    }
    
    applyInfluence(mesh, influence) {
        const paramName = influence.parameter;
        const effect = influence.effect;
        
        // Filter out false positive detections
        
        // Skip scaleZ effects on floor clearance - it should only translate
        if (paramName === 'door_floor_clearance_desired' && effect === 'scaleZ') {
            return; 
        }
        
        // Skip movement effects on swing_direction - this controls visibility, not position
        if (paramName === 'door_swing_direction_out' && (effect === 'translateY' || effect === 'translateX' || effect === 'translateZ')) {
            return; // This parameter toggles routing visibility, not position
        }
        
        // Skip movement effects on wall_post_hinging - this controls hinge offsets, not major movement
        if (paramName === 'door_wall_post_hinging' && (effect === 'scaleZ' || effect === 'translateZ')) {
            return; // This affects small hinge details, not overall position
        }
        
        const scaleFactor = this.parameterManager.getScaleFactor(paramName);
        const delta = this.parameterManager.getDelta(paramName) * this.feetToMeters;
        
        switch(effect) {
            case 'scaleX':
                mesh.scale.x *= scaleFactor;
                break;
                
            case 'scaleY':
                // Y in Revit becomes Z in Three.js (depth)
                mesh.scale.z *= scaleFactor;
                break;
                
            case 'scaleZ':
                // Z in Revit becomes Y in Three.js (height)
                mesh.scale.y *= scaleFactor;
                break;
                
            case 'translateX':
                mesh.position.x += delta;
                break;
                
            case 'translateY':
                // Y in Revit becomes Z in Three.js (depth)
                mesh.position.z += delta;
                break;
                
            case 'translateZ':
                // Z in Revit becomes Y in Three.js (height)
                mesh.position.y += delta;
                break;
                
            case 'mirrorX':
                // Skip mirror effects - they're often false positives from scaling
                // mesh.scale.x *= -1;
                break;
                
            case 'mirrorY':
                // Skip mirror effects
                // mesh.scale.z *= -1;
                break;
                
            case 'mirrorZ':
                // Skip mirror effects - these cause the reflection issue
                // mesh.scale.y *= -1;
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
