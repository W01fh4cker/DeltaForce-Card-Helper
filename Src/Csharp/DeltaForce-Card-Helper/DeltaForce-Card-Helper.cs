using DeltaForce_Card_Helper.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DeltaForce_Card_Helper
{
    public partial class DeltaForceCardHelperForm : DevExpress.XtraEditors.XtraForm
    {
        // 添加标志位
        private bool isBuying = false;
        private bool shouldStop = false;

        // 热键相关API
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID = 9000;

        public DeltaForceCardHelperForm()
        {
            InitializeComponent();
            RegisterHotKey(this.Handle, HOTKEY_ID, 0, (int)Keys.F10);
        }

        protected override void WndProc(ref Message m)
        {
            // 处理热键消息
            if (m.Msg == 0x0312 && m.WParam.ToInt32() == HOTKEY_ID && isBuying)
            {
                shouldStop = true;
                log_to_edit("🛑 用户按下 F10，停止购买...");
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 注销热键
            UnregisterHotKey(this.Handle, HOTKEY_ID);
            base.OnFormClosing(e);
        }

        public void PurchaseCard(string card_name, string ideal_price, string price_fluctuation_range, int buy_count)
        {
            if (isBuying)
            {
                log_to_edit("⚠️ 当前正在购买中，请勿重复操作");
                return;
            }

            isBuying = true;
            shouldStop = false;

            try
            {
                int buy_num = 0;
                DeltaForceUtils.MoveAndClickInDF(38, 5);
                Task.Delay(200).Wait();
                DeltaForceUtils.MoveAndClickInDF(10, 15);
                Task.Delay(200).Wait();
                DeltaForceUtils.InputText(card_name);
                Task.Delay(200).Wait();
                decimal idealPrice = decimal.Parse(ideal_price) * 10000;
                decimal range = decimal.Parse(price_fluctuation_range);
                decimal maxPrice = idealPrice + range;

                while (buy_num < buy_count && !shouldStop)
                {
                    DeltaForceUtils.MoveAndClickInDF(38, 20);
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string autoPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        "DeltaForceScreenshots",
                        $"Card_{timestamp}.png"
                    );
                    Task.Delay(200).Wait();
                    string res = DeltaForceUtils.RecognizeTextFromDeltaForce(0.8553f, 0.8111f, 0.8927f, 0.8315f, autoPath, true);
                    log_to_edit($"识别到价格: {res}");

                    if (decimal.TryParse(res, out decimal currentPrice))
                    {
                        log_to_edit($"当前价格: {currentPrice} 目标区间: ? - {maxPrice}");

                        if (currentPrice <= maxPrice)
                        {
                            DeltaForceUtils.MoveAndClickInDF(83, 84);
                            if (DeltaForceUtils.isBuySuccess())
                            {
                                log_to_edit("✅ 价格符合要求，购买成功！");
                                buy_num++;
                            }
                            else
                            {
                                log_to_edit("❌ 价格符合要求，购买失败，继续尝试");
                            }
                        }
                        else
                        {
                            log_to_edit("❌ 价格超出范围，取消重试");
                            SendKeys.SendWait("{ESC}");
                        }
                    }
                    else
                    {
                        log_to_edit("⚠️ 价格识别失败，取消重试");
                        SendKeys.SendWait("{ESC}");
                    }
                    Task.Delay(300).Wait();
                }

                if (shouldStop)
                {
                    log_to_edit("🛑 购买已停止");
                }
                else
                {
                    log_to_edit($"✅ 购买完成，共购买 {buy_num} 张卡");
                }
            }
            finally
            {
                isBuying = false;
                shouldStop = false;
            }
        }

        public void log_to_edit(string log_msg)
        {
            string formattedLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {log_msg}\r\n";
            if (log_edit.InvokeRequired)
            {
                log_edit.BeginInvoke(new Action(() => log_edit.AppendText(formattedLog)));
            }
            else
            {
                log_edit.AppendText(formattedLog);
            }
        }

        private void start_buy_button_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(card_num_text.Text, out int cardNum))
            {
                MessageBox.Show("请输入有效整数！");
                return;
            }

            PurchaseCard(card_name_text.Text,
                        ideal_price_text.Text,
                        more_price_text.Text,
                        cardNum);
        }
    }
}