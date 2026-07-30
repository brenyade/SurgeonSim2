using System;
using System.Collections.Generic;

namespace TraumaSurgeon.Save
{
    [Serializable]
    public class CaseRecord
    {
        public string procedureId;
        public string patientName;
        public string rank;
        public int score;
        public bool survived;
        public float durationSeconds;
        public float bloodLossMl;
        public int complications;
        public string notes;
        public string timestamp;
    }

    [Serializable]
    public class BestScoreEntry
    {
        public string procedureId;
        public int score;
        public string rank;
    }

    [Serializable]
    public class StaffRecord
    {
        public string role;
        public string name;
        public int skill = 3;         // 1..10
        public float salary = 1200f;
        public int trainingLevel;
    }

    [Serializable]
    public class IncidentReport
    {
        public string title;
        public string body;
        public string severity = "Minor";
        public bool acknowledged;
    }

    /// <summary>Everything the career mode persists.</summary>
    [Serializable]
    public class CareerData
    {
        public string surgeonName = "Dr. A. Vance";
        public int rank;                    // CareerRank index
        public int experience;
        public float money = 12000f;
        public float reputation = 50f;      // 0..100
        public float malpracticeRisk = 5f;  // 0..100
        public float hospitalRating = 3f;   // 0..5
        public int patientsTreated;
        public int patientsLost;
        public int shiftNumber = 1;

        public List<string> unlockedProcedures = new List<string>();
        public List<string> unlockedTools = new List<string>();
        public List<string> purchasedUpgrades = new List<string>();
        public List<string> completedTraining = new List<string>();
        public List<string> completedChallenges = new List<string>();
        public List<CaseRecord> caseHistory = new List<CaseRecord>();
        public List<BestScoreEntry> bestScores = new List<BestScoreEntry>();
        public List<StaffRecord> staff = new List<StaffRecord>();
        public List<IncidentReport> incidentReports = new List<IncidentReport>();
        public List<string> pendingCaseIds = new List<string>();
    }

    /// <summary>Root save file for one slot.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public string slotName = "New Career";
        public string lastPlayed;
        public float totalPlaySeconds;
        public CareerData career = new CareerData();
    }
}
