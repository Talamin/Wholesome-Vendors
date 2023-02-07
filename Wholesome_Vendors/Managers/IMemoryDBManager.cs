using System.Collections.Generic;
using WholesomeVendors.Database.Models;

namespace WholesomeVendors.Managers
{
    public interface IMemoryDBManager : ICycleable
    {
        List<ModelSpell> GetNormalMounts { get; }
        List<ModelSpell> GetEpicMounts { get; }
        List<ModelSpell> GetFlyingMounts { get; }
        List<ModelSpell> GetEpicFlyingMounts { get; }
        List<ModelItemTemplate> GetBags { get; }
        List<ModelItemTemplate> GetInstantPoisons { get; }
        List<ModelItemTemplate> GetDeadlyPoisons { get; }
        List<ModelItemTemplate> GetAllPoisons { get; }
        List<ModelItemTemplate> GetAllAmmos { get; }
        List<ModelItemTemplate> GetAllFoods { get; }
        List<ModelItemTemplate> GetAllDrinks { get; }

        List<ModelItemTemplate> GetAllUsableDrinks();
        List<ModelItemTemplate> GetAllUsableFoods();
        ModelNpcVendor GetNearestItemVendor(ModelItemTemplate item);
        ModelCreatureTemplate GetNearestSeller();
        ModelCreatureTemplate GetNearestRepairer();
        ModelGameObjectTemplate GetNearestMailBoxFrom(ModelCreatureTemplate npc);
        ModelGameObjectTemplate GetNearestMailBoxFromMe(int range);
        ModelCreatureTemplate GetNearestTrainer();
        ModelSpell GetRidingSpellById(int id);
    }
}
