// Team: Jan Biernacki, Kürsat, Paul, Malte | Matriculation numbers: TO BE PROVIDED
using System.Globalization;

namespace GazeLab
{
    /// <summary>Number formatting independent of the Windows language (always a decimal point).</summary>
    public static class Format
    {
        public static string Number(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
