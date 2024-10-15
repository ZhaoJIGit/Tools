using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskMGPro.Common;
using TaskMGPro.Helper;
using TaskMGPro.Models;
using TaskMGPro.Services;

namespace TaskMGPro.Pages
{
    /// <summary>
    /// AddGroupPage.xaml 的交互逻辑
    /// </summary>
    public partial class AddGroupPage : BasePage
    {
 
        public bool IsReadOnly = false;
        GroupInfo groupInfo;
        public AddGroupPage(bool isReadOnly = false, GroupInfo group = null)
        {
            IsReadOnly = isReadOnly;
            groupInfo = group;
            InitializeComponent();
            txtTitle.IsReadOnly = IsReadOnly;
            txtType.IsReadOnly = IsReadOnly;
            txtLogAddress.IsReadOnly = IsReadOnly;
            txtAddress.IsReadOnly = IsReadOnly;
            if (group != null)
            {
                txtTitle.Text = group.Title;
                txtType.Text = group.Type;
                txtLogAddress.Text = group.LogAddress;
                txtAddress.Text = group.Address;
            }

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
        private void Close(bool isRefresh)
        {
            OnPageClosed(isRefresh);
            Window.GetWindow(this).Close();
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (IsReadOnly) { Close(true); return; }
            var model = new GroupInfo
            {
                Title = txtTitle.Text,
                Type = txtType.Text,
                LogAddress = txtLogAddress.Text,
                Address = txtAddress.Text
            };
            if (groupInfo != null) { model.Id = groupInfo.Id; }
            var result = GroupService.SaveGroup(model);
            if (result > 0)
            {
                Close(true);
            }
            else
            {
                Message.Show("保存失败");
            }
        }
    }
}
