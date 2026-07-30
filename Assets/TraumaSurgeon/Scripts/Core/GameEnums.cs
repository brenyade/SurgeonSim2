namespace TraumaSurgeon.Core
{
    /// <summary>High level state of the application.</summary>
    public enum GamePhase
    {
        Boot,
        MainMenu,
        CaseSelect,
        Briefing,
        Surgery,
        PostOp,
        Hospital,
        Paused
    }

    /// <summary>Which top level mode the player picked from the main menu.</summary>
    public enum GameModeType
    {
        Career,
        PlayNow,
        Training,
        Challenge
    }

    /// <summary>Difficulty presets. See <see cref="DifficultyProfile"/> for the tuning values.</summary>
    public enum DifficultyLevel
    {
        Student = 0,
        Resident = 1,
        Attending = 2,
        ImpossibleShift = 3
    }

    /// <summary>Every discrete verb the surgical simulation understands.</summary>
    public enum SurgicalActionType
    {
        None = 0,
        ReviewChart,
        ReviewImaging,
        ScrubIn,
        PrepPatient,
        Incise,
        Cut,
        Retract,
        Suction,
        Clamp,
        Cauterize,
        Irrigate,
        Repair,
        Remove,
        Suture,
        Staple,
        Drill,
        Saw,
        Implant,
        SpreadRibs,
        InsertChestTube,
        Defibrillate,
        Compressions,
        Medicate,
        Laparoscope,
        Grasp,
        Inspect,
        Pack,
        Close,
        Confirm
    }

    /// <summary>Anatomical depth. Used to gate what the player can reach.</summary>
    public enum AnatomyLayer
    {
        Skin = 0,
        Fat = 1,
        Muscle = 2,
        Bone = 3,
        Cavity = 4,
        Organ = 5,
        Vessel = 6
    }

    /// <summary>Visual/mechanical condition of an anatomy part.</summary>
    public enum DamageState
    {
        Healthy,
        Bruised,
        Lacerated,
        Perforated,
        Ruptured,
        Removed,
        Repaired
    }

    public enum CardiacRhythm
    {
        NormalSinus,
        Tachycardia,
        Bradycardia,
        VentricularFibrillation,
        VentricularTachycardia,
        Asystole,
        PEA
    }

    public enum ConsciousnessLevel
    {
        Alert,
        Drowsy,
        Sedated,
        Unconscious,
        Anesthetized
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Critical
    }

    public enum StaffRole
    {
        Anesthesiologist,
        ScrubNurse,
        CirculatingNurse,
        SurgicalAssistant,
        ConsultantSurgeon
    }

    public enum StaffCommand
    {
        IncreaseAnesthesia,
        DecreaseAnesthesia,
        GiveBlood,
        StartCompressions,
        PrepareDefibrillator,
        Suction,
        HoldRetraction,
        CallAnotherSurgeon,
        RequestImaging,
        PrepareEmergencyClosure,
        CountSponges,
        GiveAntibiotics
    }

    public enum ScoreEventType
    {
        StepCompleted,
        PrecisionGood,
        PrecisionPoor,
        WrongTool,
        UnnecessaryAction,
        TissueDamage,
        BloodLoss,
        ComplicationResolved,
        ComplicationIgnored,
        MedicationCorrect,
        MedicationWrong,
        RetainedItem,
        InfectionRisk,
        PatientDeath
    }

    public enum SurgicalRank
    {
        F, D, C, B, A, S
    }

    /// <summary>Career ladder. Index doubles as the unlock level.</summary>
    public enum CareerRank
    {
        MedicalStudent = 0,
        JuniorResident = 1,
        SeniorResident = 2,
        SurgicalFellow = 3,
        AttendingSurgeon = 4,
        TraumaDirector = 5,
        ChiefOfSurgery = 6
    }

    public enum ComplicationType
    {
        SuddenHemorrhage,
        CardiacArrest,
        VentricularFibrillation,
        RespiratoryArrest,
        TensionPneumothorax,
        AllergicReaction,
        EquipmentMalfunction,
        AnesthesiaOverdose,
        BloodPressureCollapse,
        AccidentalVesselInjury,
        AccidentalOrganPerforation,
        SurgicalFire,
        RetainedSponge,
        InfectionRisk,
        IncorrectMedicationDose
    }

    /// <summary>Identifies a tool. Values are stable so saves/JSON can reference them.</summary>
    public enum ToolType
    {
        None = 0,
        Scalpel,
        TraumaShears,
        Forceps,
        Hemostat,
        Retractor,
        Suction,
        Electrocautery,
        NeedleHolder,
        SutureNeedle,
        SkinStapler,
        RibSpreader,
        BoneSaw,
        SurgicalDrill,
        LaparoscopicCamera,
        LaparoscopicGrasper,
        Irrigation,
        Defibrillator,
        Syringe,
        ChestTube,
        Sponge,
        FixationPlate,
        Hands
    }
}
