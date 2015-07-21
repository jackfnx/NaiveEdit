using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {

    class SimpleLine {
        public List<SimpleChar> Line { get; set; }
        public float SpacingHanzi { get; private set; }
        public float SpacingHanWestern { get; private set; }
        public float SpacingWestern { get; private set; }

        public SimpleLine() {
            Line = new List<SimpleChar>();
            SpacingHanzi = SimpleDocument.BEST_SPACING;
            SpacingHanWestern = SimpleDocument.MIN_SPACING;
            SpacingWestern = 1;
        }

        public SimpleLine(List<SimpleChar> chars)
            : this() {
            Append(chars);
        }

        public bool Contains(SimpleChar sc) {
            return Line.Contains(sc);
        }

        public void Insert(int position, List<SimpleChar> chars) {
            Line.InsertRange(position, chars);
        }

        public void Append(List<SimpleChar> chars) {
            Line.AddRange(chars);
        }

        public void Delete(int position, int count) {
            Line.RemoveRange(position, count);
        }

        public void ReCalcSpacing() {
            float len = 0;
            foreach (SimpleChar sc in Line) {
                len += sc.Width;
            }
            int spacingCount = Line.Count - 1;
            if (len + SimpleDocument.BEST_SPACING * spacingCount <= SimpleDocument.LINE_LENGTH) {
                SpacingHanzi = SimpleDocument.BEST_SPACING;
                SpacingHanWestern = SimpleDocument.MIN_SPACING;
                SpacingWestern = 1;
            } else {
                SpacingHanzi = SimpleDocument.MIN_SPACING;
                SpacingHanWestern = SimpleDocument.MIN_SPACING;
                SpacingWestern = 1;
            }
        }

        public List<SimpleChar> Overflow() {
            ReCalcSpacing();
            float x = 0;
            int overflowPosition = -1;
            for (int i = 0; i < Line.Count; i++) {
                x += CharSpacing(i);
                Line[i].X = x;
                x += Line[i].Width;
                if (x > SimpleDocument.LINE_LENGTH) {
                    overflowPosition = i;
                    break;
                }
            }
            if (overflowPosition >= 0) {
                List<SimpleChar> overflow = Line.GetRange(overflowPosition, Line.Count - overflowPosition);
                Line.RemoveRange(overflowPosition, Line.Count - overflowPosition);
                return overflow;
            } else {
                return null;
            }
        }

        public float CharSpacing(int index) {
            if (index < 0 || index >= Line.Count) {
                return 0;
            } else if (index == 0) {
                return 0;
            } else {
                SimpleChar leftChar = Line[index - 1];
                SimpleChar rightChar = Line[index];
                if (leftChar.isHanzi() && rightChar.isHanzi()) {
                    return SpacingHanzi;
                } else if (leftChar.isHanzi() != rightChar.isHanzi()) {
                    return SpacingHanWestern;
                } else {
                    return SpacingWestern;
                }
            }
        }
    }
}
