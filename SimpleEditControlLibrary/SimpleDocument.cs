using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {
    class SimpleDocument {
        public const float LINE_LENGTH = 680;
        public const float MIN_SPACING_LOOSE = 1.5f;
        public const float MIN_SPACING_TIGHT = 1.8f;
        public const float MAX_SPACING = 2.2f;
        public const float BEST_SPACING = 2.0f;
        public const float ROW_SPACING = 2;

        public enum MoveOperation { Left, Right, Up, Down, Home, End }

        private List<SimpleSection> sections;
        private Size size;
        private Font font;
        public Image DrawBuffer { get; private set; }

        private SimpleChar insertPos;

        public SimpleDocument(Size size, Font font, String text) {
            this.size = size;
            this.font = font;

            this.sections = new List<SimpleSection>();
            this.sections.Add(new SimpleSection());

            this.insertPos = this.sections.Last().End;

            Insert(text);

            DrawText();
        }

        public void Resize(int w, int h) {
            this.size.Width = w;
            this.size.Height = h;

            DrawText();
        }

        private List<List<SimpleChar>> ConvertChars(String text) {
            Image buffer = new Bitmap(this.size.Width, this.size.Height);
            Graphics g = Graphics.FromImage(buffer);

            var secs = new List<List<SimpleChar>>();
            foreach (string secString in text.Split('\n')) {
                var sec = new List<SimpleChar>();
                foreach (char ch in secString) {
                    String vStr = ch.ToString();
                    switch (ch) {
                        case ' ':
                            vStr = "a"; // 空格相当于一个英文字母
                            break;
                        case '\t':
                            vStr = "aaaa"; // TAB相当于四个英文字母
                            break;
                        default:
                            break;
                    }
                    SizeF sz = g.MeasureString(vStr, font, int.MaxValue, StringFormat.GenericTypographic);
                    sec.Add(new SimpleChar(ch, sz.Width, sz.Height));
                }
                secs.Add(sec);
            }
            return secs;
        }

        private void DrawText() {
            var positions = new List<PointF>();
            var chars = new List<char>();
            float y = 0;
            foreach (SimpleSection sec in this.sections) {
                foreach (SimpleLine line in sec.Lines) {
                    foreach (SimpleChar sc in line.Line) {
                        sc.Y = y;
                        positions.Add(new PointF(sc.X, sc.Y + this.font.Height));
                        chars.Add(sc.Ch);
                    }
                    y += ROW_SPACING + this.font.Height;
                }
            }
            
            Image buffer = new Bitmap(this.size.Width, this.size.Height);
            Graphics g = Graphics.FromImage(buffer);
            GdiPlusUtils.DrawString(g, new string(chars.ToArray()), this.font, Brushes.Black, positions.ToArray(), null);
            this.DrawBuffer = buffer;
        }

        public void Insert(String text) {
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var secs = ConvertChars(text);
            var currentSec = this.insertPos.Line.Section;
            int currentSecIndex = sections.IndexOf(currentSec);

            if (secs.Count > 1) {
                var newSec = currentSec.Split(this.insertPos);
                this.sections.Insert(currentSecIndex + 1, newSec);
            }
            currentSec.Insert(this.insertPos, secs[0]);
            var lastInsertChar = secs[0].Count > 0 ? secs[0].Last() : currentSec.End;
            for (int i = 1; i < secs.Count-1; i++) {
                SimpleSection sec = new SimpleSection();
                sec.Insert(sec.End, secs[i]);
                this.sections.Insert(currentSecIndex + i, sec);
                lastInsertChar = sec.End;
            }
            if (secs.Count > 1) {
                var nextSec = sections[currentSecIndex + secs.Count - 1];
                nextSec.Insert(nextSec.Home, secs[secs.Count - 1]);
                if (secs[secs.Count - 1].Count > 0) {
                    lastInsertChar = secs[secs.Count - 1].Last();
                } else {
                    lastInsertChar = null;
                    this.insertPos = nextSec.Home;
                }
            }

            if (lastInsertChar != null) {
                this.insertPos = NextChar(lastInsertChar);
            }

            DrawText();
        }

        public void DeleteLeft() {
            var currentSec = this.insertPos.Line.Section;
            if (this.insertPos == currentSec.Home) {                // 段首
                int currentSecIndex = sections.IndexOf(currentSec);
                if (currentSecIndex > 0) {
                    var previousSec = sections[currentSecIndex - 1];

                    previousSec.Merge(currentSec);
                    sections.Remove(currentSec);

                    if (!this.insertPos.IsPrintableChar()) {        // 空段落
                        this.insertPos = previousSec.End;
                    }
                }
            } else {
                var deleteChar = this.insertPos;
                do {
                    deleteChar = PreviousChar(deleteChar);
                } while (!deleteChar.IsPrintableChar());

                var nextChar = this.insertPos;
                if (this.insertPos == currentSec.End) {
                    nextChar = null;
                } else {
                    while (!nextChar.IsPrintableChar()) {
                        nextChar = NextChar(nextChar);
                    }
                }

                currentSec.Delete(deleteChar);
                if (!this.insertPos.IsPrintableChar()) {
                    if (nextChar != null) {
                        this.insertPos = nextChar;
                    } else {
                        this.insertPos = currentSec.End;
                    }
                }
            }

            DrawText();
        }

        public void DeleteRight() {
            var currentSec = this.insertPos.Line.Section;
            if (this.insertPos == currentSec.End) {             // 段末
                int currentSecIndex = sections.IndexOf(currentSec);
                if (currentSecIndex < sections.Count - 1) {
                    var nextSec = sections[currentSecIndex + 1];

                    this.insertPos = nextSec.Home;

                    currentSec.Merge(nextSec);
                    sections.Remove(nextSec);

                    if (!this.insertPos.IsPrintableChar()) {    // 空段落
                        this.insertPos = currentSec.End;
                    }
                }
            } else {
                var deleteChar = this.insertPos;
                while (!deleteChar.IsPrintableChar()) {
                    deleteChar = NextChar(deleteChar);
                }

                var nextChar = NextChar(deleteChar);

                currentSec.Delete(deleteChar);
                this.insertPos = nextChar;
            }

            DrawText();
        }

        public Point CursorLocation() {
            return new Point(Convert.ToInt32(this.insertPos.X), Convert.ToInt32(this.insertPos.Y));
        }

        public void SetInsertPosByMove(MoveOperation op) {
            switch (op) {
                case MoveOperation.Left:
                    this.insertPos = PreviousChar(this.insertPos);
                    // 前一个字符是行末，并且不是段末，再往前找一格
                    if (!this.insertPos.IsPrintableChar() && this.insertPos != this.insertPos.Line.Section.End) {
                        this.insertPos = PreviousChar(this.insertPos);
                    }
                    break;
                case MoveOperation.Right:
                    // 字符是行末，并且不是段末，多往后找一格
                    if (!this.insertPos.IsPrintableChar() && this.insertPos != this.insertPos.Line.Section.End) {
                        this.insertPos = NextChar(this.insertPos);
                    }
                    this.insertPos = NextChar(this.insertPos);
                    break;
                case MoveOperation.Up:
                    PointF pUp = new PointF(this.insertPos.X, this.insertPos.Y - this.font.Height - ROW_SPACING);
                    SetInsertPosByLocation(pUp);
                    break;
                case MoveOperation.Down:
                    PointF pDown = new PointF(this.insertPos.X, this.insertPos.Y + this.font.Height + ROW_SPACING);
                    SetInsertPosByLocation(pDown);
                    break;
                case MoveOperation.Home:
                    this.insertPos = HomeChar(this.insertPos);
                    break;
                case MoveOperation.End:
                    this.insertPos = EndChar(this.insertPos);
                    break;
                default:
                    break;
            }
        }

        public void SetInsertPosByLocation(PointF location) {
            this.insertPos = LocateChar(location);
        }

        private SimpleChar LocateChar(PointF location) {
            var currentLine = sections
                .SelectMany(x => x.Lines)
                .TakeWhile(x => x.Line[0].Y <= location.Y)
                .LastOrDefault();

            if (currentLine == null) {
                currentLine = sections[0].Lines[0];
            }

            for (int i = 0; i < currentLine.Line.Count - 1; i++) {
                var left = currentLine.Line[i];
                var right = currentLine.Line[i + 1];
                var midPoint = (left.Right + right.Left) / 2;
                if (midPoint >= location.X) {
                    return left;
                }
            }
            return currentLine.End;
        }

        private SimpleChar PreviousChar(SimpleChar c) {
            int index = c.Line.Line.IndexOf(c);
            if (index > 0) {
                return c.Line.Line[index - 1];
            } else {
                int lineIndex = c.Line.Section.Lines.IndexOf(c.Line);
                if (lineIndex > 0) {
                    return c.Line.Section.Lines[lineIndex - 1].End;
                } else {
                    int secIndex = sections.IndexOf(c.Line.Section);
                    if (secIndex > 0) {
                        return sections[secIndex - 1].End;
                    } else {
                        return sections[0].Home;
                    }
                }
            }
        }

        private SimpleChar NextChar(SimpleChar c) {
            int index = c.Line.Line.IndexOf(c);
            if (index < c.Line.Line.Count - 1) {
                return c.Line.Line[index + 1];
            } else {
                int lineIndex = c.Line.Section.Lines.IndexOf(c.Line);
                if (lineIndex < c.Line.Section.Lines.Count - 1) {
                    return c.Line.Section.Lines[lineIndex + 1].Home;
                } else {
                    int secIndex = sections.IndexOf(c.Line.Section);
                    if (secIndex < sections.Count - 1) {
                        return sections[secIndex + 1].Home;
                    } else {
                        return sections.Last().End;
                    }
                }
            }
        }

        private SimpleChar HomeChar(SimpleChar c) {
            if (c.Line.Line.Contains(c)) {
                return c.Line.Line.First();
            } else {
                return c;
            }
        }

        private SimpleChar EndChar(SimpleChar c) {
            if (c.Line.Line.Contains(c)) {
                return c.Line.End;
            } else {
                return c;
            }
        }
    }
}
