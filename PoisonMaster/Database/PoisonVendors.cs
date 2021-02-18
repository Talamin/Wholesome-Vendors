using System.Collections.Generic;
using robotManager.Helpful;

public class PoisonVendors
{
    public static List<PoisonNPC> PoisonVendorList { get; private set; }

    private static readonly List<PoisonNPC> PoisonVendor = new List<PoisonNPC>()
    {
        new PoisonNPC(1, new Vector3(111,222,333), "Jonny") // ^^
    };

    public static void ChoosePoisonVendorList()
    {
        PoisonVendorList = PoisonVendor;
    }
}

