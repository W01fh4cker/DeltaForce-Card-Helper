# DeltaForce-Card-Helper

# 一、使用

## 1.1 Python脚本（不推荐）

测试`python`版本为`3.9.13`，运行命令如下：

```powershell
git clone https://github.com/W01fh4cker/DeltaForce-Card-Helper.git
cd DeltaForce-Card-Helper/Src/Python
pip install -r requirements.txt -i https://mirrors.aliyun.com/pypi/simple
```

以管理员权限启动`cmd`，运行命令即可：

```powershell
python CardHelper.py
```
使用时，请确保您的三角洲行动应用处于如下界面，并且搜索框不应有任何输入内容：
![](https://github.com/user-attachments/assets/69d56d40-b745-49d3-97b0-f45a8ebde2b6)

通过键盘的`s`和`q`来控制脚本的启停，效果如图：

![](https://github.com/W01fh4cker/picx-images-hosting/raw/master/image-20250413031736546.7w70cudjqt.webp)
![](https://github.com/user-attachments/assets/dafaa758-c3cb-4037-b7b7-7b3ed38a241f)

## 1.2 C# 脚本（推荐）

使用`VS 2022`打开`DeltaForce-Card-Helper/Src/Csharp/DeltaForce-Card-Helper.sln`工程文件，需要安装相关`nuget`包，并安装`Devexpress 22.x`版本，详情自行百度，懒得自己编译的用我打包好的即可。  
> 为防止出现未知错误导致错误购买高价格卡，建议可以先填写参数1 1 1进行测试，看价格识别是否有异常，如果没有，再重新启动程序抢卡即可。

同样的，以管理员权限启动，启动时请确保您的三角洲行动应用处于如下界面，并且搜索框不应有任何输入内容：  
![](https://github.com/user-attachments/assets/69d56d40-b745-49d3-97b0-f45a8ebde2b6)
填写参数，测试如图：

![](https://github.com/user-attachments/assets/3c42a996-5866-4412-876f-ae4e7e7b3240)

# 二、说明与TODO

### ▋ 版本状态说明

⚠️ **高度实验性版本**
 本脚本处于早期开发阶段，核心功能尚未完善，仅实现基础界面框架与点击交互逻辑，仅供开发者学习技术原理或进行内部测试。

- 2025.4.15 发布0.0.2-dev版本，修复了https://github.com/W01fh4cker/DeltaForce-Card-Helper/issues/1 和 https://github.com/W01fh4cker/DeltaForce-Card-Helper/issues/2 。

### ▋ 重要风险提示

🚨 **使用本脚本可能导致**：

- 目标平台账号异常检测（包括但不限于限流、封禁）
- 系统稳定性风险（脚本崩溃导致进程中断）
- 潜在的数据丢失或损坏

### ▋ 免责声明

📜 使用者需明确知晓并同意：

1. 禁止用于违反服务条款的任何场景
2. 开发者**不承担**因使用脚本导致的直接/间接损失
3. 不得将本脚本用于商业盈利目的
4. 如继续使用视为自动接受《MIT开源协议》条款

> 📌 提示：克隆/下载本代码即代表您已完整阅读并理解上述所有声明内容
> 开源协议：MIT License ©2024 W01fh4cker
