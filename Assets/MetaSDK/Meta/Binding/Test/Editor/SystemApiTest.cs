// Copyright © 2018, Meta Company.  All rights reserved.
// 
// Redistribution and use of this software (the "Software") in binary form, without modification, is 
// permitted provided that the following conditions are met:
// 
// 1.      Redistributions of the unmodified Software in binary form must reproduce the above 
//         copyright notice, this list of conditions and the following disclaimer in the 
//         documentation and/or other materials provided with the distribution.
// 2.      The name of Meta Company (“Meta”) may not be used to endorse or promote products derived 
//         from this Software without specific prior written permission from Meta.
// 3.      LIMITATION TO META PLATFORM: Use of the Software is limited to use on or in connection 
//         with Meta-branded devices or Meta-branded software development kits.  For example, a bona 
//         fide recipient of the Software may incorporate an unmodified binary version of the 
//         Software into an application limited to use on or in connection with a Meta-branded 
//         device, while he or she may not incorporate an unmodified binary version of the Software 
//         into an application designed or offered for use on a non-Meta-branded device.
// 
// For the sake of clarity, the Software may not be redistributed under any circumstances in source 
// code form, or in the form of modified binary code – and nothing in this License shall be construed 
// to permit such redistribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
// INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED.  IN NO EVENT SHALL META COMPANY BE LIABLE FOR ANY DIRECT, 
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, 
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
using NUnit.Framework;
using System;
using types.fbs;
using MetaVariable = Meta.Interop.MetaCoreInterop.MetaVariable;

namespace Meta.Tests.SystemApi
{
    [TestFixture]
    [Ignore("Interaction with CoCo at this point is broken in Unit Test")]
    public class CalibrationSystemApiTests
    {
        bool systemStarted = false;

        public CalibrationSystemApiTests()
        {
            DllTools.AddPathVariable(Environment.ExpandEnvironmentVariables("%META_CORE%"));
            DllTools.AddPathVariable(Environment.ExpandEnvironmentVariables("%META_INTERNAL_UNITY_SDK%")
                                      + DllTools.DirSep()
                                      + "Plugins"
                                      + DllTools.DirSep()
                                      + "x86_64");
        }

        [SetUp]
        public void SetUpSystemTest()
        {
            systemStarted = true;
            string loadCalibDataAndSerialData =
                Environment.ExpandEnvironmentVariables("%META_INTERNAL_CONFIG%") +
                DllTools.DirSep() + "device" + DllTools.DirSep() + "calibration_data_integration_test.json";
            systemStarted = Plugin.SystemApi.Start(loadCalibDataAndSerialData);

            // Abort test if system didn't start.
            if (!systemStarted)
            {
                Assert.Fail("System failed to start. ");
                return;
            }
        }


        [TearDown]
        public void TearDownSystemTest()
        {
            if (systemStarted)
            {
                Plugin.SystemApi.Stop();
            }
        }


        [Test]
        public void CanStartSuccessfully()
        {
            Assert.IsTrue(systemStarted);
        }


        [Test]
        public void GetSerialNumberData()
        {
            var serialNumber = Plugin.SystemApi.GetSerialNumber();
            string expectedSerialNumberData = "META2354916001071";
            Assert.AreEqual(expectedSerialNumberData, serialNumber);
        }


        [Test]
        public void GetCalibrationString()
        {
            Meta.Plugin.NodeLoaderApiProcessor nodeLoaderApiProcessor = new Meta.Plugin.NodeLoaderApiProcessor();

            UnityEngine.Matrix4x4 expectedPose = new UnityEngine.Matrix4x4();

            expectedPose.m00 = 1.00000f;
            expectedPose.m01 = 0.00049f;
            expectedPose.m02 = -0.00142f;
            expectedPose.m03 = -0.02710f;

            expectedPose.m10 = 0.00017f;
            expectedPose.m11 = 0.90275f;
            expectedPose.m12 = 0.43016f;
            expectedPose.m13 = -0.03307f;

            expectedPose.m20 = 0.00149f;
            expectedPose.m21 = -0.43016f;
            expectedPose.m22 = 0.90275f;
            expectedPose.m23 = 0.09777f;

            expectedPose.m30 = 0.00000f;
            expectedPose.m31 = 0.00000f;
            expectedPose.m32 = 0.00000f;
            expectedPose.m33 = 1.00000f;


            var profiles = nodeLoaderApiProcessor.Load();
            var relativePose = profiles["rgb"].RelativePose;
            Assert.IsTrue((relativePose.inverse * expectedPose).isIdentity);
        }


        [Test]
        [Ignore("Get Pose is not stable. Race condition exists, where we can ask the data before its ready.")]
        public void GetPose()
        {
            const int kBuffMaxSize = 4000;
            byte[] buffer = new byte[kBuffMaxSize];
            PoseType poseType = new PoseType();

            Plugin.SystemApi.GetPose("depth", "rgb", ref buffer, out poseType);

            double expectedPositionX = -0.02749644;
            double expectedPositionY = 0.001773831;
            double expectedPositionZ = 0.001428126;

            Assert.AreEqual(expectedPositionX, poseType.Position.Value.X);
            Assert.AreEqual(expectedPositionY, poseType.Position.Value.Y);
            Assert.AreEqual(expectedPositionZ, poseType.Position.Value.Z);
        }


        [Test]
        [Ignore("Get Pose is not stable. Race condition exists, where we can ask the data before its ready.")]
        public void GetPoseBadNodes()
        {
            const int kBuffMaxSize = 4000;
            byte[] buffer = new byte[kBuffMaxSize];
            PoseType poseType = new PoseType();

            Plugin.SystemApi.GetPose("depth", "color", ref buffer, out poseType);

            double expectedPositionX = 0.0;
            double expectedPositionY = 0.0;
            double expectedPositionZ = 0.0;

            Assert.AreEqual(expectedPositionX, poseType.Position.Value.X);
            Assert.AreEqual(expectedPositionY, poseType.Position.Value.Y);
            Assert.AreEqual(expectedPositionZ, poseType.Position.Value.Z);
        }
    }

    [TestFixture]
    [Ignore("Interaction with CoCo at this point is broken in Unit Test")]
    public class PathApiTests
    {
        static string loadCalibDataAndSerialData;
        public PathApiTests()
        {
            DllTools.AddPathVariable(Environment.ExpandEnvironmentVariables("%META_CORE%"));
            DllTools.AddPathVariable(Environment.ExpandEnvironmentVariables("%META_INTERNAL_UNITY_SDK%")
                                      + DllTools.DirSep()
                                      + "Plugins"
                                      + DllTools.DirSep()
                                      + "x86_64");
            loadCalibDataAndSerialData =
                Environment.ExpandEnvironmentVariables("%META_INTERNAL_CONFIG%") +
                DllTools.DirSep() + "device" + DllTools.DirSep() + "calibration_data_integration_test.json";
        }


        [TearDown]
        public void TearDownSystemTest()
        {
            Plugin.SystemApi.Stop();
        }


        [Test]
        [Ignore("Ignoring production test till we find out how to test this on CI")]
        public void GetPathProduction()
        {
            GetPathTest(false);
        }


        [Test]
        public void GetPathDevelopment()
        {
            GetPathTest(true);
        }


        private static void GetPathTest(bool is_development_environment)
        {
            bool systemStarted = Plugin.SystemApi.Start(loadCalibDataAndSerialData, is_development_environment);

            // Abort test if system didn't start.
            if (!systemStarted)
            {
                Assert.Fail();
                return;
            }

            MetaVariable[] variables =
                new MetaVariable[] { MetaVariable.META_3RDPARTY, MetaVariable.META_APP_DATA, MetaVariable.META_BUILD,
                                     MetaVariable.META_CACHE, MetaVariable.META_CACHE_DEBUG,
                                     MetaVariable.META_CACHE_RELEASE,
                                     MetaVariable.META_CONFIG, MetaVariable.META_CORE, MetaVariable.META_DRIVER,
                                     MetaVariable.META_INSTALL,
                                     MetaVariable.META_RECORDING, MetaVariable.META_TESTING_DATA,
                                     MetaVariable.META_TOOLS,
                                     MetaVariable.META_USB, MetaVariable.META_USER_DATA };
            foreach (var v in variables)
            {
                string result = string.Empty;
                if (Plugin.SystemApi.GetPath(v, out result))
                {
                    // UnityEngine.Debug.Log( v.ToString() + ": " + result );
                    Assert.IsNotEmpty(result, v + " : should not be empty.");
                }
                else
                {
                    Assert.IsEmpty(result, v + " : should be empty.");
                }
            }
        }
    }
}
