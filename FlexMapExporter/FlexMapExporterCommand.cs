using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexMapExporter
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FlexMapExporterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Verify we're in a family document
                if (!doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Flex-Map Exporter", 
                        "This command only works in Family Documents.\n\nPlease open a family and try again.");
                    return Result.Cancelled;
                }

                // Show dialog to select output folder
                var dialog = new ExportDialog();
                bool? dialogResult = dialog.ShowDialog();
                
                if (dialogResult != true)
                {
                    return Result.Cancelled;
                }

                string outputPath = dialog.OutputPath;
                string familyName = doc.Title.Replace(".rfa", "");

                // Update dialog with progress
                dialog.UpdateStatus("Collecting family parameters...");

                // Collect all family parameters
                FamilyManager familyMgr = doc.FamilyManager;
                List<ParameterData> parameters = CollectParameters(familyMgr);

                dialog.UpdateStatus($"Found {parameters.Count} parameters. Capturing baseline geometry...");

                // Capture baseline geometry
                GeometrySnapshot baselineSnapshot = new GeometrySnapshot(doc);
                baselineSnapshot.CaptureAll();

                dialog.UpdateStatus($"Captured {baselineSnapshot.ElementCount} elements. Analyzing parameter influences...");

                // Analyze parameter influences via flex testing
                ParameterFlexer flexer = new ParameterFlexer(doc, familyMgr, baselineSnapshot);
                var influences = flexer.AnalyzeAllParameters(parameters, 
                    progress => dialog.UpdateStatus(progress));

                dialog.UpdateStatus("Exporting geometry meshes...");

                // Export geometry to GLB files
                string geometryFolder = System.IO.Path.Combine(outputPath, "geometry");
                System.IO.Directory.CreateDirectory(geometryFolder);
                baselineSnapshot.ExportMeshes(geometryFolder, familyName);

                dialog.UpdateStatus("Writing JSON configuration...");

                // Export JSON mapping with baseline measurements
                JsonExporter exporter = new JsonExporter(familyName, "2026");
                exporter.SetBaseline(baselineSnapshot);  // Add baseline dimensions
                exporter.AddParameters(parameters);
                exporter.AddGeometry(baselineSnapshot, influences, geometryFolder);
                
                string jsonPath = System.IO.Path.Combine(outputPath, $"{familyName}_config.json");
                exporter.WriteToFile(jsonPath);

                dialog.UpdateStatus($"✓ Export complete!\n\nJSON: {jsonPath}\nGeometry: {geometryFolder}");

                // Count base vs formula parameters
                int baseParams = parameters.Count(p => string.IsNullOrEmpty(p.Formula));
                int formulaParams = parameters.Count - baseParams;
                int totalInfluences = influences.Values.Sum(list => list.Count);
                
                TaskDialog.Show("Success", 
                    $"Flex-Map export completed!\n\n" +
                    $"Total parameters: {parameters.Count}\n" +
                    $"  - Base parameters tested: {baseParams}\n" +
                    $"  - Formula-based (skipped): {formulaParams}\n\n" +
                    $"Geometry elements: {baselineSnapshot.ElementCount}\n" +
                    $"Parameters with influences: {influences.Count}\n" +
                    $"Total influences detected: {totalInfluences}\n\n" +
                    $"Output: {outputPath}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Error", $"Export failed:\n\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
        }

        private List<ParameterData> CollectParameters(FamilyManager familyMgr)
        {
            var parameters = new List<ParameterData>();

            foreach (FamilyParameter param in familyMgr.Parameters)
            {
                var paramData = new ParameterData
                {
                    Name = param.Definition.Name,
                    StorageType = param.StorageType.ToString(),
                    Formula = param.Formula,
                    IsInstance = param.IsInstance
                    // ParameterGroup removed in Revit 2026 API
                };

                // Parse dependencies from formula
                if (!string.IsNullOrEmpty(param.Formula))
                {
                    paramData.Dependencies = ParseFormulaDependencies(param.Formula, familyMgr);
                }

                // Get unit type
                if (param.Definition is InternalDefinition intDef)
                {
                    paramData.Unit = intDef.GetDataType().ToString();
                }

                // Get current value and determine range/options
                if (param.StorageType == StorageType.Double)
                {
                    double? currentValue = familyMgr.CurrentType?.AsDouble(param);
                    if (currentValue.HasValue)
                    {
                        paramData.CurrentValue = currentValue.Value;
                        // Set reasonable range (±50% of current value or formula-based)
                        paramData.Range = new[] { currentValue.Value * 0.5, currentValue.Value * 1.5 };
                    }
                }
                else if (param.StorageType == StorageType.Integer)
                {
                    int? currentValue = familyMgr.CurrentType?.AsInteger(param);
                    if (currentValue.HasValue)
                    {
                        paramData.CurrentValue = currentValue.Value;
                        paramData.Range = new[] { (double)currentValue.Value - 10, (double)currentValue.Value + 10 };
                    }
                }
                else if (param.StorageType == StorageType.String)
                {
                    string? currentValue = familyMgr.CurrentType?.AsString(param);
                    paramData.CurrentValue = currentValue ?? "";
                }

                parameters.Add(paramData);
            }

            return parameters;
        }

        private List<string> ParseFormulaDependencies(string formula, FamilyManager familyMgr)
        {
            var dependencies = new List<string>();
            
            foreach (FamilyParameter param in familyMgr.Parameters)
            {
                string paramName = param.Definition.Name;
                // Simple check if parameter name appears in formula
                if (formula.Contains(paramName) && !dependencies.Contains(paramName))
                {
                    dependencies.Add(paramName);
                }
            }

            return dependencies;
        }
    }
}
