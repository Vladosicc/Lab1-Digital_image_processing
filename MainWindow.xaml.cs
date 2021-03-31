using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;

namespace SCOI1
{
    //https://s.mediasole.ru/images/369/369519/1-10-730x730.jpg
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static List<Guid_Number> NumContainers = new List<Guid_Number>();
        static List<StatusContainer> Containers = new List<StatusContainer>();
        public Bitmap _resultImage = new Bitmap(width: 1, height: 1);
        static List<string> _urls;
        static byte _numUrls = 0;
        public bool _flagClose = false;
        BackgroundWorker backgroundWorker;
        public MainWindow()
        {
            InitializeComponent();

            this.Closing += MainWindow_Closing;
            backgroundWorker = (BackgroundWorker)this.FindResource("backgroundWorker");
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_resultImage.Width == 1)
                return;
            e.Cancel = true;
            ExitWindow exitWindow = new ExitWindow(this);
            exitWindow.Owner = this;
            exitWindow.ShowDialog();
            exitWindow.Activate();
            if (_flagClose)
            {
                e.Cancel = false;
            }
        }

        private void FileIsDropped(object sender, DragEventArgs e)
        {
            var paths = (string[])e.Data.GetData("FileDrop");
            try         
            {
                foreach (var item in paths)
                {
                    CreateContainer(item);
                }
            }
            catch(Exception ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }
        }

        private void ButToTop_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            Guid id = Guid.Parse(btn.Name.Substring("pos".Length).Replace('_', '-'));
            int NumberOfCont = NumContainers.GetNumber(id);
            if (NumberOfCont == 0)
                return;
            if (Containers.Count == 1)
                return;
            var Childrens = StackMain.Children;
            UIElement[] uIElements = new UIElement[Childrens.Count];
            Childrens.CopyTo(uIElements, 0);
            var thisGroup = StackMain.Children[NumberOfCont];
            var topGroup = StackMain.Children[NumberOfCont - 1];
            uIElements[NumberOfCont] = topGroup;
            uIElements[NumberOfCont - 1] = thisGroup;
            StackMain.Children.Clear();
            for (int i = 0; i < NumContainers.Count; i++)
            {
                StackMain.Children.Add(uIElements[i]);
            }
            NumContainers.UpPos(NumberOfCont);
            Containers.UpPos(NumberOfCont);
        }

        private void ButToBot_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            Guid id = Guid.Parse(btn.Name.Substring("pos".Length).Replace('_', '-'));
            int NumberOfCont = NumContainers.GetNumber(id);
            if (NumberOfCont == NumContainers.Count - 1)
                return;
            if (Containers.Count <= 1)
                return;
            var Childrens = StackMain.Children;
            UIElement[] uIElements = new UIElement[Childrens.Count];
            Childrens.CopyTo(uIElements, 0);
            var thisGroup = StackMain.Children[NumberOfCont];
            var topGroup = StackMain.Children[NumberOfCont + 1];
            uIElements[NumberOfCont] = topGroup;
            uIElements[NumberOfCont + 1] = thisGroup;
            StackMain.Children.Clear();
            for (int i = 0; i < NumContainers.Count; i++)
            {
                StackMain.Children.Add(uIElements[i]);
            }
            NumContainers.DownPos(NumberOfCont);
            Containers.DownPos(NumberOfCont);
        }

        private void ComboBoxRGB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmbx = (ComboBox)sender;
            Guid id = Guid.Parse(cmbx.Name.Substring("RGB".Length).Replace('_', '-'));
            Containers.GetContainer(id).Channel = (Channel)cmbx.SelectedIndex;
        }

        private void Slider_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double delt = (e.Delta / 120);
            Slider slid = (Slider)e.Source;
            if (slid.Value + delt > 100)
            {
                slid.Value = 100;
                return;
            }
            if (slid.Value + delt < 0)
            {
                slid.Value = 0;
                return;
            }
            slid.Value += delt;
        } //не используется

        private void ComboBoxOper_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cmbx = (ComboBox)sender;
            Guid id = Guid.Parse(cmbx.Name.Substring("Operation".Length).Replace('_', '-'));
            Containers.GetContainer(id).Operation = (Operations)cmbx.SelectedIndex;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //Получаем слайдер
            var slid = (Slider)sender;
            //Получаем id слайдера
            Guid id = Guid.Parse(slid.Name.Substring("Slider".Length).Replace('_', '-'));
            int NumOfContainer = NumContainers.GetNumber(id);
            //Меняем значение в текстбоксе
            var textSlid = (TextBlock)((StackPanel)((GroupBox)StackMain.Children[NumOfContainer]).Content).Children[6];
            textSlid.Text = "Прозрачность - " + (int)e.NewValue + "%";
            //Меняем значение в контейнере
            Containers.GetContainer(id).Transparency = (int)e.NewValue;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileManager = new OpenFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            if (item != "")
            {
                #region NewContainer
                Guid guid = Guid.NewGuid();

                //Элементы внутри стак панели
                Image image = new Image()
                {
                    Source = new BitmapImage(new Uri(item)),
                    Height = 155,
                    Name = "image" + guid.ToString().Replace('-', '_')
                };

                Button button = new Button()
                {
                    Content = "Delete",
                    Margin = new Thickness(42, 5, 42, 0),
                    Name = "delete" + guid.ToString().Replace('-', '_')
                };
                button.Click += Delete_Click;

                TextBlock textBlockOperation = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Операция"
                };

                List<TextBlock> ListOperations = new List<TextBlock>();
                ListOperations.Add(new TextBlock()
                {
                    Text = "Нет",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Сумма",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Умножение",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Среднее-арифметическое",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Минимум",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Максимум",
                    Height = 21,
                    Width = 130
                });

                ComboBox comboBoxOperation = new ComboBox()
                {
                    Margin = new Thickness(42, 5, 42, 0),
                    ItemsSource = ListOperations.AsEnumerable(),
                    Name = "Operation" + guid.ToString().Replace('-', '_'),
                    SelectedIndex = 0
                };
                comboBoxOperation.SelectionChanged += ComboBoxOper_SelectionChanged;

                TextBlock textBlockKanal = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Канал"
                };

                List<TextBlock> ListRGB = new List<TextBlock>();
                ListRGB.Add(new TextBlock()
                {
                    Text = "RGB",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "R",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "G",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "B",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "RG",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "GB",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "RB",
                    Height = 21,
                    Width = 130
                });

                ComboBox comboBoxRGB = new ComboBox()
                {
                    Margin = new Thickness(42, 5, 42, 0),
                    ItemsSource = ListRGB.AsEnumerable(),
                    Name = "RGB" + guid.ToString().Replace('-', '_'),
                    SelectedIndex = 0
                };
                comboBoxRGB.SelectionChanged += ComboBoxRGB_SelectionChanged;

                TextBlock textBlockSlider = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Прозрачность - 0%",
                    Name = "SliderValue" + guid.ToString().Replace('-', '_') //
                };

                Slider slider = new Slider()
                {
                    Height = 21,
                    Margin = new Thickness(42, 5, 42, 0),
                    Maximum = 100,
                    Minimum = 0,
                    AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
                    Name = "Slider" + guid.ToString().Replace('-', '_')
                };
                slider.ValueChanged += Slider_ValueChanged;

                Canvas canvas = new Canvas()
                {
                    Height = 23,
                    Margin = new Thickness(87, 10, 86, 5)
                };
                Button butToBot = new Button()
                {
                    Width = 29,
                    FontSize = 16,
                    Content = "↓",
                    Name = "pos" + guid.ToString().Replace('-', '_')
                };
                butToBot.Click += ButToBot_Click;
                Canvas.SetLeft(butToBot, 96);
                Button butToTop = new Button()
                {
                    Width = 29,
                    FontSize = 16,
                    Content = "↑",
                    Name = "pos" + guid.ToString().Replace('-', '_')
                };
                butToTop.Click += ButToTop_Click;
                Canvas.SetLeft(butToTop, 36);
                canvas.Children.Add(butToTop);
                canvas.Children.Add(butToBot);
                //Кидаем внутрь (Последовательность: Image, Button, TextOper, ComboOper, TextKanal, comboRGB, TextSlider, Slider)
                StackPanel stackPanel = new StackPanel() { };
                stackPanel.Children.Add(image);
                stackPanel.Children.Add(button);
                stackPanel.Children.Add(textBlockOperation);
                stackPanel.Children.Add(comboBoxOperation);
                stackPanel.Children.Add(textBlockKanal);
                stackPanel.Children.Add(comboBoxRGB);
                stackPanel.Children.Add(textBlockSlider);
                stackPanel.Children.Add(slider);
                stackPanel.Children.Add(canvas);
                //Кидаем стак в групбокс
                GroupBox groupBox = new GroupBox() { Header = item };
                groupBox.Content = stackPanel;
                //Кидаем групбокс на вывод в мэйнстак
                StackMain.Children.Add(groupBox);
                //Колво контейнеров ++
                NumContainers.Add(new Guid_Number() { Guid = guid, Num = NumContainers.Count });
                //
                Containers.Add(new StatusContainer(guid) { Channel = 0, Operation = 0, Transparency = 0, Image = new Bitmap(item) });
                #endregion
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var but = (Button)sender;
            Guid id = Guid.Parse(but.Name.Substring("delete".Length).Replace('_', '-'));
            Containers.DeleteGuid(id);
            NumContainers.DeleteGuid(id);
            StackMain.Children.Remove((GroupBox)((StackPanel)((Button)sender).Parent).Parent);
            ((StackPanel)((Button)sender).Parent).Children.Clear();
        }

        private void ResultClick(object sender, RoutedEventArgs e)
        {
            var start = DateTime.UtcNow;
            if (Containers.Count == 1) //Одна фотка
            {
                _resultImage.Dispose();
                _resultImage = (Bitmap)Containers[0].Image.Clone();
                ResultImage.Source = _resultImage.ToBitmapSource();
            }
            if (Containers.Count < 1)
                return; //Нет фоток
            _resultImage.Dispose();
            Containers.ResizeAllBitmap(); //Задаем один размер всем битмапам
            //Клонируем ласт изображение
            _resultImage = Containers.Last().Image.Clone(new Rectangle(0, 0, Containers.Last().Image.Width, Containers.Last().Image.Height), PixelFormat.Format24bppRgb);
            for (int i = Containers.Count - 2; i >= 0; i--)
            {
                switch (Containers[i].Operation)
                {
                    case Operations.Non:
                        break;
                    case Operations.Sum:
                        GraphMethods.Summary(Source: _resultImage, container: Containers[i]);
                        break;
                    case Operations.Mult:
                        GraphMethods.Multiply(Source: _resultImage, container: Containers[i]);
                        break;
                    case Operations.Middle:
                        GraphMethods.Middle(Source: _resultImage, container: Containers[i]);
                        break;
                    case Operations.Max:
                        GraphMethods.Max(Source: _resultImage, container: Containers[i]);
                        break;
                    case Operations.Min:
                        GraphMethods.Min(Source: _resultImage, container: Containers[i]);
                        break;
                    default:
                        break;
                }
            }
            var Time = DateTime.UtcNow - start;
            WriteLog("Фотография обработана за " + Time.TotalSeconds + "c", Brushes.DarkGreen);
            ResultImage.Source = _resultImage.ToBitmapSource();
        }

        private void AddContainerClick(object sender, RoutedEventArgs e)
        {
            if (_resultImage.Width == 1)
            {
                WriteLog("Error: Object not found", Brushes.Red);
                return;
            }
            #region NewContainer
            Guid guid = Guid.NewGuid();

            //Элементы внутри стак панели
            Image image = new Image()
            {
                Source = _resultImage.ToBitmapSource(),
                Height = 155,
                Name = "image" + guid.ToString().Replace('-', '_')
            };

            Button button = new Button()
            {
                Content = "Delete",
                Margin = new Thickness(42, 5, 42, 0),
                Name = "delete" + guid.ToString().Replace('-', '_')
            };
            button.Click += Delete_Click;

            TextBlock textBlockOperation = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Операция"
            };

            List<TextBlock> ListOperations = new List<TextBlock>();
            ListOperations.Add(new TextBlock()
            {
                Text = "Нет",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Сумма",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Умножение",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Среднее-арифметическое",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Минимум",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Максимум",
                Height = 21,
                Width = 130
            });

            ComboBox comboBoxOperation = new ComboBox()
            {
                Margin = new Thickness(42, 5, 42, 0),
                ItemsSource = ListOperations.AsEnumerable(),
                Name = "Operation" + guid.ToString().Replace('-', '_'),
                SelectedIndex = 0
            };
            comboBoxOperation.SelectionChanged += ComboBoxOper_SelectionChanged;

            TextBlock textBlockKanal = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Канал"
            };

            List<TextBlock> ListRGB = new List<TextBlock>();
            ListRGB.Add(new TextBlock()
            {
                Text = "RGB",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "R",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "G",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "B",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "RG",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "GB",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "RB",
                Height = 21,
                Width = 130
            });

            ComboBox comboBoxRGB = new ComboBox()
            {
                Margin = new Thickness(42, 5, 42, 0),
                ItemsSource = ListRGB.AsEnumerable(),
                Name = "RGB" + guid.ToString().Replace('-', '_'),
                SelectedIndex = 0
            };
            comboBoxRGB.SelectionChanged += ComboBoxRGB_SelectionChanged;

            TextBlock textBlockSlider = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Прозрачность - 0%",
                Name = "SliderValue" + guid.ToString().Replace('-', '_') //
            };

            Slider slider = new Slider()
            {
                Height = 21,
                Margin = new Thickness(42, 5, 42, 0),
                Maximum = 100,
                Minimum = 0,
                AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
                Name = "Slider" + guid.ToString().Replace('-', '_')
            };
            slider.ValueChanged += Slider_ValueChanged;

            Canvas canvas = new Canvas()
            {
                Height = 23,
                Margin = new Thickness(87, 10, 86, 5)
            };
            Button butToBot = new Button()
            {
                Width = 29,
                FontSize = 16,
                Content = "↓",
                Name = "pos" + guid.ToString().Replace('-', '_')
            };
            butToBot.Click += ButToBot_Click;
            Canvas.SetLeft(butToBot, 96);
            Button butToTop = new Button()
            {
                Width = 29,
                FontSize = 16,
                Content = "↑",
                Name = "pos" + guid.ToString().Replace('-', '_')
            };
            butToTop.Click += ButToTop_Click;
            Canvas.SetLeft(butToTop, 36);
            canvas.Children.Add(butToTop);
            canvas.Children.Add(butToBot);
            //Кидаем внутрь (Последовательность: Image, Button, TextOper, ComboOper, TextKanal, comboRGB, TextSlider, Slider)
            StackPanel stackPanel = new StackPanel() { };
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(button);
            stackPanel.Children.Add(textBlockOperation);
            stackPanel.Children.Add(comboBoxOperation);
            stackPanel.Children.Add(textBlockKanal);
            stackPanel.Children.Add(comboBoxRGB);
            stackPanel.Children.Add(textBlockSlider);
            stackPanel.Children.Add(slider);
            stackPanel.Children.Add(canvas);
            //Кидаем стак в групбокс
            GroupBox groupBox = new GroupBox() { Header = guid.ToString() };
            groupBox.Content = stackPanel;
            //Кидаем групбокс на вывод в мэйнстак
            StackMain.Children.Add(groupBox);
            //Колво контейнеров ++
            NumContainers.Add(new Guid_Number() { Guid = guid, Num = NumContainers.Count });
            //
            Containers.Add(new StatusContainer(guid) { Channel = 0, Operation = 0, Transparency = 0, Image = (Bitmap)_resultImage.Clone() });
            #endregion
        }

        private void SaveAs(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fileManager = new SaveFileDialog();
            fileManager.Filter = "Файлы jpg|*.jpg|Файлы jpeg|*.jpeg|Файлы png| *.png";
            fileManager.ShowDialog();
            var item = fileManager.FileName;
            try
            {
                if (item != "")
                {
                    _resultImage.Save(item);
                }
                WriteLog("Файл " + item + " успешно сохранен", Brushes.DarkBlue);
            }
            catch
            {
                WriteLog("Не удалось сохранить в указанный файл", Brushes.Red);
            }
        }

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(_resultImage.ToBitmapSource());
        }

        private void CutClick(object sender, RoutedEventArgs e)
        {

        }

        private void PasteClick(object sender, RoutedEventArgs e)
        {
            if (Clipboard.ContainsImage())
            {
                CreateContainer(Clipboard.GetImage().ToBitmap());
            }
            else
            {
                //Log?
            }
        }

        private async void InstParse(object sender, RoutedEventArgs e)
        {
            WriteLog("Загрузка фоток из инсты " + UserName.Text);
            try
            {
                _urls = await ParseInst.ParseMethod2(UserName.Text);
                if (_urls == null)
                {
                    WriteLog("Ошибка загрузки (скорей всего не найден user)", Brushes.Red);
                }
                else
                {
                    WriteLog(_urls.Count + " фоток загружены (добавляются кнопкой Add по очереди)", Brushes.DarkGreen);
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }

            _numUrls = 0;
        }

        private void AddNewPhotoFromInst(object sender, RoutedEventArgs e)
        {
            if (_urls == null)
            {
                WriteLog("В буфер не загружены фотки", Brushes.Red);
                return;
            }
            try
            {
                var request = System.Net.WebRequest.Create(_urls[_numUrls]);
                var response = request.GetResponse();
                Bitmap loadedBitmap = null;
                using (var responseStream = response.GetResponseStream())
                {
                    loadedBitmap = new Bitmap(responseStream);
                }

                #region NewContainer
                Guid guid = Guid.NewGuid();

                //Элементы внутри стак панели
                Image image = new Image()
                {
                    Source = loadedBitmap.ToBitmapSource(),
                    Height = 155,
                    Name = "image" + guid.ToString().Replace('-', '_')
                };

                Button button = new Button()
                {
                    Content = "Delete",
                    Margin = new Thickness(42, 5, 42, 0),
                    Name = "delete" + guid.ToString().Replace('-', '_')
                };
                button.Click += Delete_Click;

                TextBlock textBlockOperation = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Операция"
                };

                List<TextBlock> ListOperations = new List<TextBlock>();
                ListOperations.Add(new TextBlock()
                {
                    Text = "Нет",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Сумма",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Умножение",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Среднее-арифметическое",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Минимум",
                    Height = 21,
                    Width = 130
                });
                ListOperations.Add(new TextBlock()
                {
                    Text = "Максимум",
                    Height = 21,
                    Width = 130
                });

                ComboBox comboBoxOperation = new ComboBox()
                {
                    Margin = new Thickness(42, 5, 42, 0),
                    ItemsSource = ListOperations.AsEnumerable(),
                    Name = "Operation" + guid.ToString().Replace('-', '_'),
                    SelectedIndex = 0
                };
                comboBoxOperation.SelectionChanged += ComboBoxOper_SelectionChanged;

                TextBlock textBlockKanal = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Канал"
                };

                List<TextBlock> ListRGB = new List<TextBlock>();
                ListRGB.Add(new TextBlock()
                {
                    Text = "RGB",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "R",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "G",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "B",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "RG",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "GB",
                    Height = 21,
                    Width = 130
                });
                ListRGB.Add(new TextBlock()
                {
                    Text = "RB",
                    Height = 21,
                    Width = 130
                });

                ComboBox comboBoxRGB = new ComboBox()
                {
                    Margin = new Thickness(42, 5, 42, 0),
                    ItemsSource = ListRGB.AsEnumerable(),
                    Name = "RGB" + guid.ToString().Replace('-', '_'),
                    SelectedIndex = 0
                };
                comboBoxRGB.SelectionChanged += ComboBoxRGB_SelectionChanged;

                TextBlock textBlockSlider = new TextBlock()
                {
                    Height = 21,
                    Width = 130,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(42, 5, 42, 0),
                    Text = "Прозрачность - 0%",
                    Name = "SliderValue" + guid.ToString().Replace('-', '_') //
                };

                Slider slider = new Slider()
                {
                    Height = 21,
                    Margin = new Thickness(42, 5, 42, 0),
                    Maximum = 100,
                    Minimum = 0,
                    AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
                    Name = "Slider" + guid.ToString().Replace('-', '_')
                };
                slider.ValueChanged += Slider_ValueChanged;

                Canvas canvas = new Canvas()
                {
                    Height = 23,
                    Margin = new Thickness(87, 10, 86, 5)
                };
                Button butToBot = new Button()
                {
                    Width = 29,
                    FontSize = 16,
                    Content = "↓",
                    Name = "pos" + guid.ToString().Replace('-', '_')
                };
                butToBot.Click += ButToBot_Click;
                Canvas.SetLeft(butToBot, 96);
                Button butToTop = new Button()
                {
                    Width = 29,
                    FontSize = 16,
                    Content = "↑",
                    Name = "pos" + guid.ToString().Replace('-', '_')
                };
                butToTop.Click += ButToTop_Click;
                Canvas.SetLeft(butToTop, 36);
                canvas.Children.Add(butToTop);
                canvas.Children.Add(butToBot);
                //Кидаем внутрь (Последовательность: Image, Button, TextOper, ComboOper, TextKanal, comboRGB, TextSlider, Slider)
                StackPanel stackPanel = new StackPanel() { };
                stackPanel.Children.Add(image);
                stackPanel.Children.Add(button);
                stackPanel.Children.Add(textBlockOperation);
                stackPanel.Children.Add(comboBoxOperation);
                stackPanel.Children.Add(textBlockKanal);
                stackPanel.Children.Add(comboBoxRGB);
                stackPanel.Children.Add(textBlockSlider);
                stackPanel.Children.Add(slider);
                stackPanel.Children.Add(canvas);
                //Кидаем стак в групбокс
                GroupBox groupBox = new GroupBox() { Header = guid.ToString() };
                groupBox.Content = stackPanel;
                //Кидаем групбокс на вывод в мэйнстак
                StackMain.Children.Add(groupBox);
                //Колво контейнеров ++
                NumContainers.Add(new Guid_Number() { Guid = guid, Num = NumContainers.Count });
                //
                Containers.Add(new StatusContainer(guid) { Channel = 0, Operation = 0, Transparency = 0, Image = loadedBitmap });
                #endregion
                WriteLog("Загружена " + _numUrls + " фотка");
                _numUrls++;
            }
            catch (System.Net.WebException ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }
            catch (Exception ex)
            {

            }
        }

        private void ExitClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void WriteLog(string message, System.Windows.Media.SolidColorBrush color = null)
        {
            if (color == null)
                color = System.Windows.Media.Brushes.Black;
            var text = new TextBlock() { Text = message, Foreground = color };
            Log.Items.Add(text);
            Log.ScrollIntoView(text);
            Log.SelectedItem = text;
        }

        private void LenaClick(object sender, RoutedEventArgs e)
        {
            try
            {
                CreateContainer("Lena.jpg");
            }
            catch (Exception ex)
            {
                WriteLog(ex.Message, Brushes.Red);
            }
        }

        private void CreateContainer(string path)
        {
            var BitmapImage = new BitmapImage(new Uri(path,UriKind.RelativeOrAbsolute));
            var Bitmap = new Bitmap(path);
            string head = "~";
            head += path.Substring(path.LastIndexOf('\\') + 1);
            if(head.Length > 50)
            {
                head = "~" + head.Substring(head.Length - 50);
            }
            CreateContainer(Bitmap, BitmapImage, head);
        }

        private void CreateContainer(BitmapSource bitmapSource)
        {
            var Bitmap = bitmapSource.ToBitmap();
            CreateContainer(Bitmap, bitmapSource);
        }

        private void CreateContainer(Bitmap bitmap)
        {
            var BitmapSource = bitmap.ToBitmapSource();
            CreateContainer(bitmap, BitmapSource);
        }

        public void CreateContainer(Bitmap bitmap, BitmapSource bitmapSource, string header = null)
        {
            #region NewContainer
            Guid guid = Guid.NewGuid();

            if(header == null)
            {
                header = guid.ToString().Replace('-', '_');
            }

            //Элементы внутри стак панели
            Image image = new Image()
            {
                Source = bitmapSource,
                Height = 155,
                Name = "image" + guid.ToString().Replace('-', '_')
            };

            Button button = new Button()
            {
                Content = "Delete",
                Margin = new Thickness(42, 5, 42, 0),
                Name = "delete" + guid.ToString().Replace('-', '_')
            };
            button.Click += Delete_Click;

            TextBlock textBlockOperation = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Операция"
            };

            List<TextBlock> ListOperations = new List<TextBlock>();
            ListOperations.Add(new TextBlock()
            {
                Text = "Нет",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Сумма",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Умножение",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Среднее-арифметическое",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Минимум",
                Height = 21,
                Width = 130
            });
            ListOperations.Add(new TextBlock()
            {
                Text = "Максимум",
                Height = 21,
                Width = 130
            });

            ComboBox comboBoxOperation = new ComboBox()
            {
                Margin = new Thickness(42, 5, 42, 0),
                ItemsSource = ListOperations.AsEnumerable(),
                Name = "Operation" + guid.ToString().Replace('-', '_'),
                SelectedIndex = 0
            };
            comboBoxOperation.SelectionChanged += ComboBoxOper_SelectionChanged;

            TextBlock textBlockKanal = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Канал"
            };

            List<TextBlock> ListRGB = new List<TextBlock>();
            ListRGB.Add(new TextBlock()
            {
                Text = "RGB",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "R",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "G",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "B",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "RG",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "GB",
                Height = 21,
                Width = 130
            });
            ListRGB.Add(new TextBlock()
            {
                Text = "RB",
                Height = 21,
                Width = 130
            });

            ComboBox comboBoxRGB = new ComboBox()
            {
                Margin = new Thickness(42, 5, 42, 0),
                ItemsSource = ListRGB.AsEnumerable(),
                Name = "RGB" + guid.ToString().Replace('-', '_'),
                SelectedIndex = 0
            };
            comboBoxRGB.SelectionChanged += ComboBoxRGB_SelectionChanged;

            TextBlock textBlockSlider = new TextBlock()
            {
                Height = 21,
                Width = 130,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(42, 5, 42, 0),
                Text = "Прозрачность - 0%",
                Name = "SliderValue" + guid.ToString().Replace('-', '_') //
            };

            Slider slider = new Slider()
            {
                Height = 21,
                Margin = new Thickness(42, 5, 42, 0),
                Maximum = 100,
                Minimum = 0,
                AutoToolTipPlacement = AutoToolTipPlacement.TopLeft,
                Name = "Slider" + guid.ToString().Replace('-', '_')
            };
            slider.ValueChanged += Slider_ValueChanged;

            Canvas canvas = new Canvas()
            {
                Height = 23,
                Margin = new Thickness(87, 10, 86, 5)
            };
            Button butToBot = new Button()
            {
                Width = 29,
                FontSize = 16,
                Content = "↓",
                Name = "pos" + guid.ToString().Replace('-', '_')
            };
            butToBot.Click += ButToBot_Click;
            Canvas.SetLeft(butToBot, 96);
            Button butToTop = new Button()
            {
                Width = 29,
                FontSize = 16,
                Content = "↑",
                Name = "pos" + guid.ToString().Replace('-', '_')

            };
            butToTop.Click += ButToTop_Click;
            Canvas.SetLeft(butToTop, 36);
            canvas.Children.Add(butToTop);
            canvas.Children.Add(butToBot);
            //Кидаем внутрь (Последовательность: Image, Button, TextOper, ComboOper, TextKanal, comboRGB, TextSlider, Slider)
            StackPanel stackPanel = new StackPanel() { };
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(button);
            stackPanel.Children.Add(textBlockOperation);
            stackPanel.Children.Add(comboBoxOperation);
            stackPanel.Children.Add(textBlockKanal);
            stackPanel.Children.Add(comboBoxRGB);
            stackPanel.Children.Add(textBlockSlider);
            stackPanel.Children.Add(slider);
            stackPanel.Children.Add(canvas);
            //Кидаем стак в групбокс
            GroupBox groupBox = new GroupBox() { Header = header };
            groupBox.Content = stackPanel;
            //Кидаем групбокс на вывод в мэйнстак
            StackMain.Children.Add(groupBox);
            //Колво контейнеров ++
            NumContainers.Add(new Guid_Number() { Guid = guid, Num = NumContainers.Count });
            //Добавляем новый объект для обработки
            Containers.Add(new StatusContainer(guid) { Channel = 0, Operation = 0, Transparency = 0, Image = bitmap });
            #endregion
        }
    }

    //План

    public class StatusContainer
    {
        public Guid Id { get; private set; }
        public Bitmap Image { get; set; }
        public Operations Operation { get; set; }
        public Channel Channel { get; set; }
        public int Transparency { get; set; }

        public StatusContainer(Guid id) { Id = id; }
    }

    public class Guid_Number
    {
        public Guid Guid { get; set; }
        public int Num { get; set; }
    }

    public static class StaticListOperations
    {
        public static StatusContainer GetContainer(this List<StatusContainer> containers, Guid req)
        {
            return containers.Where(item => item.Id == req).FirstOrDefault();
        }

        public static int GetNumber(this List<Guid_Number> guid_Numbers, Guid guidRequest)
        {
            return guid_Numbers.Where(item => item.Guid == guidRequest).FirstOrDefault().Num;
        }

        public static Guid GetGuid(this List<Guid_Number> guid_Numbers, int req)
        {
            return guid_Numbers.Where(item => item.Num == req).FirstOrDefault().Guid;
        }

        public static void DeleteGuid(this List<Guid_Number> guid_Numbers, Guid guidRequest)
        {
            guid_Numbers.Remove(guid_Numbers.Where(item => item.Guid == guidRequest).FirstOrDefault());
            for (int i = 0; i < guid_Numbers.Count; i++)
            {
                guid_Numbers[i].Num = i;
            }
        }

        public static void DeleteNum(this List<Guid_Number> guid_Numbers, int req)
        {
            guid_Numbers.Remove(guid_Numbers.Where(item => item.Num == req).FirstOrDefault());
            for (int i = 0; i < guid_Numbers.Count; i++)
            {
                guid_Numbers[i].Num = i;
            }
        }

        public static void DeleteGuid(this List<StatusContainer> statusContainers, Guid guidRequest)
        {
            StatusContainer deleteObj = statusContainers.Where(item => item.Id == guidRequest).FirstOrDefault();
            deleteObj.Image.Dispose();
            statusContainers.Remove(deleteObj);
        }

        public static void ResizeAllBitmap(this List<StatusContainer> containers)
        {
            int MaxWidth = 0;
            int MaxHeight = 0;
            foreach (var item in containers)
            {
                if (item.Image.Width > MaxWidth)
                {
                    MaxWidth = item.Image.Width;
                }
                if (item.Image.Height > MaxHeight)
                {
                    MaxHeight = item.Image.Height;
                }
            }
            for (int i = 0; i < containers.Count; i++)
            {
                if (containers[i].Image.Width != MaxWidth || containers[i].Image.Height != MaxHeight)
                {
                    var newbit = new Bitmap(containers[i].Image, MaxWidth, MaxHeight);
                    var newbit24rgb = newbit.Clone(new Rectangle(0, 0, newbit.Width, newbit.Height), PixelFormat.Format24bppRgb);
                    containers[i].Image.Dispose();
                    newbit.Dispose();
                    containers[i].Image = newbit24rgb;
                }
            }
        }

        public static void UpPos(this List<Guid_Number> guid_Numbers, int Num)
        {
            var thisnum = guid_Numbers[Num];
            guid_Numbers[Num] = guid_Numbers[Num - 1];
            guid_Numbers[Num].Num = Num;
            guid_Numbers[Num - 1] = thisnum;
            guid_Numbers[Num - 1].Num = Num - 1;
        }

        public static void DownPos(this List<Guid_Number> guid_Numbers, int Num)
        {
            var thisnum = guid_Numbers[Num];
            guid_Numbers[Num] = guid_Numbers[Num + 1];
            guid_Numbers[Num].Num = Num;
            guid_Numbers[Num + 1] = thisnum;
            guid_Numbers[Num + 1].Num = Num + 1;
        }

        public static void UpPos(this List<StatusContainer> guid_Numbers, int Num)
        {
            var thisnum = guid_Numbers[Num];
            guid_Numbers[Num] = guid_Numbers[Num - 1];
            guid_Numbers[Num - 1] = thisnum;
        }

        public static void DownPos(this List<StatusContainer> guid_Numbers, int Num)
        {
            var thisnum = guid_Numbers[Num];
            guid_Numbers[Num] = guid_Numbers[Num + 1];
            guid_Numbers[Num + 1] = thisnum;
        }
    }

    public static class GraphMethods
    {
        private static byte SumByte(int bt1, int bt2)
        {
            int result = bt1 + bt2;
            if (result > 255)
                return 255;
            if (result < 0)
                return 0;
            return (byte)result;
        }

        private static byte SumByte(float bt1, float bt2)
        {
            float result = bt1 + bt2;
            if (result > 255)
                return 255;
            if (result < 0)
                return 0;
            return (byte)result;
        }

        private static byte MiddleByte(float bt1, float bt2)
        {
            return (byte)((bt1 + bt2) / 2);

        }

        public static bool Summary(byte[] bytes1, byte[] bytes2, Channel channel = 0, int Transparency = 0)
        {
            float TranspCoef = 1 - (Transparency / 100);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = (byte)(SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]));
                }
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                    bytes1[i + 2] = SumByte(bytes1[i], bytes2[i]);
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                    bytes1[i + 2] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                    bytes1[i + 1] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], (byte)TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = SumByte(bytes1[i], bytes2[i]);
                }
            }
            return true;
        }
        public static bool Summary(Bitmap Source, Bitmap Changer, Channel channel = 0, int Transparency = 0)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);

            var bmpData1 = Changer.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr1 = bmpData1.Scan0;
            var Size1 = bmpData1.Stride * bmpData1.Height;
            byte[] bytes2 = new byte[Size1];
            System.Runtime.InteropServices.Marshal.Copy(ptr1, bytes2, 0, Size1);

            float TranspCoef = 1 - (Transparency / 100.0f);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = SumByte(bytes1[i + 1], TranspCoef * bytes2[i + 1]);
                    bytes1[i + 2] = SumByte(bytes1[i + 2], TranspCoef * bytes2[i + 2]);
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], TranspCoef * bytes2[i]);
                    bytes1[i + 2] = SumByte(bytes1[i + 2], TranspCoef * bytes2[i + 2]);
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], TranspCoef * bytes2[i]);
                    bytes1[i + 1] = SumByte(bytes1[i + 1], TranspCoef * bytes2[i + 1]);
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = SumByte(bytes1[i + 1], TranspCoef * bytes2[i + 1]);
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = SumByte(bytes1[i], TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = SumByte(bytes1[i + 2], TranspCoef * bytes2[i + 2]);
                }
            }
            //Редачим сурс
            System.Runtime.InteropServices.Marshal.Copy(bytes1, 0, ptr, Size);
            Source.UnlockBits(bmpData);
            Changer.UnlockBits(bmpData1);
            return true;
        }
        public static bool Summary(Bitmap Source, StatusContainer container)
        {
            return Summary(Source, container.Image, container.Channel, container.Transparency);
        }

        public static bool Multiply(Bitmap Source, Bitmap Changer, Channel channel = 0, int Transparency = 0)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);

            var bmpData1 = Changer.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr1 = bmpData1.Scan0;
            var Size1 = bmpData1.Stride * bmpData1.Height;
            byte[] bytes2 = new byte[Size1];
            System.Runtime.InteropServices.Marshal.Copy(ptr1, bytes2, 0, Size1);

            float TranspCoef = 1 - (Transparency / 100.0f);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f)));
                }
                //Parallel.For(0, count, (i) => { bytes1[i] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f))); });
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = (byte)(bytes1[i + 1] * (TranspCoef * (bytes2[i + 1] / 255.0f)));
                    bytes1[i + 2] = (byte)(bytes1[i + 2] * (TranspCoef * (bytes2[i + 2] / 255.0f)));
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f)));
                    bytes1[i + 2] = (byte)(bytes1[i + 2] * (TranspCoef * (bytes2[i + 2] / 255.0f)));
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f)));
                    bytes1[i + 1] = (byte)(bytes1[i + 1] * (TranspCoef * (bytes2[i + 1] / 255.0f)));
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = (byte)(bytes1[i + 1] * (TranspCoef * (bytes2[i + 1] / 255.0f)));
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f)));
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = (byte)(bytes1[i] * (TranspCoef * (bytes2[i] / 255.0f)));
                }
            }
            //Редачим сурс
            System.Runtime.InteropServices.Marshal.Copy(bytes1, 0, ptr, Size);
            Source.UnlockBits(bmpData);
            Changer.UnlockBits(bmpData1);
            return true;
        }
        public static bool Multiply(Bitmap Source, StatusContainer container)
        {
            return Multiply(Source, container.Image, container.Channel, container.Transparency);
        }

        public static bool Middle(Bitmap Source, Bitmap Changer, Channel channel = 0, int Transparency = 0)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);

            var bmpData1 = Changer.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr1 = bmpData1.Scan0;
            var Size1 = bmpData1.Stride * bmpData1.Height;
            byte[] bytes2 = new byte[Size1];
            System.Runtime.InteropServices.Marshal.Copy(ptr1, bytes2, 0, Size1);

            float TranspCoef = 1 - (Transparency / 100.0f);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = (byte)((bytes1[i] + bytes2[i]) / 2.0f);
                }
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = (byte)((bytes1[i + 1] + bytes2[i + 1]) / 2.0f);
                    bytes1[i + 2] = (byte)((bytes1[i + 2] + bytes2[i + 2]) / 2.0f);
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)((bytes1[i] + bytes2[i]) / 2.0f);
                    bytes1[i + 2] = (byte)((bytes1[i + 2] + bytes2[i + 2]) / 2.0f);
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)((bytes1[i] + bytes2[i]) / 2.0f);
                    bytes1[i + 1] = (byte)((bytes1[i + 1] + bytes2[i + 1]) / 2.0f);
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = (byte)((bytes1[i + 1] + bytes2[i + 1]) / 2.0f);
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = (byte)((bytes1[i] + bytes2[i]) / 2.0f);
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = (byte)((bytes1[i + 2] + bytes2[i + 2]) / 2.0f);
                }
            }
            //Редачим сурс
            System.Runtime.InteropServices.Marshal.Copy(bytes1, 0, ptr, Size);
            Source.UnlockBits(bmpData);
            Changer.UnlockBits(bmpData1);
            return true;
        }
        public static bool Middle(Bitmap Source, StatusContainer container)
        {
            return Middle(Source, container.Image, container.Channel, container.Transparency);
        }

        public static bool Max(Bitmap Source, Bitmap Changer, Channel channel = 0, int Transparency = 0)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);

            var bmpData1 = Changer.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr1 = bmpData1.Scan0;
            var Size1 = bmpData1.Stride * bmpData1.Height;
            byte[] bytes2 = new byte[Size1];
            System.Runtime.InteropServices.Marshal.Copy(ptr1, bytes2, 0, Size1);

            float TranspCoef = 1 - (Transparency / 100.0f);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = bytes1[i] > bytes2[i] ? bytes1[i] : (byte)(TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? bytes1[i + 1] : (byte)(TranspCoef * bytes2[i + 1]);
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? bytes1[i + 2] : (byte)(TranspCoef * bytes2[i + 2]);
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? bytes1[i] : (byte)(TranspCoef * bytes2[i]);
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? bytes1[i + 2] : (byte)(TranspCoef * bytes2[i + 2]);
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? bytes1[i] : (byte)(TranspCoef * bytes2[i]);
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? bytes1[i + 1] : (byte)(TranspCoef * bytes2[i + 1]);
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? bytes1[i + 1] : (byte)(TranspCoef * bytes2[i + 1]);
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? bytes1[i] : (byte)(TranspCoef * bytes2[i]);
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? bytes1[i + 2] : (byte)(TranspCoef * bytes2[i + 2]);
                }
            }
            //Редачим сурс
            System.Runtime.InteropServices.Marshal.Copy(bytes1, 0, ptr, Size);
            Source.UnlockBits(bmpData);
            Changer.UnlockBits(bmpData1);
            return true;
        }
        public static bool Max(Bitmap Source, StatusContainer container)
        {
            return Max(Source, container.Image, container.Channel, container.Transparency);
        }

        public static bool Min(Bitmap Source, Bitmap Changer, Channel channel = 0, int Transparency = 0)
        {
            var bmpData = Source.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] bytes1 = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, bytes1, 0, Size);

            var bmpData1 = Changer.LockBits(new Rectangle(0, 0, Source.Width, Source.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, Source.PixelFormat);
            var ptr1 = bmpData1.Scan0;
            var Size1 = bmpData1.Stride * bmpData1.Height;
            byte[] bytes2 = new byte[Size1];
            System.Runtime.InteropServices.Marshal.Copy(ptr1, bytes2, 0, Size1);

            float TranspCoef = 1 - (Transparency / 100.0f);
            int count = bytes2.Length;
            if (channel == Channel.RGB)
            {
                for (int i = 0; i < count; i++) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? (byte)(TranspCoef * bytes2[i]) : bytes1[i];
                }
            }
            if (channel == Channel.RG)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? (byte)(TranspCoef * bytes2[i + 1]) : bytes1[i + 1];
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? (byte)(TranspCoef * bytes2[i + 2]) : bytes1[i + 2];
                }
            }
            if (channel == Channel.RB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? (byte)(TranspCoef * bytes2[i]) : bytes1[i];
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? (byte)(TranspCoef * bytes2[i + 2]) : bytes1[i + 2];
                }
            }
            if (channel == Channel.GB)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? (byte)(TranspCoef * bytes2[i]) : bytes1[i];
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? (byte)(TranspCoef * bytes2[i + 1]) : bytes1[i + 1];
                }
            }
            if (channel == Channel.G)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 1] = bytes1[i + 1] > TranspCoef * bytes2[i + 1] ? (byte)(TranspCoef * bytes2[i + 1]) : bytes1[i + 1];
                }
            }
            if (channel == Channel.B)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i] = bytes1[i] > TranspCoef * bytes2[i] ? (byte)(TranspCoef * bytes2[i]) : bytes1[i];
                }
            }
            if (channel == Channel.R)
            {
                for (int i = 0; i < count; i += 3) //bgr
                {
                    bytes1[i + 2] = bytes1[i + 2] > TranspCoef * bytes2[i + 2] ? (byte)(TranspCoef * bytes2[i + 2]) : bytes1[i + 2];
                }
            }
            //Редачим сурс
            System.Runtime.InteropServices.Marshal.Copy(bytes1, 0, ptr, Size);
            Source.UnlockBits(bmpData);
            Changer.UnlockBits(bmpData1);
            return true;
        }
        public static bool Min(Bitmap Source, StatusContainer container)
        {
            return Min(Source, container.Image, container.Channel, container.Transparency);
        }
    }

    public static class ImageConvert
    {
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] Bytes = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, Bytes, 0, Size);
            bitmap.UnlockBits(bmpData);
            return BitmapSource.Create(bitmap.Width, bitmap.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null, Bytes, bmpData.Stride);
        }

        public static Bitmap ToBitmap(this BitmapSource bitmaps)
        {
            int stride = (int)bitmaps.PixelWidth * (bitmaps.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[(int)bitmaps.PixelHeight * stride];
            bitmaps.CopyPixels(pixels, stride, 0);
            var res = new Bitmap(bitmaps.PixelWidth, bitmaps.PixelHeight);
            var bmpData = res.LockBits(new Rectangle(0, 0, res.Width, res.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, res.PixelFormat);
            var ptr = bmpData.Scan0;
            var Size = bmpData.Stride * bmpData.Height;
            byte[] Bytes = new byte[Size];
            System.Runtime.InteropServices.Marshal.Copy(ptr, Bytes, 0, Size);
            System.Runtime.InteropServices.Marshal.Copy(pixels, 0, ptr, Size);
            res.UnlockBits(bmpData);
            if (res.PixelFormat != PixelFormat.Format24bppRgb)
                res = res.To24bppRgb();
            return res;
        }

        public static Bitmap To24bppRgb(this Bitmap bitmap) //Возвращает битмап в заданном формате
        {
            var newbit24rgb = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), PixelFormat.Format24bppRgb);
            bitmap.Dispose();
            return newbit24rgb;
        }

        public static BitmapSource ToBitmapSource(this byte[] Bytes, int stride, int width, int height)
        {
            return BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null, Bytes, stride);
        }

        public static Bitmap ChangeFormat(this Bitmap bitmap, PixelFormat px)
        {
            var newbit = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), px);
            bitmap.Dispose();
            return newbit;
        }
    }

    public static class ParseInst
    {
        /// <summary>
        /// //Ищет через сурсы. Не находит все фото в коллекциях (640x640 only)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> Parse(string user) //Ищет через сурсы (640x640 only)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("src") + "src\"=\"".Length);
                    string url1 = str.Substring(0, str.IndexOf("\""));
                    if (url1.StartsWith("http") && (str.IndexOf("config_width\":640") - url1.Length) < 10)
                    {
                        string url = url1.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            Random random = new Random((int)DateTime.Now.Ticks);
            if (Urls.Count > 0)
            {
                return Urls;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Находит фотки в оригинальном разрешении. 
        /// Берет все фотки из групп фотографий (Ищет через display_url) (Работает дольше, чем обычный Parse)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> ParseMethod2(string user) //Находит фотки в оригинальном разрешении (Ищет через display_url) (Работает дольше, чем обычный Parse)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("display_url\":\"") + "display_url\":\"".Length);
                    string url1 = str.Substring(0, str.IndexOf("\""));
                    if (url1.StartsWith("http"))
                    {
                        string url = url1.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            if (Urls.Count > 0)
            {
                //Убираем все повторы
                var UrldWithoutR = new List<String>();
                foreach (var ur in Urls)
                {
                    var item = UrldWithoutR.FirstOrDefault(i => i == ur);
                    if (item == null)
                    {
                        UrldWithoutR.Add(ur);
                    }
                }
                //
                return UrldWithoutR;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Ищет по шорткодам (надо тестить, очень странно работает)
        /// Возвращает список ссылок на фотографии или null
        /// <param name="user"></param>
        /// </summary>
        public static async Task<List<string>> ParseMethodTest(string user) //Находит скрытые? фотки, ищет по шорткодам (надо тестить, очень странно работает)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/" + user + "/");
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            string str;

            using (Stream resStream = response.GetResponseStream())
            {
                int count = 0;
                do
                {
                    count = resStream.Read(buf, 0, buf.Length);
                    if (count != 0)
                    {
                        sb.Append(Encoding.Default.GetString(buf, 0, count));
                    }
                }
                while (count > 0);
                str = sb.ToString();
            }

            List<string> Urls = new List<string>();
            str = str.Substring(str.IndexOf("body"));
            while (true)
            {
                try
                {
                    str = str.Substring(str.IndexOf("shortcode\":\"") + "shortcode\":\"".Length);
                    string shortcode = str.Substring(0, str.IndexOf('"'));
                    string url1 = str.Substring(str.IndexOf("display_url\":\"") + "display_url\":\"".Length);
                    string url2 = url1.Substring(0, url1.IndexOf('"'));
                    if (url2.StartsWith("http"))
                    {
                        string url = url2.Replace("\\u0026", "&");
                        Urls.Add(url);
                    }
                }
                catch
                {
                    break;
                }
            }
            Random random = new Random((int)DateTime.Now.Ticks);
            if (Urls.Count > 0)
            {
                return Urls;
            }
            else
            {
                return null;
            }
        }
    }

    public enum Operations
    {
        Non,
        Sum,
        Mult,
        Middle,
        Min,
        Max
    }

    public enum Channel
    {
        RGB,
        R,
        G,
        B,
        RG,
        GB,
        RB
    }
}
