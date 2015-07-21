using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {
    class GdiPlusUtils {
        private enum DriverStringOptions {
            CmapLookup = 1,
            Vertical = 2,
            Advance = 4,
            LimitSubpixel = 8,
        }

        [DllImport("Gdiplus.dll", CharSet=CharSet.Unicode)]
        private extern static int GdipDrawDriverString(IntPtr hG, string text, int length, IntPtr hFont, IntPtr hBrush, PointF[] positions, int flags, IntPtr matrix);

        public static void DrawString(Graphics g, string text, Font font, Brush brush, PointF[] positions, Matrix matrix) {
            if (g == null)
                throw new ArgumentNullException("g");
            if (font == null)
                throw new ArgumentNullException("font");
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (positions == null)
                throw new ArgumentNullException("positions");

            FieldInfo hGField = typeof(Graphics).GetField("nativeGraphics", BindingFlags.Instance | BindingFlags.NonPublic);
            IntPtr hG = (IntPtr)hGField.GetValue(g);

            FieldInfo hFontField = typeof(Font).GetField("nativeFont", BindingFlags.Instance | BindingFlags.NonPublic);
            IntPtr hFont = (IntPtr)hFontField.GetValue(font);

            FieldInfo hBrushField = typeof(Brush).GetField("nativeBrush", BindingFlags.Instance | BindingFlags.NonPublic);
            IntPtr hBrush = (IntPtr)hBrushField.GetValue(brush);

            IntPtr hMatrix = IntPtr.Zero;
            if (matrix != null) {
                FieldInfo hMatrixField = typeof(Matrix).GetField("nativeMatrix", BindingFlags.Instance | BindingFlags.NonPublic);
                hMatrix = (IntPtr)hMatrixField.GetValue(matrix);
            }

            int result = GdipDrawDriverString(hG, text, text.Length, hFont, hBrush, positions, (int)DriverStringOptions.CmapLookup, hMatrix);
        }
    }
}
