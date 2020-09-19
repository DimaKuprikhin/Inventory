using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using Excel = Microsoft.Office.Interop.Excel;

namespace InventoryManager
{
    class Database
    {
        /// <summary>
        /// <barcode, id>
        /// </summary>
        public List<Tuple<string, string>> Pairs { get; private set; } =
            new List<Tuple<string, string>>();

        /// <summary>
        /// Конструктор базы кодов, работающий с файлом csv.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isCsv"> Перегрузка со втрорым bool, для загрузки из csc. </param>
        public Database(string path, bool isCsv)
        {
            try
            {
                StreamReader reader = new StreamReader(path);
                while(!reader.EndOfStream)
                {
                    string s = reader.ReadLine();
                    string[] numbers = s.Split(',');
                    Pairs.Add(new Tuple<string, string>(numbers[0], numbers[1]));
                }
                reader.Close();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message + Environment.NewLine +
                    "Возникла ошибка при чтении файла с базой");
            }
        }

        public Database(string path)
        {
            Excel.Application app = new Excel.Application();
            Excel.Workbook book = app.Workbooks.Open(path);
            Excel.Worksheet sheet = book.Sheets[1];
            try
            {
                int lines = sheet.UsedRange.Rows.Count;
                for (int i = 2; i < lines; ++i)
                {
                    var idString = sheet.Cells[i, 1].Value2;
                    if (idString == null) break;
                    string id = idString.ToString();

                    var eanString = sheet.Cells[i, 2].Value2;
                    if (eanString != null)
                    {
                        string[] ean = eanString.ToString().Split(',');
                        for (int j = 0; j < ean.Length; ++j)
                            if (ean[j] != "")
                                Pairs.Add(new Tuple<string, string>(ean[j].Trim(), id));
                    }

                    var upsString = sheet.Cells[i, 3].Value2;
                    if (upsString != null)
                    {
                        string[] ups = upsString.ToString().Split(',');
                        for (int j = 0; j < ups.Length; ++j)
                            if (ups[j] != "")
                                Pairs.Add(new Tuple<string, string>(ups[j].Trim(), id));
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
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

        /// <summary>
        /// Конструктор для загрузки базы кодов из файла excel.
        /// </summary>
        /// <param name="path"></param>
        public Database(string path, bool isCsv, bool multithreading)
        {
            DateTime start = DateTime.Now;
            Excel.Application app = new Excel.Application();
            Excel.Workbook book = app.Workbooks.Open(path);
            Excel.Worksheet sheet = book.Sheets[1];
            try
            {
                int lines = sheet.UsedRange.Rows.Count;
                Thread[] threads = new Thread[6];
                for (int i = 0; i < threads.Length; ++i)
                {
                    threads[i] = new Thread(ReadPairs);
                    if (i != threads.Length - 1)
                        threads[i].Start(new Tuple<Excel.Worksheet, int, int>
                            (sheet, Math.Max(2, lines / threads.Length * i), lines / threads.Length * (i + 1)));
                    else
                        threads[i].Start(new Tuple<Excel.Worksheet, int, int>
                            (sheet, lines / threads.Length * i, lines));
                }
                for (int i = 0; i < threads.Length; ++i)
                    threads[i].Join();
                Console.WriteLine((DateTime.Now - start).TotalSeconds);
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

        private void ReadPairs(object param)
        {
            Tuple<Excel.Worksheet, int, int> tuple = (Tuple<Excel.Worksheet, int, int>)param;
            Excel.Worksheet sheet = tuple.Item1;
            int firstLine = tuple.Item2;
            int lastLine = tuple.Item3;
            for (int i = firstLine; i < lastLine; ++i) 
            {
                var idString = sheet.Cells[i, 1].Value2;
                if (idString == null) break;
                string id = idString.ToString();

                var eanString = sheet.Cells[i, 2].Value2;
                if (eanString != null)
                {
                    string[] ean = eanString.ToString().Split(',');
                    for (int j = 0; j < ean.Length; ++j)
                        if (ean[j] != "")
                            Pairs.Add(new Tuple<string, string>(ean[j], id));
                }

                var upsString = sheet.Cells[i, 3].Value2;
                if (upsString != null)
                {
                    string[] ups = upsString.ToString().Split(',');
                    for (int j = 0; j < ups.Length; ++j)
                        if (ups[j] != "")
                            Pairs.Add(new Tuple<string, string>(ups[j], id));
                }
            }
        }

        public List<string> FindPair(string value)
        {
            List<string> result = new List<string>();
            for(int i = 0; i < Pairs.Count; ++i)
            {
                if (Pairs[i].Item1 == value)
                    result.Add(Pairs[i].Item2);
            }
            return result;
        }

        public void AddNewPair(string barcode, string id)
        {
            Pairs.Add(new Tuple<string, string>(barcode, id));
        }

        public void Save()
        {
            string fileName = $"database{DateTime.Now.Hour},{DateTime.Now.Minute}," +
                $"{DateTime.Now.Day},{DateTime.Now.Month},{DateTime.Now.Year}.csv";
            StreamWriter writer = new StreamWriter(File.Open(fileName, FileMode.Create));
            for(int i = 0; i < Pairs.Count; ++i)
            {
                writer.WriteLine($"{Pairs[i].Item1},{Pairs[i].Item2}");
                writer.Flush();
            }
            writer.Close();
        }
    }
}
