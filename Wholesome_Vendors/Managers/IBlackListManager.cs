using System.Collections.Generic;
using WholesomeVendors.Database.Models;

namespace WholesomeVendors.Managers
{
    public interface IBlackListManager : ICycleable
    {
        bool IsVendorValid(ModelCreatureTemplate creatureTemplate);
        bool IsMailBoxValid(ModelGameObjectTemplate goTemplate);
        void AddNPCToBlacklist(int npcId);
        void AddNPCToBlacklist(HashSet<int> npcIds);
    }
}
