import * as THREE from 'three';
import { ADDITION, Brush, Evaluator, SUBTRACTION } from 'three-bvh-csg';

export class CSGProcessor {
    constructor() {
        this.evaluator = new Evaluator();
        this.solidMeshes = [];
        this.voidMeshes = [];
        this.resultMesh = null;
    }
    
    setSolids(meshes) {
        this.solidMeshes = meshes.filter(m => !m.userData.isVoid);
    }
    
    setVoids(meshes) {
        this.voidMeshes = meshes.filter(m => m.userData.isVoid && m.visible);
    }
    
    performCSG() {
        if (this.solidMeshes.length === 0) {
            console.warn('No solid meshes to process');
            return null;
        }
        
        // Start with the main solid (largest by vertex count)
        const mainSolid = this.solidMeshes.reduce((prev, current) => 
            (current.geometry.attributes.position.count > prev.geometry.attributes.position.count) ? current : prev
        );
        
        // Create brush from main solid
        let resultBrush = new Brush(mainSolid.geometry, mainSolid.material);
        resultBrush.updateMatrixWorld();
        
        // Subtract each visible void
        for (const voidMesh of this.voidMeshes) {
            if (!voidMesh.visible) continue;
            
            try {
                const voidBrush = new Brush(voidMesh.geometry);
                voidBrush.position.copy(voidMesh.position);
                voidBrush.rotation.copy(voidMesh.rotation);
                voidBrush.scale.copy(voidMesh.scale);
                voidBrush.updateMatrixWorld();
                
                // Perform subtraction
                const newBrush = this.evaluator.evaluate(resultBrush, voidBrush, SUBTRACTION);
                resultBrush = newBrush;
                
            } catch (error) {
                console.warn(`Failed to subtract void ${voidMesh.userData.elementId}:`, error);
            }
        }
        
        // Create final mesh
        const geometry = resultBrush.geometry.clone();
        geometry.computeVertexNormals();
        
        const material = new THREE.MeshStandardMaterial({
            color: 0x8B7355, // Wood color
            metalness: 0.0,
            roughness: 0.85,
            side: THREE.DoubleSide
        });
        
        this.resultMesh = new THREE.Mesh(geometry, material);
        this.resultMesh.castShadow = true;
        this.resultMesh.receiveShadow = true;
        
        return this.resultMesh;
    }
    
    dispose() {
        if (this.resultMesh) {
            this.resultMesh.geometry.dispose();
            this.resultMesh.material.dispose();
        }
    }
}
