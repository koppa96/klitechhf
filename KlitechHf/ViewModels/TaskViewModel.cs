using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using KlitechHf.Annotations;

namespace KlitechHf.ViewModels
{
    /// <summary>
    /// A ViewModel for displaying the currently running background task and the loading animation.
    /// </summary>
    public class TaskViewModel : INotifyPropertyChanged
    {
        private string _backgroundTaskName;
        private bool _backgroundTaskRunning;
        private bool _isBusy;

        public string BackgroundTaskName {
            get => _backgroundTaskName;
            set
            {
                _backgroundTaskName = value;
                OnPropertyChanged();
            }
        }

        public bool BackgroundTaskRunning {
            get => _backgroundTaskRunning;
            set
            {
                _backgroundTaskRunning = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public TaskViewModel()
        {
            BackgroundTaskRunning = false;
            IsBusy = false;
        }

        /// <summary>
        /// Starts a background task animation with the given name. If there is a background task it will be overwritten.
        /// </summary>
        /// <param name="name">The name of the background task</param>
        public void StartBackgroundTask(string name)
        {
            BackgroundTaskRunning = true;
            BackgroundTaskName = name;
        }

        /// <summary>
        /// Stops the currently shown background task animation.
        /// </summary>
        public void StopBackgroundTask()
        {
            BackgroundTaskRunning = false;
            BackgroundTaskName = null;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
