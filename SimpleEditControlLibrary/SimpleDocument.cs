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
        
        public enum DirectOfMove { Left, Right, Up, Down }

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

            this.insertPos = this.sections.Last().Lines.Last().Line.Last();

            Insert(text);

            DrawText();
        }

        public void Resize(int w, int h) {
            this.size.Width = w;
            this.size.Height = h;

            DrawText();
        }

        private List<SimpleChar> ConvertChars(String text) {
            Image buffer = new Bitmap(this.size.Width, this.size.Height);
            Graphics g = Graphics.FromImage(buffer);

            var chars = new List<SimpleChar>(text.Length);
            foreach (char ch in text) {
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
                chars.Add(new SimpleChar(ch, sz.Width, sz.Height));
            }
            return chars;
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
            text = text.Replace("\r\n", "\n");
            var chars = ConvertChars(text);
            var currentSec = this.insertPos.Line.Section;
            int currentSecIndex = sections.IndexOf(currentSec);

            if (currentSec.IsSectionEnd(this.insertPos)) {
                currentSec.Append(chars);
                this.insertPos = currentSec.Lines.Last().Line.Last();
            } else {
                currentSec.Insert(ref this.insertPos, chars);
            }
            List<SimpleSection> block = new List<SimpleSection>();
            while (chars.Count != 0) {
                trimLeftReturn(chars);
                SimpleSection sec = new SimpleSection();
                sec.Append(chars);
                block.Add(sec);
            }
            this.sections.InsertRange(currentSecIndex + 1, block);

            DrawText();
        }

        private void trimLeftReturn(List<SimpleChar> chars) {
            if (chars.Count != 0 && (chars[0].Ch == '\r' || chars[0].Ch == '\n'))
                chars.RemoveAt(0);
            if (chars.Count != 0 && (chars[0].Ch == '\r' || chars[0].Ch == '\n'))
                chars.RemoveAt(0);
        }
        
        public Point CursorLocation() {
            return new Point(Convert.ToInt32(this.insertPos.X), Convert.ToInt32(this.insertPos.Y));
        }

        public void SetInsertPosByMove(DirectOfMove dir) {
            switch (dir) {
                case DirectOfMove.Left:
                    this.insertPos = PreviousChar(this.insertPos);
                    break;
                case DirectOfMove.Right:
                    this.insertPos = NextChar(this.insertPos);
                    break;
                case DirectOfMove.Up:
                    PointF pUp = new PointF(this.insertPos.X, this.insertPos.Y - this.font.Height - ROW_SPACING);
                    SetInsertPosByLocation(pUp);
                    break;
                case DirectOfMove.Down:
                    PointF pDown = new PointF(this.insertPos.X, this.insertPos.Y + this.font.Height + ROW_SPACING);
                    SetInsertPosByLocation(pDown);
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
            return currentLine.Line.Last();
        }

        private SimpleChar PreviousChar(SimpleChar c) {
            int index = c.Line.Line.IndexOf(c);
            if (index > 0) {
                return c.Line.Line[index - 1];
            } else {
                int lineIndex = c.Line.Section.Lines.IndexOf(c.Line);
                if (lineIndex > 0) {
                    return c.Line.Section.Lines[lineIndex - 1].Line.Last();
                } else {
                    int secIndex = sections.IndexOf(c.Line.Section);
                    if (secIndex > 0) {
                        return sections[secIndex - 1].Lines.Last().Line.Last();
                    } else {
                        return sections[0].Lines[0].Line[0];
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
                        return sections[secIndex + 1].Lines[0].Line[0];
                    } else {
                        return sections.Last().Lines.Last().Line.Last();
                    }
                }
            }
        }
    }
}
