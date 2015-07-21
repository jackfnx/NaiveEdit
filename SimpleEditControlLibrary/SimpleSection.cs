using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {

    class SimpleSection {
        public List<SimpleLine> Lines { get; set; }

        public SimpleSection() {
            this.Lines = new List<SimpleLine>();
            this.Lines.Add(new SimpleLine());
        }

        public SimpleSection Append(List<SimpleChar> chars) {
            List<SimpleChar> pickUps = PickUpChars(chars);

            SimpleLine lastLine = Lines.LastOrDefault();
            lastLine.Append(pickUps);
            List<SimpleChar> overflow = lastLine.Overflow();
            while (overflow != null) {
                SimpleLine newLine = new SimpleLine(overflow);
                this.Lines.Add(newLine);
                overflow = newLine.Overflow();
            }

            return this;
        }

        public bool Contains(SimpleChar sc) {
            return Lines.Exists(x => x.Line.Contains(sc));
        }

        public SimpleSection Insert(SimpleChar position, List<SimpleChar> chars) {
            List<SimpleChar> pickUps = PickUpChars(chars);

            int lineIndex = Lines.FindIndex(x => x.Contains(position));
            SimpleLine currentLine = Lines[lineIndex];
            currentLine.Insert(currentLine.Line.IndexOf(position), pickUps);

            List<SimpleChar> overflow = currentLine.Overflow();
            for (int j = lineIndex + 1; j < Lines.Count && overflow != null; j++) {
                SimpleLine line = Lines[j];
                line.Insert(0, overflow);
                overflow = line.Overflow();
            }
            while (overflow != null) {
                SimpleLine newLine = new SimpleLine(overflow);
                Lines.Add(newLine);
                overflow = newLine.Overflow();
            }

            return this;
        }

        private List<SimpleChar> PickUpChars(List<SimpleChar> chars) {
            int i = chars.FindIndex(x => (x.Ch == '\r' || x.Ch == '\n')); // 找到第一个回车
            i = i < 0 ? chars.Count : i;
            List<SimpleChar> pickUps = chars.GetRange(0, i); // 取得第一个回车前的内容
            chars.RemoveRange(0, i); // 清除本段内容
            return pickUps;
        }
    }
}
