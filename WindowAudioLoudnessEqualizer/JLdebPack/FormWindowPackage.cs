﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JLdebPack
{
    public class FormWindowPackage
    {
        public enum PointMode { Position, Offset, AboveTaskbar };
        public System.Drawing.Point PointFromMouse(int xVal, int yVal, PointMode mode)
        {
            System.Drawing.Point p = System.Windows.Forms.Cursor.Position;
            switch (mode)
            {
                case PointMode.AboveTaskbar:
                    p.X += (int)(xVal);
                    p.Y = (int)(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height + yVal);
                    break;
                case PointMode.Position:
                    p.X = (int)(xVal);
                    p.Y = (int)(yVal);
                    break;
                case PointMode.Offset:
                    p.X += (int)(xVal);
                    p.Y += (int)(yVal);
                    break;
            }
            return p;
        }
        public System.Drawing.Point CurrentFormToAboveTaskbar(System.Drawing.Point p, System.Drawing.Size s)
        {
            p.Y = (int)(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - s.Height);
            return p;
        }
    }
}
