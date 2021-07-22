using System;
using System.Collections.Generic;
using System.Linq;
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
        public Stack<Tuple<Item, int>> History { get; private set; } = 
            new Stack<Tuple<Item, int>>();
        public static readonly Color DefaultItemColor = Color.FromRgb(255, 255, 255);
        public static readonly Color LastItemColor = Color.FromRgb(255, 255, 0);
        public static readonly Color FullItemColor = Color.FromRgb(82, 186, 80);

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
                    next.PreviousNumber = next.CurrentNumber;
                    next.Number = int.Parse(sheet.Cells[i, 5].Value2.ToString());
                    next.To = sheet.Cells[i, 6].Value2.ToString();
                    next.From = sheet.Cells[i, 7].Value2.ToString();
                    var comment = sheet.Cells[i, 9].Value2;
                    if (comment == null)
                        next.Comment = "";
                    else
                        next.Comment = comment.ToString();
                    next.Log.Clear();
                    var log = sheet.Cells[i, 10].Value2;
                    if (log != null)
                        next.Log.Append(log.ToString());
                    if(next.CurrentNumber == next.Number)
                        next.ColorOfRow = new SolidColorBrush(FullItemColor);
                    else
                        next.ColorOfRow = new SolidColorBrush(DefaultItemColor);
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
                Providers = items.Select(item => item.From).Distinct().ToList();
                Providers.Add("ИЗЛИШЕК");
            }
        }

        public void UpdateVisibleItems(List<string> providers, 
            string name, bool isOnlyUnfulled)
        {
            if(name != null)
                name = name.ToLower();
            VisibleItems = new List<Item>();
            for(int i = 0; i < items.Count; ++i)
            {
                for (int j = 0; j < providers.Count; ++j)
                {
                    if (items[i].From == providers[j] && 
                        (name == null || name == "" || 
                        items[i].Name.ToLower().Contains(name)) && 
                        (!isOnlyUnfulled || isOnlyUnfulled && 
                        items[i].CurrentNumber < items[i].Number))
                    {
                        VisibleItems.Add(items[i]);
                        break;
                    }
                }
            }
        }

        public Item Add(List<string> ids)
        {
            // Ищем все подходящие товары.
            List<Item> found = VisibleItems.FindAll(i => ids.Find(s => s == i.Id) != null);
            if(found.Select(i => i.Id).Distinct().Count() > 1)
                throw new ArgumentException("несколько товаров для этого штрихкода");
            if(found.Count == 0)
                return null;
            // Выбираем самый приоритетный.
            int index = -1;
            for(int i = 0; i < found.Count; ++i)
            {
                if (found[i].CurrentNumber >= found[i].Number)
                    continue;
                if (index == -1)
                    index = i;
                if(GetToPrior(found[i]) < GetToPrior(found[index]) ||
                    (GetToPrior(found[i]) == GetToPrior(found[index]) && 
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
                    result = new Item(found[0].Id, found[0].Name, "ИЗЛИШЕК", "ИЗЛИШЕК");
                    items.Add(result);
                }
            }
            else
                result = found[index];
            if (History.Count > 0)
                UpdateItemColor(History.Peek().Item1);
            result.AddWithLogging(1);
            History.Push(new Tuple<Item, int>(result, 1));
            result.ColorOfRow = new SolidColorBrush(LastItemColor);
            FixNumbers();
            return result;
        }

        public Item Add(Item item, int number)
        {
            if (History.Count > 0)
                UpdateItemColor(History.Peek().Item1);
            int add = item.To == "ИЗЛИШЕК" ? number : 
                Math.Min(number, item.Number - item.CurrentNumber);
            item.AddWithLogging(add);
            if(add != 0)
                History.Push(new Tuple<Item, int>(item, add));
            number -= add;
            Item result = number > 0 ? new Item() : item;
            if(number > 0)
                return Add(new List<string> { item.Id });
            result.ColorOfRow = new SolidColorBrush(LastItemColor);
            FixNumbers();
            return result;
        }

        /// <summary>
        /// Приравнивает значения PreviousNumber к CurrentNumber в товарах.
        /// </summary>
        private void FixNumbers()
        {
            for (int i = 0; i < items.Count; ++i)
                items[i].PreviousNumber = items[i].CurrentNumber;
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
                    sheet.Cells[i + 2, 10] = items[i].Log.ToString();
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
                book.Save();
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
            Item item = History.Peek().Item1;
            item.AddWithLogging(-History.Peek().Item2);
            UpdateItemColor(item);
            if (item.To == "ИЗЛИШЕК" && item.CurrentNumber == 0)
                items.Remove(item);
            History.Pop();
            if(History.Count > 0)
                History.Peek().Item1.ColorOfRow = new SolidColorBrush(LastItemColor);
        }

        private int GetToPrior(Item item)
        {
            if (item.To.ToLower() == "яндекс") return -1;
            if (item.To.ToLower() == "доставка" && item.Number == 1) return 0;
            if (item.To.ToLower() == "н.новгород" && item.Number == 1) return 1;
            if (item.To.ToLower() == "воронеж" && item.Number == 1) return 2;
            if (item.To.ToLower() == "рязань" && item.Number == 1) return 3;
            if (item.To.ToLower() == "доставка" && item.Number > 1) return 4;
            if (item.To.ToLower() == "н.новгород" && item.Number > 1) return 5;
            if (item.To.ToLower() == "воронеж" && item.Number > 1) return 6;
            if (item.To.ToLower() == "рязань" && item.Number > 1) return 7;
            if (item.To.ToLower() == "магазин") return 8;
            throw new ArgumentException("Неизвестная точка доставки. " +
                "Допустимы только: Доставка, Н.Новгород, Воронеж, Рязань, Магазин");
        }

        private void UpdateItemColor(Item item)
        {
            if (item.CurrentNumber == item.Number)
                item.ColorOfRow = new SolidColorBrush(FullItemColor);
            else
                item.ColorOfRow = new SolidColorBrush(DefaultItemColor);
        }
    }
}
