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

namespace Meta.Interop
{
    /// <summary>
    /// Class containing coco interop related datastructures and methods.
    /// </summary>
    public static class MetaCoreInterop
    {
        #region C API Data structures
        public enum InitStatus
        {
            NO_ERROR = 0,
            FILE_NOT_FOUND,
            FILE_ERROR,
            INVALID_CONFIGURATIONS
        }

        /// <summary>
        /// These should be consistent with os_environment.h:enum class MetaVariable
        /// </summary>
        public enum MetaVariable
        {
            META_USER_DATA,
            META_CACHE,
            META_CACHE_DEBUG,       // crash
            META_USB,               // crash
            META_TESTING_DATA,
            META_DRIVER,            // crash
            META_CACHE_RELEASE,     // crash
            META_INSTALL,
            META_APP_DATA,
            META_BUILD,
            META_CORE,
            META_RECORDING,
            META_CONFIG,
            META_TOOLS,             // crash
            META_3RDPARTY
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Vec3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Quat
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MetaPose
        {
            public Vec3 position;
            public Quat rotation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MetaPointCloud
        {
            public int num_points;
            public IntPtr points;
        }
        #endregion

        #region C API Methods
        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern int meta_get_frame_hands(byte[] buffer);

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool meta_get_pose(string source_frame, string target_frame, byte[] buffer);

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern MetaPose meta_get_latest_head_pose();

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern InitStatus meta_init(string config_file, bool is_development_environment);

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meta_start(bool profile);

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meta_wait_start_complete();

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meta_stop();

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool meta_get_serial_number([MarshalAs(UnmanagedType.BStr), In, Out] ref string serial_number);

        [DllImport(DllReferences.MetaCore)]
        public static extern void meta_get_device_status(out int deviceStatus, out int connectionStatus,
                                                         out int streamingStatus );
                                                         

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool meta_update_attribute(string task, string attribute, string value);


        /// <summary>
        /// Enable or disable rgb stream
        /// </summary>
        /// <param name="enable">whether we should enable or disable</param>
        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meta_enable_rgb_stream(bool enable);

        ///<summary>
        ///Gets RGB data and writes to the given ptr.
        ///</summary>
        ///<param name="buffer"> The buffer - size should be 1280x720x3 bytes.  Preferably use unmanaged memory
        ///<param name="translation">The translation with which we should render virtual conent</param>
        ///<param name="rotation">The rotation with which we should render virtual conent</param>
        // allocated by Marshal class.</param>
        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern void meta_get_rgb_frame(IntPtr buffer, [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)] double[] translation,
                                                      [MarshalAs(UnmanagedType.LPArray, SizeConst = 4)] double[] rotation);

        [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool get_meta_variable(MetaVariable variableId, [MarshalAs(UnmanagedType.BStr), Out] out string result);

        #endregion

        #region TODO
        // TODO: Enable this if needed.
        // [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        // public static extern void meta_update_texture_test (IntPtr texture_ptr);

        // [DllImport("MetaVisionDLL", EntryPoint = "getCurrentMicroseconds")]
        // public static extern long GetCurrentMicroseconds();

        // TODO: Enable this.
        // [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        // public static extern void meta_record(bool target_state);

        // TODO: Enable this.
        // [DllImport(DllReferences.MetaCore, CallingConvention = CallingConvention.Cdecl)]
        // public static extern bool meta_start_web_server(int port);
        #endregion
    }
}
