using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DiscordBot.Models
{
    public static class RandomNumberGenerator
    {
        private static readonly RNGCryptoServiceProvider _Generator = new RNGCryptoServiceProvider();

        public static int NumberBetween(int minimumValue, int maximumValue)
        {
            byte[] randomNumber = new byte[1];

            _Generator.GetBytes(randomNumber);

            double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            //Use Math.Max, and subtracting 0.00000000001,
            //to ensure "multiplier" will always be between 0.0 and .99999999999
            //Otherwise, it's possible for it to be "1" which causes problems in rounding.

            double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

            //add one more to the range, to allow for the rounding done with Math.Floor
            int range = maximumValue - minimumValue + 1;

            double randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }
    }
}
