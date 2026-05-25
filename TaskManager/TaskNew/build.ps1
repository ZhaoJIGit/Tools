# Task Manager Pro 构建脚本
# 用法: .\build.ps1 [clean|restore|build|run|publish]

param(
    [string]$Action = "build",
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ProjectFile = "TaskManagerNew.csproj"
$OutputDir = "bin\$Configuration\net8.0-windows\$Runtime"
$PublishDir = "publish"

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Clean {
    Write-Info "清理构建输出..."
    if (Test-Path "bin") {
        Remove-Item -Path "bin" -Recurse -Force
    }
    if (Test-Path "obj") {
        Remove-Item -Path "obj" -Recurse -Force
    }
    Write-Info "清理完成"
}

function Restore {
    Write-Info "还原 NuGet 包..."
    dotnet restore $ProjectFile
    if ($LASTEXITCODE -ne 0) {
        Write-Error "包还原失败"
        exit 1
    }
    Write-Info "包还原完成"
}

function Build {
    Write-Info "构建项目..."
    dotnet build $ProjectFile -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Error "构建失败"
        exit 1
    }
    
    # 检查输出文件
    $exePath = Join-Path $OutputDir "TaskManagerNew.exe"
    if (Test-Path $exePath) {
        Write-Info "构建成功: $exePath"
        
        # 显示文件信息
        $fileInfo = Get-Item $exePath
        Write-Info "文件大小: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
        Write-Info "创建时间: $($fileInfo.CreationTime)"
    } else {
        Write-Warning "未找到可执行文件"
    }
}

function Run {
    Write-Info "运行应用程序..."
    dotnet run --project $ProjectFile
}

function Publish {
    Write-Info "发布应用程序..."
    
    # 创建发布目录
    $fullPublishDir = Join-Path $OutputDir $PublishDir
    if (Test-Path $fullPublishDir) {
        Remove-Item -Path $fullPublishDir -Recurse -Force
    }
    
    # 发布为独立应用程序
    dotnet publish $ProjectFile -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -o $fullPublishDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "发布失败"
        exit 1
    }
    
    Write-Info "发布完成: $fullPublishDir"
    
    # 显示发布结果
    $files = Get-ChildItem $fullPublishDir
    Write-Info "发布文件列表:"
    foreach ($file in $files) {
        $size = if ($file.Length -gt 1MB) {
            "$([math]::Round($file.Length / 1MB, 2)) MB"
        } elseif ($file.Length -gt 1KB) {
            "$([math]::Round($file.Length / 1KB, 2)) KB"
        } else {
            "$($file.Length) B"
        }
        Write-Info "  $($file.Name) ($size)"
    }
    
    # 创建 ZIP 包
    $zipPath = "TaskManagerPro-$Configuration-$Runtime.zip"
    Write-Info "创建 ZIP 包: $zipPath"
    Compress-Archive -Path "$fullPublishDir\*" -DestinationPath $zipPath -Force
    Write-Info "ZIP 包创建完成"
}

function Test {
    Write-Info "运行测试..."
    # 这里可以添加单元测试
    Write-Info "测试完成（暂无测试）"
}

function Help {
    Write-Host "Task Manager Pro 构建脚本" -ForegroundColor Cyan
    Write-Host "用法: .\build.ps1 [action] [options]" -ForegroundColor White
    Write-Host ""
    Write-Host "可用操作:" -ForegroundColor Yellow
    Write-Host "  clean     清理构建输出" -ForegroundColor White
    Write-Host "  restore   还原 NuGet 包" -ForegroundColor White
    Write-Host "  build     构建项目（默认）" -ForegroundColor White
    Write-Host "  run       运行应用程序" -ForegroundColor White
    Write-Host "  publish   发布应用程序" -ForegroundColor White
    Write-Host "  test      运行测试" -ForegroundColor White
    Write-Host "  help      显示帮助信息" -ForegroundColor White
    Write-Host ""
    Write-Host "选项:" -ForegroundColor Yellow
    Write-Host "  -Configuration Debug|Release (默认: Release)" -ForegroundColor White
    Write-Host "  -Runtime      win-x64|win-x86 (默认: win-x64)" -ForegroundColor White
    Write-Host ""
    Write-Host "示例:" -ForegroundColor Green
    Write-Host "  .\build.ps1                    # 构建项目" -ForegroundColor White
    Write-Host "  .\build.ps1 clean             # 清理构建" -ForegroundColor White
    Write-Host "  .\build.ps1 publish           # 发布应用程序" -ForegroundColor White
    Write-Host "  .\build.ps1 run               # 运行应用程序" -ForegroundColor White
}

# 主执行逻辑
Write-Info "Task Manager Pro 构建脚本"
Write-Info "操作: $Action"
Write-Info "配置: $Configuration"
Write-Info "运行时: $Runtime"

switch ($Action.ToLower()) {
    "clean" { Clean }
    "restore" { Restore }
    "build" { Build }
    "run" { Run }
    "publish" { Publish }
    "test" { Test }
    "help" { Help }
    default {
        Write-Error "未知操作: $Action"
        Write-Host ""
        Help
        exit 1
    }
}

Write-Info "操作完成"