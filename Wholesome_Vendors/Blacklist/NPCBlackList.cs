using System.Collections.Generic;
using WholesomeToolbox;
using WholesomeVendors.Database.Models;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

namespace WholesomeVendors.Blacklist
{
    public static class NPCBlackList
    {
        public static void AddNPCListToBlacklist()
        {
            if (WTPlayer.IsHorde())
                AddNPCToBlacklist(_hordeBlacklist);
            else
                AddNPCToBlacklist(_allianceBlacklist);

            if (ObjectManager.Me.Level > 10)
                AddNPCToBlacklist(new HashSet<int> { 5871, 8307, 3489 }); // starter zone vendors
        }

        public static void AddNPCToBlacklist(int npcId)
        {
            if (!_sessionBlacklist.Contains(npcId))
            {
                _sessionBlacklist.Add(npcId);
            }
        }

        public static void AddNPCToBlacklist(HashSet<int> npcIds)
        {
            foreach (int id in npcIds)
                AddNPCToBlacklist(id);
        }

        public static bool IsVendorValid(ModelCreatureTemplate creatureTemplate)
        {
            bool isPlayerDK = ObjectManager.Me.WowClass == WoWClass.DeathKnight;
            return creatureTemplate.Creature != null
                && !_sessionBlacklist.Contains(creatureTemplate.entry)
                && creatureTemplate.Creature.map == Usefuls.ContinentId
                && creatureTemplate.IsNeutralOrFriendly
                && creatureTemplate.faction != 1555 // Darkmoon Faire
                && (isPlayerDK || creatureTemplate.faction != 2050) // Ebon blade
                && (isPlayerDK || creatureTemplate.faction != 2082) // Ebon blade
                && (isPlayerDK || creatureTemplate.faction != 2083) // Ebon blade
                && GetListUsableZones().Contains(creatureTemplate.Creature.zoneid + 1);
        }

        public static bool IsGameObjectValid(ModelGameObjectTemplate goTemplate)
        {
            return goTemplate.GameObject != null
                && !_sessionBlacklist.Contains(goTemplate.entry)
                && goTemplate.GameObject.map == Usefuls.ContinentId
                && GetListUsableZones().Contains(goTemplate.GameObject.zoneid + 1);
        }


        private static readonly HashSet<int> _hordeBlacklist = new HashSet<int>
    {
        10857, // Neutral in alliance camp
        8305, // Kixxle
        14961, // Neutral vendor in Alliance camp
        15124, // Vendor in Refuge Pointe, unreachable for Horde
        3771, // Vendor inside Alliance, but it´s Horde
        14963, // Gapp Jinglepocket, Neutral in Ashenvale
    };

        private static readonly HashSet<int> _allianceBlacklist = new HashSet<int>
    {
        15125, // Kosco Copperpinch
        2805, // Deneb walker
        3537, // Merchant Supreme Zixil
    };

        private static readonly HashSet<int> _sessionBlacklist = new HashSet<int>
    {
        14637, // Zorbin Fandazzle
        15898, // Event NPC
        23511, // Event NPC
        23605, // Event NPC
        23606, // Event NPC
        24510, // Event NPC
        35342, // Event NPC
        15012, // Event NPC
        36382, // Event NPC
        22264, // Ogri'la Steelshaper
        3180, // Dark Iron Entrepreneur in Wetlands
        34685, // event NPC
        3093, // Grod from TB, detected in Tirisfal Glades
        4085, // Nizzik in StoneTalon, hard to reach
        5134, // NPC died
        543,  // unable to generate path
        198,   // starter trainer without all spells or use minLevel in filter for this
        7952,
        7772, // bugged Vendor
        23533,
        23603,
        23604,
        24494,
        24495,
        24501,
        26309,
        26328, // not prefered           
        26325, // bugged Hunter Trainer
        26332,
        26724,
        26758, // bugged warlocktrainer
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

        private static HashSet<int> GetListUsableZones()
        {
            HashSet<int> listZones = new HashSet<int>();
            foreach (KeyValuePair<int, int> zones in ZoneLevelDictionary)
            {
                if (zones.Value <= ObjectManager.Me.Level)
                {
                    listZones.Add(zones.Key);
                    //Main.Logger("Added: " + zones.Key + " to safe zones");
                }
            }
            return listZones;
        }

        private static readonly Dictionary<int, int> ZoneLevelDictionary = new Dictionary<int, int>
        {
            {465,1}, //AzuremystIsle
            {28,1}, //DunMorogh
            {5,1}, //Durotar
            {31,1}, //Elwynn
            {463,1}, //EversongWoods
            {42,1}, //Teldrassil
            {21,1}, //Tirisfal
            {10,1}, //Mulgore

            {481,5}, //SilvermoonCity
            {342,5}, //Ironforge
            {322,5}, //Ogrimmar
            {302,5}, //Stormwind
            {472,5}, //TheExodar
            {363,5}, //ThunderBluff
            {383,5}, //Undercity
            {382,5}, //Darnassus

            {14,10}, //Kalimdor
            {15,10}, //Azeroth

            {22,10}, //Silverpine
            {36,10}, //LochModan
            {464,10}, //Ghostlands
            {11,10}, //Barrens
            {43,10}, //Darkshore
            {477,10}, //BloodmystIsle
            {40,10}, //Westfall
            {37,15}, //Redridge
            {82,15}, //StonetalonMountains
            {44,18}, //Ashenvale
            {35,18}, //Duskwood
            {25,20}, //Hilsbrad
            {41,20}, //Wetlands
            {62,25}, //ThousandNeedles
            {16,30}, //Alterac
            {17,30}, //Arathi
            {102,30}, //Desolace
            {142,30}, //Dustwallow
            {38,30}, //Stranglethorn
            {18,35}, //Badlands
            {39,35}, //SwampOfSorrows
            {27,40}, //Hinterlands
            {162,40}, //Tanaris
            {122,42}, //Feralas
            {182,45}, //Aszhara
            {20,45}, //BlastedLands
            {29,45}, //SearingGorge
            {183,48}, //Felwood
            {202,48}, //UngoroCrater
            {30,50}, //BurningSteppes
            {23,51}, //WesternPlaguelands
            {24,53}, //EasternPlaguelands
            {282,53}, //Winterspring
            {242,55}, //Moonglade
            {262,55}, //Silithus
            {466,58}, //Hellfire
            {467,60}, //Zangarmarsh
            {479,62}, //TerokkarForest
            {476,65}, //BladesEdgeMountains
            {478,65}, //Nagrand
            {480,67}, //Netherstorm
            {474,67}, //ShadowmoonValley
            {482,65}, //ShattrathCity
            {487,68}, //BoreanTundra
            {32,68}, //DeadwindPass
            {492,68}, //HowlingFjord
            {489,71}, //Dragonblight
            {491,73}, //GrizzlyHills
            {497,75}, //ZulDrak
            {494,76}, //SholazarBasin
            {511,77}, //CrystalsongForest
            {542,77}, //HrothgarsLanding
            {605,77}, //IcecrownCitadel
            {505,80}, //Dalaran
        };
    }
}