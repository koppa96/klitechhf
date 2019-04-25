using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Data;
using OneDriveServices.Authentication;
using OneDriveServices.Drive;
using OneDriveServices.Drive.Model.DriveItems;
using Prism.Commands;
using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;

namespace KlitechHf.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private DriveService _drive;
        private DriveFolder _currentFolder;
        private ObservableCollection<DriveItem> _children;
        private bool _isLoading;

        public DriveFolder CurrentFolder {
            get => _currentFolder;
            set
            {
                _currentFolder = value; 
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<DriveItem> Children
        {
            get => _children;
            set
            {
                _children = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }


        public ICommand LogoutCommand { get; }

        public MainPageViewModel()
        {
            Children = new ObservableCollection<DriveItem>();
            _drive = DriveService.Instance;

            LogoutCommand = new DelegateCommand(Logout);
            IsLoading = false;
        }

        public override async void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            await LoginAsync();
        }

        private async Task LoginAsync()
        {
            IsLoading = true;
            await AuthService.Instance.LoginAsync();
            CurrentFolder = await _drive.GetRootAsync();
            Children = new ObservableCollection<DriveItem>(await CurrentFolder.GetChildrenAsync());
            IsLoading = false;
        }

        private async void Logout()
        {
            CurrentFolder = null;
            Children.Clear();
            AuthService.Instance.Logout();
            await LoginAsync();
        }
    }
}
