using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleEditControlLibrary
{
    public partial class SimpleEdit: UserControl
    {
        private SimpleDocument sDoc;

        public SimpleEdit() {

            sDoc = new SimpleDocument(this.Size, this.Font, "你是否涉及阿弗拉的实际负拉动世界经司法鉴定所肩负发送大量飞机福萨利发骚了福萨利的解放拉萨将大幅拉升");

            InitializeComponent();
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            sDoc.Resize(this.Width, this.Height);
        }

        [DllImport("user32.dll")]
        private extern static void CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);

        [DllImport("user32.dll")]
        private extern static void DestroyCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        private extern static void ShowCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        private extern static void HideCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        private extern static bool SetCaretPos(int x, int y);

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);

            CreateCaret(this.Handle, IntPtr.Zero, 2, this.FontHeight);
            Point p = sDoc.CursorLocation();
            SetCaretPos(p.X, p.Y);
            ShowCaret(this.Handle);
        }

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);

            HideCaret(this.Handle);
            //DestroyCaret(this.Handle);
        }
        
        private const int WM_IME_SETCONTEXT = 0x0281;
        private const int WM_IME_CHAR = 0x0286;
        private const int WM_CHAR = 0x0102;
        private const int WM_KEYDOWN = 0x0100;
        private const int PM_REMOVE = 0x0001;
        private const int GCS_RESULTSTR = 0x0800;
        private const int GCS_COMPSTR = 0x0008;

        [DllImport("imm32.dll")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("imm32.dll")]
        public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        public static extern int ImmGetCompositionString(IntPtr hIMC, int dwIndex, StringBuilder lpBuf, int dwBufLen);

        [DllImport("imm32.dll")]
        public static extern int ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);

        [DllImport("imm32.dll")]
        public static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref COMPOSITIONFORM lpCompForm);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTAPI {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COMPOSITIONFORM {
            public uint dwStyle;
            public POINTAPI ptCurrentPos;
            public RECT rcArea;
        }

        private IntPtr hIMC; // 输入法Handle

        private void SimpleEdit_Load(object sender, EventArgs e) {
            this.hIMC = ImmGetContext(this.Handle);
        }

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            if (m.Msg == WM_IME_SETCONTEXT && m.WParam.ToInt32() == 1) {
                ImmAssociateContext(this.Handle, this.hIMC);
            }

            switch (m.Msg) {
                case WM_KEYDOWN:
                    switch ((Keys)(int)m.WParam) {
                        case Keys.Delete:
                            sDoc.DeleteRight();
                            this.Refresh();
                            break;
                    }
                    break;
                case WM_CHAR: // 英文
                    if ((Keys)(int)m.WParam == Keys.Back) {
                        sDoc.DeleteLeft();
                        this.Refresh();
                    } else {
                        sDoc.Insert(Convert.ToString((char)m.WParam));
                        this.Refresh();
                    }
                    break;
                case WM_IME_CHAR: // 中文
                    if (m.WParam.ToInt32() == PM_REMOVE) { // 如果不做这个判断.会打印出重复的中文 
                        StringBuilder sb = new StringBuilder();
                        int size = ImmGetCompositionString(this.hIMC, GCS_COMPSTR, null, 0);
                        size += sizeof(Char);
                        ImmGetCompositionString(this.hIMC, GCS_RESULTSTR, sb, size);
                        sDoc.Insert(sb.ToString());
                        this.Refresh();
                    }
                    break;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            switch (keyData) {
                case Keys.Left:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.Left);
                    this.Refresh();
                    break;
                case Keys.Right:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.Right);
                    this.Refresh();
                    break;
                case Keys.Up:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.Up);
                    this.Refresh();
                    break;
                case Keys.Down:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.Down);
                    this.Refresh();
                    break;
                case Keys.Home:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.Home);
                    this.Refresh();
                    break;
                case Keys.End:
                    sDoc.SetInsertPosByMove(SimpleDocument.MoveOperation.End);
                    this.Refresh();
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            using (Graphics g = this.CreateGraphics()) {
                g.DrawImage(sDoc.DrawBuffer, 0, 0);
            }

            Point p = sDoc.CursorLocation();

            // 更新光标位置
            SetCaretPos(p.X, p.Y);

            // 更新输入法悬浮窗口位置
            COMPOSITIONFORM cf = new COMPOSITIONFORM();
            cf.dwStyle = 2;
            cf.ptCurrentPos.x = p.X + 10;
            cf.ptCurrentPos.y = p.Y + 10;
            ImmSetCompositionWindow(this.hIMC, ref cf);
        }

        private void SimpleEdit_MouseClick(object sender, MouseEventArgs e) {
            sDoc.SetInsertPosByLocation(e.Location);
            this.Refresh();
        }
    }
}
