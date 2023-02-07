using WholesomeVendors.Database.Models;

namespace WholesomeVendors.Managers
{
    public interface IVendorTimerManager : ICycleable
    {
        ModelCreatureTemplate LastVendorTraveledTo { get; set; }

        void ClearReadies();
        bool IsVendorOnTimer(ModelCreatureTemplate vendorTemplate);
        void AddTimerToPreviousVendor(ModelCreatureTemplate currentVendor);
    }
}
