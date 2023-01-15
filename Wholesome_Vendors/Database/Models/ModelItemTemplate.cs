using System.Collections.Generic;

namespace WholesomeVendors.Database.Models
{
    public class ModelItemTemplate
    {
        public int Entry { get; set; }
        public string Name { get; set; }
        public int FoodType { get; set; }
        public int BuyPrice { get; set; }
        public int BuyCount { get; set; }
        public int Subclass { get; set; }
        public int RequiredLevel { get; set; }
        public int displayid { get; set; }
        public int ContainerSlots { get; set; }
        public int AllowableRace { get; set; }

        public List<ModelNpcVendor> VendorsSellingThisItem { get; set; }
    }
}
