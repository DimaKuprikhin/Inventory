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
        }

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

        public void OnVisibleItemsChanged(object sender, EventArgs e)
        {
            List<string> providers = new List<string>();
            for (int i = 1; i < window.Providers.Count; ++i)
                if (window.Providers[i].IsChecked)
                    providers.Add(window.Providers[i].Name);
            table.UpdateVisibleItems(providers, window.SearchText, window.IsOnlyUnfilled);
            window.SetDataGrid(table.VisibleItems);
        }

        public void OnLoadDatabase(object sender, EventArgs e)
        {
            database = new Database(window.DatabaseFilePath, true);
        }

        public void OnSaveDatabase(object sender, EventArgs e)
        {
            database.Save();
        }

        public void OnInputBarcode(object sender, EventArgs e)
        {
            List<string> ids = database.FindPair(window.Barcode);
            Item result;
            try
            {
                result = table.Add(ids);
            }
            catch(ArgumentException)
            {
                window.ShowMessage(       "Найдено два разных товара в " +
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
            window.ShowHeap(result.To);
            window.ShowName(result.Name);
            OnVisibleItemsChanged(this, EventArgs.Empty);
            window.SetDataGrid(table.VisibleItems);
            window.Clear = true;
            if (table.History.Count > 0)
                window.IsCancelActive = true;
        }

        public void OnAddLink(object sender, EventArgs e)
        {
            database.AddNewPair(window.Barcode, window.SelectedItem.Id);
            window.IsCancelActive = true;
        }

        public void OnCancel(object sender, EventArgs e)
        {
            table.Cancel();
            window.IsCancelActive = table.History.Count > 0;
            window.SetDataGrid(table.VisibleItems);
        }

        public void OnAddWithoutBarcode(object sender, EventArgs e)
        {
            Item result = table.Add(window.SelectedItem);
            window.ShowHeap(result.To);
            window.ShowName(result.Name);
            OnVisibleItemsChanged(this, EventArgs.Empty);
            window.SetDataGrid(table.VisibleItems);
            window.Clear = true;
            window.IsCancelActive = true;
        }
    }
}
