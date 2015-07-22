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
        public SimpleSection Section { get; set; }

        public SimpleLine() {
            Line = new List<SimpleChar>();
            Line.Add(SimpleChar.LineEnd);
            SpacingHanzi = SimpleDocument.BEST_SPACING;
            SpacingHanWestern = SimpleDocument.MIN_SPACING;
            SpacingWestern = 1;
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

        public void Fill(List<SimpleChar> lineChars) {
            ReCalcSpacing();

            var line = new List<SimpleChar>();
            float x = 0;
            int i = 0;
            for (; i < lineChars.Count; i++) {
                var sc = lineChars[i];
                var spacing = CharSpacing(i);

                if (x + spacing + sc.Width > SimpleDocument.LINE_LENGTH) {
                    break;
                }

                x += spacing;
                sc.X = x;
                line.Add(sc);
                sc.Line = this;
                x += sc.Width;
            }
            var end = SimpleChar.LineEnd;
            end.X = x;
            line.Add(end);
            end.Line = this;
            lineChars.RemoveRange(0, i < lineChars.Count ? i : lineChars.Count);

            this.Line = line;
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
