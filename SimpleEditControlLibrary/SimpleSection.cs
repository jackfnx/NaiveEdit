using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {

    class SimpleSection {
        public List<SimpleLine> Lines { get; set; }
        public SimpleChar End { get { return Lines.Last().End; } }
        public SimpleChar Home { get { return Lines.First().Home; } }

        public SimpleSection() {
            this.Lines = new List<SimpleLine>();
            this.Lines.Add(new SimpleLine(this));
        }

        public void Insert(SimpleChar position, List<SimpleChar> insertChars) {
            var oldText = Lines.SelectMany(x => x.Line).ToList();

            int insertPos = oldText.IndexOf(position);
            var leftText = oldText.Take(insertPos);
            var rightText = oldText.Skip(insertPos);

            List<SimpleChar> newText= leftText.Concat(insertChars).Concat(rightText).Where(x => !x.IsLineEnd).ToList();
            var lines = new List<SimpleLine>();
            bool isLoose = true;
            do {
                var line = new SimpleLine(this);
                line.Fill(newText, isLoose=!isLoose);
                lines.Add(line);
            } while (newText.Count > 0);

            this.Lines = lines;
        }

        public SimpleSection Split(SimpleChar position) {
            var oldText = Lines.SelectMany(x => x.Line).ToList();

            int splitPos = oldText.IndexOf(position);
            var leftText = oldText.Take(splitPos).Where(x => !x.IsLineEnd).ToList();
            var rightText = oldText.Skip(splitPos).Where(x => !x.IsLineEnd).ToList();

            var lines = new List<SimpleLine>();
            bool isLoose = true;
            do {
                var line = new SimpleLine(this);
                line.Fill(leftText, isLoose = !isLoose);
                lines.Add(line);
            } while (leftText.Count > 0);

            this.Lines = lines;

            var splitOne = new SimpleSection();
            splitOne.Insert(splitOne.End, rightText);
            return splitOne;
        }

        public void Merge(SimpleSection next) {
            var oldText = Lines.SelectMany(x => x.Line);
            var appendText = next.Lines.SelectMany(x => x.Line);

            List<SimpleChar> newText = oldText.Concat(appendText).Where(x => !x.IsLineEnd).ToList();

            var lines = new List<SimpleLine>();
            bool isLoose = true;
            do {
                var line = new SimpleLine(this);
                line.Fill(newText, isLoose = !isLoose);
                lines.Add(line);
            } while (newText.Count > 0);

            this.Lines = lines;
        }

        public void Delete(SimpleChar sc) {
            var oldText = Lines.SelectMany(x => x.Line);

            List<SimpleChar> newText = oldText.Where(x => x != sc && !x.IsLineEnd).ToList();

            var lines = new List<SimpleLine>();
            bool isLoose = true;
            do {
                var line = new SimpleLine(this);
                line.Fill(newText, isLoose = !isLoose);
                lines.Add(line);
            } while (newText.Count > 0);

            this.Lines = lines;
        }

        public override string ToString() {
            return string.Format("[{0} lines]", Lines.Count);
        }
    }
}
