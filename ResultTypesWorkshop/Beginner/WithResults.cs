using Beginner.World;
using FluentAssertions;

namespace Beginner;

using ResultCastle;

[TestClass]
public sealed class WithResults
{
    [TestMethod]
    public void ShaveThePrince()
    {
        Insta.Posts.Should().BeGreaterThan(0);
    }
}