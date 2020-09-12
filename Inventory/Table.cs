using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Excel = Microsoft.Office.Interop.Excel;

namespace InventoryManager
{
    class Table
    {
        private readonly List<Item> items = new List<Item>();
        public List<Item> VisibleItems { get; private set; } = new List<Item>();
        public List<string> Providers { get; private set; } = new List<string>();
        public Stack<Item> History { get; private set; } = new Stack<Item>();

        public Table(string path)
        {
            Excel.Application app = new Excel.Application();
            Excel.Workbook book = app.Workbooks.Open(path);
            Excel.Worksheet sheet = book.Sheets[1];
            try
            {
                int lines = sheet.UsedRange.Rows.Count;
                for (int i = 2; i <= lines; ++i)
                {
                    Item next = new Item();
                    var orderString = sheet.Cells[i, 1].Value2;
                    if (orderString == null)
                        break;
                    next.Order = orderString.ToString();
                    next.Id = sheet.Cells[i, 2].Value2.ToString();
                    next.Name = sheet.Cells[i, 3].Value2.ToString();
                    var currNumber = sheet.Cells[i, 4].Value2;
                    if (currNumber == null)
                        next.CurrentNumber = 0;
                    else
                        next.CurrentNumber = int.Parse(currNumber.ToString());
                    next.Number = int.Parse(sheet.Cells[i, 5].Value2.ToString());
                    next.To = sheet.Cells[i, 6].Value2.ToString();
                    next.From = sheet.Cells[i, 7].Value2.ToString();
                    var comment = sheet.Cells[i, 9].Value2;
                    if (comment == null)
                        next.Comment = "";
                    else
                        next.Comment = comment.ToString();
                    if(next.CurrentNumber == next.Number)
                        next.ColorOfRow = new SolidColorBrush(Color.FromRgb(82, 186, 80));
                    else
                        next.ColorOfRow = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    items.Add(next);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Marshal.ReleaseComObject(sheet);
                book.Close();
                Marshal.ReleaseComObject(book);
                app.Quit();
                Marshal.ReleaseComObject(app);
                for (int i = 0; i < items.Count; ++i)
                    VisibleItems.Add(items[i]);
                FindProviders();
            }
        }

        private void FindProviders()
        {
            for (int i = 0; i < items.Count; ++i)
            {
                bool has = false;
                for (int j = 0; j < Providers.Count; ++j)
                {
                    if (items[i].From == Providers[j])
                    {
                        has = true;
                        break;
                    }
                }
                if (!has)
                    Providers.Add(items[i].From);
            }
            Providers.Add("ИЗЛИШЕК");
        }

        public void UpdateVisibleItems(List<string> providers, string name, 
            bool isOnlyUnfulled)
        {
            if(name != null)
                name = name.ToLower();
            VisibleItems = new List<Item>();
            for(int i = 0; i < items.Count; ++i)
            {
                for (int j = 0; j < providers.Count; ++j)
                {
                    if (items[i].From == providers[j] && 
                        (name == null || name == "" || items[i].Name.ToLower().Contains(name)) && 
                        (!isOnlyUnfulled || isOnlyUnfulled && items[i].CurrentNumber < items[i].Number))
                    {
                        VisibleItems.Add(items[i]);
                        break;
                    }
                }
            }
        }

        public Item Add(string id)
        {
            List<Item> found = VisibleItems.FindAll(item => item.Id == id);
            if(found.Count == 0)
                return null;
            int index = -1;
            for(int i = 0; i < found.Count; ++i)
            {
                if (found[i].CurrentNumber >= found[i].Number)
                    continue;
                if (index == -1)
                    index = i;
                if(GetPrior(found[i]) < GetPrior(found[index]) ||
                    (GetPrior(found[i]) == GetPrior(found[index]) && 
                    found[i].Number < found[index].Number))
                    index = i;
            }
            Item result = null;
            if (found.Count > 0 && index == -1)
            {
                if (found[found.Count - 1].To == "ИЗЛИШЕК")
                    result = found[found.Count - 1];
                else
                {
                    Item item = new Item();
                    item.Id = found[0].Id;
                    item.Name = found[0].Name;
                    item.To = "ИЗЛИШЕК";
                    item.From = "ИЗЛИШЕК";
                    items.Add(item);
                    result = item;
                }
            }
            else
                result = found[index];
            if (History.Count > 0)
            {
                if (History.Peek().CurrentNumber == History.Peek().Number)
                    History.Peek().ColorOfRow = new SolidColorBrush(Color.FromRgb(82, 186, 80));
                else
                    History.Peek().ColorOfRow = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            }
            ++result.CurrentNumber;
            History.Push(result);
            result.ColorOfRow = new SolidColorBrush(Color.FromRgb(67, 162, 240));
            return result;
        }

        public Item Add(Item item)
        {
            return Add(item.Id);
        }

        public void Save(string path)
        {
            Excel.Application app = new Excel.Application();
            Excel.Workbook book = app.Workbooks.Open(path);
            Excel.Worksheet sheet = book.Sheets[1];
            try
            {
                for(int i = 0; i < items.Count; ++i)
                {
                    sheet.Cells[i + 2, 4] = items[i].CurrentNumber.ToString();
                    sheet.Cells[i + 2, 9] = items[i].Comment;
                    if (items[i].To == "ИЗЛИШЕК")
                    {
                        sheet.Cells[i + 2, 1] = items[i].Order;
                        sheet.Cells[i + 2, 2] = items[i].Id;
                        sheet.Cells[i + 2, 3] = items[i].Name;
                        sheet.Cells[i + 2, 5] = items[i].Number;
                        sheet.Cells[i + 2, 6] = items[i].To;
                        sheet.Cells[i + 2, 7] = items[i].From;
                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Marshal.ReleaseComObject(sheet);
                book.Close();
                Marshal.ReleaseComObject(book);
                app.Quit();
                Marshal.ReleaseComObject(app);
            }
        }

        public void Cancel()
        {
            --History.Peek().CurrentNumber;
            History.Peek().ColorOfRow = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            History.Pop();
            if(History.Count > 0)
                History.Peek().ColorOfRow = new SolidColorBrush(Color.FromRgb(67, 162, 240));
        }

        private int GetPrior(Item item)
        {
            if (item.To == "Доставка" && item.Number == 1) return 0;
            if (item.To == "Н.Новгород" && item.Number == 1) return 1;
            if (item.To == "Воронеж" && item.Number == 1) return 2;
            if (item.To == "Рязань" && item.Number == 1) return 3;
            if (item.To == "Доставка" && item.Number > 1) return 4;
            if (item.To == "Н.Новгород" && item.Number > 1) return 5;
            if (item.To == "Воронеж" && item.Number > 1) return 6;
            if (item.To == "Рязань" && item.Number > 1) return 7;
            if (item.To == "Магазин") return 8;
            throw new ArgumentException("Неизвестная точка доставки." +
                "Допустимы только: Доставка, Н.Новгород, Воронеж, Рязань, Магазин");
        }
    }
}
