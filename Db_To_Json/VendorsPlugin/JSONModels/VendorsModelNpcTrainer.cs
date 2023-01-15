using System.Collections.Generic;

namespace Db_To_Json.VendorsPlugin.JSONModels
{
    internal class VendorsModelNpcTrainer
    {
        public int ID { get; }
        public int SpellID { get; }
        public int MoneyCost { get; }
        public int ReqSkillLine { get; }
        public int ReqSkillRank { get; }
        public int ReqLevel { get; }

        public List<VendorsModelCreatureTemplate> VendorTemplates = new List<VendorsModelCreatureTemplate>();
    }
}
