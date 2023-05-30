using FinancialToolSupport;
using GoogleFinancialSupport;
using GoogleFinancialSupport.DTO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ImportGoogleSpreadSheets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class FilesInfo : INotifyPropertyChanged
        {
            public string Id { get; set; }
            public string Name { get; set; }

            private bool isSelected;
            public bool IsSelected
            {
                get { return isSelected; }
                set
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public ObservableCollection<FilesInfo> Files { get; set; } = new ObservableCollection<FilesInfo>();

        private GoogleService _service;

        public MainWindow()
        {
            InitializeComponent();
            _service = new GoogleService(@"C:\Users\ggibe\Documents\financial-spreedsheet-read.json");
            DataContext = this;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            var files = _service.GetFilesName();
            Files.Clear();
            files.ForEach(f => { Files.Add(new FilesInfo { Id = f.Id, Name = f.Name, IsSelected = false}); });
        }

        private void btnGenerateData_Click(object sender, RoutedEventArgs e)
        {
            IGenerator generator = new GoogleGenerator(_service, edtPath.Text);

            var fileNames = new List<string>();
            foreach(FilesInfo item in cblFiles.Items)
            {
                if(item.IsSelected)
                {
                    fileNames.Add(item.Name);
                }
            }
            generator.Generate(fileNames);
        }
    }
}
