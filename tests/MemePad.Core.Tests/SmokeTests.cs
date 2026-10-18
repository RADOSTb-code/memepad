namespace MemePad.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void Core_assembly_loads()
    {
        Assert.Equal("MemePad.Core", typeof(MemePad.Core.Model.Sound).Assembly.GetName().Name);
    }
}
