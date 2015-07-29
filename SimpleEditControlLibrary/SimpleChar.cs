using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {

    class SimpleChar {
        public static SimpleChar LineEnd {
            get { var sc = new SimpleChar('\0', 0, 0); sc.IsLineEnd = true; return sc; }
        }

        public char Ch { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public float Left { get { return X; } }
        public float Top { get { return Y; } }
        public float Right { get { return X + Width; } }
        public float Bottom { get { return Y + Height; } }

        public bool IsLineEnd { get; private set; }

        public SimpleLine Line { get; set; }

        public SimpleChar(char ch, float width, float height) {
            this.Ch = ch;
            this.Width = width;
            this.Height = height;
            this.X = 0;
            this.Y = 0;
        }

        public bool isHanzi() {
            return Ch >= 0x4e00 && Ch <= 0x9fbb;
        }

        public bool IsPrintableChar() {
            return !IsLineEnd;
        }

        public override string ToString() {
            return string.Format("{0} ({1},{2})", IsLineEnd ? "LineEnd" : Ch.ToString(), X, Y);
        }
    }
}
