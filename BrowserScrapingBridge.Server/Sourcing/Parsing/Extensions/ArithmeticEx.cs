#nullable disable warnings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserScrapingBridge.Server.Sourcing.Parsing.Extensions
{
    public static class ArithmeticEx
    {
        public static int Ceiling(this int org, int value)
        {
            int standard = 1;
            for (int i = 0; i < value; i++) { standard *= 10; }
            int temp = Convert.ToInt32((((int)org / 10) + 1) * 10 * standard);

            int result = (int)temp / standard;

            return result;
        }
        public static long Ceiling(this long org, int value)
        {
            int standard = 1;
            for (int i = 0; i < value; i++) { standard *= 10; }
            var tmp = (int)org / 10;
            var tmp2 = (int)tmp % 10;

            long temp = Convert.ToInt64((tmp2 != 0 ? tmp + 1 : tmp) * 10 * standard);

            long result = (long)temp / standard;

            return result;
        }
        public static float Ceiling(this float org, int value, int amount = 10)
        {
            int standard = 1;
            for (int i = 0; i < value; i++) { standard *= 10; }
            //long temp = Convert.ToInt64((((int)org / 10) + 1) * 10 * standard);
            long temp = Convert.ToInt64(((org / amount)) * amount * standard);

            float result = (float)temp / standard;

            return result;
        }
        public static float Ceiling(this float? org, int value, int amount = 10)
        {
            int standard = 1;
            for (int i = 0; i < value; i++) { standard *= 10; }
            //long temp = Convert.ToInt64((((int?)org / 10) + 1) * 10 * standard);
            long temp = Convert.ToInt64((((double?)org / amount)) * amount * standard);

            float result = (float)temp / standard;

            return result;
        }
        public static double Ceiling(this double org, int value, int amount = 10)
        {
            //var origianl = Math.Floor(org);
            //임시
            if (amount == 1) return Math.Round(org, value);

            var origianl = Math.Round(org, value);
            int standard = 1;

            for (int i = 0; i < value; i++) { standard *= 10; }
            var tmp = (long)(origianl * amount) / (1 * amount * amount);
            var tmp2 = (long)(origianl * amount) % (1 * amount * amount);
            long temp = Convert.ToInt64((tmp2 != 0 ? tmp + 1 : tmp) * amount * standard);
            //long temp = Convert.ToInt64((((double)org / amount)) * amount * standard);

            double result = (double)temp / standard;

            return result;
        }
        public static decimal Ceiling(this decimal org, int value)
        {
            int standard = 1;
            for (int i = 0; i < value; i++) { standard *= 10; }
            long temp = Convert.ToInt64((((double)org / 10) + 1) * 10 * standard);

            decimal result = (decimal)temp / standard;

            return result;
        }
    }
}
