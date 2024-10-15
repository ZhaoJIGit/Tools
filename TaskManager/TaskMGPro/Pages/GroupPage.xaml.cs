using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using System.Xml.Linq;
using TaskMGPro.Common;
using TaskMGPro.Helper;
using TaskMGPro.Models;
using TaskMGPro.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TaskMGPro.Pages
{
    /// <summary>
    /// HomePage.xaml 的交互逻辑
    /// </summary>
    public partial class GroupPage : BasePage
    {

        public GroupPage()
        {
            InitializeComponent();
            RefreshData();

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.Background = BackgroundColor.ToBrushColor();
        }

        private void BtnGroup_Click(object sender, RoutedEventArgs e)
        {
            // 创建弹出框
            PopupWindow popup = new PopupWindow();
            // 创建MyPage实例并传递参数
            AddGroupPage page = new AddGroupPage();
            page.PageClosed += MyPage_PageClosed;
            // 在弹窗中加载指定的Page
            popup.LoadPageWithParameters(page);
            // 显示弹出框
            var result = popup.ShowDialog();

        }
        private void MyPage_PageClosed(object? sender, PupupWindowEventArgs<bool> e)
        {
            if (e.Data)
            {
                RefreshData();
            }
        }
        private void RefreshData()
        {
            var groupList = GroupService.GetGroupList();
            listView.ItemsSource = groupList;
        }
        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var group = button?.Tag as GroupInfo;
            if (group != null)
            {
                // 创建弹出框
                PopupWindow popup = new PopupWindow();
                // 创建MyPage实例并传递参数
                AddGroupPage page = new AddGroupPage(false, group);
                page.PageClosed += MyPage_PageClosed;
                // 在弹窗中加载指定的Page
                popup.LoadPageWithParameters(page);
                // 显示弹出框
                var result = popup.ShowDialog();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var group = button?.Tag as GroupInfo;
            if (group != null)
            {
                var result = Message.Question($@"确认删除：{group.Title} 吗?");
                if (result)
                {
                    var list = listView.ItemsSource as List<GroupInfo>;
                    var reslut = GroupService.DeleteGroup(group.Id);
                    if (reslut > 0)
                    {
                        list?.Remove(group);
                        listView.Items.Refresh();  // 刷新 DataGrid 显示
                    }
                    else
                    {
                        Message.Show($"删除失败");
                    }
                }

            }
        }
        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 获取被双击的行
            if (listView.SelectedItem is GroupInfo selectedGroup)
            {
                MessageBox.Show($"Double clicked on: {selectedGroup.Title}");
            }
        }

    }


}
