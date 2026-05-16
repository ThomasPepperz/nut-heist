using NutHeist.Core;
using NUnit.Framework;

namespace NutHeist.Tests.EditMode
{
    public sealed class ScaleManagerTests
    {
        [Test]
        public void ToUnits_ScalesByRealMeterToUnit()
        {
            Assert.AreEqual(2.5f, ScaleManager.ToUnits(2.5f));
        }

        [Test]
        public void SquirrelHeights_DividesBySquirrelHeight()
        {
            float h = ScaleManager.SquirrelHeights(ScaleManager.SQUIRREL_HEIGHT);
            Assert.AreEqual(1f, h, 0.0001f);
        }

        [Test]
        public void SquirrelHeights_FenceExample_IsReadableMultiple()
        {
            float fenceInSquirrelHeights = ScaleManager.SquirrelHeights(ScaleManager.FENCE_HEIGHT);
            Assert.That(fenceInSquirrelHeights, Is.GreaterThan(6f));
            Assert.That(fenceInSquirrelHeights, Is.LessThan(8f));
        }
    }
}
