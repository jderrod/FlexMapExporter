export class UIManager {
    constructor(config, parameterManager, onParameterChange) {
        this.config = config;
        this.parameterManager = parameterManager;
        this.onParameterChange = onParameterChange;
        this.controls = new Map();
        
        // Feet to inches for display
        this.feetToInches = 12;
    }
    
    buildUI() {
        const container = document.getElementById('parameters-container');
        
        // Get parameters that have influences (are actually used)
        const usedParamNames = new Set(
            this.config.geometry.flatMap(g => g.influences.map(i => i.parameter))
        );
        
        const usedParams = this.config.parameters.filter(p => usedParamNames.has(p.name));
        
        // Group parameters by type
        const dimensionParams = usedParams.filter(p => p.storageType === 'Double');
        const integerParams = usedParams.filter(p => p.storageType === 'Integer');
        
        // Build dimension controls
        if (dimensionParams.length > 0) {
            const group = this.createParameterGroup('Dimensions', dimensionParams);
            container.appendChild(group);
        }
        
        // Build toggle controls
        if (integerParams.length > 0) {
            const group = this.createParameterGroup('Options', integerParams);
            container.appendChild(group);
        }
        
        console.log(`✓ UI built with ${usedParams.length} parameter controls`);
    }
    
    createParameterGroup(title, parameters) {
        const group = document.createElement('div');
        group.className = 'parameter-group';
        
        const heading = document.createElement('h3');
        heading.textContent = title;
        heading.style.cssText = 'font-size: 14px; margin-bottom: 15px; color: #764ba2; text-transform: uppercase; letter-spacing: 1px;';
        group.appendChild(heading);
        
        parameters.forEach(param => {
            const control = this.createParameterControl(param);
            if (control) {
                group.appendChild(control);
            }
        });
        
        return group;
    }
    
    createParameterControl(param) {
        const div = document.createElement('div');
        div.className = 'parameter';
        
        if (param.storageType === 'Double') {
            return this.createSliderControl(param);
        } else if (param.storageType === 'Integer') {
            return this.createToggleControl(param);
        }
        
        return null;
    }
    
    createSliderControl(param) {
        const div = document.createElement('div');
        div.className = 'parameter';
        
        // Clean parameter name for display
        const displayName = this.formatParameterName(param.name);
        
        // Get range or use defaults
        const currentValue = this.parameterManager.getValue(param.name);
        const min = param.range ? param.range[0] : currentValue * 0.5;
        const max = param.range ? param.range[1] : currentValue * 1.5;
        
        // Label
        const label = document.createElement('div');
        label.className = 'parameter-label';
        
        const nameSpan = document.createElement('span');
        nameSpan.textContent = displayName;
        
        const valueSpan = document.createElement('span');
        valueSpan.className = 'parameter-value';
        const unitSpan = document.createElement('span');
        unitSpan.className = 'parameter-unit';
        unitSpan.textContent = 'in';
        
        valueSpan.textContent = this.formatValue(currentValue);
        valueSpan.appendChild(unitSpan);
        
        label.appendChild(nameSpan);
        label.appendChild(valueSpan);
        
        // Slider
        const slider = document.createElement('input');
        slider.type = 'range';
        slider.min = min;
        slider.max = max;
        slider.step = (max - min) / 100;
        slider.value = currentValue;
        
        // Store references
        this.controls.set(param.name, { slider, valueSpan });
        
        // Event listener
        slider.addEventListener('input', (e) => {
            const value = parseFloat(e.target.value);
            valueSpan.textContent = this.formatValue(value);
            valueSpan.appendChild(unitSpan);
            this.onParameterChange(param.name, value);
        });
        
        div.appendChild(label);
        div.appendChild(slider);
        
        return div;
    }
    
    createToggleControl(param) {
        const div = document.createElement('div');
        div.className = 'parameter';
        
        const displayName = this.formatParameterName(param.name);
        
        // Label
        const label = document.createElement('div');
        label.className = 'parameter-label';
        label.style.marginBottom = '8px';
        label.textContent = displayName;
        
        // Toggle buttons
        const toggleGroup = document.createElement('div');
        toggleGroup.className = 'toggle-group';
        
        const currentValue = this.parameterManager.getValue(param.name);
        
        const yesBtn = document.createElement('button');
        yesBtn.className = 'toggle-btn' + (currentValue !== 0 ? ' active' : '');
        yesBtn.textContent = 'Yes';
        
        const noBtn = document.createElement('button');
        noBtn.className = 'toggle-btn' + (currentValue === 0 ? ' active' : '');
        noBtn.textContent = 'No';
        
        // Store references
        this.controls.set(param.name, { yesBtn, noBtn });
        
        // Event listeners
        yesBtn.addEventListener('click', () => {
            yesBtn.classList.add('active');
            noBtn.classList.remove('active');
            this.onParameterChange(param.name, 1);
        });
        
        noBtn.addEventListener('click', () => {
            noBtn.classList.add('active');
            yesBtn.classList.remove('active');
            this.onParameterChange(param.name, 0);
        });
        
        toggleGroup.appendChild(yesBtn);
        toggleGroup.appendChild(noBtn);
        
        div.appendChild(label);
        div.appendChild(toggleGroup);
        
        return div;
    }
    
    formatParameterName(name) {
        // Convert snake_case to Title Case
        return name
            .replace(/_/g, ' ')
            .split(' ')
            .map(word => word.charAt(0).toUpperCase() + word.slice(1))
            .join(' ');
    }
    
    formatValue(feet) {
        // Convert feet to inches for display
        const inches = feet * this.feetToInches;
        return inches.toFixed(1);
    }
    
    updateAllControls() {
        this.config.parameters.forEach(param => {
            const control = this.controls.get(param.name);
            if (!control) return;
            
            const value = this.parameterManager.getValue(param.name);
            
            if (control.slider) {
                // Slider control
                control.slider.value = value;
                control.valueSpan.textContent = this.formatValue(value);
                const unitSpan = document.createElement('span');
                unitSpan.className = 'parameter-unit';
                unitSpan.textContent = 'in';
                control.valueSpan.appendChild(unitSpan);
            } else if (control.yesBtn && control.noBtn) {
                // Toggle control
                if (value !== 0) {
                    control.yesBtn.classList.add('active');
                    control.noBtn.classList.remove('active');
                } else {
                    control.noBtn.classList.add('active');
                    control.yesBtn.classList.remove('active');
                }
            }
        });
    }
}
