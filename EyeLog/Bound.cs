using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EyeLog
{
    internal class Bound
    {
        int x;
        int y;
        int width;
        int height;
        public Bound(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool isInside(int x, int y)
        {
            return (this.x<x && this.x+width>x)&&(this.y<y && this.y+height>y);
        }
    }
}
