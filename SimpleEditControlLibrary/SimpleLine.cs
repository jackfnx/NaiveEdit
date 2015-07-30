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
        public SimpleSection Section { get; private set; }
        public SimpleChar End { get { return Line.Last(); } }
        public SimpleChar Home { get { return Line.First(); } }

        public SimpleLine(SimpleSection section) {
            this.Section = section;

            this.Line = new List<SimpleChar>();
            var lineEnd = SimpleChar.LineEnd;
            lineEnd.Line = this;
            this.Line.Add(lineEnd);

            SpacingHanzi = SimpleDocument.BEST_SPACING;
            SpacingHanWestern = SimpleDocument.MIN_SPACING_LOOSE;
            SpacingWestern = 1;
        }

        public void Fill(List<SimpleChar> lineChars, bool isLoose) {
            CalcSpacing(lineChars, isLoose);

            var line = new List<SimpleChar>();
            float x = 0;
            int i = 0;
            for (; i < lineChars.Count; i++) {
                var sc = lineChars[i];
                var leftSpacing = CharLeftSpacing(lineChars, i);

                if (x + leftSpacing + sc.Width > SimpleDocument.LINE_LENGTH + 0.1f) {
                    break;
                }

                x += leftSpacing;
                sc.X = x;
                sc.Line = this;
                line.Add(sc);
                x += sc.Width;
            }
            var end = SimpleChar.LineEnd;
            end.X = x;
            end.Line = this;
            line.Add(end);
            lineChars.RemoveRange(0, i < lineChars.Count ? i : lineChars.Count);

            this.Line = line;
        }

        private void CalcSpacing(List<SimpleChar> lineChars, bool isLoose) {
            SpacingHanWestern = SimpleDocument.BEST_SPACING;
            SpacingWestern = 0;

            float minSpace = isLoose ? SimpleDocument.MIN_SPACING_LOOSE : SimpleDocument.MIN_SPACING_TIGHT;

            float totalSpacing = SimpleDocument.LINE_LENGTH;
            if (lineChars.Count > 0) {
                totalSpacing -= lineChars[0].Width;
            }
            int spacingHanziCount = 0;
            for (int i = 0; i < lineChars.Count - 1; i++) {
                SimpleChar leftChar = lineChars[i];
                SimpleChar rightChar = lineChars[i + 1];
                if (leftChar.isHanzi() && rightChar.isHanzi()) {
                    spacingHanziCount++;
                } else if (leftChar.isHanzi() != rightChar.isHanzi()) {
                    totalSpacing -= SpacingHanWestern;
                } else {
                    totalSpacing -= SpacingWestern;
                }
                totalSpacing -= rightChar.Width;
                float aveSpacing = totalSpacing / spacingHanziCount;
                if (aveSpacing > SimpleDocument.MAX_SPACING) {
                    SpacingHanzi = SimpleDocument.BEST_SPACING;
                } else if (aveSpacing >= minSpace) {
                    SpacingHanzi = aveSpacing;
                } else {
                    break;
                }
            }
        }

        private float CharLeftSpacing(List<SimpleChar> line, int index) {
            if (index < 0 || index >= line.Count) {
                return 0;
            } else if (index == 0) {
                return 0;
            } else {
                SimpleChar leftChar = line[index - 1];
                SimpleChar rightChar = line[index];
                if (leftChar.isHanzi() && rightChar.isHanzi()) {
                    return SpacingHanzi;
                } else if (leftChar.isHanzi() != rightChar.isHanzi()) {
                    return SpacingHanWestern;
                } else {
                    return SpacingWestern;
                }
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (var sc in Line) {
                if (!sc.IsLineEnd) {
                    sb.Append(sc.Ch);
                }
            }
            sb.Append(string.Format("[{0} chars]", sb.Length));
            return sb.ToString();
        }
    }
}
