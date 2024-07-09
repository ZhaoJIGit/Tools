using CommunityToolkit.Maui.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MauiApp3
{
    public class MediaViewerPageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private int _currentIndex = 0;

        private List<MediaViewerItemModel> _mediaViewerItems = new List<MediaViewerItemModel>();


        private MediaViewerModel _mediaViewer;

        public MediaViewerModel MediaViewer
        {
            get => _mediaViewer;
            set
            {
                if (_mediaViewer != value)
                {
                    _mediaViewer = value;
                    OnPropertyChanged(); // reports this property
                }
            }
        }

        private bool _nextBtnStatus = false;

        public bool NextBtnStatus
        {
            get => _nextBtnStatus;
            set
            {
                if (_nextBtnStatus != value)
                {
                    _nextBtnStatus = value;
                    OnPropertyChanged(); // reports this property
                }
            }
        }

        private bool _preBtnStatus = false;

        public bool PreBtnStatus
        {
            get => _preBtnStatus;
            set
            {
                if (_preBtnStatus != value)
                {
                    _preBtnStatus = value;
                    OnPropertyChanged(); // reports this property
                }
            }
        }

        public ICommand NextCommand { get; private set; }

        public ICommand PreCommand { get; private set; }



        public MediaViewerPageViewModel()
        {
            _mediaViewerItems.Add(new MediaViewerItemModel() { Url = "C:\\Users\\DELL\\Desktop\\temp\\1.png", IsImage = true, Show = true });
            _mediaViewerItems.Add(new MediaViewerItemModel() { Url = "C:\\Users\\DELL\\Desktop\\temp\\2.png", IsImage = true });
            _mediaViewerItems.Add(new MediaViewerItemModel() { Url = "C:\\Users\\DELL\\AppData\\Local\\Packages\\2599D704-EBA7-4C21-A8F0-CB1E3C79D945_hqhyg1cvr7f3y\\LocalState\\Storage\\Files\\2024\\05\\30\\5224ac80-55b5-4dbd-ab28-ffadd5303058\\测试视频2.mp4", IsImage = false });
            this.MediaViewer = new MediaViewerModel()
            {
                ImageSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? ImageSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url) : null,
                IsImage = _mediaViewerItems.ToArray()[_currentIndex].IsImage,
                IsVideo = !_mediaViewerItems.ToArray()[_currentIndex].IsImage,
                MediaSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? null : MediaSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url)
            };
            this.PreBtnStatus = false;
            this.NextBtnStatus = true;
            NextCommand = new Command(() =>
            {
                _currentIndex++;
                this.MediaViewer = new MediaViewerModel()
                {
                    ImageSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? ImageSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url) : null,
                    IsImage = _mediaViewerItems.ToArray()[_currentIndex].IsImage,
                    IsVideo = !_mediaViewerItems.ToArray()[_currentIndex].IsImage,
                    MediaSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? null : MediaSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url)
                };
                if (_currentIndex >= _mediaViewerItems.Count-1)
                {
                    this.PreBtnStatus = true;
                    this.NextBtnStatus = false;
                }
                else
                {
                    this.PreBtnStatus = true;
                    this.NextBtnStatus = true;
                }
            });

            PreCommand = new Command(() =>
            {
                _currentIndex--;
                this.MediaViewer = new MediaViewerModel()
                {
                    ImageSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? ImageSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url) : null,
                    IsImage = _mediaViewerItems.ToArray()[_currentIndex].IsImage,
                    IsVideo = !_mediaViewerItems.ToArray()[_currentIndex].IsImage,
                    MediaSource = _mediaViewerItems.ToArray()[_currentIndex].IsImage ? null : MediaSource.FromFile(_mediaViewerItems.ToArray()[_currentIndex].Url)
                };
                if (_currentIndex <= 0)
                {
                    this.PreBtnStatus = false;
                    this.NextBtnStatus = true;
                }
                else
                {
                    this.PreBtnStatus = true;
                    this.NextBtnStatus = true;
                }
            });
        }

        public void OnPropertyChanged([CallerMemberName] string name = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    }
}
