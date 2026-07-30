namespace TraumaSurgeon.Anatomy
{
    /// <summary>
    /// Canonical part identifiers. JSON procedure data references these strings, so keeping
    /// them in one place stops typos from silently breaking a case.
    /// </summary>
    public static class AnatomyIds
    {
        // Surface layers
        public const string SkinAbdomen = "skin_abdomen";
        public const string FatAbdomen = "fat_abdomen";
        public const string MuscleAbdomen = "muscle_abdomen";
        public const string SkinChest = "skin_chest";
        public const string FatChest = "fat_chest";
        public const string MuscleChest = "muscle_chest";
        public const string SkinLeg = "skin_leg";
        public const string MuscleLeg = "muscle_leg";
        public const string SkinArm = "skin_arm";
        public const string Scalp = "scalp";

        // Skeleton
        public const string Ribs = "ribs";
        public const string Sternum = "sternum";
        public const string Skull = "skull";
        public const string FemurLeft = "femur_left";
        public const string FemurRight = "femur_right";
        public const string HumerusLeft = "humerus_left";

        // Thorax
        public const string LungLeft = "lung_left";
        public const string LungRight = "lung_right";
        public const string Heart = "heart";
        public const string CoronaryArtery = "coronary_artery";
        public const string PleuralSpace = "pleural_space";

        // Abdomen
        public const string Liver = "liver";
        public const string Spleen = "spleen";
        public const string Stomach = "stomach";
        public const string Intestines = "intestines";
        public const string Mesentery = "mesentery";
        public const string KidneyLeft = "kidney_left";
        public const string KidneyRight = "kidney_right";
        public const string Appendix = "appendix";
        public const string Gallbladder = "gallbladder";

        // Vessels
        public const string Aorta = "aorta";
        public const string VenaCava = "vena_cava";
        public const string FemoralArtery = "femoral_artery";

        // Head
        public const string Brain = "brain";
        public const string Hematoma = "hematoma";

        // Foreign bodies / implants
        public const string Bullet = "bullet";
        public const string FixationPlate = "fixation_plate";
        public const string BypassGraft = "bypass_graft";
    }
}
