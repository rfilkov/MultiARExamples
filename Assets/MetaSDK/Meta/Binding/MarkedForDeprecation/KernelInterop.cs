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
using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;


namespace Meta._Deprecated.Binding
{
    public class KernelInterop
    {
        /// <summary>
        /// DLL call to start the camera, IMU and the Metavision DLL
        /// </summary>
        [DllImport("MetaVisionDLL", EntryPoint = "initMetaVisionCamera")]
        public static extern int InitMetaVisionCamera(DataAcquisitionSystem iDAQ, ref DeviceInfo cameraInfo, IMUModel imuModel);

        public static int InitMetaVisionCamera()
        {
            DeviceInfo deviceInfo = new DeviceInfo();
            return InitMetaVisionCamera(DataAcquisitionSystem.DVT351, ref deviceInfo, IMUModel.UnknownIMU);
        }

        ///<summary>
        /// DLL call to stop the Metavision DLL, camera and IMU
        ///</summary>
        [DllImport("MetaVisionDLL", EntryPoint = "deinitMeta")]
        public static extern void DeinitMeta();

        /// <summary>
        /// Enables the virtual webcam feed. 
        /// </summary>
        [DllImport("MetaVisionDLL", EntryPoint = "enableVirtualWebcam")]
        public static extern void EnableVirtualWebcam();

        /// <summary>   Handler, called when the new data. </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void NewDataHandler();

        /// <summary>   Builds hand consumer. </summary>
        /// <param name="handConsumerType"> Type of the hand consumer. </param>
        /// <returns>   true if it succeeds, false if it fails. </returns>
        [DllImport("MetaVisionDLL", EntryPoint = "BuildHandConsumer")]
        internal static extern bool BuildHandConsumer(string handConsumerType);

        public static bool BuildHandConsumer()
        {
            return BuildHandConsumer("META1LEGACY_NOHANDPROCESSOR");
        }

        /// <summary>   Gets sensor meta data. </summary>
        /// <param name="sensorMetaData">   Information describing the sensor meta. </param>
        /// <returns>   true if it succeeds, false if it fails. </returns>
        [DllImport("MetaVisionDLL", EntryPoint = "GetSensorMetaData")]
        public static extern bool GetSensorMetaData(ref SensorMetaData sensorMetaData);

        public static bool AreSensorsInitialized()
        {
            SensorMetaData sensorMetaData = new SensorMetaData();
            return KernelInterop.GetSensorMetaData(ref sensorMetaData);
        }

        // We'll also pass native pointer to a texture in Unity.
        // The plugin will fill texture data from native code.s
        [DllImport("MetaUnityDepthVisualizer", EntryPoint = "SetTextureFromUnity")]
        public static extern void SetTextureFromUnity(IntPtr texture, int height, int width);

        //todo: a better system to get this data
        [StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Ansi)]
        public struct SensorMetaData
        {
            /// <summary>   The height. </summary>
            public int height;
            /// <summary>   The width. </summary>
            public int width;
            /// <summary>   The focal length x coordinate. </summary>
            public float focalLengthX;
            /// <summary>   The focal length y coordinate. </summary>
            public float focalLengthY;
            /// <summary>   The principal point x coordinate. </summary>
            public float principalPointX;
            /// <summary>   The principal point y coordinate. </summary>
            public float principalPointY;
        }

        // TYPES

        /// <summary>
        /// Basic info from the camera
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct DeviceInfo
        {
            public int colorHeight, colorWidth;
            public int depthHeight, depthWidth;
            public bool streamingColor, streamingDepth;
            public float depthFps;
            public float colorFps;
            public CameraModel cameraModel;
            public IMUModel imuModel;
        };

        public enum CameraModel
        {
            UnknownCamera = -1,
            DS325 = 0,
            DS535 = 1
        };

        public enum IMUModel
        {
            UnknownIMU = -1,
            MPU9150Serial = 0,
            MPU9150HID = 1
        };

        /// Values that represent data acquisition systems.
        public enum DataAcquisitionSystem
        {
            /// Unknown data acquisition system
            Playback = 0,
            DVT3 = 19,
            //Handles the base frequency shift in the PMD depth camera
            DVT351 = 20 
        };
    }
}
