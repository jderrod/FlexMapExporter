import * as THREE from 'three';

export class FamilyLoader {
    constructor() {
        this.config = null;
    }
    
    async loadConfig(path) {
        const response = await fetch(path);
        if (!response.ok) {
            throw new Error(`Failed to load config: ${response.statusText}`);
        }
        this.config = await response.json();
        console.log('✓ Config loaded:', this.config.family);
        return this.config;
    }
    
    async loadAllGeometry(config, basePath) {
        const meshPromises = config.geometry.map(geomConfig => 
            this.loadGeometryElement(geomConfig, basePath)
        );
        
        const meshes = await Promise.all(meshPromises);
        console.log(`✓ Loaded ${meshes.length} geometry elements`);
        return meshes;
    }
    
    async loadGeometryElement(geomConfig, basePath) {
        const response = await fetch(basePath + geomConfig.meshFile);
        if (!response.ok) {
            throw new Error(`Failed to load ${geomConfig.meshFile}: ${response.statusText}`);
        }
        
        const meshData = await response.json();
        return this.createMeshFromData(meshData, geomConfig);
    }
    
    createMeshFromData(meshData, geomConfig) {
        if (!meshData.meshes || meshData.meshes.length === 0) {
            console.warn(`No mesh data for element ${meshData.elementId}`);
            return null;
        }
        
        const solidData = meshData.meshes[0]; // Use first mesh
        
        // Create BufferGeometry
        const geometry = new THREE.BufferGeometry();
        
        // Convert vertices from Revit coordinates (feet) to meters and flatten
        // Revit: Z-up, right-handed
        // Three.js: Y-up, right-handed
        // Transform: X→X, Y→Z, Z→Y
        const vertices = [];
        for (const v of solidData.vertices) {
            const feetToMeters = 0.3048;
            vertices.push(
                v[0] * feetToMeters,  // X stays X
                v[2] * feetToMeters,  // Z becomes Y (up)
                v[1] * feetToMeters   // Y becomes Z (depth)
            );
        }
        
        const vertexArray = new Float32Array(vertices);
        geometry.setAttribute('position', new THREE.BufferAttribute(vertexArray, 3));
        
        // Set indices from triangles
        if (solidData.triangles && solidData.triangles.length > 0) {
            const indices = solidData.triangles.flat();
            geometry.setIndex(indices);
        }
        
        // Compute normals for proper lighting
        geometry.computeVertexNormals();
        
        // Compute bounding sphere for frustum culling
        geometry.computeBoundingSphere();
        
        // Create material based on element type
        const material = this.createMaterial(geomConfig);
        
        // Create mesh
        const mesh = new THREE.Mesh(geometry, material);
        
        // Store metadata
        mesh.userData = {
            elementId: geomConfig.elementId,
            elementName: geomConfig.elementName,
            elementType: geomConfig.elementType,
            isVoid: geomConfig.isVoid,
            influences: geomConfig.influences,
            originalPosition: mesh.position.clone(),
            originalScale: new THREE.Vector3(1, 1, 1), // Baseline scale is 1,1,1
            originalRotation: mesh.rotation.clone()
        };
        
        return mesh;
    }
    
    createMaterial(geomConfig) {
        if (geomConfig.isVoid) {
            // All voids shown in red for visibility
            return new THREE.MeshStandardMaterial({
                color: 0xFF0000,    // Bright red
                metalness: 0.3,
                roughness: 0.6,
                side: THREE.DoubleSide,
                transparent: false,
                emissive: 0xFF0000,
                emissiveIntensity: 0.2
            });
        } else {
            // Solid elements - use different colors based on category
            let color = 0x888888; // Default gray
            
            // Color by category if available
            if (geomConfig.category) {
                switch (geomConfig.category) {
                    case 'Specialty Equipment':
                        color = 0x4A90E2; // Blue for equipment
                        break;
                    case 'Walls':
                        color = 0xCCCCCC; // Light gray for walls
                        break;
                    default:
                        color = 0x888888; // Medium gray
                }
            }
            
            return new THREE.MeshStandardMaterial({
                color: color,
                metalness: 0.1,
                roughness: 0.7,
                side: THREE.DoubleSide
            });
        }
    }
}
