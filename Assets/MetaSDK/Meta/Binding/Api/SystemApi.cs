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
using Environment = System.Environment;
using File = System.IO.File;
using MetaCoreInterop = Meta.Interop.MetaCoreInterop;
using MetaVariable = Meta.Interop.MetaCoreInterop.MetaVariable;
using InitStatus = Meta.Interop.MetaCoreInterop.InitStatus;
using FrameHands = types.fbs.FrameHands; // Flatbuffers
using PoseType = types.fbs.PoseType;   // Flatbuffers
using Resources = UnityEngine.Resources;
using Texture2D = UnityEngine.Texture2D;
using Debug = UnityEngine.Debug;
using Transform = UnityEngine.Transform;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System;
using System.Collections;


namespace Meta.Plugin
{
    public static class SystemApi
    {

        /// <summary>
        /// Initializes the system (sensors, algorithms, etc). Returns false on failure, true on success.
        /// </summary>
        /// <param name="json_config_file">Configuration file with which to initialize coco</param>
        /// <param name="json_config_file">boolean to specify if we are running in development environment or production
        // environment</param>
        /// <param name="initialize_web_server">Specify weather to initialize stats web server.</param>
        public static bool Start(string json_config_file = "", bool is_development_environment = true, bool initialize_web_server = false)
        {
            // -- Initialize library
            InitStatus result = MetaCoreInterop.meta_init(json_config_file, is_development_environment);
            if (result != InitStatus.NO_ERROR)
            {
                // Debug.LogError("Meta initialization result: " + result);
                return false;
            }

            // -- Start MetaCore
            MetaCoreInterop.meta_start(initialize_web_server);

            // Note: Disabled; see MET-1833.
            // MetaCoreInterop.meta_wait_start_complete();

            return true;
        }


        /// <summary>
        /// Coroutine which waits for Meta Configuration to complete
        /// then calls an Action
        /// </summary>
        /// <param name="action">Action to call on Meta Ready</param>
        /// <returns></returns>
        public static IEnumerator MetaReady(Action action)
        {
            MetaCoreInterop.meta_wait_start_complete();
            action();
            yield return null;
        }


        /// <summary>
        /// Stops currently running coco instance.
        /// </summary>
        public static void Stop()
        {
            MetaCoreInterop.meta_stop();
        }


        /// <summary>
        /// Returns latest frame's hands.
        /// </summary>
        /// <param name="buffer">Byte buffer to use for deserialization.</param>
        /// <param name="frameHands">FrameHands datastructure to populate.</param>
        /// <returns></returns>
        public static bool GetFrameHandsFlatbufferObject(ref byte[] buffer, out FrameHands frameHands)
        {
            if (MetaCoreInterop.meta_get_frame_hands(buffer) == 0)
            {
                frameHands = new FrameHands();

                return false;
            }

            var byteBuffer = new FlatBuffers.ByteBuffer(buffer);

            frameHands = FrameHands.GetRootAsFrameHands(byteBuffer);
            return true;
        }


        public static bool GetPose(string source, string target, ref byte[] buffer, out PoseType poseType)
        {
            if (MetaCoreInterop.meta_get_pose(source, target, buffer))
            {
                poseType = new PoseType();

                return false;
            }

            var byteBuffer = new FlatBuffers.ByteBuffer(buffer);

            poseType = PoseType.GetRootAsPoseType(byteBuffer);
            return true;
        }


        public static string GetSerialNumber()
        {
            string data = null;
            MetaCoreInterop.meta_get_serial_number(ref data);
            return data;
        }

        /// <summary>
        /// Gets a snapshot of the device status.
        /// </summary>
        /// <returns>The device status snapshot</returns>
        public static DeviceStatusSnapshot GetDeviceStatus()
        {
            int deviceStatus, connectionStatus, streamingStatus = 0;
            MetaCoreInterop.meta_get_device_status(out deviceStatus, out connectionStatus, out streamingStatus);
            return new DeviceStatusSnapshot((DeviceStatusSnapshot.DeviceStatus)deviceStatus, 
                                            (DeviceStatusSnapshot.ConnectionStatus)connectionStatus, 
                                            streamingStatus);
        }

        /// <summary>
        /// Applies latest head pose, if available to referenced transform
        /// </summary>
        /// <param name="transformToApply">Transform to apply head pose to.</param>
        public static void ApplyHeadPose(ref Transform transformToApply)
        {
            var pose = MetaCoreInterop.meta_get_latest_head_pose();

            transformToApply.localPosition = new Vector3(pose.position.x,
                                                          pose.position.y,
                                                          pose.position.z);

            transformToApply.localRotation = new Quaternion(pose.rotation.x,
                                                             pose.rotation.y,
                                                             pose.rotation.z,
                                                             pose.rotation.w);
        }


        /// <summary>
        /// Updated a coco attribute.
        /// </summary>
        /// <param name="blockName">Name of block to update.</param>
        /// <param name="attributeName">Name of paramiter to update.</param>
        /// <param name="attributeValue">Target string value for specified attribute.</param>
        /// <returns></returns>
        public static bool SetAttribute(string blockName, string attributeName, string attributeValue)
        {
            if (!MetaCoreInterop.meta_update_attribute(blockName, attributeName, attributeValue))
            {
                Debug.Log("Failed to update attribute: " + blockName + " " + attributeName);
                return false;
            }

            return true;
        }


        internal static void ToggleDebugDrawing(bool targetState)
        {
            var targetStateString = targetState ? "true" : "false";
            var attribute = "draw";

            MetaCoreInterop.meta_update_attribute("HandsDataPreprocessingBlock", attribute, targetStateString);
            MetaCoreInterop.meta_update_attribute("HandSegmentationBlock", attribute, targetStateString);
            MetaCoreInterop.meta_update_attribute("HandTrackingBlock", attribute, targetStateString);
            MetaCoreInterop.meta_update_attribute("HandFeatureExtractionBlock", attribute, targetStateString);
        }


        public static bool GetPath(MetaVariable variable, out string result)
        {
            return MetaCoreInterop.get_meta_variable(variable, out result);
        }
    }
}
