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

            public SimpleLine() {
                Line = new List<SimpleChar>();
                SpacingHanzi = BEST_SPACING;
                SpacingHanWestern = MIN_SPACING;
                SpacingWestern = 1;
            }

            public SimpleLine(List<SimpleChar> chars)
                : this() {
                Append(chars);
            }

            public bool Contains(SimpleChar sc) {
                return Line.Contains(sc);
            }

            public void Insert(int position, List<SimpleChar> chars) {
                Line.InsertRange(position, chars);
            }

            public void Append(List<SimpleChar> chars) {
                Line.AddRange(chars);
            }

            public void Delete(int position, int count) {
                Line.RemoveRange(position, count);
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
                ReCalcSpacing();
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

        class SimpleSection {
            public List<SimpleLine> Lines { get; set; }

            public SimpleSection() {
                this.Lines = new List<SimpleLine>();
                this.Lines.Add(new SimpleLine());
            }

            public SimpleSection(List<SimpleChar> chars):this() {
                Append(chars);
            }

            public void Append(List<SimpleChar> chars) {
                List<SimpleChar> rest = PickUpChars(chars);

                SimpleLine lastLine = Lines.LastOrDefault();
                lastLine.Append(rest);
                List<SimpleChar> overflow = lastLine.Overflow();
                while (overflow != null) {
                    SimpleLine newLine = new SimpleLine(overflow);
                    this.Lines.Add(newLine);
                    overflow = newLine.Overflow();
                }
            }

            public bool Contains(SimpleChar sc) {
                return Lines.Exists(x => x.Line.Contains(sc));
            }

            public void Insert(SimpleChar position, List<SimpleChar> chars) {
                List<SimpleChar> rest = PickUpChars(chars);

                int lineIndex = Lines.FindIndex(x => x.Contains(position));
                SimpleLine currentLine = Lines[lineIndex];
                currentLine.Insert(currentLine.Line.IndexOf(position), rest);

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
            }

            private List<SimpleChar> PickUpChars(List<SimpleChar> chars) {
                int i = chars.FindIndex(x => (x.Ch == '\r' || x.Ch == '\n')); // 找到第一个回车
                i = i < 0 ? chars.Count : i;
                List<SimpleChar> rest = chars.GetRange(0, i); // 取得第一个回车前的内容
                chars.RemoveRange(0, i); // 清除本段内容
                return rest;
            }
        }

        //private List<SimpleLine> lines;
        private List<SimpleSection> sections;
        private Size size;
        private Font font;
        public Image DrawBuffer { get; private set; }

        private SimpleChar insertPos;

        public SimpleDocument(Size size, Font font, String text) {
            this.size = size;
            this.font = font;
            //this.font = new Font("微软雅黑", 24f);

            this.sections = new List<SimpleSection>();
            this.sections.Add(new SimpleSection());

            this.insertPos = null;

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
            float y = 0;
            foreach (SimpleSection sec in this.sections) {
                foreach (SimpleLine line in sec.Lines) {
                    float x = 0;
                    for (int j = 0; j < line.Line.Count; j++) {
                        SimpleChar sc = line.Line[j];
                        x += line.CharSpacing(j);
                        g.FillRectangle(Brushes.Yellow, new RectangleF(x, y, sc.Width, sc.Height));
                        g.DrawString(sc.ToString(), this.font, Brushes.Black, x, y, StringFormat.GenericTypographic);
                        x += sc.Width;
                    }
                    y += ROW_SPACING + this.font.Height;
                }
            }
            this.DrawBuffer = buffer;
        }

        public void Insert(String text) {
            List<SimpleChar> chars = ConvertChars(text);
            if (this.insertPos == null) {
                SimpleSection lastSec = sections.LastOrDefault();
                lastSec.Append(chars);
                while (chars.Count != 0) {
                    trimLeftReturn(chars);
                    this.sections.Add(new SimpleSection(chars));
                }
            } else {
                int secIndex = sections.FindIndex(x => x.Contains(this.insertPos));
                SimpleSection currentSec = sections[secIndex];
                currentSec.Insert(this.insertPos, chars);
                List<SimpleSection> block = new List<SimpleSection>();
                while (chars.Count != 0) {
                    trimLeftReturn(chars);
                    block.Add(new SimpleSection(chars));
                }
                this.sections.InsertRange(secIndex, block);
            }

            DrawText();
        }

        private void trimLeftReturn(List<SimpleChar> chars) {
            if (chars.Count != 0 && chars[0].Ch == '\r' || chars[0].Ch == '\n')
                chars.RemoveAt(0);
            if (chars.Count != 0 && (chars[0].Ch == '\r' || chars[0].Ch == '\n'))
                chars.RemoveAt(0);
        }

        public Point CursorLocation() {
            return CharLocation(this.insertPos);
        }

        private Point CharLocation(SimpleChar activeChar) {
            float left = 0;
            float top = 0;

            SimpleLine currentLine = null;
            foreach (SimpleSection sec in sections) {
                foreach (SimpleLine line in sec.Lines) {
                    if (activeChar != null && line.Contains(activeChar)) {
                        currentLine = line;
                        break;
                    }
                    top += font.Height + ROW_SPACING;
                }
            }
            if (currentLine == null) {
                currentLine = sections.LastOrDefault().Lines.LastOrDefault();
                top -= font.Height + ROW_SPACING;
            }

            for (int i = 0; i < currentLine.Line.Count; i++) {
                SimpleChar sc = currentLine.Line[i];
                if (activeChar != null && sc == activeChar) {
                    break;
                }
                left += currentLine.CharSpacing(i) + sc.Width;
            }

            Point p = new Point(Convert.ToInt32(left), Convert.ToInt32(top));
            return p;
        }

        public void MoveCursorTo(Point location) {
            this.insertPos = LocateChar(location);
        }

        private SimpleChar LocateChar(Point location) {
            float top = 0;

            SimpleLine currentLine = null;
            foreach (SimpleSection sec in sections) {
                foreach (SimpleLine line in sec.Lines) {
                    top += font.Height + ROW_SPACING;
                    if (top > location.Y) {
                        currentLine = line;
                        break;
                    }
                }
            }
            bool tail = false;
            if (currentLine == null) {
                currentLine = sections.LastOrDefault().Lines.LastOrDefault();
                tail = true;
            }

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
