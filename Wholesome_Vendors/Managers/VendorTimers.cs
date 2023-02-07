using robotManager.Helpful;
using System.Collections.Generic;
using WholesomeVendors.Database.Models;

namespace WholesomeVendors.Managers
{
    /*
     * This is used to avoid back and forth between vendors
     * When going to a vendor, we set it on a timer so it cannot be accessed again
     */

    public class VendorTimerManager : IVendorTimerManager
    {
        private List<VendorTimer> _allTimers = new List<VendorTimer>();
        public ModelCreatureTemplate LastVendorTraveledTo { get; set; }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        public void ClearReadies() => _allTimers.RemoveAll(vt => vt.Timer.IsReady);

        public bool IsVendorOnTimer(ModelCreatureTemplate vendorTemplate)
        {
            VendorTimer vendorTimer = _allTimers.Find(vt => vt.Vendor.entry == vendorTemplate.entry);
            return vendorTimer != null && !vendorTimer.Timer.IsReady;
        }

        public void AddTimerToPreviousVendor(ModelCreatureTemplate currentVendor)
        {
            if (LastVendorTraveledTo == null || LastVendorTraveledTo.entry != currentVendor.entry)
            {
                if (LastVendorTraveledTo != null)
                {
                    VendorTimer timer = _allTimers.Find(vt => vt.Vendor.entry == LastVendorTraveledTo.entry);
                    if (timer == null)
                    {
                        _allTimers.Add(new VendorTimer(LastVendorTraveledTo));
                    }
                    else
                    {
                        timer.IncrementTimer();
                    }
                }
                LastVendorTraveledTo = currentVendor;
            }
        }
    }

    public class VendorTimer
    {
        private int _time = 60000;
        public ModelCreatureTemplate Vendor { get; private set; }
        public Timer Timer { get; private set; }

        public VendorTimer(ModelCreatureTemplate vendor)
        {
            Vendor = vendor;
            Timer = new Timer(_time);
        }

        public void IncrementTimer()
        {
            _time += 60000;
            Timer = new Timer(_time);
        }
    }
}
