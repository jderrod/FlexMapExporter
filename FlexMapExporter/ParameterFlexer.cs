using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FlexMapExporter
{
    public class ParameterFlexer
    {
        private readonly Document _doc;
        private readonly FamilyManager _familyMgr;
        private readonly GeometrySnapshot _baseline;

        public ParameterFlexer(Document doc, FamilyManager familyMgr, GeometrySnapshot baseline)
        {
            _doc = doc;
            _familyMgr = familyMgr;
            _baseline = baseline;
        }

        public Dictionary<string, List<ParameterInfluence>> AnalyzeAllParameters(
            List<ParameterData> parameters, 
            Action<string>? progressCallback = null)
        {
            var allInfluences = new Dictionary<string, List<ParameterInfluence>>();

            int current = 0;
            int total = parameters.Count;
            int skippedFormulas = 0;
            int testedParams = 0;

            foreach (var paramData in parameters)
            {
                current++;
                
                // Check if this parameter has a formula (will be skipped)
                bool hasFormula = !string.IsNullOrEmpty(paramData.Formula);
                
                if (hasFormula)
                {
                    skippedFormulas++;
                    progressCallback?.Invoke($"Skipping {current}/{total}: {paramData.Name} (formula-based)");
                }
                else
                {
                    testedParams++;
                    progressCallback?.Invoke($"Testing {current}/{total}: {paramData.Name} (base parameter)");
                }

                var influences = FlexParameter(paramData);
                if (influences.Any())
                {
                    allInfluences[paramData.Name] = influences;
                    progressCallback?.Invoke($"  ✓ Found {influences.Count} influence(s) for {paramData.Name}");
                }
            }
            
            progressCallback?.Invoke($"\nAnalysis complete: Tested {testedParams} base parameters, skipped {skippedFormulas} formula-based parameters");

            return allInfluences;
        }

        private List<ParameterInfluence> FlexParameter(ParameterData paramData)
        {
            var influences = new List<ParameterInfluence>();

            // Find the FamilyParameter
            FamilyParameter? param = _familyMgr.Parameters
                .Cast<FamilyParameter>()
                .FirstOrDefault(p => p.Definition.Name == paramData.Name);

            if (param == null) return influences;

            // CRITICAL: Skip parameters with formulas - they're calculated from others
            // Only flex "base" parameters that actually control the model
            if (!string.IsNullOrEmpty(param.Formula))
            {
                // Skip - this is a derived parameter
                return influences;
            }

            // Use TransactionGroup for rollback capability
            using (TransactionGroup transGroup = new TransactionGroup(_doc, $"Flex {paramData.Name}"))
            {
                transGroup.Start();

                try
                {
                    // Perform flex test based on parameter type
                    if (param.StorageType == StorageType.Double)
                    {
                        influences.AddRange(FlexDoubleParameter(param, paramData));
                    }
                    else if (param.StorageType == StorageType.Integer)
                    {
                        influences.AddRange(FlexIntegerParameter(param, paramData));
                    }
                    else if (param.StorageType == StorageType.String)
                    {
                        influences.AddRange(FlexStringParameter(param, paramData));
                    }
                }
                catch (Exception ex)
                {
                    // If flex test fails, just log and continue
                    System.Diagnostics.Debug.WriteLine($"Flex test failed for {paramData.Name}: {ex.Message}");
                }
                finally
                {
                    // Always rollback to restore original state
                    transGroup.RollBack();
                }
            }

            return influences;
        }

        private List<ParameterInfluence> FlexDoubleParameter(FamilyParameter param, ParameterData paramData)
        {
            var influences = new List<ParameterInfluence>();

            double? currentValue = _familyMgr.CurrentType?.AsDouble(param);
            if (!currentValue.HasValue || Math.Abs(currentValue.Value) < 1e-9) return influences;

            // Test BOTH positive and negative changes with LARGER delta (50%)
            // This catches more subtle effects and bidirectional behaviors
            double[] deltaMultipliers = { 0.5, -0.3 }; // Test +50% and -30%
            
            foreach (double multiplier in deltaMultipliers)
            {
                double delta = currentValue.Value * multiplier;
                double newValue = currentValue.Value + delta;
                
                // Skip if new value would be negative for positive-only parameters
                if (newValue < 0 && currentValue.Value > 0) continue;

                // Apply the change
                using (Transaction trans = new Transaction(_doc, "Flex Double"))
                {
                    trans.Start();
                    try
                    {
                        _familyMgr.Set(param, newValue);
                        _doc.Regenerate();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.RollBack();
                        continue; // Skip if this value is invalid
                    }
                }

                // Capture flexed geometry
                var flexedSnapshot = new GeometrySnapshot(_doc);
                flexedSnapshot.CaptureAll();

                // Compare with baseline
                var comparison = _baseline.CompareTo(flexedSnapshot);

                // Classify changes
                var newInfluences = ClassifyChanges(param.Definition.Name, comparison, delta, currentValue.Value);
                
                // Add only new influences we haven't seen yet
                foreach (var inf in newInfluences)
                {
                    if (!influences.Any(existing => 
                        existing.ElementId == inf.ElementId && 
                        existing.Effect == inf.Effect))
                    {
                        influences.Add(inf);
                    }
                }
                
                // If we found changes, no need to test the other direction
                if (newInfluences.Any()) break;
            }

            return influences;
        }

        private List<ParameterInfluence> FlexIntegerParameter(FamilyParameter param, ParameterData paramData)
        {
            var influences = new List<ParameterInfluence>();

            int? currentValue = _familyMgr.CurrentType?.AsInteger(param);
            if (!currentValue.HasValue) return influences;

            // Test both +1 and -1 for integer parameters (often boolean 0/1)
            int[] deltas = { 1, -1 };
            
            foreach (int delta in deltas)
            {
                int newValue = currentValue.Value + delta;

                using (Transaction trans = new Transaction(_doc, "Flex Integer"))
                {
                    trans.Start();
                    try
                    {
                        _familyMgr.Set(param, newValue);
                        _doc.Regenerate();
                        trans.Commit();
                    }
                    catch
                    {
                        trans.RollBack();
                        continue;
                    }
                }

                var flexedSnapshot = new GeometrySnapshot(_doc);
                flexedSnapshot.CaptureAll();

                var comparison = _baseline.CompareTo(flexedSnapshot);
                var newInfluences = ClassifyChanges(param.Definition.Name, comparison, delta, currentValue.Value);
                
                // Add unique influences
                foreach (var inf in newInfluences)
                {
                    if (!influences.Any(existing => 
                        existing.ElementId == inf.ElementId && 
                        existing.Effect == inf.Effect))
                    {
                        influences.Add(inf);
                    }
                }
                
                if (newInfluences.Any()) break;
            }

            return influences;
        }

        private List<ParameterInfluence> FlexStringParameter(FamilyParameter param, ParameterData paramData)
        {
            var influences = new List<ParameterInfluence>();

            string? currentValue = _familyMgr.CurrentType?.AsString(param);
            
            // Try toggling between common string values
            string[] testValues = { "Left", "Right", "Top", "Bottom", "Yes", "No", "True", "False" };
            
            foreach (string testValue in testValues)
            {
                if (testValue == currentValue) continue;

                try
                {
                    using (Transaction trans = new Transaction(_doc, "Flex String"))
                    {
                        trans.Start();
                        _familyMgr.Set(param, testValue);
                        _doc.Regenerate();
                        trans.Commit();
                    }

                    var flexedSnapshot = new GeometrySnapshot(_doc);
                    flexedSnapshot.CaptureAll();

                    var comparison = _baseline.CompareTo(flexedSnapshot);
                    
                    if (comparison.HasChanges)
                    {
                        influences.AddRange(ClassifyChanges(param.Definition.Name, comparison, 0, 0));
                        break; // Found a change, no need to test more values
                    }
                }
                catch
                {
                    // Value not valid for this parameter, try next one
                    continue;
                }
            }

            return influences;
        }

        private List<ParameterInfluence> ClassifyChanges(
            string parameterName, 
            ComparisonResult comparison, 
            double delta, 
            double baseValue)
        {
            var influences = new List<ParameterInfluence>();

            // Elements that appeared/disappeared = visibility toggle
            foreach (var addedId in comparison.AddedElements)
            {
                influences.Add(new ParameterInfluence
                {
                    Parameter = parameterName,
                    ElementId = addedId,
                    Effect = "visibilityToggle",
                    Description = "Element appears when parameter changes"
                });
            }

            foreach (var removedId in comparison.RemovedElements)
            {
                influences.Add(new ParameterInfluence
                {
                    Parameter = parameterName,
                    ElementId = removedId,
                    Effect = "visibilityToggle",
                    Description = "Element disappears when parameter changes"
                });
            }

            // Modified elements = scale, translation, or topology changes
            foreach (var kvp in comparison.ModifiedElements)
            {
                ElementId id = kvp.Key;
                GeometryChange change = kvp.Value;

                // Dimension changes = scaling
                if (change.DimensionChanges.Contains("X"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "scaleX",
                        Description = $"X dimension changes with parameter"
                    });
                }

                if (change.DimensionChanges.Contains("Y"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "scaleY",
                        Description = $"Y dimension changes with parameter"
                    });
                }

                if (change.DimensionChanges.Contains("Z"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "scaleZ",
                        Description = $"Z dimension changes with parameter"
                    });
                }

                // Translation changes
                if (change.TranslationChanges.Contains("X"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "translateX",
                        Description = $"X position changes with parameter"
                    });
                }

                if (change.TranslationChanges.Contains("Y"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "translateY",
                        Description = $"Y position changes with parameter"
                    });
                }

                if (change.TranslationChanges.Contains("Z"))
                {
                    influences.Add(new ParameterInfluence
                    {
                        Parameter = parameterName,
                        ElementId = id,
                        Effect = "translateZ",
                        Description = $"Z position changes with parameter"
                    });
                }

                // Topology changes = complex deformation or mirroring
                if (change.TopologyChanged)
                {
                    // Heuristic: if dimension changes are symmetric, might be a mirror
                    bool possibleMirror = change.DimensionChanges.Count == 0 && 
                                         change.TranslationChanges.Any();
                    
                    if (possibleMirror)
                    {
                        string axis = change.TranslationChanges.First();
                        influences.Add(new ParameterInfluence
                        {
                            Parameter = parameterName,
                            ElementId = id,
                            Effect = $"mirror{axis}",
                            Description = $"Element mirrors across {axis} axis"
                        });
                    }
                    else
                    {
                        influences.Add(new ParameterInfluence
                        {
                            Parameter = parameterName,
                            ElementId = id,
                            Effect = "topologyChange",
                            Description = "Geometry shape changes (complex deformation)"
                        });
                    }
                }
            }

            return influences;
        }
    }

    public class ParameterInfluence
    {
        public string Parameter { get; set; } = string.Empty;
        public ElementId ElementId { get; set; } = ElementId.InvalidElementId;
        public string Effect { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
