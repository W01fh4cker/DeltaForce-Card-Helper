import time
import pyautogui
import cv2
import numpy as np
import ctypes
import pyperclip
import keyboard
import threading
from pytesseract import pytesseract
import ttkbootstrap as ttk
from ttkbootstrap.constants import *
from ttkbootstrap.dialogs import Messagebox

ctypes.windll.shcore.SetProcessDpiAwareness(1)
screen_width, screen_height = pyautogui.size()

running = False
lock = threading.Lock()

pyautogui.PAUSE = 0.02
pyautogui.FAILSAFE = False

def set_running(value):
    global running
    with lock:
        running = value

def TakeScreenshot(region):
    screenshot = pyautogui.screenshot(region=region)
    screenshot_np = np.array(screenshot)
    screenshot_bgr = cv2.cvtColor(screenshot_np, cv2.COLOR_RGB2BGR)
    gray = cv2.cvtColor(screenshot_bgr, cv2.COLOR_BGR2GRAY)
    denoised = cv2.fastNlMeansDenoising(gray, h=15, templateWindowSize=7, searchWindowSize=21)
    scale_percent = 200
    width = int(denoised.shape[1] * scale_percent / 100)
    height = int(denoised.shape[0] * scale_percent / 100)
    resized = cv2.resize(denoised, (width, height), interpolation=cv2.INTER_CUBIC)
    return resized

def RecognizePrice(region):
    results = []
    for _ in range(2):
        img = TakeScreenshot(region)
        text = pytesseract.image_to_string(
            img,
            lang='eng',
            config='--psm 7 --oem 3 -c tessedit_char_whitelist=0123456789,'
        ).strip().replace(",", "")
        if text.isdigit():
            results.append(int(text))
            time.sleep(0.05)
    return str(max(set(results), key=results.count)) if results else "N/A"

def MoveClick(ox, oy):
    try:
        x = int(screen_width * ox)
        y = int(screen_height * oy)
        pyautogui.click(x, y, duration=0.1)
        time.sleep(0.15)
        return True
    except:
        return False

class TradingBotApp:
    def __init__(self, master):
        self.master = master
        myappid = "DF-Card-Helper-0.0.1-dev"
        ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID(myappid)
        master.wm_iconbitmap('logo.ico')
        master.title("三角洲交易行抢卡助手 v0.0.1-dev")
        master.geometry("600x700+1200+50")
        self.style = ttk.Style(theme='minty')
        self.status = ttk.StringVar(value="状态：已停止")
        self.worker_thread = None
        self.create_widgets()
        keyboard.add_hotkey('s', self.start_bot)
        keyboard.add_hotkey('q', self.stop_bot)
        self.master.attributes('-alpha', 0.95)
        self.master.bind("<B1-Motion>", self._drag_window)
        self.master.bind("<Button-1>", self._start_drag)

    def _start_drag(self, event):
        self._x = event.x
        self._y = event.y

    def _drag_window(self, event):
        deltax = event.x - self._x
        deltay = event.y - self._y
        x = self.master.winfo_x() + deltax
        y = self.master.winfo_y() + deltay
        self.master.geometry(f"+{x}+{y}")

    def create_widgets(self):
        main_frame = ttk.Frame(self.master)
        main_frame.pack(padx=10, pady=10, fill=BOTH, expand=True)

        input_frame = ttk.Labelframe(main_frame, text="参数设置", bootstyle=PRIMARY)
        input_frame.pack(fill=X, pady=5)

        ttk.Label(input_frame, text="卡牌名称：").grid(row=0, column=0, padx=5, pady=5, sticky=W)
        self.card_entry = ttk.Entry(input_frame)
        self.card_entry.grid(row=0, column=1, padx=5, pady=5, sticky=EW)

        ttk.Label(input_frame, text="理想价格：").grid(row=1, column=0, padx=5, pady=5, sticky=W)
        self.price_entry = ttk.Entry(input_frame)
        self.price_entry.grid(row=1, column=1, padx=5, pady=5, sticky=EW)

        btn_frame = ttk.Frame(main_frame)
        btn_frame.pack(pady=10, fill=X)

        self.start_btn = ttk.Button(btn_frame, text="启动 (快捷键：S)", bootstyle=INFO, command=self.start_bot)
        self.start_btn.pack(side=LEFT, padx=5)

        self.stop_btn = ttk.Button(btn_frame, text="停止 (快捷键：Q)", bootstyle=DANGER, command=self.stop_bot)
        self.stop_btn.pack(side=LEFT, padx=5)

        status_frame = ttk.Frame(main_frame)
        status_frame.pack(fill=X, pady=10)
        self.status_label = ttk.Label(status_frame, textvariable=self.status, bootstyle=(PRIMARY, INVERSE))
        self.status_label.pack()

        log_frame = ttk.Labelframe(main_frame, text="运行日志", bootstyle=INFO)
        log_frame.pack(fill=BOTH, expand=True, pady=5)

        self.log_text = ttk.Text(
            log_frame,
            height=12,
            state=DISABLED,
            font=('Consolas', 14),
            foreground='#00FF00',
            background='#2D2D2D',
            insertbackground='yellow',
            wrap='none'  # 禁用自动换行
        )
        scrl = ttk.Scrollbar(log_frame, command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=scrl.set)
        scrl.pack(side=RIGHT, fill=Y)
        self.log_text.pack(fill=BOTH, expand=True, padx=5, pady=5)

        clear_btn = ttk.Button(log_frame, text="清空日志", bootstyle=WARNING, command=self.clear_log)
        clear_btn.pack(side=LEFT, padx=5, pady=5)

    def clear_log(self):
        self.log_text.config(state=NORMAL)
        self.log_text.delete(1.0, END)
        self.log_text.config(state=DISABLED)

    def update_log(self, message):
        self.master.after(0, lambda: self._append_log(f"[{time.strftime('%H:%M:%S')}] {message}"))

    def _append_log(self, message):
        self.log_text.config(state=NORMAL)
        self.log_text.insert(END, message + "\n")
        self.log_text.see(END)
        self.log_text.config(state=DISABLED)

    def start_bot(self):
        if not self.validate_input():
            return
        if not self.worker_thread or not self.worker_thread.is_alive():
            self.status.set("状态：运行中...")
            self.worker_thread = threading.Thread(target=self.run_bot, daemon=True)
            self.worker_thread.start()
            set_running(True)

    def stop_bot(self):
        set_running(False)
        self.status.set("状态：已停止")

    def validate_input(self):
        card = self.card_entry.get()
        price = self.price_entry.get()
        if not card.strip():
            Messagebox.show_error("请输入有效的卡牌名称", "输入错误", parent=self.master)
            return False
        if not price.isdigit() or int(price) <= 0:
            Messagebox.show_error("价格必须为正整数", "输入错误", parent=self.master)
            return False
        return True

    def run_bot(self):
        card_name = self.card_entry.get()
        ideal_price = int(self.price_entry.get())
        price_region = (
            int(screen_width * 0.8323),
            int(screen_height * 0.8528),
            int(screen_width * 0.0505),
            int(screen_height * 0.0176)
        )

        try:
            init_region = (screen_width//2 - 100, screen_height//2 - 50, 200, 100)
            initial_text = pytesseract.image_to_string(TakeScreenshot(init_region), lang='chi_sim')
            if "交易" in initial_text and not MoveClick(0.3573, 0.0389):
                self.update_log("无法关闭初始弹窗")
        except:
            pass

        if not MoveClick(0.0833, 0.1556):
            self.update_log("无法定位搜索框")
            return

        pyperclip.copy(card_name)
        pyautogui.hotkey('ctrl', 'v')
        self.update_log(f"开始监控：{card_name}，目标价格：{ideal_price}万")

        while running:
            try:
                if not MoveClick(0.3375, 0.2204):
                    continue

                time.sleep(0.3)
                price = RecognizePrice(price_region)

                if price.isdigit():
                    price_int = int(price)
                    if price_int <= ideal_price * 10000:
                        self.update_log(f"识别价格：{price} --> 符合条件！推荐购买（≤{ideal_price}万）")
                        pyautogui.click(int(screen_width * 0.8323), int(screen_height * 0.8528))
                        self.master.bell()
                    else:
                        self.update_log(f"识别价格：{price} 价格过高（>{ideal_price}万）")

                pyautogui.press('esc')
                time.sleep(0.3)

            except:
                self.stop_bot()

if __name__ == "__main__":
    root = ttk.Window()
    TradingBotApp(root)
    root.mainloop()