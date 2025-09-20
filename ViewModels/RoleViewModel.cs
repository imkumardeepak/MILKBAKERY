using System.Collections.Generic;

namespace Milk_Bakery.ViewModels
{
    public class RoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public List<MenuItemViewModel> MenuItems { get; set; }
    }


}