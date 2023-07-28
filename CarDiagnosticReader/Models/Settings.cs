using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarDiagnosticReader.Models
{
    public class Settings : INotifyPropertyChanged
    {
        private string _heatmapColorScheme;
        public string HeatmapColorScheme
        {
            get { return _heatmapColorScheme; }
            set
            {
                if (_heatmapColorScheme != value)
                {
                    _heatmapColorScheme = value;
                    OnPropertyChanged("HeatmapColorScheme");
                }
            }
        }
        // Similarly add other settings

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
