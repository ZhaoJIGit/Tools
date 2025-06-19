using Notes.APP.Common;
using Notes.APP.Models;
using Notes.APP.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Notes.APP.Pages
{
    /// <summary>
    /// ListPage.xaml 的交互逻辑
    /// </summary>
    public partial class ListPage : BasePage
    {
        public ObservableCollection<NoteModel> notes { get; set; } = new ObservableCollection<NoteModel>();
        public ListPage()
        {
            InitializeComponent();
            notesList.ItemsSource = notes;

        }
        // 事件处理方法
        private void OnRefreshEvent(object sender, EventArgs e)
        {
            GetNotes();
        }
        private void Note_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ListWindow.RefreshEvent += OnRefreshEvent; // 订阅事件
            GetNotes();

            

        }
        private void Note_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is NoteModel note)
            {
                _NoteService.UpdateNote(note);
                var windows = Application.Current.Windows.OfType<Window>().Where(i => i.Tag != null);
                if (windows.Any(i => i.Tag.Equals(note.NoteId)))
                {
                    var win = windows.First(i => i.Tag.Equals(note.NoteId));
                    (win as MainWindow).ReloadData();
                }
                
            }
           
        }
        private void GetNotes(bool clear=false)
        {
            notes.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (NoteModel n in e.NewItems)
                        n.PropertyChanged += Note_PropertyChanged;
                }
            };
            if (clear) {
                notes.Clear();
            }
            var list = _NoteService.GetNotes();
            // 删除已经不存在的项
            var toRemove = notes.Where(n => !list.Any(x => x.NoteId == n.NoteId)).OrderByDescending(i => i.IsTopUp).ToList();
            foreach (var item in toRemove)
                notes.Remove(item);

            // 更新或新增
            foreach (var newNote in list.OrderByDescending(i => i.IsTopUp))
            {
                var exist = notes.FirstOrDefault(n => n.NoteId == newNote.NoteId);
                if (exist != null)
                {
                    // 只更新属性，不替换对象
                    exist.NoteName= newNote.NoteName;
                    exist.StatusTag = newNote.StatusTag;  // 这会自动刷新UI
                    exist.UpdateTime = newNote.UpdateTime;  // 这会自动刷新UI
                    exist.Content = newNote.Content;
                }
                else
                {
                    notes.Add(newNote);
                }
            }
            //notes= notes.OrderByDescending(i=>i.IsTopUp);

            //notes.Clear();
            //var list = _NoteService.GetNotes();
            //foreach (var item in list)
            //{
            //    notes.Add(item);
            //}
           
        }

        private void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var clickedItem = notesList.SelectedItem as NoteModel; // 获取被选中的项
            if (clickedItem != null)
            {
                OpenNote(clickedItem);
            }
        }
        private void OpenNote(NoteModel note)
        {
            var windows = Application.Current.Windows.OfType<Window>().Where(i => i.Tag != null);
            if (windows.Any(i => i.Tag.Equals(note.NoteId)))
            {
                var win = windows.First(i => i.Tag.Equals(note.NoteId));
                win.Activate();
                win.WindowState = WindowState.Normal;
                win.Show();
            }
            else
            {
                MainWindow mainWindow = new MainWindow(note);
                mainWindow.Tag = note.NoteId;
                mainWindow.Show();

            }
        }
        private void CloseNote(NoteModel note)
        {
            var windows = Application.Current.Windows.OfType<Window>().Where(i => i.Tag != null);
            if (windows.Any(i => i.Tag.Equals(note.NoteId)))
            {
                windows.First(i => i.Tag.Equals(note.NoteId)).Close();
            }
        }
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _ConfirmMessage.ShowConfirm("确认删除吗？", () =>
            {
                // 删除逻辑
                var selectedItem = notesList.SelectedItem;
                if (selectedItem != null)
                {
                    // 删除选中的项
                    var note = selectedItem as NoteModel;
                    notes.Remove(note);
                    _NoteService.DeleteNote(note!.NoteId);
                    CloseNote(note);
                }
            });
        }

        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // 编辑逻辑
            var selectedItem = notesList.SelectedItem;
            if (selectedItem != null)
            {
                var note = selectedItem as NoteModel;
                OpenNote(note);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //var selectedItem = notesList.SelectedItem;
            //if (selectedItem != null)
            //{
            //    var note = selectedItem as NoteModel;
            //    note.StatusTag = !note.StatusTag;
            //    _NoteService.UpdateNote(note);
            //}
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 阻止事件继续冒泡到 ListViewItem
            e.Handled = true;
        }

        private void FixedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = notesList.SelectedItem;
            if (selectedItem != null)
            {
                var note = selectedItem as NoteModel;
                note.Fixed = !note.Fixed;
                _NoteService.UpdateNote(note);
            }
        }

        private void IsTopUpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = notesList.SelectedItem;
            if (selectedItem != null)
            {
                var note = selectedItem as NoteModel;
                note.IsTopUp= !note.IsTopUp;
                _NoteService.UpdateNote(note);
                GetNotes(true);
            }
          
        }

        private void NoteContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (notesList.SelectedItem is NoteModel selectedNote)
            {
                btnTopUp.Header = selectedNote.IsTopUp ? "取消置顶" : "置顶";
                btnFixed.Header = selectedNote.Fixed ? "取消固定" : "固定桌面";
                iconFixed.Text = selectedNote.Fixed ? "\uE718" : "\uE840";
            }
            else
            {
                btnTopUp.Header = "置顶"; // 没选中时默认文字
                btnFixed.Header = "固定桌面";
                iconFixed.Text = "\uE840";
            }
        }
    }
}
