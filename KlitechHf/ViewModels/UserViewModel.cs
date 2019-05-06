using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using KlitechHf.Annotations;
using OneDriveServices.Authentication.Model;

namespace KlitechHf.ViewModels
{
    /// <summary>
    /// A ViewModel for the display of the current user's data.
    /// </summary>
    public class UserViewModel : INotifyPropertyChanged
    {
        private User _currentUser;

        public User CurrentUser {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
