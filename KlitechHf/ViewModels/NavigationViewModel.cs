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
    /// <summary>
    /// A ViewModel for the navigation history part of the View.
    /// </summary>
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

        /// <summary>
        /// Appends a navigation item to the back of the navigation history.
        /// </summary>
        /// <param name="item">The item to be appended</param>
        public void AddItem(NavigationItem item)
        {
            NavigationItems.Add(item);
        }

        /// <summary>
        /// Removes the last element from the navigation history.
        /// </summary>
        public void RemoveLast()
        {
            NavigationItems.RemoveAt(NavigationItems.Count - 1);
        }

        /// <summary>
        /// Removes all the items from the navigation history that are after this item.
        /// </summary>
        /// <param name="item">The item after which the later elements will be removed</param>
        public void RemoveLaterThan(NavigationItem item)
        {
            for (int i = NavigationItems.Count - 1; i > NavigationItems.IndexOf(item); i--)
            {
                NavigationItems.RemoveAt(i);
            }
        }

        /// <summary>
        /// Clears the navigation history.
        /// </summary>
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
