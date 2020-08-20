using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using InventoryManager;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Inventory
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Presenter presenter;

        public MainWindow()
        {
            InitializeComponent();
            presenter = new Presenter(this);
            this.DataContext = this;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.SingleBorderWindow;
        }


        private bool isTableLoaded = false;
        private bool isDatabaseLoaded = false;
        public void ActivateButtons()
        {
            if (isTableLoaded && isDatabaseLoaded)
            {
                addLinkButton.IsEnabled = true;
                searchTextBox.IsEnabled = true;
                barcodeTextBox.IsEnabled = true;
                providersCheckBox.IsEnabled = true;
                saveTableButton.IsEnabled = true;
                saveDatabaseButton.IsEnabled = true;
            }
        }


        public event EventHandler<EventArgs> loadTable;
        public string TableFilePath { get; private set; }
        private void OnLoadTable(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Excel файл (*.xls)|*.xls|Excel файл (*.xlsx)|*.xlsx";
                bool? dialogResult = dialog.ShowDialog();
                if (dialogResult == true)
                {
                    TableFilePath = dialog.FileName;
                    loadTable?.Invoke(this, EventArgs.Empty);
                }
                isTableLoaded = true;
                ActivateButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine
                    + ex.StackTrace + Environment.NewLine +
                    "Ошибка при загрузке таблицы");
            }
        }
        public event EventHandler<EventArgs> saveTable;
        private void OnSaveTable(object sender, EventArgs e)
        {
            try
            {
                saveTable?.Invoke(this, EventArgs.Empty);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine
                    + ex.StackTrace + Environment.NewLine +
                    "Ошибка при сохранении таблицы");
            }
        }


        public event EventHandler<EventArgs> loadDatabase;
        public string DatabaseFilePath { get; private set; }
        private void OnLoadDatabase(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "CSV файл (*.csv)|*.csv";
                bool? dialogResult = dialog.ShowDialog();
                if (dialogResult == true)
                {
                    DatabaseFilePath = dialog.FileName;
                    loadDatabase?.Invoke(this, EventArgs.Empty);
                }
                isDatabaseLoaded = true;
                saveDatabaseButton.IsEnabled = true;
                ActivateButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine
                    + ex.StackTrace + Environment.NewLine +
                    "Ошибка при загрузке базы");
            }
        }


        public event EventHandler<EventArgs> saveDatabase;
        private void OnSaveDatabase(object sender, EventArgs e)
        {
            try
            {
                saveDatabase?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine 
                    + ex.StackTrace + Environment.NewLine +
                    "Ошибка при сохранении базы");
            }
        }


        public class Box
        {
            public bool IsChecked { get; set; } = true;
            public string Name { get; set; }
            public Box(string name) { Name = name; }
        }
        public List<Box> Providers { get; private set; } = new List<Box>();
        private bool isAllChosen = true;
        public void SetProviders(List<string> providers)
        {
            Providers = new List<Box>();
            Providers.Add(new Box("Все"));
            for (int i = 0; i < providers.Count; ++i)
                Providers.Add(new Box(providers[i]));
            providersCheckBox.ItemsSource = Providers;
        }
        public void OnCheckBoxChanged(object sender, EventArgs e)
        {
            try
            {
                if (Providers[0].IsChecked ^ isAllChosen)
                {
                    isAllChosen = Providers[0].IsChecked;
                    for (int i = 1; i < providersCheckBox.Items.Count; ++i)
                    {
                        Providers[i].IsChecked = isAllChosen;
                    }
                }
                bool hasUnselected = false;
                for (int i = 1; i < Providers.Count; ++i)
                    if (!Providers[i].IsChecked)
                        hasUnselected = true;
                if (hasUnselected)
                {
                    isAllChosen = false;
                    Providers[0].IsChecked = false;
                }
                else
                {
                    isAllChosen = true;
                    Providers[0].IsChecked = true;
                }
                visibleItemsChanged?.Invoke(this, EventArgs.Empty);
                providersCheckBox.ItemsSource = Providers;
                providersCheckBox.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка при изменение поставщиков");
            }
        }


        public event EventHandler<EventArgs> inputBarcode;
        public string Barcode { get; private set; }
        public bool Clear { get; set; } = false;
        private bool isFirstLevel = true;
        private void OnBarcodeTextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!isFirstLevel)
                    return;
                if (barcodeTextBox.Text == "")
                {
                    heapTextBox.Text = "";
                    return;
                }
                Barcode = barcodeTextBox.Text;
                inputBarcode?.Invoke(this, EventArgs.Empty);
                if (Clear)
                {
                    isFirstLevel = false;
                    barcodeTextBox.Text = "";
                    isFirstLevel = true;
                }
                Clear = false;
                cancelButton.IsEnabled = IsCancelActive;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка при вводе штрихкода");
            }
        }


        public event EventHandler<EventArgs> visibleItemsChanged;
        public string SearchText { get; private set; } = "";
        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            try
            {
                SearchText = searchTextBox.Text;
                visibleItemsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка вводе в строку поиска");
            }
        }


        public event EventHandler<EventArgs> addLink;
        public Item SelectedItem { get; private set; }
        private void OnAddLink(object sender, EventArgs e)
        {
            try
            {
                if (barcodeTextBox.Text == "")
                {
                    MessageBox.Show("Пустой штрихкод");
                    return;
                }
                if (dataGridView.SelectedItems.Count == 1 ||
                    dataGridView.SelectedCells.Count == 1)
                {
                    SelectedItem = dataGridView.SelectedItems[0] as Item;
                }
                else
                {
                    MessageBox.Show("Для связывания должен быть выбран ровно один товар");
                    return;
                }
                addLink?.Invoke(this, EventArgs.Empty);
                OnBarcodeTextChanged(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка при связывании");
            }
        }


        public void ShowHeap(string heap)
        {
            heapTextBox.Text = heap;
        }


        public void SetDataGrid(List<Item> items)
        {
            try
            {
                dataGridView.ItemsSource = items;
                for (int i = 0; i < items.Count; ++i)
                    if (items[i].ColorOfRow.Color.R == 67)
                        dataGridView.ScrollIntoView(dataGridView.Items[i]);
                dataGridView.UpdateLayout();
                dataGridView.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка при отображении таблицы");
            }
        }


        public void OnClosing(object sender, CancelEventArgs e)
        {
            if (isTableLoaded && isDatabaseLoaded)
            {
                MessageBoxResult result =
                    MessageBox.Show("Хотите сохранить таблицу и " +
                    "базу перед закрытием?", "", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    OnSaveDatabase(this, EventArgs.Empty);
                    OnSaveTable(this, EventArgs.Empty);
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }

        public bool IsCancelActive { get; set; } = false;
        public event EventHandler<EventArgs> cancel;
        public void OnCancel(object sender, EventArgs e)
        {
            try
            {
                cancel?.Invoke(this, EventArgs.Empty);
                cancelButton.IsEnabled = IsCancelActive;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine +
                    ex.StackTrace + Environment.NewLine +
                    "Ошибка при отмене");
            }
        }
    }
}