using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PublishTool.Models
{
    public class SiteModel : INotifyPropertyChanged
    {
        public string Id { get; set; }=Guid.NewGuid().ToString();

        public string Name { get; set; }

        public string FilePath { get; set; }

        public string SitePath { get; set; }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(name));
        }
    }

    public enum LogType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
