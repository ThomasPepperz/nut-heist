using System.Collections.Generic;
using NutHeist.Progress;
using NUnit.Framework;
using UnityEngine;

namespace NutHeist.Tests.EditMode
{
    /// <summary>Edit Mode: Awake is not guaranteed; tests call <see cref="NutProgress.CollectNut"/> on the instance directly.</summary>
    public sealed class NutProgressTests
    {
        GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
                _host = null;
            }
        }

        [Test]
        public void CollectNut_IncrementsTotalCollected()
        {
            _host = new GameObject("NutProgress_Test");
            var progress = _host.AddComponent<NutProgress>();

            progress.CollectNut();
            progress.CollectNut();

            Assert.AreEqual(2, progress.TotalCollected);
        }

        [Test]
        public void CollectNut_InvokesTotalChanged_WithNewTotal()
        {
            _host = new GameObject("NutProgress_Test");
            var progress = _host.AddComponent<NutProgress>();
            var seen = new List<int>();

            progress.TotalChanged += seen.Add;
            progress.CollectNut();
            progress.CollectNut();

            Assert.AreEqual(new[] { 1, 2 }, seen);
        }
    }
}
