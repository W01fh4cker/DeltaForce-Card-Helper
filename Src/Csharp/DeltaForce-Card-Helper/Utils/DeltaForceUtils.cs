using System;
using Tesseract;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace DeltaForce_Card_Helper.Utils
{
    internal class DeltaForceUtils
    {
        private const string TargetProcessName = "DeltaForceClient-Win64-Shipping";
        private const string TesseractDataPath = @".\tessdata";

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

        private const int SW_RESTORE = 9;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public static string getDFWindowSize()
        {
            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length == 0)
            {
                Console.WriteLine("未检测到三角洲进程，请先启动游戏！");
                return string.Empty;
            }

            Process targetProcess = processes[0];
            IntPtr mainWindowHandle = targetProcess.MainWindowHandle;

            if (mainWindowHandle == IntPtr.Zero)
            {
                return string.Empty;
            }
            if (GetWindowRect(mainWindowHandle, out RECT rect))
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                return $"{width}x{height}";
            }
            else
            {
                Console.WriteLine("获取窗口尺寸失败");
                return string.Empty;
            }
        }

        public static void RestoreWindow()
        {
            Process[] processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length == 0)
            {
                Console.WriteLine($"未找到进程: {TargetProcessName}");
                return;
            }
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                GetWindowThreadProcessId(hWnd, out int processId);
                foreach (Process process in processes)
                {
                    //Console.WriteLine(process.ProcessName);
                    if (process.Id == processId)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                        SetForegroundWindow(hWnd);
                        return false;
                    }
                }
                return true;
            }, IntPtr.Zero);
        }

        public static bool MoveAndClickInDF(int xPercent, int yPercent)
        {
            var processes = Process.GetProcessesByName(TargetProcessName);
            if (processes.Length == 0)
            {
                Console.WriteLine("Delta Force进程未找到");
                return false;
            }

            IntPtr hWnd = processes[0].MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("无法获取窗口句柄");
                return false;
            }

            RestoreWindow();

            if (!GetWindowRect(hWnd, out RECT rect))
            {
                Console.WriteLine("获取窗口尺寸失败");
                return false;
            }

            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;

            POINT targetPoint = new POINT
            {
                X = (int)(windowWidth * (xPercent / 100.0)),
                Y = (int)(windowHeight * (yPercent / 100.0))
            };

            ClientToScreen(hWnd, ref targetPoint);

            if (!SetCursorPos(targetPoint.X, targetPoint.Y))
            {
                Console.WriteLine("鼠标移动失败");
                return false;
            }

            System.Threading.Thread.Sleep(50);

            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)targetPoint.X, (uint)targetPoint.Y, 0, 0);

            return true;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern short VkKeyScan(char c);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_CONTROL = 0x11;
        private const byte VK_V = 0x56;
        private const byte VK_SHIFT = 0x10;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        public static void InputText(string inputStr)
        {
            RestoreWindow();
            bool clipboardSuccess = false;

            Thread clipboardThread = new Thread(() =>
            {
                try
                {
                    string original = Clipboard.GetText();
                    try
                    {
                        Clipboard.SetText(inputStr);
                        keybd_event(VK_CONTROL, 0, 0, 0);
                        keybd_event(VK_V, 0, 0, 0);
                        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, 0);
                        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
                        Thread.Sleep(100);
                        clipboardSuccess = true;
                        Clipboard.SetText(original);
                    }
                    catch { }
                }
                catch { }
            });

            clipboardThread.SetApartmentState(ApartmentState.STA);
            clipboardThread.Start();
            clipboardThread.Join();

            if (clipboardSuccess) return;

            foreach (char c in inputStr)
            {
                short keyCode = VkKeyScan(c);
                if (keyCode == -1) continue;

                byte vk = (byte)(keyCode & 0xFF);
                byte shiftState = (byte)((keyCode >> 8) & 0xFF);

                bool shiftPressed = (shiftState & 1) != 0;
                if (shiftPressed) keybd_event(VK_SHIFT, 0, 0, 0);

                keybd_event(vk, 0, 0, 0);
                keybd_event(vk, 0, KEYEVENTF_KEYUP, 0);
                if (shiftPressed) keybd_event(VK_SHIFT, 0, KEYEVENTF_KEYUP, 0);

                Thread.Sleep(10);
            }
        }

        public static string RecognizeTextFromDeltaForce(float leftPercent, float topPercent,
                                                       float rightPercent, float bottomPercent,
                                                       string savePath, bool isDigit)
        {
            // 1. 获取Delta Force窗口句柄
            Process[] processes = Process.GetProcessesByName("DeltaForceClient-Win64-Shipping");
            if (processes.Length == 0)
            {
                Console.WriteLine("Delta Force进程未找到");
                return string.Empty;
            }

            IntPtr hWnd = processes[0].MainWindowHandle;
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("无法获取窗口句柄");
                return string.Empty;
            }

            // 2. 获取窗口尺寸并计算截图区域
            if (!GetWindowRect(hWnd, out RECT rect))
            {
                Console.WriteLine("获取窗口尺寸失败");
                return string.Empty;
            }

            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;

            int left = rect.Left + (int)(windowWidth * leftPercent);
            int top = rect.Top + (int)(windowHeight * topPercent);
            int width = (int)(windowWidth * (rightPercent - leftPercent));
            int height = (int)(windowHeight * (bottomPercent - topPercent));

            // 3. 截图
            using (Bitmap bmp = new Bitmap(width, height))
            {
                
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(left, top, 0, 0, new Size(width, height));
                }
                // bmp.Save(savePath);
                if (isDigit) {
                    using (var engine = new TesseractEngine(TesseractDataPath, "eng", EngineMode.LstmOnly))
                    {
                        engine.SetVariable("tessedit_char_whitelist", "0123456789");
                        engine.SetVariable("psm", "11");
                        using (var page = engine.Process(bmp))
                        {
                            return page.GetText().Trim();
                        }
                    }
                }
                else
                {
                    using (var engine = new TesseractEngine(TesseractDataPath, "chi_sim", EngineMode.LstmOnly))
                    {
                        using (var original = new Bitmap(bmp))
                        {
                            var scaledWidth = original.Width * 8;
                            var scaledHeight = original.Height * 8;
                            using (var scaledBitmap = new Bitmap(scaledWidth, scaledHeight))
                            {
                                using (var graphics = Graphics.FromImage(scaledBitmap))
                                {
                                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                                    graphics.DrawImage(original, new Rectangle(0, 0, scaledWidth, scaledHeight));
                                }
                                using (var page = engine.Process(scaledBitmap))
                                {
                                    return page.GetText().Trim().Replace(" ", "");
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool isBuySuccess() =>
            !RecognizeTextFromDeltaForce(0.4211f, 0.1710f, 0.5926f, 0.2094f, null, false)?.Contains("失败") ?? true;
    }
}
