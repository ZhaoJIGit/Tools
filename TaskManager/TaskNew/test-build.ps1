Write-Host "=== Task Manager Pro 构建测试 ===" -ForegroundColor Cyan
Write-Host "测试时间: $(Get-Date)" -ForegroundColor Yellow

# 检查项目文件
Write-Host "`n1. 检查项目文件..." -ForegroundColor Green
$csFiles = Get-ChildItem -Recurse -File -Include *.cs
Write-Host "  找到 $($csFiles.Count) 个 C# 文件" -ForegroundColor White

$xamlFiles = Get-ChildItem -Recurse -File -Include *.xaml
Write-Host "  找到 $($xamlFiles.Count) 个 XAML 文件" -ForegroundColor White

# 检查命名空间
Write-Host "`n2. 检查命名空间..." -ForegroundColor Green
$namespaceIssues = @()
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -TotalCount 20
    if ($content -match "namespace\s+(\w+)") {
        $ns = $matches[1]
        if ($ns -ne "TaskManagerNew" -and $ns -ne "TaskManagerNew.Models" -and $ns -ne "TaskManagerNew.ViewModels" -and $ns -ne "TaskManagerNew.Views" -and $ns -ne "TaskManagerNew.Services" -and $ns -ne "TaskManagerNew.Utilities") {
            $namespaceIssues += "$($file.Name): $ns"
        }
    }
}

if ($namespaceIssues.Count -gt 0) {
    Write-Host "  发现命名空间问题:" -ForegroundColor Red
    foreach ($issue in $namespaceIssues) {
        Write-Host "    $issue" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ✓ 所有命名空间正确" -ForegroundColor Green
}

# 检查依赖项
Write-Host "`n3. 检查项目依赖项..." -ForegroundColor Green
$projectContent = Get-Content "TaskManagerNew.csproj" -Raw
if ($projectContent -match "PackageReference") {
    Write-Host "  ✓ 项目包含 NuGet 包引用" -ForegroundColor Green
} else {
    Write-Host "  ⚠ 项目可能缺少 NuGet 包引用" -ForegroundColor Yellow
}

# 检查配置文件
Write-Host "`n4. 检查配置文件..." -ForegroundColor Green
if (Test-Path "appsettings.json") {
    Write-Host "  ✓ 找到 appsettings.json" -ForegroundColor Green
    $config = Get-Content "appsettings.json" -Raw | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($config) {
        Write-Host "  ✓ 配置文件格式正确" -ForegroundColor Green
    } else {
        Write-Host "  ⚠ 配置文件格式可能有问题" -ForegroundColor Yellow
    }
} else {
    Write-Host "  ❌ 缺少 appsettings.json" -ForegroundColor Red
}

# 检查主题文件
Write-Host "`n5. 检查主题文件..." -ForegroundColor Green
if (Test-Path "Themes\DarkTheme.xaml") {
    Write-Host "  ✓ 找到主题文件" -ForegroundColor Green
} else {
    Write-Host "  ❌ 缺少主题文件" -ForegroundColor Red
}

# 总结
Write-Host "`n=== 构建测试总结 ===" -ForegroundColor Cyan
Write-Host "总文件数: $($csFiles.Count + $xamlFiles.Count)" -ForegroundColor White

if ($namespaceIssues.Count -eq 0) {
    Write-Host "命名空间问题: 0" -ForegroundColor Green
} else {
    Write-Host "命名空间问题: $($namespaceIssues.Count)" -ForegroundColor Red
}

if (Test-Path "appsettings.json") {
    Write-Host "配置文件: ✓" -ForegroundColor Green
} else {
    Write-Host "配置文件: ❌" -ForegroundColor Red
}

if (Test-Path "Themes\DarkTheme.xaml") {
    Write-Host "主题文件: ✓" -ForegroundColor Green
} else {
    Write-Host "主题文件: ❌" -ForegroundColor Red
}

if ($namespaceIssues.Count -eq 0 -and (Test-Path "appsettings.json") -and (Test-Path "Themes\DarkTheme.xaml")) {
    Write-Host "`n✅ 项目结构检查通过！" -ForegroundColor Green
    Write-Host "可以尝试使用 'dotnet build' 进行构建。" -ForegroundColor Cyan
} else {
    Write-Host "`n⚠ 项目结构存在一些问题，需要修复。" -ForegroundColor Yellow
}