using System;
using System.Windows.Forms;

namespace DeltaForce_Card_Helper
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread] // 必须添加这个属性，因为WinForms需要单线程单元(STA)模型
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DeltaForceCardHelperForm());
        }
    }
}