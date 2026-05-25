# Task Manager Pro - 构建指南

## 项目概述

Task Manager Pro 是一个增强版的进程管理器，基于 WPF 和 .NET 8.0 构建。它提供了以下功能：

- 进程管理（查找、监控、关闭）
- 任务群组管理
- 实时性能监控（CPU、内存）
- 自动刷新和缓存
- 现代化的暗色主题界面

## 项目结构

```
TaskNew/
├── Models/                    # 数据模型
│   ├── Enums.cs              # 枚举定义
│   ├── ProcessInfo.cs        # 进程信息模型
│   └── TaskGroupInfo.cs      # 任务群组模型
├── ViewModels/               # 视图模型
│   └── MainViewModel.cs      # 主视图模型
├── Views/                    # 视图
│   └── MainWindow.xaml       # 主窗口
├── Services/                 # 服务层
│   ├── ConfigurationService.cs # 配置服务
│   ├── ProcessCache.cs       # 进程缓存
│   ├── ProcessService.cs     # 进程服务
│   └── TaskManagerService.cs # 任务管理服务
├── Utilities/                # 工具类
│   └── Converters.cs         # WPF 转换器
├── Themes/                   # 主题资源
│   └── DarkTheme.xaml       # 暗色主题
├── appsettings.json          # 配置文件
├── AssemblyInfo.cs          # 程序集信息
├── Program.cs               # 程序入口点
├── App.xaml                 # 应用程序定义
├── TaskManagerNew.csproj    # 项目文件
└── BUILD.md                 # 构建指南
```

## 构建要求

### 开发环境
- .NET 8.0 SDK 或更高版本
- Visual Studio 2022 或更高版本（推荐）
- 或使用命令行工具（dotnet CLI）

### 依赖项
项目依赖以下 NuGet 包：
- CommunityToolkit.Mvvm (8.2.1)
- Newtonsoft.Json (13.0.3)
- System.Management (8.0.0)
- Microsoft.Extensions.* (8.0.0)
- LiveChartsCore.SkiaSharpView.WPF (2.0.0-rc5.1)

## 构建步骤

### 方法一：使用 Visual Studio

1. 打开 `TaskManagerNew.csproj` 文件
2. 等待 Visual Studio 还原 NuGet 包
3. 按 F5 编译并运行

### 方法二：使用命令行

```bash
# 切换到项目目录
cd D:\0OpenClaw\project\TaskManage\TaskNew

# 还原 NuGet 包
dotnet restore

# 编译项目
dotnet build

# 运行应用程序
dotnet run

# 发布独立应用程序
dotnet publish -c Release -r win-x64 --self-contained true
```

### 方法三：使用 PowerShell 脚本

```powershell
# 构建脚本
.\build.ps1

# 清理脚本
.\clean.ps1
```

## 配置说明

### 配置文件 (appsettings.json)

```json
{
  "ProcessManager": {
    "RefreshInterval": 5000,           // 刷新间隔（毫秒）
    "CacheDuration": 300000,           // 缓存持续时间（毫秒）
    "DefaultSearchPaths": [            // 默认搜索路径
      "C:\\Projects",
      "D:\\Work"
    ],
    "AutoSaveInterval": 60000,         // 自动保存间隔（毫秒）
    "MaxConcurrentProcesses": 10,      // 最大并发进程数
    "EnablePerformanceMonitoring": true, // 启用性能监控
    "PerformanceUpdateInterval": 1000, // 性能更新间隔（毫秒）
    "LogLevel": "Information"          // 日志级别
  },
  "UI": {
    "Theme": "Dark",                   // 主题（Dark/Light）
    "ShowSystemProcesses": false,      // 显示系统进程
    "GroupByTask": true,               // 按任务分组
    "AutoRefresh": true,               // 自动刷新
    "DefaultView": "List"              // 默认视图
  }
}
```

## 功能特性

### 1. 进程管理
- 按名称、目录、PID 搜索进程
- 实时显示 CPU 和内存使用率
- 支持批量关闭进程
- 进程状态监控（运行中、已停止、异常）

### 2. 任务群组管理
- 创建、编辑、删除任务群组
- 将进程添加到群组
- 群组统计信息（总CPU、总内存）
- 群组批量操作（启动、停止、重启）

### 3. 性能监控
- 实时 CPU 使用率图表
- 内存使用量监控
- 进程运行时间统计
- 性能数据缓存

### 4. 用户体验
- 现代化的暗色主题
- 响应式界面设计
- 键盘快捷键支持
- 加载状态指示器

## 开发指南

### 添加新功能

1. **添加新模型**：在 `Models/` 目录中创建新的数据模型
2. **添加新服务**：在 `Services/` 目录中实现业务逻辑
3. **添加新视图模型**：在 `ViewModels/` 目录中创建 MVVM 视图模型
4. **添加新视图**：在 `Views/` 目录中创建 WPF 界面

### 代码规范

- 使用异步编程（async/await）处理耗时操作
- 遵循 MVVM 模式，分离视图和业务逻辑
- 使用依赖注入管理服务生命周期
- 添加适当的错误处理和日志记录

### 测试建议

1. **单元测试**：测试服务层和业务逻辑
2. **集成测试**：测试服务之间的交互
3. **UI 测试**：测试界面交互和响应

## 部署说明

### 独立部署

```bash
# 发布为独立应用程序
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# 输出目录：bin\Release\net8.0\win-x64\publish\
```

### 安装程序

可以使用以下工具创建安装程序：
- WiX Toolset
- Inno Setup
- NSIS

## 故障排除

### 常见问题

1. **无法找到 .NET 8.0 SDK**
   - 下载并安装 .NET 8.0 SDK
   - 检查环境变量 PATH

2. **NuGet 包还原失败**
   - 检查网络连接
   - 清除 NuGet 缓存：`dotnet nuget locals all --clear`
   - 更新 NuGet 源

3. **WMI 访问被拒绝**
   - 以管理员身份运行应用程序
   - 检查 Windows 管理规范服务是否运行

4. **内存泄漏**
   - 确保正确释放资源（实现 IDisposable）
   - 使用弱引用处理事件
   - 定期清理缓存

### 日志文件

应用程序日志位于：
```
%LOCALAPPDATA%\TaskManagerPro\logs\
```

错误日志位于：
```
%LOCALAPPDATA%\TaskManagerPro\error.log
```

## 性能优化

### 建议配置

1. **调整刷新间隔**：根据系统负载调整 `RefreshInterval`
2. **优化缓存策略**：根据内存使用情况调整 `CacheDuration`
3. **限制并发数**：根据 CPU 核心数调整 `MaxConcurrentProcesses`
4. **启用/禁用监控**：根据需求调整 `EnablePerformanceMonitoring`

### 内存管理

- 使用对象池重用对象
- 及时释放大对象
- 使用弱引用缓存
- 定期清理未使用的资源

## 更新日志

### v2.0.0 (2026-02-26)
- 初始版本发布
- 基于 MVVM 架构重构
- 添加任务群组管理
- 实现性能监控
- 现代化暗色主题

## 许可证

本项目基于 MIT 许可证开源。

## 支持与贡献

如有问题或建议，请：
1. 查看 GitHub Issues
2. 提交 Pull Request
3. 联系开发团队

---

**祝您使用愉快！**