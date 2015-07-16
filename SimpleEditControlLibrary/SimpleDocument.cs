using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEditControlLibrary {
    class SimpleDocument {
        private const float LINE_LENGTH = 480;
        private const float MIN_SPACING = 3;
        private const float MAX_SPACING = 10;
        private const float BEST_SPACING = 5;
        private const float ROW_SPACING = 10;

        class SimpleChar {
            public char Ch { get; set; }
            public float Width { get; set; }
            public float Height { get; set; }

            public SimpleChar(char ch, float width, float height) {
                this.Ch = ch;
                this.Width = width;
                this.Height = height;
            }

            public override string ToString() {
                return Ch.ToString();
            }

            public bool isHanzi() {
                return Ch >= 0x4e00 && Ch <= 0x9fbb;
            }
        }

        class SimpleLine {
            public List<SimpleChar> Line { get; set; }
            public float SpacingHanzi { get; private set; }
            public float SpacingHanWestern { get; private set; }
            public float SpacingWestern { get; private set; }

            public SimpleLine(List<SimpleChar> chars) {
                Line = new List<SimpleChar>();
                SpacingHanzi = BEST_SPACING;
                SpacingHanWestern = MIN_SPACING;
                SpacingWestern = 1;
                Append(chars);
            }

            public void Insert(int position, List<SimpleChar> chars) {
                Line.InsertRange(position, chars);
                ReCalcSpacing();
            }

            public void Append(List<SimpleChar> chars) {
                Line.AddRange(chars);
                ReCalcSpacing();
            }

            public void Delete(int position, int count) {
                Line.RemoveRange(position, count);
                ReCalcSpacing();
            }

            public void ReCalcSpacing() {
                float len = 0;
                foreach (SimpleChar sc in Line) {
                    len += sc.Width;
                }
                int spacingCount = Line.Count - 1;
                if (len + BEST_SPACING * spacingCount <= LINE_LENGTH) {
                    SpacingHanzi = BEST_SPACING;
                    SpacingHanWestern = MIN_SPACING;
                    SpacingWestern = 1;
                } else {
                    SpacingHanzi = MIN_SPACING;
                    SpacingHanWestern = MIN_SPACING;
                    SpacingWestern = 1;
                }
            }

            public List<SimpleChar> Overflow() {
                float len = 0;
                int index = -1;
                for (int i = 0; i < Line.Count; i++) {
                    SimpleChar sc = Line[i];
                    len += CharSpacing(i) + sc.Width;
                    if (len > LINE_LENGTH) {
                        index = i;
                        break;
                    }
                }
                if (index >= 0) {
                    List<SimpleChar> overflow = Line.GetRange(index, Line.Count - index);
                    Line.RemoveRange(index, Line.Count - index);
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

        private List<SimpleLine> lines;
        private Size size;
        private Font font;
        public Image DrawBuffer { get; private set; }

        private SimpleChar insertPos;

        public SimpleDocument(Size size, Font font, String text) {
            this.size = size;
            this.font = font;
            //this.font = new Font("微软雅黑", 24f);

            this.lines = new List<SimpleLine>();

            this.insertPos = null;

            List<SimpleChar> chars = ConvertChars(text);
            List<SimpleChar> overflow = chars;
            do {
                SimpleLine newLine = new SimpleLine(overflow);
                this.lines.Add(newLine);
                overflow = newLine.Overflow();
            } while (overflow != null);

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

            List<SimpleChar> chars = new List<SimpleChar>(text.Length);
            foreach (char ch in text) {
                SizeF sz = g.MeasureString(ch.ToString(), font, int.MaxValue, StringFormat.GenericTypographic);
                chars.Add(new SimpleChar(ch, sz.Width, sz.Height));
            }
            return chars;
        }

        private void DrawText() {
            Image buffer = new Bitmap(this.size.Width, this.size.Height);
            Graphics g = Graphics.FromImage(buffer);
            for (int i = 0; i < this.lines.Count; i++) {
                SimpleLine line = this.lines[i];
                float y = i * (this.font.Height + ROW_SPACING);
                float x = 0;
                for (int j = 0; j < line.Line.Count; j++) {
                    SimpleChar sc = line.Line[j];
                    x += line.CharSpacing(j);
                    g.FillRectangle(Brushes.Yellow, new RectangleF(x, y, sc.Width, sc.Height));
                    g.DrawString(sc.ToString(), this.font, Brushes.Black, x, y, StringFormat.GenericTypographic);
                    x += sc.Width;
                }
            }
            this.DrawBuffer = buffer;
        }

        public void Insert(String text) {
            List<SimpleChar> chars = ConvertChars(text);
            if (this.insertPos == null) {
                SimpleLine lastLine = lines.LastOrDefault();
                lastLine.Append(chars);
                List<SimpleChar> overflow = lastLine.Overflow();
                while (overflow != null) {
                    SimpleLine newLine = new SimpleLine(overflow);
                    lines.Add(newLine);
                    overflow = newLine.Overflow();
                }
            } else {
                int lineIndex = lines.FindIndex(x => x.Line.Contains(this.insertPos));
                SimpleLine currentLine = lines[lineIndex];
                int charIndex = currentLine.Line.IndexOf(this.insertPos);
                currentLine.Insert(charIndex, chars);

                List<SimpleChar> overflow = currentLine.Overflow();
                for (int i = lineIndex + 1; i < lines.Count && overflow != null; i++) {
                    SimpleLine line = lines[i];
                    line.Insert(0, overflow);
                    overflow = line.Overflow();
                }
                while (overflow != null) {
                    SimpleLine newLine = new SimpleLine(overflow);
                    lines.Add(newLine);
                    overflow = newLine.Overflow();
                }
            }

            DrawText();
        }

        public Point CursorLocation() {
            return CharLocation(this.insertPos);
        }

        private Point CharLocation(SimpleChar activeChar) {
            Point p = new Point(0, 0);
            SimpleLine currentLine;
            if (activeChar == null) {
                currentLine = lines.Last();
                p.Y = Convert.ToInt32((lines.Count - 1) * (font.Height + ROW_SPACING));
            } else {
                int line = lines.FindIndex(x => x.Line.Contains(activeChar));
                currentLine = lines[line];
                p.Y = Convert.ToInt32(line * (font.Height + ROW_SPACING));
            }

            float left = 0;
            for (int i = 0; i < currentLine.Line.Count; i++) {
                SimpleChar sc = currentLine.Line[i];
                if (sc == activeChar) {
                    break;
                }
                left += currentLine.CharSpacing(i) + sc.Width;
            }
            p.X = Convert.ToInt32(left);
            return p;
        }

        public void MoveCursorTo(Point location) {
            this.insertPos = LocateChar(location);
        }

        private SimpleChar LocateChar(Point location) {
            int line = (int)((float)location.Y / (font.Height + ROW_SPACING));
            bool tail = false;
            if (line >= this.lines.Count) {
                line = this.lines.Count - 1;
                tail = true;
            }
            SimpleLine currentLine = lines[line];
            if (tail) {
                return currentLine.Line.LastOrDefault();
            } else {
                float left = 0;
                for (int i = 0; i < currentLine.Line.Count; i++) {
                    SimpleChar sc = currentLine.Line[i];
                    left += currentLine.CharSpacing(i) + sc.Width;
                    if (left > location.X) {
                        return sc;
                    }
                }
                return currentLine.Line.LastOrDefault();
            }
        }
    }
}
