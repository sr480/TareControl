using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using System.IO;

namespace Charts.ViewModel
{
    public class DBManager : INotifyPropertyChanged
    {
        private const string DEFAULTEXTENSION = ".sndbml";
        private const string DEFAULTFILTER = "Файл базы данных датчиков (.sndml)|*.sndbml";
        private XmlSerializer _Serializer;
        private bool _IsSaved = true;
        private SensorsDB _DataBase;
        private string _FileName;

        public string FileName
        {
            get { return _FileName; }
            set
            {
                if (_FileName == value)
                    return;
                _FileName = value;
                RaisePropertyChanged("FileName");
                RaisePropertyChanged("ShortFileName");
            }
        }
        public string ShortFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    return "База данных не открыта";
                return FileName.Substring(FileName.LastIndexOf('\\')+1);
            }
        }
        public SensorsDB DataBase
        {
            get { return _DataBase; }
            set
            {
                if (_DataBase == value)
                    return;
                _DataBase = value;
                RaisePropertyChanged("DataBase");
            }
        }
        public bool IsSaved
        {
            get { return _IsSaved; }
            set
            {
                if (_IsSaved == value)
                    return;
                _IsSaved = value;
                RaisePropertyChanged("IsSaved");
            }
        }

        public Command Open { get; private set; }
        public Command Save { get; private set; }
        public Command SaveAs { get; private set; }

        public DBManager()
        {
            Open = new Command((x) => OpenAction(), (x) => true);
            Save = new Command((x) => SaveAction(), (x) => true);
            SaveAs = new Command((x) => SaveAsAction(), (x) => true);
            _Serializer = new XmlSerializer(typeof(SensorsDB));
            DataBase = new SensorsDB();
        }

        private void OpenAction()
        {
            if (!IsSaved)
                if (SynchronisationHelper.ShowMessage("Не сохраненные данные будут утрачены. Продолжить?", "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    return;
            Microsoft.Win32.OpenFileDialog opDlg = new Microsoft.Win32.OpenFileDialog();
            opDlg.DefaultExt = DEFAULTEXTENSION;
            opDlg.Filter = DEFAULTFILTER;
            if (opDlg.ShowDialog() == true)
                OpenFile(opDlg.FileName);
        }
        private void SaveAction()
        {
            if (string.IsNullOrEmpty(FileName))
                SaveAsAction();
            else
                SaveToFile(FileName);
        }
        private void SaveAsAction()
        {
            Microsoft.Win32.SaveFileDialog svDlg = new Microsoft.Win32.SaveFileDialog();
            svDlg.DefaultExt = DEFAULTEXTENSION;
            svDlg.Filter = DEFAULTFILTER;
            if (svDlg.ShowDialog() == true)
                SaveToFile(svDlg.FileName);
        }
        private void SaveToFile(string path)
        {
            try
            {
                using (TextWriter fs = new StreamWriter(path))
                {
                    _Serializer.Serialize(fs, DataBase);
                    FileName = path;
                }
            }
            catch (Exception ex)
            {
                SynchronisationHelper.ShowMessage(ex.Message, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            IsSaved = true;
        }
        private void OpenFile(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    DataBase = (SensorsDB)_Serializer.Deserialize(fs);
                    FileName = path;
                    IsSaved = true;
                }
            }
            catch (Exception ex)
            {
                SynchronisationHelper.ShowMessage(ex.Message, "Ошибка открытия", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        public void AddValue(int sensorNum, int sensorAddress, CalibrationPoint point)
        {
            GetSensor(sensorNum, sensorAddress).Points.Add(point);
            IsSaved = false;
        }
        public SensorInfo GetSensor(int sensorNum, int sensorAddress)
        {
            SensorInfo sensor;
            if (DataBase.Sensors.Count(s => s.SensorAddress == sensorAddress) > 0)
            {
                sensor = DataBase.Sensors.Single(s => s.SensorAddress == sensorAddress);                
            }
            else
            {
                sensor = new SensorInfo { SensorAddress = sensorAddress, SensorNumber = sensorNum };
                DataBase.Sensors.Add(sensor);
            }
            IsSaved = false;
            return sensor;
        }
        public void ClearPoints(int sensorNum, int sensorAddress)
        {
            GetSensor(sensorNum, sensorAddress).Points.Clear();
            IsSaved = false;
        }
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
