using HomeFix.Core.Devices;

namespace HomeFix.Core.Workshop
{
    /// <summary>
    /// A test applied to one device; <see cref="ServiceWorkshop.FindAll"/> keeps the devices for
    /// which it returns true. Any method or lambda of this shape can be passed in.
    /// </summary>
    /// <param name="device">The device to test.</param>
    public delegate bool DeviceFilter(HomeDevice device);
}
