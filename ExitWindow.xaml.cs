using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SCOI1
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class ExitWindow : Window
    {
        MainWindow _sender;
        public ExitWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _sender = mainWindow;
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileManager = new SaveFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            try
            {
                if (item != "")
                {
                    _sender._resultImage.Save(item);
                    _sender._resultImage.Dispose();
                    _sender._flagClose = true;
                }
            }
            catch(Exception ex)
            {
                _sender.WriteLog("Не удалось сохранить в указанный файл", System.Windows.Media.Brushes.Red);
            }
            this.Close();
        }

        private void ReturnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DontSaveClick(object sender, RoutedEventArgs e)
        {
            _sender._flagClose = true;
            _sender._resultImage.Dispose();
            this.Close();
        }
    }
}
