using System.Collections.Generic;

namespace Db_To_Json.VendorsPlugin.JSONModels
{
    internal class VendorsModelItemTemplate
    {
        public int Entry { get; }
        public string Name { get; }
        public int FoodType { get; }
        public int BuyPrice { get; }
        public int BuyCount { get; }
        public int Subclass { get; }
        public int RequiredLevel { get; }
        public int displayid { get; }
        public int ContainerSlots { get; }
        public int AllowableRace { get; }

        public List<ModelNpcVendor> VendorsSellingThisItem = new List<ModelNpcVendor>();
    }
}
