using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace roblox32bitpainter_starving_artist_
{
    public class PixelToDraw
    {
        public Color color { get; set; }
        public Vector point { get; set; }
        public PixelToDraw(Color colord, Vector pointd)
        {
            color = colord;
            point = pointd;
        }
    }
}
