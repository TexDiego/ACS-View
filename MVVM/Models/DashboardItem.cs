using ACS_View.Enums;
using SQLite;

namespace ACS_View.MVVM.Models
{
    internal class DashboardItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Name = "UX_Item", Order = 1, Unique = true)]
        public DashboardItemType ItemType { get; set; }

        [Indexed(Name = "UX_Item", Order = 2, Unique = true)]
        public int ItemId { get; set; }

        public bool IsVisible { get; set; }

        public int DisplayOrder { get; set; }
    }
}