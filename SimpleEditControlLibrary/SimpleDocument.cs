using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {
    class SimpleDocument {
        public const float LINE_LENGTH = 480;
        public const float MIN_SPACING = 3;
        public const float MAX_SPACING = 10;
        public const float BEST_SPACING = 5;
        public const float ROW_SPACING = 2;

        public enum MoveOperation { Left, Right, Up, Down, Home, End }

        //private List<SimpleLine> lines;
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
            g.DrawLine(Pens.LightGray, LINE_LENGTH, 0, LINE_LENGTH, this.size.Height);
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
            var lastInsertChar = secs[0].Count > 0 ? secs[0].Last() : null;
            for (int i = 1; i < secs.Count-1; i++) {
                SimpleSection sec = new SimpleSection();
                sec.Insert(sec.End, secs[i]);
                this.sections.Insert(currentSecIndex + i, sec);
                lastInsertChar = secs[i].Count > 0 ? secs[i].Last() : lastInsertChar;
            }
            if (secs.Count > 1) {
                var nextSec = sections[currentSecIndex + secs.Count - 1];
                nextSec.Insert(nextSec.Home, secs[secs.Count - 1]);
            }

            if (lastInsertChar != null) {
                this.insertPos = NextChar(lastInsertChar);
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
                    break;
                case MoveOperation.Right:
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
                if (left.Right <= location.X && right.Right > location.X) {
                    return right;
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
                if (lineIndex < c.Line.Line.Count - 1) {
                    return c.Line.Section.Lines[lineIndex + 1].Line[0];
                } else {
                    int secIndex = sections.IndexOf(c.Line.Section);
                    if (secIndex > sections.Count - 1) {
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
