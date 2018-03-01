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

// TODO(amesolon): Get rid of the following dependencies.
using System.Collections.Generic;  // For Dictionary
using System.Linq;  // For functional iteration.
using Double   = System.Double;
using JSONNode = SimpleJSON.JSONNode;
using JSON     = SimpleJSON.JSON;


namespace Meta.Interop
{
    /// This class depends on MiniJSON, temporarily, until we use flatbuffers.
    /// TODO: Use Flatbuffers instead of JSON.
    public static class NodeLoaderInterop
    {
        [DllImport(DllReferences.MetaCore, EntryPoint = "meta_get_calibration_json_data")]
        public static extern int getJsonData([MarshalAs(UnmanagedType.BStr), In, Out] ref string json);

        public static void GetJsonData(ref string json)
        {
            getJsonData(ref json);
        }

        public struct NodeData
        {
            public string entry_name;

            /// A serialized array that represents a 4x4 matrix.  The representation is row-major:
            /// double[] = [row0---row1---row2---row3---]. The last row of the homogeneous 4x4 matrix
            /// is not transfered because it is always [0 0 0 1].
            public double[] pose_data;

            /// This is relevant only if the node represents a camera. Otherwise, this is null.
            /// The expected values for this are: the focal length (fx, fy) and the principal point (cx, cy).
            public double[] projection_model_data;
        }

            
        public static class Loader
        {

            private static string ParseDllInput()
            {
                string jsonString = null;
                NodeLoaderInterop.GetJsonData(ref jsonString);
                if (jsonString != null)
                {
                    if (jsonString.Length != 0)
                    {
                        return jsonString;
                    }
                }
                return null;
            }

            public static Dictionary<string, NodeData> Load()
            {
                string jsonString = ParseDllInput();

                if (jsonString == null)
                {
                    return null;
                }

                var JsonRootNode = JSON.Parse(jsonString);

                if (JsonRootNode == null)
                {
                    return null;
                }

                var nodes = JsonRootNode.AsArray;

                Dictionary<string, NodeData> profiles = new Dictionary<string, NodeData>();

                int nodeCounter = 0;
                foreach (JSONNode n in nodes)
                {
                    string name = null;
                    try
                    {
                        name = n["name"];
                        // Matrix4x4 poseMat = Matrix4x4.zero;
                        double[] r = n["relative_pose"].AsArray.Childs.Select(d => Double.Parse(d)).ToArray();
                        if (r.Length < 12) {
                            // Debug.LogError("CalibrationParameterLoader: array was too short.");
                            // throw();
                        }

                        double[] cameraModel = n["camera_model"].AsArray.Childs.Select(d => Double.Parse(d)).ToArray();

                        profiles.Add(name, new NodeData { entry_name = name, pose_data = r, projection_model_data = cameraModel });

                        // Debug.Log(profiles[name].RelativePose + "|||" + 
                        //          string.Join(" ", (profiles[name].CameraModel.Select(x => x.ToString())).ToArray()));

                    }
                    catch
                    {
                        if (name != null)
                        {
                            //Debug.LogError(
                            //    string.Format(
                            //        "CalibrationParameter parsing error: node named '{0}' was not formatted correctly.", name));
                        }
                        else
                        {
                            //Debug.LogError(
                            //    string.Format("CalibrationParameter parsing error: node {0} was not formatted correctly.",
                            //        nodeCounter));
                        }
                    }

                    nodeCounter++;
                }

                return profiles;
            }
        }
    }
}

