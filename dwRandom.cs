using System;

namespace DeterministicWorld
{
    public class dwRandom
    {
        private Random prng;

        public dwRandom(int seed)
        {
            prng = new Random(seed);
        }

        public int get()
        {
            return prng.Next();
        }

    }
}
