﻿using System;
using System.Collections.Generic;

namespace DeterministicWorld
{
    public abstract partial class dwWorld2D
    {
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
                obj.destroy();
            }
        }

        //Data accessors
        public dwObject2D[] getObjects()
        {
            return objects.ToArray();
        }
    }
}
