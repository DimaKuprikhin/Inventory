using System;
using System.Text;
using System.Windows.Media;

namespace InventoryManager
{
    public class Item
    {
        public string Order { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        
        /// <summary>
        /// В PreviousNumber храним предыдущее значение CurrentNumber для того,
        /// чтобы при изменении количества руками через таблицу, мы могли 
        /// знать, на какое количество изменилось количество товара. В иных
        /// случаях добавление товара, добавляется 1.
        /// Все изменения CurrentNumber записываются в лог и отображаются в 
        /// таблице.
        /// </summary>
        public int PreviousNumber { get; set; } = 0;
        private int currentNumber = 0;
        public int CurrentNumber
        {
            get => currentNumber;
            set
            {
                if (value < 0)
                    return;
                PreviousNumber = currentNumber;
                currentNumber = value;
            }
        }
        public void AddWithLogging(int number)
        {
            if (number == 0)
                return;
            CurrentNumber += number;
            if (Log.Length != 0)
                Log.Append(Environment.NewLine);
            DateTime now = DateTime.Now;
            if (currentNumber > PreviousNumber)
                Log.Append($"Добавлено {currentNumber - PreviousNumber} {now.Hour}:{now.Minute}");
            if (currentNumber < PreviousNumber)
                Log.Append($"Убрано {PreviousNumber - currentNumber} {now.Hour}:{now.Minute}");
            PreviousNumber = currentNumber;
        }

        public int Number { get; set; } = 0;
        public string To { get; set; }
        public string From { get; set; }
        public string Comment { get; set; }
        public StringBuilder Log { get; set; } = new StringBuilder();
        public SolidColorBrush ColorOfRow { get; set; } = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public Item() { }

        public Item(string id, string name, string to, string from)
        {
            Id = id;
            Name = name;
            To = to;
            From = from;
        }
    }
}