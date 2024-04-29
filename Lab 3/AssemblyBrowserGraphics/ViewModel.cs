using AssemblyBrowserLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;

namespace AssemblyBrowserGraphics
{
    public class ViewModel : INotifyPropertyChanged
    {
        // Объект, который парсит структуру
        private readonly IAssemblyBrowser _model = new AssemblyBrowserLibrary.AssemblyBrowser();
        // Путь открытого файла
        private string _openedFile;

        public ViewModel()
        {
            // Список всех обработанных пространств имен
            Containers = new List<ContainerInfo>();
        }

        public List<ContainerInfo> Containers { get; set; }

        /*
         * Для каждого свойства, которому потребуются уведомления об изменениях, 
         * вызывается OnPropertyChanged при каждом обновлении свойства
         */
        public string OpenedFile
        {
            get
            {
                return _openedFile;
            }
            set
            {
                _openedFile = value;
                Containers = null;
                try
                {
                    // Получение всех обработанных пространств имен 
                    Containers = new List<ContainerInfo>(_model.GetNamespaces(value)); 
                }
                catch (Exception e)
                {
                    _openedFile = $"Error: [{e.Message}]";
                }
                OnPropertyChanged(nameof(Containers));
            }
        }
        // Обязательно нужно объявить эту переменную, чтобы реализовать INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        // Метод, для ловли изменений(выбор пути файла)
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand OpenFile { get { return new OpenFileCommand(OpenAssembly); } }

        public void OpenAssembly()
        {
            // После нажатия на открытие файла, открываем диалог
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = @"Assemblies|*.dll;*.exe";
                openFileDialog.Title = @"Select assembly";
                openFileDialog.Multiselect = false;
                // Если был выбран файл
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // вызываем set поля FileName
                    OpenedFile = openFileDialog.FileName;
                    OnPropertyChanged(nameof(OpenedFile));
                }
            }
        }
    }
}
