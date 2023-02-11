using System.Collections.Generic;

namespace Db_To_Json.VendorsPlugin.JSONModels
{
    internal class VendorsModelSpell
    {
        public VendorsModelItemTemplate AssociatedItem;
        public VendorsModelNpcTrainer NpcTrainer;
        public List<VendorsModelNpcTrainer> NpcTrainers;

        public int Id { get; }
        public int effectBasePoints_2 { get; }
        public string name_lang_1 { get; }
    }
}
