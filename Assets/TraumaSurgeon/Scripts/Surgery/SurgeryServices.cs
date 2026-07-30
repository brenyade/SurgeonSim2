using TraumaSurgeon.Patient;

namespace TraumaSurgeon.Surgery
{
    /// <summary>
    /// Tiny service locator scoped to a single operation. SurgeryManager populates it when a case
    /// is set up and clears it on teardown, so tools and UI can reach surgery-scoped components
    /// without hunting through the hierarchy.
    /// </summary>
    public static class SurgeryServices
    {
        public static PatientController Patient;
        public static SpongeTracker Sponges;
        public static LaparoscopySystem Laparoscopy;

        public static void Clear()
        {
            Patient = null;
            Sponges = null;
            Laparoscopy = null;
        }
    }
}
