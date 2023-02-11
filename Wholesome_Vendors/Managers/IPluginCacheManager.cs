using System.Collections.Generic;
using WholesomeVendors.Database.Models;
using WholesomeVendors.Utils;
using wManager.Wow.Enums;

namespace WholesomeVendors.Managers
{
    public interface IPluginCacheManager : ICycleable
    {
        string RangedWeaponType { get; }
        List<int> KnownMountSpells { get; }
        bool IsInInstance { get; }
        List<WVItem> ItemsToSell { get; }
        List<WVItem> ItemsToMail { get; }
        bool InLoadingScreen { get; }
        List<WVItem> BagItems { get; }
        int EmptyContainerSlots { get; }
        int RidingSkill { get; }
        int Money { get; }
        bool IsInBloodElfStartingZone { get; }
        bool IsInDraeneiStartingZone { get; }
        bool IsInOutlands { get; }
        int NbFreeSlots { get; }
        List<ModelItemTemplate> UsableAmmos { get; }
        int NbAmmosInBags { get; }
        int NbDrinksInBags { get; }
        int NbFoodsInBags { get; }
        int NbDeadlyPoisonsInBags { get; }
        int NbInstantPoisonsInBags { get; }
        List<(SkillLine, int)> WeaponsSpellsToLearn { get; }
        List<string> KnownSkills { get; }

        void RecordKnownMounts();
        bool HaveEnoughMoneyFor(int amount, ModelItemTemplate item);
        void SetItemToUnMailable(WVItem umItem);

    }
}
