using System.Collections.Generic;
using robotManager.Helpful;
using wManager.Wow.Enums;

public class PoisonVendors
{
    public static List<PoisonNPC> PoisonVendorList { get; private set; }

    private static readonly List<PoisonNPC> PoisonVendor = new List<PoisonNPC>()
    {
        new PoisonNPC(1326, new Vector3(-11851.2, 641.523, 46.1372), (ContinentId)0, "Sloan McCoy", "Poison Supplies"),
        new PoisonNPC(3090, new Vector3(-12085.4, -2553.96, -35.0681), (ContinentId)0, "Gerald Crawley", "Poison Supplies"),
        new PoisonNPC(3135, new Vector3(-5428.15, -164.489, 351.642), (ContinentId)0, "Malissa", "Poison Supplies"),
        new PoisonNPC(3334, new Vector3(-7579.89, -1280.52, 245.605), (ContinentId)0, "Rekkul", "Poison Supplies"),
        new PoisonNPC(3490, new Vector3(1942.63, 6605.49, 143.986), (ContinentId)530, "Hula'mahi", "Reagents, Herbs & Poison Supplies"),
        new PoisonNPC(3542, new Vector3(-3781.52, -12073.5, 7.09626), (ContinentId)530, "Jaysin Lanyda", "Poisons & Reagents"),
        new PoisonNPC(3551, new Vector3(7662.56, -1562.82, 968.522), (ContinentId)571, "Patrice Dwyer", "Poison Supplies"),
        new PoisonNPC(3561, new Vector3(-5721.43, -225.057, 354.432), (ContinentId)0, "Kyrai", "Poison Supplies"),
        new PoisonNPC(4585, new Vector3(-7913.1, -919.873, 136.472), (ContinentId)0, "Ezekiel Graves", "Poison Supplies"),
        new PoisonNPC(5139, new Vector3(-10548.8, 444.955, 37.6471), (ContinentId)0, "Kurdrum Barleybeard", "Reagents & Poison Supplies"),
        new PoisonNPC(6779, new Vector3(-6799.06, -1092.28, 243.978), (ContinentId)0, "Smudge Thunderwood", "Poison Supplies"),
        new PoisonNPC(10364, new Vector3(585.429, -4653.6, 29.3109), (ContinentId)1, "Yaelika Farclaw", "Reagents & Poison Supplies"),
        new PoisonNPC(16268, new Vector3(-214.404, 2146.14, 80.9466), (ContinentId)33, "Eralan", "Poison Supplies"),
        new PoisonNPC(16683, new Vector3(870.975, -91.2215, 34.4972), (ContinentId)0, "Darlia", "Poison Supplies"),
        new PoisonNPC(19013, new Vector3(814.853, 686.977, 53.6461), (ContinentId)0, "Vanteg", "Reagents & Poison Supplies"),
        new PoisonNPC(19014, new Vector3(871.699, 692.057, 53.6579), (ContinentId)0, "Ogir", "Reagents & Poison Supplies"),
        new PoisonNPC(19049, new Vector3(602.256, 393.83, 30.5363), (ContinentId)0, "Karokka", "Poison Supplies"),
        new PoisonNPC(20081, new Vector3(1450.51, -2796.33, 144.393), (ContinentId)1, "Bortega", "Reagents & Poison Supplies"),
        new PoisonNPC(20121, new Vector3(-1977.61, -2129.67, 91.7917), (ContinentId)1, "Fingin", "Poison Supplies"),
        new PoisonNPC(22479, new Vector3(-8315.63, -3644.79, 16.1387), (ContinentId)1, "Sab'aoth", "Reagents & Poison Supplies"),
        new PoisonNPC(3559, new Vector3(-5651.38, -262.833, 372.083), (ContinentId)0, "Temp Poisoning Vendor Dwarf", "Poison Supplies"),
        new PoisonNPC(16754, new Vector3(731.766, -862.035, 165.141), (ContinentId)0, "Fingle Dipswitch", "Poison Supplies"),
        new PoisonNPC(21642, new Vector3(-4239.1, -856.807, -54.7915), (ContinentId)1, "Alrumi", "Reagents & Poison Supplies"),
        new PoisonNPC(22652, new Vector3(-8123.05, -2253.33, 12.2517), (ContinentId)1, "Kurdrum Barleybeard (1)", "Reagents & Poison Supplies"),
        new PoisonNPC(22660, new Vector3(-8516.75, -2516.04, 43.5695), (ContinentId)1, "Yaelika Farclaw (1)", "Reagents & Poison Supplies"),
        new PoisonNPC(23145, new Vector3(-8504.63, -2861.31, 10.4551), (ContinentId)1, "Rumpus", "Reagents & Poison Supplies"),
        new PoisonNPC(3558, new Vector3(7690.04, -1594.69, 965.279), (ContinentId)571, "[UNUSED] Temp Poisoning Vendor Undead", "Poison Supplies"),
        new PoisonNPC(25043, new Vector3(-1880.65, 7339.34, -21.0874), (ContinentId)530, "Sereth Duskbringer", "Poison Supplier"),
        new PoisonNPC(24357, new Vector3(-7230.64, -1586.4, -270.174), (ContinentId)1, "Maethor Skyshadow", "Poison & Reagents"),
        new PoisonNPC(23732, new Vector3(-6680.83, -617.397, -269.569), (ContinentId)1, "Sorely Twitchblade", "Poison Supplier"),
        new PoisonNPC(24148, new Vector3(-7069.64, -1505.92, -261.936), (ContinentId)1, "David Marks", "Poison Vendor"),
        new PoisonNPC(24313, new Vector3(-8111.6, -1200.64, -336.83), (ContinentId)1, "Celina Summers", "Reagents and Poisons"),
        new PoisonNPC(24349, new Vector3(-6863.86, -1978.84, -271.559), (ContinentId)1, "Jessica Evans", "Reagents and Poisons"),
        new PoisonNPC(25312, new Vector3(-752.45, -545.726, -26.9532), (ContinentId)1, "Cel", "Reagent and Poison Vendor"),
        new PoisonNPC(25736, new Vector3(-374.643, 237.531, -67.0627), (ContinentId)48, "Supply Master Taz'ishi", "Poison & Reagents"),
        new PoisonNPC(26382, new Vector3(-1605.31, -1253.39, 134.487), (ContinentId)1, "Balfour Blackblade", "Reagents and Poisons"),
        new PoisonNPC(26568, new Vector3(-2328.49, -382.269, -7.89994), (ContinentId)1, "Zebu'tan", "Herbalism & Poison Supplies"),
        new PoisonNPC(26598, new Vector3(-1926.67, 439.294, 133.715), (ContinentId)1, "Mistie Flitterdawn", "Reagents and Poisons"),
        new PoisonNPC(26900, new Vector3(-2873.88, -264.709, 54.0072), (ContinentId)1, "Tinky Stabberson", "Poison & Reagent Supplies"),
        new PoisonNPC(26945, new Vector3(-1506.08, 2876.04, 92.2821), (ContinentId)1, "Zend'li Venomtusk", "Poison Supplies"),
        new PoisonNPC(26950, new Vector3(-1341.33, 2712.64, 93.7684), (ContinentId)1, "Sanut Swiftspear", "Reagents and Poisons"),
        new PoisonNPC(27031, new Vector3(-983.877, 950.558, 92.7051), (ContinentId)1, "Apothecary Rose", "Alchemy & Poison Supplies"),
        new PoisonNPC(27038, new Vector3(-1063.02, 913.021, 91.842), (ContinentId)1, "Drolfy", "Alchemy & Poison Supplies"),
        new PoisonNPC(27053, new Vector3(-875.69, 1047.52, 91.6211), (ContinentId)1, "Lanus Longleaf", "Herbalism & Poison Supplies"),
        new PoisonNPC(27089, new Vector3(-1916.97, 1022.23, 90.6896), (ContinentId)1, "Saffron Reynolds", "Poison Supplies"),
        new PoisonNPC(27133, new Vector3(-2111.4, 2656.51, 60.4208), (ContinentId)1, "Seer Yagnar", "Poison & Reagents"),
        new PoisonNPC(27149, new Vector3(-2211.93, 2685.67, 60.3135), (ContinentId)1, "Arrluk", "Poison & Reagents"),
        new PoisonNPC(27176, new Vector3(-2258.05, 2677.96, 63.0635), (ContinentId)1, "Mystic Makittuq", "Poison & Reagents"),
        new PoisonNPC(27186, new Vector3(-2162.96, 1359.47, 79.6642), (ContinentId)1, "Oogrooq", "Poison & Reagents"),
        new PoisonNPC(28347, new Vector3(-284.851, 807.462, 91.0039), (ContinentId)1, "Miles Sidney", "Poison Supplies"),
        new PoisonNPC(28832, new Vector3(-786.96, 1460.36, 91.692), (ContinentId)1, "Chin'ika", "Poison Supplier"),
        new PoisonNPC(28869, new Vector3(-1818.57, 2331.53, 65.173), (ContinentId)1, "Deathdrip", "Poison Supplier"),
        new PoisonNPC(29015, new Vector3(-881.608, 785.112, 140.225), (ContinentId)1, "Shaman Partak", "Reagents & Poisons"),
        new PoisonNPC(29037, new Vector3(-1049.84, 1385.91, 63.6663), (ContinentId)1, "Soo-jam", "Reagents & Poisons"),
        new PoisonNPC(29339, new Vector3(-14.5638, -693.197, -19.2577), (ContinentId)1, "Apothecary Tepesh", "Alchemy & Poison Supplies"),
        new PoisonNPC(29348, new Vector3(94.5783, -482.316, 15.8381), (ContinentId)1, "Apothecary Chaney", "Alchemy & Poison Supplies"),
        new PoisonNPC(29535, new Vector3(1045.58, 112.173, 15.9995), (ContinentId)1, "Alchemist Cinesra", "Poison Vendor"),
        new PoisonNPC(29909, new Vector3(2035.3, 813.816, 34.8428), (ContinentId)0, "Nilika Blastbeaker", "Poisons, Reagents & Alchemical Supplies"),
        new PoisonNPC(29922, new Vector3(2490.99, 1455.66, 5.48871), (ContinentId)0, "Corig the Cunning", "Poisons & Reagents"),
        new PoisonNPC(29947, new Vector3(1509.44, 779.212, 135.238), (ContinentId)1, "Apothecary Maple", "Poisons & Reagents"),
        new PoisonNPC(29961, new Vector3(1404.94, 1026.07, 183.011), (ContinentId)1, "Brangrimm", "Poisons & Reagents"),
        new PoisonNPC(29968, new Vector3(1537.03, 540.094, 171.952), (ContinentId)1, "Hapanu Coldwind", "Poisons & Reagents"),
        new PoisonNPC(30010, new Vector3(2518.23, 1290.1, 273.199), (ContinentId)1, "Fylla Ingadottir", "Poisons & Reagents"),
        new PoisonNPC(30069, new Vector3(-296.946, 140.831, -46.5533), (ContinentId)70, "Initiate Roderick", "Poisons & Reagents"),
        new PoisonNPC(30239, new Vector3(-432.132, 230.477, -211.508), (ContinentId)90, "Alanura Firecloud", "Poisons & Reagents"),
        new PoisonNPC(30244, new Vector3(-545.239, 274.565, -207.823), (ContinentId)90, "Miura Brightweaver", "Poisons & Reagents"),
        new PoisonNPC(30306, new Vector3(-823.79, 401.27, -316.433), (ContinentId)90, "Bileblow", "Poisons"),
        new PoisonNPC(30438, new Vector3(-4424.5, -4243.94, -5.50659), (ContinentId)1, "Supply Officer Thalmers", "Poisons, Reagents & Trade Supplies"),
        new PoisonNPC(32028, new Vector3(2310.22, 398.027, 33.8948), (ContinentId)0, "Kurdrum Barleybeard (2)", "Reagents & Poison Supplies"),
        new PoisonNPC(32765, new Vector3(3685.9, 1185.94, -45.0512), (ContinentId)1, "Yaelika Farclaw (2)", "Reagents & Poison Supplies"),
        new PoisonNPC(37348, new Vector3(4893.95, 340.619, 38.0942), (ContinentId)1, "Kurdrum Barleybeard (3)", "Reagents & Poison Supplies"),
        new PoisonNPC(37485, new Vector3(6796.29, -662.288, 89.3403), (ContinentId)1, "Yaelika Farclaw (3)", "Reagents & Poison Supplies"),
    };

    public static void ChoosePoisonVendorList()
    {
        PoisonVendorList = PoisonVendor;
    }
}

