// Jan Biernacki, 3275061; Malte Lührs, 3138900; Kürsat Yurdakul, 3282791; Paul Prigge, 3097391
using System.Globalization;

namespace GazeLab
{
    public static class Format
    {
        public static string Number(float value, string format)
        {
            return value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
