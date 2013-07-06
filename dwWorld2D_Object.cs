using System;
using System.Collections.Generic;

namespace DeterministicWorld
{
    public abstract partial class dwWorld2D
    {
        private List<dwObject2D> objects;

        //Object interaction
        public void addObject(dwObject2D obj)
        {
            if (obj != null)
            {
                objects.Add(obj);
            }
        }

        public void removeObject(dwObject2D obj)
        {
            if (obj != null)
            {
                objects.Remove(obj);
            }
        }

        //Data accessors
        public dwObject2D[] getObjects()
        {
            return objects.ToArray();
        }
    }
}
