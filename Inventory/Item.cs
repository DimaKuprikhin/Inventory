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

        public int PreviousNumber = 0;
        private int currentNumber = 0;
        public int CurrentNumber
        {
            get => currentNumber;
            set
            {
                if (value < 0 || value == currentNumber) return;
                if (Log.Length != 0) 
                    Log.Append(Environment.NewLine);
                DateTime now = DateTime.Now;
                if (value > currentNumber)
                    Log.Append($"Добавлено {value - currentNumber} {now.Hour}:{now.Minute} {now.Day}.{now.Month}");
                if (value < currentNumber)
                    Log.Append($"Убрано {currentNumber - value} {now.Hour}:{now.Minute} {now.Day}.{now.Month}");
                PreviousNumber = currentNumber;
                currentNumber = value;
            }
        }
        public int Number { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Comment { get; set; }
        public StringBuilder Log { get; set; } = new StringBuilder();
        public SolidColorBrush ColorOfRow { get; set; } = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public Item() { }
    }
}
