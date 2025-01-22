using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Helper
{
    public static class RandomGenerator
    {
        private static Random Random = new Random();
        public static string GenerateString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[Random.Next(x.Length)]).ToArray());
        }
        public static int GenerateNumber(int min , int max)
        {
            return Random.Next(min, max);
        }
    }
}
