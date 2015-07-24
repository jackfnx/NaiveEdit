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
            var line = new SimpleLine(this);
            this.Lines.Add(line);
        }

        public void Append(List<SimpleChar> chars) {
            var oldText = Lines.SelectMany(x => x.Line).Where(x => !x.IsLineEnd);
            var appendText = PickUpChars(chars);

            var newText = oldText.Concat(appendText).ToList();

            var lines = new List<SimpleLine>();
            do {
                var line = new SimpleLine(this);
                line.Fill(newText);
                lines.Add(line);
            } while (newText.Count > 0);

            this.Lines = lines;
        }

        public void Insert(ref SimpleChar position, List<SimpleChar> chars) {
            var oldText = Lines.SelectMany(x => x.Line).ToList();
            var insertText = PickUpChars(chars);

            int insertPos = oldText.IndexOf(position);
            var leftText = oldText.Take(insertPos);
            var rightText = oldText.Skip(insertPos);

            var newText = leftText.Concat(insertText).Concat(rightText).Where(x => !x.IsLineEnd).ToList();

            var lines = new List<SimpleLine>();
            do {
                var line = new SimpleLine(this);
                line.Fill(newText);
                lines.Add(line);
            } while (newText.Count > 0);

            // 更新光标位置：插入的最后一个字符后面
            var insertTextLastChar = insertText.LastOrDefault();
            if (insertTextLastChar!=null) {
                var insertingLine=lines.Find(x => x.Line.Contains(insertTextLastChar));
                int insertPosLeft = insertingLine.Line.IndexOf(insertTextLastChar);
                if (insertPosLeft >= 0 && insertPosLeft < insertingLine.Line.Count - 1) {
                    position = insertingLine.Line[insertPosLeft + 1]; // 更新光标位置
                }
            }

            this.Lines = lines;
        }

        private List<SimpleChar> PickUpChars(List<SimpleChar> chars) {
            int i = chars.FindIndex(x => (x.Ch == '\r' || x.Ch == '\n')); // 找到第一个回车
            i = i < 0 ? chars.Count : i;
            List<SimpleChar> pickUps = chars.GetRange(0, i); // 取得第一个回车前的内容
            chars.RemoveRange(0, i); // 清除本段内容
            return pickUps;
        }

        public bool IsSectionEnd(SimpleChar sc) {
            return sc == Lines.Last().Line.Last();
        }

        public override string ToString() {
            return string.Format("[{0} lines]", Lines.Count);
        }
    }
}
