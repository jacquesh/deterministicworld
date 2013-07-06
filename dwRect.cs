using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeterministicWorld
{
    public struct dwRect
    {
        public int Left
        {
            get;
            private set;
        }

        public int Top
        {
            get;
            private set;
        }
        public int Width
        {
            get;
            private set;
        }
        public int Height
        {
            get;
            private set;
        }

        public int Right
        {
            get
            { return Left + Width; }
        }
        public int Bottom
        {
            get
            { return Top + Height; }
        }

        public dwRect(int x, int y, int width, int height)
            : this()
        {
            Left = x;
            Top = y;
            Width = width;
            Height = height;
        }

        public bool intersects(dwRect other)
        {
            if (Left < other.Right && Right > other.Left)
                if (Top < other.Bottom && Bottom > other.Top)
                    return true;

            return false;
        }

    }
}
