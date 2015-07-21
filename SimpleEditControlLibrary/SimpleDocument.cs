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
                String vStr = ch.ToString();
                switch (ch) {
                    case ' ':
                        vStr = "a";
                        break;
                    case '\t':
                        vStr = "aaaa";
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
            List<PointF> positions = new List<PointF>();
            List<char> chars = new List<char>();
            float y = 0;
            foreach (SimpleSection sec in this.sections) {
                foreach (SimpleLine line in sec.Lines) {
                    for (int j = 0; j < line.Line.Count; j++) {
                        positions.Add(new PointF(line.Line[j].X, y + this.font.Height));
                        chars.Add(line.Line[j].Ch);
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
            List<SimpleChar> chars = ConvertChars(text);
            if (this.insertPos == null) {
                SimpleSection lastSec = sections.LastOrDefault();
                lastSec.Append(chars);
                while (chars.Count != 0) {
                    trimLeftReturn(chars);
                    this.sections.Add(new SimpleSection().Append(chars));
                }
            } else {
                int secIndex = sections.FindIndex(x => x.Contains(this.insertPos));
                SimpleSection currentSec = sections[secIndex];
                currentSec.Insert(this.insertPos, chars);
                List<SimpleSection> block = new List<SimpleSection>();
                while (chars.Count != 0) {
                    trimLeftReturn(chars);
                    block.Add(new SimpleSection().Append(chars));
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

        public void SetInsertPos(Point location) {
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
