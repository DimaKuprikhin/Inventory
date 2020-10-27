using Inventory;
using System;
using System.Collections.Generic;

namespace InventoryManager
{
    class Presenter
    {
        private Database database;
        private Table table;
        private MainWindow window;

        public Presenter(MainWindow window)
        {
            this.window = window;
            window.loadTable += OnLoadTable;
            window.visibleItemsChanged += OnVisibleItemsChanged;
            window.loadDatabase += OnLoadDatabase;
            window.saveDatabase += OnSaveDatabase;
            window.inputBarcode += OnInputBarcode;
            window.addLink += OnAddLink;
            window.saveTable += OnSaveTable;
            window.cancel += OnCancel;
            window.addWithoutBarcode += OnAddWithoutBarcode;
            window.cellChanged += OnCellChanged;
        }

        // Табица.
        private void OnLoadTable(object sender, EventArgs e)
        {
            table = new Table(window.TableFilePath);
            window.SetDataGrid(table.VisibleItems);
            window.SetProviders(table.Providers);
        }
        private void OnSaveTable(object sender, EventArgs e)
        {
            table.Save(window.TableFilePath);
        }

        // База.
        public void OnLoadDatabase(object sender, EventArgs e)
        {
            database = new Database(window.DatabaseFilePath);
        }
        public void OnSaveDatabase(object sender, EventArgs e)
        {
            database.Save();
        }

        // Отображение.
        /// <summary>
        /// Обновляет вид таблицы согласно выбранным поставшикам и тексту в
        /// поле поиска.
        /// </summary>
        public void OnVisibleItemsChanged(object sender, EventArgs e)
        {
            List<string> providers = new List<string>();
            for (int i = 1; i < window.Providers.Count; ++i)
                if (window.Providers[i].IsChecked)
                    providers.Add(window.Providers[i].Name);
            table.UpdateVisibleItems(providers, window.SearchText, window.IsOnlyUnfilled);
            window.SetDataGrid(table.VisibleItems);
        }
        /// <summary>
        /// Выводим информацию о добавленном товаре: кучу и его наименование.
        /// Также обновляем вид таблицы, активируем кнопки отмены и очищаем поля.
        /// </summary>
        private void ShowItem(string heap, string name)
        {
            window.ShowHeap(heap);
            window.ShowName(name);
            OnVisibleItemsChanged(this, EventArgs.Empty);
            window.Clear = true;
            if(table.History.Count > 0)
                window.IsCancelActive = true;
        }

        // Добавление товара.
        /// <summary>
        /// Добавляет новую пару штрихкода и id в базу и добавляет 1 в 
        /// выбранный пользователем товар.
        /// </summary>
        public void OnAddLink(object sender, EventArgs e)
        {
            database.AddNewPair(window.Barcode, window.SelectedItem.Id);
            Item result = table.Add(window.SelectedItem, 1);
            ShowItem(result.To, result.Name);
        }
        /// <summary>
        /// Добавляет 1 в выбранный пользователем товар.
        /// </summary>
        public void OnAddWithoutBarcode(object sender, EventArgs e)
        {
            Item result = table.Add(window.SelectedItem, 1);
            ShowItem(result.To, result.Name);
        }
        /// <summary>
        /// Отменяет последнее добавление товара.
        /// </summary>
        public void OnCancel(object sender, EventArgs e)
        {
            table.Cancel();
            window.IsCancelActive = table.History.Count > 0;
            OnVisibleItemsChanged(this, EventArgs.Empty);
        }
        /// <summary>
        /// Добавляет 1 в товар, соответствующий введеному пользователем 
        /// штрихкоду. Если такого нет, то выводит в поле кучи "Не найдено".
        /// Если есть два разных товара с подходящим id, то показывает 
        /// сообщение во всплывающем окне.
        /// </summary>
        public void OnInputBarcode(object sender, EventArgs e)
        {
            List<string> ids = database.FindPair(window.Barcode);
            Item result;
            try
            {
                result = table.Add(ids);
            }
            catch (ArgumentException)
            {
                window.ShowMessage("Найдено два разных товара в " +
                    Environment.NewLine + "текущей таблице для введеного" +
                    Environment.NewLine + "штрихкода. Удалите один из них" +
                    Environment.NewLine + "или добавьте товар без штрихкода.");
                return;
            }
            if (result == null)
            {
                window.ShowHeap("Не найдено");
                return;
            }
            ShowItem(result.To, result.Name);
        }
        /// <summary>
        /// Обрабатывает изменение количества товара руками в таблице.
        /// </summary>
        public void OnCellChanged(object sender, EventArgs e)
        {
            Item result = table.Add(window.SelectedItem, window.AddedNumber);
            ShowItem(result.To, result.Name);
        }
    }
}