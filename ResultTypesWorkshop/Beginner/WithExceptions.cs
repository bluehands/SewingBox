using Beginner.World;
using FluentAssertions;

namespace Beginner;

using ExceptionCastle;

[TestClass]
public sealed class WithExceptions
{
    [TestMethod]
    public void ShaveThePrince()
    {
        var prince = Castle.FindPrince();
        AwakePrince? awakePrince = null;
        if (prince is SleepingPrince sleepingPrince)
        {
            try
            {
                awakePrince = sleepingPrince.Awake();
            }
            catch (Exception e)
            {
                Insta.Post($"You won't believe it.... : {e.Message}");
            }
        }
        else
            awakePrince = (AwakePrince)prince;

        if (awakePrince != null)
        {
            var razor = Castle.FindRazor();
            if (razor != null)
            {
                var beautifulPrince = awakePrince.Shave(razor);
                var picture = beautifulPrince.TakePicture();
                Insta.Post(picture);
            }
            else
            {
                Insta.Post("You won't believe it....: Razor missing in castle!!!");
            }
        }


        Insta.Posts.Should().BeGreaterThan(0);
    }
}