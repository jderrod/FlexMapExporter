using System.Collections.Generic;

namespace FlexMapExporter
{
    public class ParameterData
    {
        public string Name { get; set; } = string.Empty;
        public string StorageType { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public string? Formula { get; set; }
        public List<string>? Dependencies { get; set; }
        public double[]? Range { get; set; }
        public List<string>? Options { get; set; }
        public object? CurrentValue { get; set; }
        public bool IsInstance { get; set; }
        public string? ParameterGroup { get; set; }
    }
}
