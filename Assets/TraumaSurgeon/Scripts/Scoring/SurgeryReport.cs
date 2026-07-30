using System;
using System.Collections.Generic;
using TraumaSurgeon.Core;
using UnityEngine;

namespace TraumaSurgeon.Scoring
{
    /// <summary>One scored line in the post-operation report.</summary>
    [Serializable]
    public class ScoreLine
    {
        public string label;
        public int points;
        public int maxPoints;
        public string detail;

        public ScoreLine(string label, int points, int maxPoints, string detail = "")
        {
            this.label = label;
            this.points = points;
            this.maxPoints = maxPoints;
            this.detail = detail;
        }
    }

    /// <summary>
    /// The complete outcome of one operation: raw metrics, the scored breakdown and a rank.
    /// </summary>
    [Serializable]
    public class SurgeryReport
    {
        public string procedureId;
        public string procedureName;
        public string patientName;
        public string difficulty;

        // Raw metrics
        public bool patientSurvived = true;
        public float finalStability;
        public float bloodLossMl;
        public float durationSeconds;
        public float parTimeSeconds = 480f;
        public int stepsCompleted;
        public int stepsTotal;
        public float accuracyAverage = 1f;
        public float tissueDamage;
        public int wrongToolUses;
        public int unnecessaryActions;
        public int complicationsTriggered;
        public int complicationsResolved;
        public int correctDoses;
        public int incorrectDoses;
        public int retainedItems;
        public float infectionRisk;
        public int unitsTransfused;

        // Scored breakdown
        public List<ScoreLine> lines = new List<ScoreLine>();
        public int totalScore;
        public int maxScore = 1000;
        public string rank = "C";
        public string summary;
        public List<string> notes = new List<string>();

        public SurgicalRank Rank
        {
            get
            {
                if (Enum.TryParse(rank, out SurgicalRank parsed))
                {
                    return parsed;
                }

                return SurgicalRank.C;
            }
        }

        public float Percentage => maxScore <= 0 ? 0f : Mathf.Clamp01((float)totalScore / maxScore);

        public string DurationText
        {
            get
            {
                int minutes = Mathf.FloorToInt(durationSeconds / 60f);
                int seconds = Mathf.FloorToInt(durationSeconds % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
        }
    }
}
