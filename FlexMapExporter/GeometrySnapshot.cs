using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FlexMapExporter
{
    public class GeometrySnapshot
    {
        private readonly Document _doc;
        private readonly Dictionary<ElementId, ElementGeometry> _geometryData;

        public int ElementCount => _geometryData.Count;
        public IReadOnlyDictionary<ElementId, ElementGeometry> GeometryData => _geometryData;

        public GeometrySnapshot(Document doc)
        {
            _doc = doc;
            _geometryData = new Dictionary<ElementId, ElementGeometry>();
        }

        public void CaptureAll()
        {
            // Get ALL geometry elements in the family including voids
            // Don't filter by class - capture everything with geometry
            FilteredElementCollector collector = new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType();

            Options options = new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,  // Changed to true to capture hidden elements
                DetailLevel = ViewDetailLevel.Fine
            };

            foreach (Element element in collector)
            {
                GeometryElement? geomElement = element.get_Geometry(options);
                if (geomElement == null) continue;

                // Check if element is a void
                bool isVoid = false;
                if (element is Extrusion ext)
                {
                    isVoid = !ext.IsSolid;
                }
                else if (element is GenericForm form)
                {
                    isVoid = !form.IsSolid;
                }
                else if (element is Blend blend)
                {
                    isVoid = !blend.IsSolid;
                }
                else if (element is Revolution rev)
                {
                    isVoid = !rev.IsSolid;
                }
                else if (element is Sweep sweep)
                {
                    isVoid = !sweep.IsSolid;
                }

                var elementGeom = new ElementGeometry
                {
                    ElementId = element.Id,
                    Category = element.Category?.Name ?? "Unknown",
                    ElementName = element.Name,
                    ElementType = element.GetType().Name,
                    BoundingBox = element.get_BoundingBox(null),
                    IsVoid = isVoid
                };

                // Extract solid geometry (or void geometry)
                bool hasGeometry = false;
                foreach (GeometryObject geomObj in geomElement)
                {
                    ProcessGeometryObject(geomObj, elementGeom);
                    if (geomObj is Solid || geomObj is Mesh)
                    {
                        hasGeometry = true;
                    }
                }

                // Include this element if it has geometry OR if it's a void
                // (voids might not have volume but are still important)
                if (hasGeometry || isVoid || elementGeom.HasGeometry)
                {
                    _geometryData[element.Id] = elementGeom;
                }
            }
        }

        private void ProcessGeometryObject(GeometryObject geomObj, ElementGeometry elementGeom)
        {
            if (geomObj is Solid solid)
            {
                // Capture solids even with zero volume (void cuts)
                if (solid.Faces.Size > 0 || solid.Edges.Size > 0)
                {
                    elementGeom.Solids.Add(CaptureSolid(solid));
                }
            }
            else if (geomObj is GeometryInstance instance)
            {
                GeometryElement instGeom = instance.GetInstanceGeometry();
                foreach (GeometryObject obj in instGeom)
                {
                    ProcessGeometryObject(obj, elementGeom);
                }
            }
            else if (geomObj is Mesh mesh)
            {
                elementGeom.Meshes.Add(CaptureMesh(mesh));
            }
        }

        private SolidData CaptureSolid(Solid solid)
        {
            var solidData = new SolidData
            {
                Volume = solid.Volume,
                SurfaceArea = solid.SurfaceArea,
                Vertices = new List<XYZ>(),
                Triangles = new List<int[]>()
            };

            // Extract vertices and triangle indices from all faces
            int vertexOffset = 0;
            foreach (Face face in solid.Faces)
            {
                Mesh mesh = face.Triangulate();
                
                // Add vertices for this face
                var faceVertices = new List<XYZ>();
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    MeshTriangle triangle = mesh.get_Triangle(i);
                    
                    // Add vertices if not already in list (simple approach)
                    XYZ v0 = triangle.get_Vertex(0);
                    XYZ v1 = triangle.get_Vertex(1);
                    XYZ v2 = triangle.get_Vertex(2);
                    
                    int i0 = FindOrAddVertex(solidData.Vertices, v0);
                    int i1 = FindOrAddVertex(solidData.Vertices, v1);
                    int i2 = FindOrAddVertex(solidData.Vertices, v2);
                    
                    solidData.Triangles.Add(new[] { i0, i1, i2 });
                }
            }

            return solidData;
        }
        
        private int FindOrAddVertex(List<XYZ> vertices, XYZ vertex)
        {
            const double tolerance = 0.0001; // ~0.03mm
            
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].DistanceTo(vertex) < tolerance)
                {
                    return i;
                }
            }
            
            vertices.Add(vertex);
            return vertices.Count - 1;
        }

        private MeshData CaptureMesh(Mesh mesh)
        {
            var meshData = new MeshData
            {
                Vertices = new List<XYZ>()
            };

            for (int i = 0; i < mesh.NumTriangles; i++)
            {
                MeshTriangle triangle = mesh.get_Triangle(i);
                meshData.Vertices.Add(triangle.get_Vertex(0));
                meshData.Vertices.Add(triangle.get_Vertex(1));
                meshData.Vertices.Add(triangle.get_Vertex(2));
            }

            return meshData;
        }

        public ComparisonResult CompareTo(GeometrySnapshot other)
        {
            var result = new ComparisonResult();

            // Find added, removed, and modified elements
            var currentIds = new HashSet<ElementId>(_geometryData.Keys);
            var otherIds = new HashSet<ElementId>(other._geometryData.Keys);

            result.AddedElements = otherIds.Except(currentIds).ToList();
            result.RemovedElements = currentIds.Except(otherIds).ToList();

            // Compare common elements
            var commonIds = currentIds.Intersect(otherIds);
            foreach (var id in commonIds)
            {
                var thisGeom = _geometryData[id];
                var otherGeom = other._geometryData[id];

                var change = CompareElementGeometry(thisGeom, otherGeom);
                if (change.HasChange)
                {
                    result.ModifiedElements[id] = change;
                }
            }

            return result;
        }

        private GeometryChange CompareElementGeometry(ElementGeometry baseline, ElementGeometry modified)
        {
            var change = new GeometryChange();

            // Compare bounding boxes
            if (baseline.BoundingBox != null && modified.BoundingBox != null)
            {
                XYZ baselineMin = baseline.BoundingBox.Min;
                XYZ baselineMax = baseline.BoundingBox.Max;
                XYZ modifiedMin = modified.BoundingBox.Min;
                XYZ modifiedMax = modified.BoundingBox.Max;

                double deltaX = Math.Abs(modifiedMax.X - modifiedMin.X - (baselineMax.X - baselineMin.X));
                double deltaY = Math.Abs(modifiedMax.Y - modifiedMin.Y - (baselineMax.Y - baselineMin.Y));
                double deltaZ = Math.Abs(modifiedMax.Z - modifiedMin.Z - (baselineMax.Z - baselineMin.Z));

                const double tolerance = 0.0001; // ~0.03mm - more sensitive

                if (deltaX > tolerance) change.DimensionChanges.Add("X");
                if (deltaY > tolerance) change.DimensionChanges.Add("Y");
                if (deltaZ > tolerance) change.DimensionChanges.Add("Z");

                // Detect translation
                double transX = Math.Abs(modifiedMin.X - baselineMin.X);
                double transY = Math.Abs(modifiedMin.Y - baselineMin.Y);
                double transZ = Math.Abs(modifiedMin.Z - baselineMin.Z);

                if (transX > tolerance) change.TranslationChanges.Add("X");
                if (transY > tolerance) change.TranslationChanges.Add("Y");
                if (transZ > tolerance) change.TranslationChanges.Add("Z");
            }

            // Compare vertex count (detect topology changes)
            int baselineVertexCount = baseline.Solids.Sum(s => s.Vertices.Count) + 
                                     baseline.Meshes.Sum(m => m.Vertices.Count);
            int modifiedVertexCount = modified.Solids.Sum(s => s.Vertices.Count) + 
                                     modified.Meshes.Sum(m => m.Vertices.Count);

            if (Math.Abs(modifiedVertexCount - baselineVertexCount) > 0)
            {
                change.TopologyChanged = true;
            }

            return change;
        }

        public void ExportMeshes(string outputFolder, string familyName)
        {
            // Export each element with proper mesh topology
            foreach (var kvp in _geometryData)
            {
                ElementId id = kvp.Key;
                ElementGeometry geom = kvp.Value;

                string filename = $"{familyName}_{id.Value}.json";
                string filepath = Path.Combine(outputFolder, filename);

                // Export with proper mesh structure for three.js
                var meshJson = new
                {
                    elementId = id.Value,
                    elementName = geom.ElementName,
                    elementType = geom.ElementType,
                    category = geom.Category,
                    isVoid = geom.IsVoid,
                    
                    // Bounding box for quick size checks
                    boundingBox = geom.BoundingBox != null ? new
                    {
                        min = new[] { geom.BoundingBox.Min.X, geom.BoundingBox.Min.Y, geom.BoundingBox.Min.Z },
                        max = new[] { geom.BoundingBox.Max.X, geom.BoundingBox.Max.Y, geom.BoundingBox.Max.Z }
                    } : null,
                    
                    // Mesh data
                    meshes = geom.Solids.Select(s => new
                    {
                        type = "solid",
                        volume = s.Volume,
                        surfaceArea = s.SurfaceArea,
                        vertices = s.Vertices.Select(v => new[] { v.X, v.Y, v.Z }).ToList(),
                        triangles = s.Triangles,
                        vertexCount = s.Vertices.Count,
                        triangleCount = s.Triangles.Count
                    }).ToList()
                };

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(meshJson, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filepath, json);
            }
        }
    }

    public class ElementGeometry
    {
        public ElementId ElementId { get; set; } = ElementId.InvalidElementId;
        public string Category { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public bool IsVoid { get; set; }
        public BoundingBoxXYZ? BoundingBox { get; set; }
        public List<SolidData> Solids { get; set; } = new List<SolidData>();
        public List<MeshData> Meshes { get; set; } = new List<MeshData>();

        public bool HasGeometry => Solids.Any() || Meshes.Any();
    }

    public class SolidData
    {
        public double Volume { get; set; }
        public double SurfaceArea { get; set; }
        public List<XYZ> Vertices { get; set; } = new List<XYZ>();
        public List<int[]> Triangles { get; set; } = new List<int[]>();
    }

    public class MeshData
    {
        public List<XYZ> Vertices { get; set; } = new List<XYZ>();
    }

    public class ComparisonResult
    {
        public List<ElementId> AddedElements { get; set; } = new List<ElementId>();
        public List<ElementId> RemovedElements { get; set; } = new List<ElementId>();
        public Dictionary<ElementId, GeometryChange> ModifiedElements { get; set; } = new Dictionary<ElementId, GeometryChange>();

        public bool HasChanges => AddedElements.Any() || RemovedElements.Any() || ModifiedElements.Any();
    }

    public class GeometryChange
    {
        public List<string> DimensionChanges { get; set; } = new List<string>();
        public List<string> TranslationChanges { get; set; } = new List<string>();
        public bool TopologyChanged { get; set; }

        public bool HasChange => DimensionChanges.Any() || TranslationChanges.Any() || TopologyChanged;
    }
}
