using System.Collections.Generic;
using TraumaSurgeon.Anatomy;
using TraumaSurgeon.Core;
using TraumaSurgeon.Data;
using UnityEngine;

namespace TraumaSurgeon.Modes
{
    /// <summary>
    /// Training mode. Rather than duplicating the surgery pipeline, each station is compiled into
    /// a small in-memory <see cref="ProcedureData"/> and run through the normal SurgeryManager with
    /// the Student profile, where the patient cannot die.
    /// </summary>
    public static class TrainingManager
    {
        /// <summary>Builds and launches the drill for a station.</summary>
        public static void StartStation(TrainingStationData station)
        {
            if (station == null || !GameManager.Exists)
            {
                return;
            }

            ProcedureData procedure = BuildProcedure(station);
            GameManager.Instance.SelectCase(procedure, DifficultyLevel.Student, GameModeType.Training);
        }

        private static ProcedureData BuildProcedure(TrainingStationData station)
        {
            var procedure = new ProcedureData
            {
                id = "training_" + station.id,
                displayName = station.displayName + " (Training)",
                shortName = station.displayName,
                category = "Training",
                bodyRegion = RegionFor(station.Action),
                description = station.description,
                tutorial = "Training mode: the patient cannot die. Repeat the skill until the counter fills.",
                parTimeSeconds = Mathf.RoundToInt(station.timeLimitSeconds),
                payout = 0f,
                reputation = 0f,
                experience = 100,
                requiredTools = ToolsFor(station),
                possibleComplications = new string[0],
                failureConditions = new[] { "None - this is a practice station." },
                patient = BuildPatient(station),
                injuries = InjuriesFor(station),
                steps = StepsFor(station)
            };

            return procedure;
        }

        private static PatientTemplateData BuildPatient(TrainingStationData station)
        {
            var patient = new PatientTemplateData
            {
                name = "Training Manikin",
                age = 45,
                sex = "Simulated",
                weightKg = 75f,
                bloodType = "O-",
                allergies = "None",
                history = "High fidelity training manikin.",
                presentation = station.description,
                imagingCaption = "Simulated imaging for the training station.",
                imagingType = "XRay",
                startPain = 0f,
                infectionRisk = 0f
            };

            if (station.Action == SurgicalActionType.Compressions ||
                station.Action == SurgicalActionType.Defibrillate)
            {
                patient.startHeartRate = 0f;
                patient.startSystolic = 0f;
                patient.startDiastolic = 0f;
                patient.startSpO2 = 62f;
            }

            if (station.Action == SurgicalActionType.InsertChestTube)
            {
                patient.startSpO2 = 84f;
                patient.startRespRate = 26f;
            }

            return patient;
        }

        private static string RegionFor(SurgicalActionType action)
        {
            switch (action)
            {
                case SurgicalActionType.InsertChestTube:
                case SurgicalActionType.Compressions:
                case SurgicalActionType.Defibrillate:
                    return "Chest";
                case SurgicalActionType.Drill:
                    return "Limb";
                default:
                    return "Abdomen";
            }
        }

        private static string[] ToolsFor(TrainingStationData station)
        {
            var tools = new List<string>();
            if (station.Tool != ToolType.None)
            {
                tools.Add(station.Tool.ToString());
            }

            switch (station.Action)
            {
                case SurgicalActionType.Suture:
                    tools.Add(ToolType.NeedleHolder.ToString());
                    tools.Add(ToolType.Forceps.ToString());
                    break;
                case SurgicalActionType.Incise:
                    tools.Add(ToolType.Scalpel.ToString());
                    break;
                case SurgicalActionType.Clamp:
                case SurgicalActionType.Cauterize:
                    tools.Add(ToolType.Hemostat.ToString());
                    tools.Add(ToolType.Electrocautery.ToString());
                    tools.Add(ToolType.Suction.ToString());
                    break;
                case SurgicalActionType.Drill:
                    tools.Add(ToolType.SurgicalDrill.ToString());
                    tools.Add(ToolType.Scalpel.ToString());
                    break;
                case SurgicalActionType.InsertChestTube:
                    tools.Add(ToolType.ChestTube.ToString());
                    tools.Add(ToolType.Scalpel.ToString());
                    break;
                case SurgicalActionType.Compressions:
                    tools.Add(ToolType.Hands.ToString());
                    break;
                case SurgicalActionType.Defibrillate:
                    tools.Add(ToolType.Defibrillator.ToString());
                    tools.Add(ToolType.Hands.ToString());
                    break;
                case SurgicalActionType.Laparoscope:
                    tools.Add(ToolType.LaparoscopicCamera.ToString());
                    tools.Add(ToolType.LaparoscopicGrasper.ToString());
                    break;
                case SurgicalActionType.Grasp:
                    tools.Add(ToolType.Forceps.ToString());
                    tools.Add(ToolType.Hemostat.ToString());
                    tools.Add(ToolType.Retractor.ToString());
                    break;
            }

            return tools.ToArray();
        }

        private static InjuryData[] InjuriesFor(TrainingStationData station)
        {
            switch (station.Action)
            {
                case SurgicalActionType.Suture:
                    return new[]
                    {
                        new InjuryData
                        {
                            partId = AnatomyIds.SkinAbdomen, damageState = "Lacerated", severity = 40f,
                            bleedRate = 0.4f, requiresRepair = true
                        }
                    };

                case SurgicalActionType.Clamp:
                case SurgicalActionType.Cauterize:
                    return new[]
                    {
                        new InjuryData
                        {
                            partId = AnatomyIds.Mesentery, damageState = "Lacerated", severity = 45f,
                            bleedRate = 4f, requiresRepair = true
                        },
                        new InjuryData
                        {
                            partId = AnatomyIds.Liver, damageState = "Lacerated", severity = 30f,
                            bleedRate = 3f, requiresRepair = true
                        }
                    };

                case SurgicalActionType.Drill:
                    return new[]
                    {
                        new InjuryData
                        {
                            partId = AnatomyIds.FemurLeft, damageState = "Ruptured", severity = 60f,
                            bleedRate = 0.5f, requiresRepair = true
                        }
                    };

                case SurgicalActionType.InsertChestTube:
                    return new[]
                    {
                        new InjuryData
                        {
                            partId = AnatomyIds.LungLeft, damageState = "Bruised", severity = 25f,
                            bleedRate = 0f, requiresRepair = false
                        }
                    };

                default:
                    return new InjuryData[0];
            }
        }

        private static ProcedureStepData[] StepsFor(TrainingStationData station)
        {
            var steps = new List<ProcedureStepData>();

            // Every station starts by prepping so anaesthesia and the field are set up.
            steps.Add(new ProcedureStepData
            {
                id = "prep",
                title = "Prepare the station",
                description = "Walk to the table and press E on the patient prep zone.",
                action = SurgicalActionType.PrepPatient.ToString(),
                hint = "Press E at the highlighted prep zone beside the table.",
                repetitions = 1
            });

            switch (station.Action)
            {
                case SurgicalActionType.Suture:
                    steps.Add(Step("expose", "Open the practice incision", SurgicalActionType.Incise,
                        AnatomyIds.SkinAbdomen, ToolType.Scalpel,
                        "Cut along the dotted guide line with the scalpel.", 1));
                    steps.Add(Step("suture", "Place your stitches", SurgicalActionType.Suture,
                        AnatomyIds.SkinAbdomen, ToolType.NeedleHolder,
                        "Space the stitches evenly along the wound.", station.targetRepetitions));
                    break;

                case SurgicalActionType.Incise:
                    steps.Add(Step("incise", "Controlled incisions", SurgicalActionType.Incise,
                        AnatomyIds.SkinAbdomen, ToolType.Scalpel,
                        "Stay on the guide line. Hold Shift for a steady hand.", 1));
                    steps.Add(Step("incise_fat", "Open the fat layer", SurgicalActionType.Incise,
                        AnatomyIds.FatAbdomen, ToolType.Scalpel, "Take the next layer down.", 1));
                    steps.Add(Step("incise_muscle", "Open the muscle layer", SurgicalActionType.Incise,
                        AnatomyIds.MuscleAbdomen, ToolType.Scalpel, "Careful - organs sit underneath.", 1));
                    break;

                case SurgicalActionType.Clamp:
                    steps.Add(OpenAbdomenSteps(steps));
                    steps.Add(Step("clamp", "Clamp every bleeder", SurgicalActionType.Clamp,
                        string.Empty, ToolType.Hemostat,
                        "Aim at the pulsing red markers and click.", station.targetRepetitions));
                    break;

                case SurgicalActionType.Cauterize:
                    steps.Add(OpenAbdomenSteps(steps));
                    steps.Add(Step("cauterise", "Seal the small bleeders", SurgicalActionType.Cauterize,
                        string.Empty, ToolType.Electrocautery,
                        "Hold the left button on a bleeder to seal it.", station.targetRepetitions));
                    break;

                case SurgicalActionType.Drill:
                    steps.Add(Step("skin", "Open the thigh", SurgicalActionType.Incise,
                        AnatomyIds.SkinLeg, ToolType.Scalpel, "Follow the guide line.", 1));
                    steps.Add(Step("muscle", "Open the muscle", SurgicalActionType.Incise,
                        AnatomyIds.MuscleLeg, ToolType.Scalpel, "Expose the femur.", 1));
                    steps.Add(Step("drill", "Drill pilot holes", SurgicalActionType.Drill,
                        AnatomyIds.FemurLeft, ToolType.SurgicalDrill,
                        "Space the holes out along the bone.", station.targetRepetitions));
                    break;

                case SurgicalActionType.InsertChestTube:
                    steps.Add(Step("skin", "Incise the chest wall", SurgicalActionType.Incise,
                        AnatomyIds.SkinChest, ToolType.Scalpel, "Cut along the guide line.", 1));
                    steps.Add(Step("fat", "Open the fat layer", SurgicalActionType.Incise,
                        AnatomyIds.FatChest, ToolType.Scalpel, "Keep going down.", 1));
                    steps.Add(Step("muscle", "Open the muscle layer", SurgicalActionType.Incise,
                        AnatomyIds.MuscleChest, ToolType.Scalpel, "The pleural space is underneath.", 1));
                    steps.Add(Step("tube", "Insert the chest tube", SurgicalActionType.InsertChestTube,
                        AnatomyIds.PleuralSpace, ToolType.ChestTube,
                        "Aim for the pleural space, not the lung.", 1));
                    steps.Add(Step("confirm", "Confirm placement", SurgicalActionType.Confirm,
                        AnatomyIds.PleuralSpace, ToolType.ChestTube,
                        "Right click with the chest tube to confirm and drain.", 1));
                    break;

                case SurgicalActionType.Compressions:
                    steps.Add(Step("cpr", "Deliver quality compressions", SurgicalActionType.Compressions,
                        string.Empty, ToolType.Hands,
                        "Click at about 110 per minute on the chest.", station.targetRepetitions));
                    break;

                case SurgicalActionType.Defibrillate:
                    steps.Add(Step("cpr", "Compressions first", SurgicalActionType.Compressions,
                        string.Empty, ToolType.Hands, "Build up perfusion before you shock.", 2));
                    steps.Add(Step("shock", "Charge and shock", SurgicalActionType.Defibrillate,
                        string.Empty, ToolType.Defibrillator,
                        "Right click to charge, left click to shock.", station.targetRepetitions));
                    break;

                case SurgicalActionType.Laparoscope:
                    steps.Add(Step("scope", "Insert the laparoscope", SurgicalActionType.Laparoscope,
                        string.Empty, ToolType.LaparoscopicCamera,
                        "Left click to insert the scope and open the video feed.", 1));
                    steps.Add(Step("navigate", "Identify the structures", SurgicalActionType.Inspect,
                        string.Empty, ToolType.LaparoscopicCamera,
                        "Right click on structures to identify them on the monitor.",
                        station.targetRepetitions));
                    break;

                default:
                    steps.Add(Step("handle", "Handle the instruments", SurgicalActionType.Grasp,
                        string.Empty, ToolType.None,
                        "Switch tools with 1-9 and use them on the manikin.", station.targetRepetitions));
                    break;
            }

            return steps.ToArray();
        }

        /// <summary>Adds the three abdominal layer steps and returns the last one.</summary>
        private static ProcedureStepData OpenAbdomenSteps(List<ProcedureStepData> steps)
        {
            steps.Add(Step("skin", "Open the skin", SurgicalActionType.Incise,
                AnatomyIds.SkinAbdomen, ToolType.Scalpel, "Follow the guide line.", 1));
            steps.Add(Step("fat", "Open the fat", SurgicalActionType.Incise,
                AnatomyIds.FatAbdomen, ToolType.Scalpel, "Second layer.", 1));
            return Step("muscle", "Open the abdominal wall", SurgicalActionType.Incise,
                AnatomyIds.MuscleAbdomen, ToolType.Scalpel, "Now you can reach the bleeders.", 1);
        }

        private static ProcedureStepData Step(string id, string title, SurgicalActionType action,
            string target, ToolType tool, string hint, int repetitions)
        {
            return new ProcedureStepData
            {
                id = id,
                title = title,
                description = hint,
                action = action.ToString(),
                targetPart = target,
                requiredTool = tool == ToolType.None ? string.Empty : tool.ToString(),
                hint = hint,
                repetitions = Mathf.Max(1, repetitions)
            };
        }
    }
}
