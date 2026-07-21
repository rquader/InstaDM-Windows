using System.Text.Json.Nodes;
using InstaDM.Core.Navigation;

namespace InstaDM.Core.Tests;

/// <summary>
/// The Node harness (tests/InstaDM.WebHarness.Tests) splices
/// tests/Fixtures/local-spa-harness/policy.default.json into the guard
/// script — the same splice the app performs at runtime with the live
/// payload. This test pins the checked-in fixture to the C# builder's
/// actual output, so the JS-side tests can never silently run against a
/// stale or divergent policy.
/// </summary>
[TestClass]
public sealed class PolicyFixtureDriftTests
{
    [TestMethod]
    public void DefaultPolicyFixture_MatchesBuilderOutput()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "policy.default.json");
        Assert.IsTrue(File.Exists(fixturePath), $"fixture not copied to output: {fixturePath}");

        var fixture = JsonNode.Parse(File.ReadAllText(fixturePath));
        var actual = JsonNode.Parse(PolicyScriptBuilder.BuildPayloadJson(new NavigationPolicy()));

        Assert.IsTrue(JsonNode.DeepEquals(fixture, actual),
            "policy.default.json has drifted from PolicyScriptBuilder output; " +
            "regenerate the fixture from BuildPayloadJson(new NavigationPolicy()).");
    }
}
