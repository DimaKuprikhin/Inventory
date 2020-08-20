using System.Windows.Media;

namespace InventoryManager
{

    public class Item
    {
        public string Order { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public int CurrentNumber { get; set; }
        public int Number { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public SolidColorBrush ColorOfRow { get; set; } = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

        public Item() { }
    }
}
