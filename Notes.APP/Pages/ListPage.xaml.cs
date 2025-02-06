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
        private void OpenNote(NoteModel note) {
            var windows = Application.Current.Windows.OfType<Window>();
            if (windows.Any(i => i.Tag == note.NoteId))
            {
                windows.First(i => i.Tag == note.NoteId).Activate();
            }
            else
            {
                MainWindow mainWindow = new MainWindow(note);
                mainWindow.Tag = note.NoteId;
                mainWindow.Show();
            }

        }
    }
}
