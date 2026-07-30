namespace TraumaSurgeon.Audio
{
    /// <summary>
    /// Every sound cue in the game. Clips are generated procedurally at boot
    /// (see <see cref="ProceduralAudio"/>) so the project ships with no binary audio assets.
    /// Drop real .wav files into Resources/TraumaSurgeonAudio/&lt;SoundId&gt;.wav to override any cue.
    /// </summary>
    public enum SoundId
    {
        MonitorBeep,
        MonitorBeepCritical,
        Flatline,
        OxygenAlarm,
        BloodPressureAlarm,
        Heartbeat,
        Incision,
        ScissorsCut,
        ClampClick,
        Suction,
        Cautery,
        Drill,
        BoneSaw,
        StaplerFire,
        SutureStitch,
        DefibCharge,
        DefibShock,
        Irrigation,
        ToolPickup,
        ToolDrop,
        UiClick,
        UiHover,
        ObjectiveComplete,
        SuccessChime,
        FailureBuzz,
        Warning,
        Ambience,
        Announcement,
        Ventilator
    }
}
