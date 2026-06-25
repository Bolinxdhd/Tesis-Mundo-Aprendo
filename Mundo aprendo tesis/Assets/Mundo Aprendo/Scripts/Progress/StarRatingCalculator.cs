using UnityEngine;

namespace Bolin
{
    public static class StarRatingCalculator
    {
        public static int FromMistakes(int mistakes, int threeStarMaxErrors, int twoStarMaxErrors, int oneStarMaxErrors)
        {
            int safeMistakes = Mathf.Max(0, mistakes);
            int safeThree = Mathf.Max(0, threeStarMaxErrors);
            int safeTwo = Mathf.Max(safeThree, twoStarMaxErrors);
            int safeOne = Mathf.Max(safeTwo, oneStarMaxErrors);

            if (safeMistakes <= safeThree) return 3;
            if (safeMistakes <= safeTwo) return 2;
            if (safeMistakes <= safeOne) return 1;
            return 0;
        }
    }
}
