using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

[TestClass]
public sealed class SmokeTests
{
    [TestMethod]
    public void SurfaceTaxonomy_ContainsFailClosedDefault()
    {
        // Malformed must be the first (default) enum member so an
        // uninitialized surface can never accidentally read as allowed.
        Assert.AreEqual(InstagramSurface.Malformed, default(InstagramSurface));
    }
}
