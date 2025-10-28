using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FlexMapExporter
{
    public class JsonExporter
    {
        private readonly FlexMapConfig _config;

        public JsonExporter(string familyName, string revitVersion)
        {
            _config = new FlexMapConfig
            {
                Family = familyName,
                RevitVersion = revitVersion,
                Parameters = new List<ParameterConfig>(),
                Geometry = new List<GeometryConfig>(),
                Baseline = new BaselineConfig()
            };
        }
        
        public void SetBaseline(GeometrySnapshot snapshot)
        {
            // Calculate overall bounding box from all geometry
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            
            foreach (var geom in snapshot.GeometryData.Values)
            {
                if (geom.BoundingBox != null)
                {
                    minX = Math.Min(minX, geom.BoundingBox.Min.X);
                    minY = Math.Min(minY, geom.BoundingBox.Min.Y);
                    minZ = Math.Min(minZ, geom.BoundingBox.Min.Z);
                    maxX = Math.Max(maxX, geom.BoundingBox.Max.X);
                    maxY = Math.Max(maxY, geom.BoundingBox.Max.Y);
                    maxZ = Math.Max(maxZ, geom.BoundingBox.Max.Z);
                }
            }
            
            if (minX != double.MaxValue)
            {
                _config.Baseline.Width = maxX - minX;
                _config.Baseline.Height = maxZ - minZ;
                _config.Baseline.Depth = maxY - minY;
                _config.Baseline.Origin = new[] { minX, minY, minZ };
                _config.Baseline.Center = new[] { (minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2 };
            }
        }

        public void AddParameters(List<ParameterData> parameters)
        {
            foreach (var param in parameters)
            {
                var paramConfig = new ParameterConfig
                {
                    Name = param.Name,
                    StorageType = param.StorageType,
                    Unit = param.Unit,
                    Formula = param.Formula,
                    Dependencies = param.Dependencies,
                    Range = param.Range,
                    Options = param.Options,
                    CurrentValue = param.CurrentValue  // Add initial/baseline value
                };

                _config.Parameters.Add(paramConfig);
            }
        }

        public void AddGeometry(
            GeometrySnapshot snapshot, 
            Dictionary<string, List<ParameterInfluence>> influences,
            string geometryFolder)
        {
            foreach (var kvp in snapshot.GeometryData)
            {
                ElementId elementId = kvp.Key;
                ElementGeometry geom = kvp.Value;

                // Find all influences for this element
                var elementInfluences = new List<InfluenceConfig>();

                foreach (var paramInfluences in influences.Values)
                {
                    foreach (var influence in paramInfluences.Where(i => i.ElementId == elementId))
                    {
                        elementInfluences.Add(new InfluenceConfig
                        {
                            Parameter = influence.Parameter,
                            Effect = influence.Effect
                        });
                    }
                }

                // Create geometry config with full metadata
                string meshFile = $"{_config.Family}_{elementId.Value}.json";
                
                var geomConfig = new GeometryConfig
                {
                    ElementId = (int)elementId.Value,
                    ElementName = geom.ElementName,
                    ElementType = geom.ElementType,
                    Category = geom.Category,
                    IsVoid = geom.IsVoid,
                    MeshFile = meshFile,
                    Influences = elementInfluences
                };

                _config.Geometry.Add(geomConfig);
            }
        }

        public void WriteToFile(string filepath)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(_config, settings);
            File.WriteAllText(filepath, json);
        }
    }

    // JSON Schema Classes
    public class FlexMapConfig
    {
        [JsonProperty("family")]
        public string Family { get; set; } = string.Empty;

        [JsonProperty("revitVersion")]
        public string RevitVersion { get; set; } = string.Empty;
        
        [JsonProperty("baseline")]
        public BaselineConfig Baseline { get; set; } = new BaselineConfig();

        [JsonProperty("parameters")]
        public List<ParameterConfig> Parameters { get; set; } = new List<ParameterConfig>();

        [JsonProperty("geometry")]
        public List<GeometryConfig> Geometry { get; set; } = new List<GeometryConfig>();
    }

    public class BaselineConfig
    {
        [JsonProperty("width")]
        public double Width { get; set; }
        
        [JsonProperty("height")]
        public double Height { get; set; }
        
        [JsonProperty("depth")]
        public double Depth { get; set; }
        
        [JsonProperty("origin")]
        public double[]? Origin { get; set; }
        
        [JsonProperty("center")]
        public double[]? Center { get; set; }
    }
    
    public class ParameterConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("storageType")]
        public string StorageType { get; set; } = string.Empty;

        [JsonProperty("unit")]
        public string? Unit { get; set; }

        [JsonProperty("formula")]
        public string? Formula { get; set; }

        [JsonProperty("dependencies")]
        public List<string>? Dependencies { get; set; }

        [JsonProperty("range")]
        public double[]? Range { get; set; }

        [JsonProperty("options")]
        public List<string>? Options { get; set; }
        
        [JsonProperty("currentValue")]
        public object? CurrentValue { get; set; }
    }

    public class GeometryConfig
    {
        [JsonProperty("elementId")]
        public int ElementId { get; set; }
        
        [JsonProperty("elementName")]
        public string ElementName { get; set; } = string.Empty;
        
        [JsonProperty("elementType")]
        public string ElementType { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;
        
        [JsonProperty("isVoid")]
        public bool IsVoid { get; set; }

        [JsonProperty("meshFile")]
        public string MeshFile { get; set; } = string.Empty;

        [JsonProperty("influences")]
        public List<InfluenceConfig> Influences { get; set; } = new List<InfluenceConfig>();
    }

    public class InfluenceConfig
    {
        [JsonProperty("parameter")]
        public string Parameter { get; set; } = string.Empty;

        [JsonProperty("effect")]
        public string Effect { get; set; } = string.Empty;
    }
}
