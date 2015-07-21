using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {

    class SimpleChar {
        public char Ch { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float X { get; set; }
        public float Y { get; set; }

        public SimpleChar(char ch, float width, float height) {
            this.Ch = ch;
            this.Width = width;
            this.Height = height;
            this.X = 0;
            this.Y = 0;
        }

        public override string ToString() {
            return Ch.ToString();
        }

        public bool isHanzi() {
            return Ch >= 0x4e00 && Ch <= 0x9fbb;
        }
    }
}
