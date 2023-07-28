using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace CarDiagnosticReader.Controls
{
    /// <summary>
    /// Interaction logic for LoggerControl.xaml
    /// </summary>
    public partial class LoggerControl : UserControl, INotifyPropertyChanged
    {
        private ObservableCollection<string> logMessages;

        public ObservableCollection<string> LogMessages
        {
            get { return logMessages; }
            set
            {
                logMessages = value;
                OnPropertyChanged();
            }
        }

        public LoggerControl()
        {
            InitializeComponent();
            LogMessages = new ObservableCollection<string>();
            DataContext = this;
        }

        public void AddLogMessage(string message)
        {
            LogMessages.Add(message);
            ScrollToLastMessage();
        }

        private void ScrollToLastMessage()
        {
            // Scroll to the last item in the ListBox
            if (logListBox.Items.Count > 0)
            {
                var lastItem = logListBox.Items[logListBox.Items.Count - 1];
                logListBox.ScrollIntoView(lastItem);
            }
        }

        // Implement INotifyPropertyChanged interface members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
