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
