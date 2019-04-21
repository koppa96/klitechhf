using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using KlitechHf.Services;
using Prism.Commands;
using Prism.Windows.Mvvm;

namespace KlitechHf.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private bool _loginButtonEnabled;

        public bool LoginButtonEnabled {
            get => _loginButtonEnabled;
            set
            {
                _loginButtonEnabled = value;
                RaisePropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public MainPageViewModel()
        {
            LoginCommand = new DelegateCommand(LoginAsync);
            LoginButtonEnabled = true;
        }

        private async void LoginAsync()
        {
            LoginButtonEnabled = false;
            await AuthService.Instance.LoginAsync();
            LoginButtonEnabled = true;
        }
    }
}
