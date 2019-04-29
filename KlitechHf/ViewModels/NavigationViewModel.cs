using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using KlitechHf.Annotations;
using KlitechHf.Model;

namespace KlitechHf.ViewModels
{
    public class NavigationViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<NavigationItem> _navigationItems;

        public ObservableCollection<NavigationItem> NavigationItems {
            get => _navigationItems;
            set
            {
                _navigationItems = value; 
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NavigationViewModel()
        {
            NavigationItems = new ObservableCollection<NavigationItem>();
        }

        public void AddItem(NavigationItem item)
        {
            NavigationItems.Add(item);
        }

        public void RemoveLast()
        {
            NavigationItems.RemoveAt(NavigationItems.Count - 1);
        }

        public void RemoveLaterThan(NavigationItem item)
        {
            for (int i = NavigationItems.Count - 1; i > NavigationItems.IndexOf(item); i--)
            {
                NavigationItems.RemoveAt(i);
            }
        }

        public void Clear()
        {
            NavigationItems.Clear();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
