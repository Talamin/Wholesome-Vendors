namespace WholesomeVendors.Utils
{
    public class WVItem
    {
        public string Name { get; }
        public string ItemLink { get; }
        public int Quality { get; }
        public int ILevel { get; }
        public int ReqLevel { get; }
        public string Class { get; }
        public string SubClass { get; }
        public int MaxStack { get; }
        public string EquipSlot { get; }
        public string Texture { get; }
        public int SellPrice { get; }
        public int Entry { get; }
        public int InBag { get; }
        public int InSlot { get; }
        public int Count { get; }

        public WVItem(string itemInfos)
        {
            string[] infos = itemInfos.Split('£');
            Name = infos[0];
            ItemLink = infos[1];
            Quality = int.Parse(infos[2]);
            ILevel = int.Parse(infos[3]);
            ReqLevel= int.Parse(infos[4]);
            Class = infos[5];
            SubClass = infos[6];
            MaxStack= int.Parse(infos[7]);
            EquipSlot = infos[8];
            Texture = infos[9];
            SellPrice= int.Parse(infos[10]);
            Entry = int.Parse(infos[11]);
            InBag= int.Parse(infos[12]);
            InSlot= int.Parse(infos[13]);
            Count = int.Parse(infos[14]);
        }

        public bool IsEquippable => EquipSlot != null && EquipSlot.Length > 0;
    }
}
