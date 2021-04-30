using System.Collections.Generic;

public static class NPCBlackList
{
    public static void AddNPCListToBlacklist(IEnumerable<int> ids)
    {
        foreach (var id in myBlacklist)
            AddNPCToBlacklist(id);
    }

    public static void AddNPCToBlacklist(int npcId)
    {
        if (!myBlacklist.Contains(npcId))
        {
            myBlacklist.Add(npcId);
            Main.Logger("Added to NPC blacklist: " + npcId);
        }
    }

    public static readonly HashSet<int> OnlyFoodBlacklist = new HashSet<int>
    {
            3312, //only Meat Vendor
            3342, // only food Vendor
            3329, //only Mushrooms
            3368, //only meat
            3547, //Mushrooms only
            3480, // Only Bread   
    };

    public static readonly HashSet<int> myBlacklist = new HashSet<int>
    {
        8305, // Kixxle
        14961, // Neutral vendor in Alliance camp
        4085, // Nizzik in StoneTalon, hard to reach
        15124, // Vendor in Refuge Pointe, unreachable for Horde
        5134, //NPC died
        543,  //unable to generate path
        198,   //starter trainer without all spells or use minLevel in filter for this
        3771, //Vendor inside Alliance, but it´s Horde
        7952,
        7772, //bugged Vendor
        23533,
        23603,
        23604,
        24494,
        24495,
        24501,
        26309,
        26328, //not prefered           
        26325, //bugged Hunter Trainer
        26332,
        26724,
        26758, //bugged warlocktrainer
        26738,
        26739,
        26740,
        26741,
        26742,
        26743,
        26744,
        26745,
        26746,
        26747,
        26748,
        26765,
        26749,
        26751,
        26752,
        26753,
        26754,
        26755,
        26756,
        26757,
        26759,
        5958, // Portal trainer
        5957, // Portal Trainer
        2492, // Portal Trainer
        2485, // Portal Trainer
        2489, // Portal Trainer
        4165, // Portal Trainer
        5783,
    };
}