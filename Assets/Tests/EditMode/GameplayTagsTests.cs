using NutHeist.Core;
using NUnit.Framework;

namespace NutHeist.Tests.EditMode
{
    public sealed class GameplayTagsTests
    {
        [Test]
        public void Tags_AreNonEmptyUnityTagStrings()
        {
            Assert.IsFalse(string.IsNullOrEmpty(GameplayTags.Player));
            Assert.IsFalse(string.IsNullOrEmpty(GameplayTags.Climbable));
            Assert.IsFalse(string.IsNullOrEmpty(GameplayTags.Hazard));
            Assert.IsFalse(string.IsNullOrEmpty(GameplayTags.MovingPlatformTag));
            Assert.IsFalse(string.IsNullOrEmpty(GameplayTags.Nut));
        }

        [Test]
        public void Tags_MatchExpectedIdentifiers()
        {
            Assert.AreEqual("Player", GameplayTags.Player);
            Assert.AreEqual("Climbable", GameplayTags.Climbable);
            Assert.AreEqual("Hazard", GameplayTags.Hazard);
            Assert.AreEqual("MovingPlatform", GameplayTags.MovingPlatformTag);
            Assert.AreEqual("Nut", GameplayTags.Nut);
        }
    }
}
