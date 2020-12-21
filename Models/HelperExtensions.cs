using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChartBlazorApp.Models
{
    public static class HelperExtensions
    {
        public static double?[] _toNullableArray(this IEnumerable<double> array, int roundDigit, double? defval = null)
        {
            return array.Select(x => x > 0 ? Math.Round(x, roundDigit) : defval).ToArray();
        }
    }
}
