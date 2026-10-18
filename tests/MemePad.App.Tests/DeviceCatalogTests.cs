using MemePad.App.Audio;

namespace MemePad.App.Tests;

public class DeviceCatalogTests
{
    [Fact]
    public void Catalog_created_on_ui_thread_works_from_timer_thread()
    {
        // The app creates the catalog on the WPF (STA) thread, but reconnects run on thread-pool (MTA) threads.
        DeviceCatalog? catalog = null;
        var sta = new Thread(() => catalog = new DeviceCatalog());
        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();

        Exception? error = null;
        var mta = new Thread(() =>
        {
            try
            {
                _ = catalog!.FindCableInputId();
                _ = catalog.Microphones();
                _ = catalog.GetMixFormat(null);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        mta.SetApartmentState(ApartmentState.MTA);
        mta.Start();
        mta.Join();

        Assert.Null(error);
    }
}
