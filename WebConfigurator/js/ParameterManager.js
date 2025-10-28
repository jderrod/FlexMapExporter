export class ParameterManager {
    constructor(config) {
        this.config = config;
        this.parameters = new Map();
        this.baselineValues = new Map();
        
        // Initialize all parameters with their current values
        config.parameters.forEach(param => {
            const value = param.currentValue !== undefined ? param.currentValue : 0;
            this.parameters.set(param.name, value);
            this.baselineValues.set(param.name, value);
        });
        
        console.log(`✓ Parameter manager initialized with ${this.parameters.size} parameters`);
    }
    
    setValue(paramName, value) {
        if (!this.parameters.has(paramName)) {
            console.warn(`Parameter ${paramName} not found`);
            return;
        }
        
        this.parameters.set(paramName, value);
    }
    
    getValue(paramName) {
        return this.parameters.get(paramName);
    }
    
    getBaselineValue(paramName) {
        return this.baselineValues.get(paramName);
    }
    
    getScaleFactor(paramName) {
        const current = this.getValue(paramName);
        const baseline = this.getBaselineValue(paramName);
        
        if (!baseline || baseline === 0) return 1.0;
        return current / baseline;
    }
    
    getDelta(paramName) {
        const current = this.getValue(paramName);
        const baseline = this.getBaselineValue(paramName);
        return current - baseline;
    }
    
    resetAll() {
        this.baselineValues.forEach((value, name) => {
            this.parameters.set(name, value);
        });
    }
    
    getParameterInfo(paramName) {
        return this.config.parameters.find(p => p.name === paramName);
    }
}
