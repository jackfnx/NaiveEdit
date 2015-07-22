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

        public void Append(List<SimpleChar> chars) {
            var oldText = Lines.SelectMany(x => x.Line).Where(x => !x.IsLineEnd);
            var pickUps = PickUpChars(chars);

            var newText = new List<SimpleChar>(oldText);
            newText.AddRange(pickUps);

            var lines = new List<SimpleLine>();
            do {
                var line = new SimpleLine();
                line.Fill(newText);
                lines.Add(line);
                line.Section = this;
            } while (newText.Count > 0);

            this.Lines = lines;
        }

        public void Insert(SimpleChar position, List<SimpleChar> chars) {
            var oldText = Lines.SelectMany(x => x.Line);
            var pickUps = PickUpChars(chars);

            var newText = new List<SimpleChar>(oldText);
            int index = newText.IndexOf(position);
            if (index >= 0) {
                if (chars.Count > 0) {
                    chars.AddRange(newText.GetRange(index, newText.Count - index));
                    newText.RemoveRange(index, newText.Count - index);
                    newText.AddRange(pickUps);
                } else {
                    newText.InsertRange(index, pickUps);
                }
            } else {
                newText.AddRange(pickUps);
            }

            var lines = new List<SimpleLine>();
            do {
                var line = new SimpleLine();
                line.Fill(newText);
                lines.Add(line);
                line.Section = this;
            } while (newText.Count > 0);

            this.Lines = lines;
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
