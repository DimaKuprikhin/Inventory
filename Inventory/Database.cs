using System;
using System.Threading;
using System.Linq;
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
        public Database(string path)
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

        public List<string> FindPair(string value)
        {
            return Pairs.FindAll(p => p.Item1 == value).Select(p => p.Item2).ToList();
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
