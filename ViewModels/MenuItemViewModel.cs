using System.Collections.Generic;

namespace Milk_Bakery.ViewModels
{
    public class MenuItemViewModel
    {
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public bool HasAccess { get; set; }
        public List<MenuItemViewModel> Children { get; set; }
    }
}