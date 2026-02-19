using ACS_View.Enums;

namespace ACS_View.MVVM.Models.DTOs
{
    internal class DashboardItemResult
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public DashboardItemType ItemType { get; set; }
        public string Name { get; set; }
        public int Total { get; set; }
        public int DisplayOrder { get; set; }
    }
}