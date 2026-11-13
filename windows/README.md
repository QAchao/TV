# Windows EXE

双击 LibreTV.exe 会启动本机服务，并在默认浏览器打开 http://127.0.0.1:8080/。若服务已经启动，会直接打开页面。关闭浏览器后服务仍在后台运行。

EXE 内置 Windows x64 Node.js 和 LibreTV。首次运行会解压到 %LOCALAPPDATA%\LibreTV。如果 EXE 同级有 .env.local 或 LibreTV\.env.local，会使用该配置。否则首次运行会生成密码并复制到剪贴板。

## 构建

在仓库根目录运行 npm ci 和 npm run build，再将 .next/static 与 public 复制到 .next/standalone/.next/static 与 .next/standalone/public。使用 Windows x64 上的 Node.js 22 和 .NET Framework 4.x 构建。

    .\windows\Build.ps1 -RuntimePath .\.next\standalone

也可以将 -RuntimePath 指向现有的 LibreTV standalone 部署目录。输出位于 windows/dist/LibreTV.exe。构建会读取现有部署的源配置，或使用 windows/sources.env。
