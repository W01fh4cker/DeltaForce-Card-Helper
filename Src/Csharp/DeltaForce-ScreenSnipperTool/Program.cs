using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices.ComTypes;


namespace DeltaForce_ScreenSnipperTool
{
    public class SnippingTool : Form
    {
        private Rectangle selectionRect;
        private Point startPos;
        private bool isSelecting = false;
        private Bitmap screenBitmap;

        // 目标窗口信息
        private Rectangle targetWindowRect;
        private const string TARGET_PROCESS = "DeltaForceClient-Win64-Shipping";

        public SnippingTool()
        {
            InitializeComponent();
            InitializeTargetWindow();
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.TopMost = true;
            this.DoubleBuffered = true;
            this.Opacity = 0.3;
            this.BackColor = Color.White;
            this.TransparencyKey = Color.Magenta;
            this.Cursor = Cursors.Cross;
            this.KeyPreview = true;

            // 捕获整个屏幕
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                screenBitmap = new Bitmap((int)g.VisibleClipBounds.Width,
                                         (int)g.VisibleClipBounds.Height);
                using (Graphics bg = Graphics.FromImage(screenBitmap))
                {
                    bg.CopyFromScreen(0, 0, 0, 0, screenBitmap.Size);
                }
            }

            this.BackgroundImage = screenBitmap;
        }

        private void InitializeTargetWindow()
        {
            var process = Process.GetProcessesByName(TARGET_PROCESS).FirstOrDefault();
            if (process != null && process.MainWindowHandle != IntPtr.Zero)
            {
                GetWindowRect(process.MainWindowHandle, out RECT rect);
                targetWindowRect = new Rectangle(rect.Left, rect.Top,
                                               rect.Right - rect.Left,
                                               rect.Bottom - rect.Top);
            }
            else
            {
                // 如果找不到目标窗口，使用整个屏幕
                targetWindowRect = Screen.PrimaryScreen.Bounds;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPos = e.Location;
                selectionRect = new Rectangle(e.Location, new Size(0, 0));
                isSelecting = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                int x = Math.Min(e.X, startPos.X);
                int y = Math.Min(e.Y, startPos.Y);
                int width = Math.Abs(e.X - startPos.X);
                int height = Math.Abs(e.Y - startPos.Y);

                selectionRect = new Rectangle(x, y, width, height);
                this.Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                isSelecting = false;
                if (selectionRect.Width > 10 && selectionRect.Height > 10)
                {
                    CalculateAndShowCoordinates();
                }
                this.Close();
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.Red, 2))
            {
                e.Graphics.DrawRectangle(pen, selectionRect);
            }

            // 绘制半透明选区
            using (Brush brush = new SolidBrush(Color.FromArgb(120, 0, 0, 255)))
            {
                e.Graphics.FillRectangle(brush, selectionRect);
            }

            base.OnPaint(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
            base.OnKeyDown(e);
        }

        private void CalculateAndShowCoordinates()
        {
            Rectangle screenSelection = this.RectangleToScreen(selectionRect);
            double left = (double)(screenSelection.Left - targetWindowRect.Left) / targetWindowRect.Width * 100;
            double top = (double)(screenSelection.Top - targetWindowRect.Top) / targetWindowRect.Height * 100;
            double right = (double)(screenSelection.Right - targetWindowRect.Left) / targetWindowRect.Width * 100;
            double bottom = (double)(screenSelection.Bottom - targetWindowRect.Top) / targetWindowRect.Height * 100;
            string result = $"{left / 100:F4}f, {top / 100:F4}f, {right / 100:F4}f, {bottom / 100:F4}f";
            Console.WriteLine(result);
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"res_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.txt"), result);
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new SnippingTool());
        }
    }
}
