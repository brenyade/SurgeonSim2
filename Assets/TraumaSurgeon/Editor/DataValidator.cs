using System.Collections.Generic;
using System.Text;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using UnityEditor;
using UnityEngine;

namespace TraumaSurgeon.EditorTools
{
    /// <summary>
    /// Validates the JSON content: every anatomy id, tool name, action name and complication
    /// referenced by a procedure must exist. Run it after editing the data files.
    /// </summary>
    public static class DataValidator
    {
        [MenuItem("Trauma Surgeon/Validate Data Files", priority = 21)]
        public static void Validate()
        {
            DataLibrary.Reload();

            var errors = new List<string>();
            var warnings = new List<string>();

            HashSet<string> anatomyIds = CollectAnatomyIds();

            if (DataLibrary.Procedures.Count == 0)
            {
                errors.Add("No procedures loaded.");
            }

            if (DataLibrary.Tools.Count == 0)
            {
                errors.Add("No tools loaded.");
            }

            foreach (ToolData tool in DataLibrary.Tools)
            {
                if (tool.Type == ToolType.None)
                {
                    errors.Add($"Tool '{tool.id}' does not match any ToolType.");
                }

                if (tool.Primary == SurgicalActionType.None)
                {
                    warnings.Add($"Tool '{tool.id}' has no primary action.");
                }
            }

            foreach (ProcedureData procedure in DataLibrary.Procedures)
            {
                string context = $"Procedure '{procedure.id}'";

                if (procedure.steps == null || procedure.steps.Length == 0)
                {
                    errors.Add($"{context} has no steps.");
                    continue;
                }

                foreach (string toolName in procedure.requiredTools)
                {
                    if (!System.Enum.TryParse(toolName, true, out ToolType _))
                    {
                        errors.Add($"{context} requires unknown tool '{toolName}'.");
                    }
                }

                foreach (string complication in procedure.possibleComplications)
                {
                    if (!System.Enum.TryParse(complication, true, out ComplicationType type))
                    {
                        errors.Add($"{context} lists unknown complication '{complication}'.");
                    }
                    else if (DataLibrary.GetComplication(type) == null)
                    {
                        warnings.Add($"{context} lists complication '{complication}' with no data entry.");
                    }
                }

                foreach (InjuryData injury in procedure.injuries)
                {
                    if (!anatomyIds.Contains(injury.partId))
                    {
                        errors.Add($"{context} injures unknown anatomy id '{injury.partId}'.");
                    }
                }

                foreach (ProcedureStepData step in procedure.steps)
                {
                    string stepContext = $"{context} step '{step.id}'";

                    if (step.Action == SurgicalActionType.None)
                    {
                        errors.Add($"{stepContext} has an unknown action '{step.action}'.");
                    }

                    if (!string.IsNullOrEmpty(step.targetPart) && !anatomyIds.Contains(step.targetPart))
                    {
                        errors.Add($"{stepContext} targets unknown anatomy id '{step.targetPart}'.");
                    }

                    if (!string.IsNullOrEmpty(step.requiredTool))
                    {
                        if (!System.Enum.TryParse(step.requiredTool, true, out ToolType toolType))
                        {
                            errors.Add($"{stepContext} requires unknown tool '{step.requiredTool}'.");
                        }
                        else if (System.Array.IndexOf(procedure.requiredTools, step.requiredTool) < 0 &&
                                 toolType != ToolType.Hands && toolType != ToolType.Sponge &&
                                 toolType != ToolType.Suction && toolType != ToolType.Syringe)
                        {
                            warnings.Add($"{stepContext} needs '{step.requiredTool}' but it is not in " +
                                         "the procedure's requiredTools list, so it will not be on the tray.");
                        }
                    }
                }
            }

            foreach (ChallengeData challenge in DataLibrary.Challenges)
            {
                if (DataLibrary.GetProcedure(challenge.procedureId) == null)
                {
                    errors.Add($"Challenge '{challenge.id}' references unknown procedure " +
                               $"'{challenge.procedureId}'.");
                }
            }

            foreach (TrainingStationData station in DataLibrary.TrainingStations)
            {
                if (station.Action == SurgicalActionType.None)
                {
                    warnings.Add($"Training station '{station.id}' has an unknown action.");
                }
            }

            var report = new StringBuilder();
            report.AppendLine("[Trauma Surgeon] Data validation:");
            report.AppendLine($"  Procedures: {DataLibrary.Procedures.Count}");
            report.AppendLine($"  Tools: {DataLibrary.Tools.Count}");
            report.AppendLine($"  Complications: {DataLibrary.Complications.Count}");
            report.AppendLine($"  Training stations: {DataLibrary.TrainingStations.Count}");
            report.AppendLine($"  Challenges: {DataLibrary.Challenges.Count}");
            report.AppendLine($"  Upgrades: {DataLibrary.Upgrades.Count}");

            foreach (string warning in warnings)
            {
                report.AppendLine("  WARNING: " + warning);
            }

            foreach (string error in errors)
            {
                report.AppendLine("  ERROR: " + error);
            }

            if (errors.Count > 0)
            {
                Debug.LogError(report.ToString());
            }
            else if (warnings.Count > 0)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                report.AppendLine("  All references valid.");
                Debug.Log(report.ToString());
            }
        }

        /// <summary>Reads the anatomy ids straight out of <see cref="AnatomyIds"/> by reflection.</summary>
        private static HashSet<string> CollectAnatomyIds()
        {
            var ids = new HashSet<string>();
            foreach (System.Reflection.FieldInfo field in typeof(AnatomyIds).GetFields(
                         System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    ids.Add((string)field.GetRawConstantValue());
                }
            }

            return ids;
        }
    }
}
