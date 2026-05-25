using PublishDesk.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

public class AddSiteViewModel : INotifyPropertyChanged
{
    #region 属性

    public EditMode Mode { get; }

    public string Title =>
        Mode == EditMode.Add
            ? "添加站点"
            : "编辑站点";

    public string Name { get; set; }

    public string SitePath { get; set; }

    public string FilePath { get; set; }

    public SiteModel Original { get; }

    #endregion

    #region 状态

    private bool _isSaving;

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            _isSaving = value;
            OnPropertyChanged(nameof(IsSaving));
        }
    }

    #endregion

    #region 命令

    public RelayCommand SaveCommand { get; }

    public RelayCommand CancelCommand { get; }

    #endregion

    #region 窗口结果

    public bool? DialogResult { get; set; }

    #endregion

    public AddSiteViewModel(EditMode mode, SiteModel site = null)
    {
        Mode = mode;

        if (mode == EditMode.Edit && site != null)
        {
            Original = site;

            Name = site.Name;
            SitePath = site.SitePath;
            FilePath = site.FilePath;
        }

        SaveCommand = new RelayCommand(async () => await SaveAsync());

        CancelCommand = new RelayCommand(Cancel);
    }

    #region 保存（核心）

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;

            if (Mode == EditMode.Add)
            {
                var model = new SiteModel
                {
                    Name = Name,
                    SitePath = SitePath,
                    FilePath = FilePath
                };

                // 提交新增
                await ApiClient.PostAsync("/api/Publish/AddSite", model);
            }
            else
            {
                Original.Name = Name;
                Original.SitePath = SitePath;
                Original.FilePath = FilePath;

                // 提交编辑
                await ApiClient.PostAsync(
                    $"/api/Publish/UpdateSite",
                    Original);
            }

            DialogResult = true;

            OnPropertyChanged(nameof(DialogResult));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    #endregion

    #region 取消

    private void Cancel()
    {
        DialogResult = false;
        OnPropertyChanged(nameof(DialogResult));
    }

    #endregion

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this,
            new PropertyChangedEventArgs(name));
    }
}