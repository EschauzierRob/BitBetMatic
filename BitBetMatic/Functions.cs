using System;

namespace BitBetMatic
{
    public static class Functions
    {
        public static decimal ToDecimal(double? doubleVal) => doubleVal.HasValue ? Convert.ToDecimal(doubleVal.Value) : 0;

        public static decimal GetLower(decimal a, decimal b) => a>b? b:a;
        public static decimal GetHigher(decimal a, decimal b) => a<b? b:a;
    }
}