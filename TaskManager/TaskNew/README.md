# Task Manager Pro - 增强版进程管理器

## 项目简介

Task Manager Pro 是一个基于 WPF 和 .NET 8.0 构建的现代化进程管理器。它是对原有 TaskManage 项目的全面重构和增强，采用了 MVVM 架构、依赖注入和现代化设计模式。

## 主要特性

### 🚀 核心功能
- **进程管理**：查找、监控、关闭 .NET 进程
- **任务群组**：按项目或功能分组管理进程
- **性能监控**：实时显示 CPU、内存使用率
- **批量操作**：支持全选、批量关闭进程

### 🎨 用户体验
- **现代化界面**：暗色主题，响应式设计
- **实时反馈**：加载状态、进度指示器
- **键盘支持**：快捷键操作，提高效率
- **多视图**：列表视图、分组视图、详情视图

### ⚡ 性能优化
- **异步处理**：所有耗时操作异步执行，不阻塞 UI
- **智能缓存**：进程信息缓存，减少 WMI 查询
- **并行处理**：多线程处理大量进程
- **资源管理**：自动清理，防止内存泄漏

### 🔧 可配置性
- **配置文件**：JSON 配置文件，支持热重载
- **主题切换**：支持明暗主题
- **刷新策略**：可配置的自动刷新间隔
- **搜索模式**：按名称、目录、PID 等多种搜索方式

## 项目结构

```
TaskNew/
├── Models/                    # 数据模型层
│   ├── Enums.cs              # 枚举定义
│   ├── ProcessInfo.cs        # 进程信息模型
│   └── TaskGroupInfo.cs      # 任务群组模型
├── ViewModels/               # 视图模型层
│   └── MainViewModel.cs      # 主视图模型
├── Views/                    # 视图层
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
└── TaskManagerNew.csproj    # 项目文件
```

## 技术栈

- **框架**: .NET 8.0 WPF
- **架构**: MVVM (Model-View-ViewModel)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **UI 框架**: CommunityToolkit.Mvvm
- **序列化**: Newtonsoft.Json
- **进程管理**: System.Management (WMI)
- **图表**: LiveChartsCore (预留)

## 快速开始

### 构建要求
- .NET 8.0 SDK
- Windows 10/11

### 构建步骤

```bash
# 克隆项目
cd D:\0OpenClaw\project\TaskManage\TaskNew

# 还原包
dotnet restore

# 构建项目
dotnet build

# 运行应用
dotnet run
```

或使用构建脚本：

```powershell
# 清理构建
.\build.ps1 clean

# 构建项目
.\build.ps1 build

# 发布应用
.\build.ps1 publish
```

## 配置说明

### 基本配置 (appsettings.json)

```json
{
  "ProcessManager": {
    "RefreshInterval": 5000,           // 刷新间隔（毫秒）
    "CacheDuration": 300000,           // 缓存持续时间
    "MaxConcurrentProcesses": 10,      // 最大并发数
    "EnablePerformanceMonitoring": true // 启用性能监控
  },
  "UI": {
    "Theme": "Dark",                   // 界面主题
    "AutoRefresh": true,               // 自动刷新
    "GroupByTask": true                // 按任务分组
  }
}
```

## 使用指南

### 1. 搜索进程
- 在搜索框中输入进程名称、PID 或路径
- 选择搜索模式（按名称、按目录、按PID）
- 点击搜索按钮或按 Enter 键

### 2. 管理任务群组
- **创建群组**：点击"新建"按钮
- **添加进程**：选中进程，点击"添加到群组"
- **删除群组**：选中群组，点击"删除"
- **群组操作**：支持批量启动、停止、重启

### 3. 进程操作
- **选择进程**：点击复选框或使用全选功能
- **关闭进程**：选中进程，点击"关闭选中进程"
- **查看详情**：双击进程查看详细信息
- **性能监控**：实时显示 CPU、内存使用率

### 4. 系统设置
- **自动刷新**：启用/禁用自动刷新
- **主题切换**：明暗主题切换
- **缓存管理**：手动清除缓存
- **日志查看**：查看应用程序日志

## 架构设计

### MVVM 架构
```
View (XAML) → ViewModel → Service → Model
      ↑           ↑          ↑        ↑
   绑定命令     业务逻辑   数据访问  数据模型
```

### 依赖注入
```csharp
// 服务注册
services.AddSingleton<IConfigurationService, ConfigurationService>();
services.AddSingleton<IProcessService, ProcessService>();
services.AddSingleton<ITaskManagerService, TaskManagerService>();
services.AddSingleton<MainViewModel>();
services.AddSingleton<MainWindow>();
```

### 异步编程
```csharp
public async Task<List<ProcessInfo>> FindProcessesAsync(
    string searchTerm,
    ProcessSearchMode mode,
    CancellationToken cancellationToken = default)
{
    // 异步执行耗时操作
    return await Task.Run(() => {
        // 进程查询逻辑
    }, cancellationToken);
}
```

## 性能优化

### 1. 缓存策略
- 进程信息缓存（5分钟）
- WMI 查询结果缓存
- 性能数据缓存

### 2. 并行处理
```csharp
var options = new ParallelOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount
};

await Parallel.ForEachAsync(processes, options, async (process, ct) => {
    // 并行处理每个进程
});
```

### 3. 资源管理
- 实现 IDisposable 接口
- 使用 using 语句确保资源释放
- 定时清理过期缓存

## 扩展开发

### 添加新功能

1. **添加新模型**
```csharp
public class NewModel : ObservableObject
{
    [ObservableProperty]
    private string _property;
}
```

2. **添加新服务**
```csharp
public interface INewService
{
    Task<Result> DoSomethingAsync();
}

public class NewService : INewService
{
    // 实现逻辑
}
```

3. **添加新视图模型**
```csharp
public partial class NewViewModel : ObservableObject
{
    [RelayCommand]
    private async Task ExecuteAsync()
    {
        // 命令逻辑
    }
}
```

## 故障排除

### 常见问题

1. **无法访问进程信息**
   - 以管理员身份运行
   - 检查 WMI 服务是否运行

2. **内存占用过高**
   - 调整缓存策略
   - 减少自动刷新频率
   - 清理不需要的进程

3. **界面卡顿**
   - 减少并发处理数
   - 优化数据绑定
   - 使用虚拟化列表

### 日志文件
- 应用程序日志：`%LOCALAPPDATA%\TaskManagerPro\logs\`
- 错误日志：`%LOCALAPPDATA%\TaskManagerPro\error.log`

## 版本历史

### v2.0.0 (2026-02-26)
- 基于 MVVM 架构全面重构
- 添加任务群组管理功能
- 实现实时性能监控
- 现代化暗色主题界面
- 支持配置文件管理
- 优化性能和资源管理

### v1.0.0 (原始版本)
- 基础进程管理功能
- 简单的任务分组
- 基本界面设计

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 创建 Pull Request

## 许可证

MIT License

## 联系方式

如有问题或建议，请提交 Issue 或联系维护者。

---

**感谢使用 Task Manager Pro！** 🚀