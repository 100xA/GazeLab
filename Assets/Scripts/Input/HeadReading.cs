// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
namespace GazeLab
{
    /// <summary>One head-pose reading (degrees) or a simulated nod.</summary>
    public struct HeadReading
    {
        public readonly bool IsAvailable;
        public readonly float Pitch, Yaw;
        /// <summary>Mouse simulation: a nod was triggered by key press.</summary>
        public readonly bool NodPressed;
        public readonly string Status;

        HeadReading(bool available, float pitch, float yaw, bool nodPressed, string status)
        {
            IsAvailable = available;
            Pitch = pitch;
            Yaw = yaw;
            NodPressed = nodPressed;
            Status = status ?? "";
        }

        public static HeadReading Unavailable(string status) { return new HeadReading(false, 0f, 0f, false, status); }
        public static HeadReading Angles(float pitch, float yaw) { return new HeadReading(true, pitch, yaw, false, "Head pose tracked"); }
        public static HeadReading Simulated(bool nodPressed, string status) { return new HeadReading(false, 0f, 0f, nodPressed, status); }
    }
}
