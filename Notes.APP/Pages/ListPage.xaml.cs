using Notes.APP.Models;
using Notes.APP.Services;
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

namespace Notes.APP.Pages
{
    /// <summary>
    /// ListPage.xaml 的交互逻辑
    /// </summary>
    public partial class ListPage : Page
    {
        public ObservableCollection<NoteModel> notes { get; set; } = new ObservableCollection<NoteModel>();
        public ListPage()
        {
            InitializeComponent();
        }

        private void Note_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetNotes();
            notesList.ItemsSource = notes;
        }
        private void GetNotes()
        {
            var service = new NoteService();
            var list = service.GetNotes();
            foreach (var item in list)
            {
                notes.Add(item);
            }
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
                windows.First(i => i.Tag.Equals(note.NoteId)).Activate();
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
            // 删除逻辑
            var selectedItem = notesList.SelectedItem;
            if (selectedItem != null)
            {
                // 删除选中的项
                var note = selectedItem as NoteModel;
                notes.Remove(note);
                var service = new NoteService();
                service.DeleteNote(note!.NoteId);
                CloseNote(note);
            }

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
    }
}
