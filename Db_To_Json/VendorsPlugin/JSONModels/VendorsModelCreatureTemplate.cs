namespace Db_To_Json.VendorsPlugin.JSONModels
{
    internal class VendorsModelCreatureTemplate
    {
        public int entry { get; }
        public string name { get; }
        public string subname { get; }
        public uint faction { get; }
        public int minLevel { get; }
        public int maxLevel { get; }

        public VendorsModelCreature Creature;
    }
}
