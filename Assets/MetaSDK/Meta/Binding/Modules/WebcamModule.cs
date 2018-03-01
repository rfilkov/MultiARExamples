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
using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

using WebcamInterop     = Meta.Interop.WebcamInterop;
using MetaCoreInterop   = Meta.Interop.MetaCoreInterop;
using MetaUIContent     = Meta.Rendering.MetaUIContent;

namespace Meta
{
    public class WebcamModule : MonoBehaviour, IWebcamStateChangeListener
    {
        [SerializeField]
        private Camera _contentCamera = null;
        [SerializeField]
        private Camera _compositeCamera = null;
        [SerializeField]
        private Material _contentMaterial;
        [SerializeField]
        private Material _compositeMaterial;

        // Option for enabling the rendering pluging for the virtual webcam to be viewed in 3rd party apps.  
        // Must be changed while the scene is NOT running. 
        [SerializeField]
        private bool _enableWebcamPlugin = true;

        private const int TextureWidth = 1280;
        private const int TextureHeight = 720;
        private const int BitDepth = 24;
        private const float PluginFps = 30;
        private const float QuadPositionX = 0;
        private const float QuadPositionY = 0;
        private const float QuadPositionZ = 9;
        private const float QuadScaleX = 18.39301f;
        private const float QuadScaleY = 10.34606f;
        private const float QuadScaleZ = 0.892518f;

        private WebcamMirrorModes _targetDisplay = WebcamMirrorModes.None;

        private bool _initialized = false;
        private bool _webcamOn = false;
        private Mesh _quad;
        private Transform _rgbTransform;
        private Transform _cameraRigTransform;
        private IntPtr _texturePtr;
        private Texture2D _webcamTexture;
        private RenderTexture _renderTextureContent;
        private RenderTexture _renderTextureComposite;

        private Camera _mirrorCamera;
        private CommandBuffer _drawRgb;
        private CommandBuffer _drawComposite;
        private CommandBuffer _drawMirrorMode;
        private bool _textureRequest = false;

        private IntPtr _rgbBuffer;

        /// <summary>
        /// Provides a texture containing the raw feed from headset's camera (if a headset is connected).
        /// If a headset is not connected, the feed will display the Meta logo.
        /// </summary>
        public Texture InputTexture
        {
            get { return _webcamTexture; }
        }

        private void Awake()
        {
            _rgbBuffer = Marshal.AllocHGlobal(TextureWidth * TextureHeight * (BitDepth / 8));
            _contentMaterial = new Material(_contentMaterial);
            _compositeMaterial = new Material(_compositeMaterial);

            _drawRgb = new CommandBuffer();
            _drawComposite = new CommandBuffer();
            _drawMirrorMode = new CommandBuffer();

            GameObject child = new GameObject("Quad") { hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector };
            child.transform.SetParent(_contentCamera.transform, false);
            child.transform.localPosition = new Vector3(QuadPositionX, QuadPositionY, QuadPositionZ);
            child.transform.localScale = new Vector3(QuadScaleX, QuadScaleY, QuadScaleZ);
            _rgbTransform = child.transform;
            _cameraRigTransform = GameObject.FindObjectOfType<MetaManager>().transform;

            _renderTextureContent = CreateRenderTexture(RenderTextureFormat.ARGB32);
            _renderTextureComposite = CreateRenderTexture(RenderTextureFormat.BGRA32);

            _contentCamera.targetTexture = _renderTextureContent;
            _compositeCamera.targetTexture = _renderTextureComposite;

            _texturePtr = _renderTextureComposite.GetNativeTexturePtr();

            //SetupMirrorMode(_targetDisplay);
            WebcamToggle(_webcamOn);

            _webcamTexture = Meta.Plugin.CameraApi.GetRgbFrame();
        }

        private void OnEnable()
        {
            if (_enableWebcamPlugin)
            {
                // Start webcam rendering
                WebcamInterop.Initalize(_texturePtr, PluginFps);
                WebcamInterop.Run();
            }

            MetaCoreInterop.meta_enable_rgb_stream(true);

            InitializeMaterial();

            if (_enableWebcamPlugin)
            {
                StartCoroutine(UpdatePlugin());
            }
        }

        private void Start()
        {
            _quad = CreateQuad();
            _drawRgb.name = "Draw Webcam RGB";
            SetupRgbCommands();
            _contentCamera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _drawRgb);

            _drawComposite.name = "Draw Composite Image";
            SetupCompositeCommands();
            _compositeCamera.AddCommandBuffer(CameraEvent.AfterEverything, _drawComposite);
        }

        private void Update()
        {
            if (!_initialized)
            {
                InitializeMaterial();
                _initialized = true;
            }

            Plugin.CameraApi.UpdateRgbFrame();

            if (!MetaUIContent.IsUIPresent)
            {
                UpdateCameraPose();
            }

            if (IsWebcamBeingWatched() != _webcamOn)
            {
                WebcamToggle(!_webcamOn);
            }
        }

        private void UpdateCameraPose()
        {
            var translation_data = Plugin.CameraApi._translation;
            Quaternion rotation = TypeUtilities.QuaternionFromDouble(Plugin.CameraApi._rotation);
            Vector3 translation = new Vector3(
                (float)translation_data[0],
                (float)translation_data[1],
                (float)translation_data[2]
                );

            Transform parentTransform = _cameraRigTransform.parent;
            if (parentTransform)
            {
                translation = parentTransform.TransformPoint(translation);
                rotation = parentTransform.rotation * rotation;
            }
            _contentCamera.transform.rotation = rotation;
            _contentCamera.transform.position = translation;

            _compositeCamera.transform.rotation = rotation;
            _compositeCamera.transform.position = translation;
        }

        private void LateUpdate()
        {
            if (!_webcamOn)
            {
                return;
            }

            _drawRgb.Clear();
            _drawComposite.Clear();

            SetupRgbCommands();
            SetupCompositeCommands();
        }

        private void OnDisable()
        {
            if (_enableWebcamPlugin)
            {
                StopCoroutine(UpdatePlugin());
                WebcamInterop.Stop();
            }

            MetaCoreInterop.meta_enable_rgb_stream(false);

            _drawRgb.Clear();
            _drawComposite.Clear();
            _textureRequest = false;
        }

        private void OnDestroy()
        {
            Marshal.FreeHGlobal(_rgbBuffer);
        }
        /// <summary>
        /// Provides a texture showing the composited output of the headset feed and the virtual imagery.
        /// As a side effect, webcam compositing will be turned on if it is not already on, after the next time WebcamModule's update runs.
        /// Disable this component to turn webcam compositing off (if it is not on for another reason).
        /// </summary>
        public Texture GetOutputTexure()
        {
            _textureRequest = true;
            return _renderTextureComposite;
        }

        public void OnStateChanged(WebcamMirrorModes changedToMode)
        {
            SetupMirrorMode(changedToMode);
            _targetDisplay = changedToMode;
        }

        private RenderTexture CreateRenderTexture(RenderTextureFormat renderTextureFormat)
        {
            RenderTexture renderTexture = new RenderTexture(TextureWidth, TextureHeight, BitDepth, renderTextureFormat, RenderTextureReadWrite.Default);
            renderTexture.antiAliasing = 1;
            renderTexture.wrapMode = TextureWrapMode.Clamp;
            renderTexture.filterMode = FilterMode.Bilinear;
            renderTexture.autoGenerateMips = false;
            renderTexture.anisoLevel = 0;
            renderTexture.Create();

            return renderTexture;
        }

        private void SetupRgbCommands()
        {
            _drawRgb.DrawMesh(_quad, _rgbTransform.localToWorldMatrix, _contentMaterial);
        }

        private void SetupCompositeCommands()
        {
            _drawComposite.DrawMesh(_quad, _rgbTransform.localToWorldMatrix, _compositeMaterial);
        }

        /// <summary>
        /// Assign textures to the output material
        /// </summary>
        private void InitializeMaterial()
        {
            Material compositeMaterial = _compositeMaterial;
            compositeMaterial.SetTexture("_ContentTex", _renderTextureContent);
            compositeMaterial.SetTexture("_WebcamTex", _webcamTexture);
            _contentMaterial.SetTexture("_MainTex", _webcamTexture);
        }

        private void SetupMirrorMode(WebcamMirrorModes mirrorMode)
        {
            if (mirrorMode == WebcamMirrorModes.None)
            {
                if (_mirrorCamera != null)
                {
                    Destroy(_mirrorCamera.gameObject);
                }
                return;
            }

            if (_mirrorCamera == null)
            {
                GameObject child = new GameObject("Webcam Display");
                child.transform.SetParent(transform, false);
                _mirrorCamera = child.AddComponent<Camera>();
                _mirrorCamera.cullingMask = _compositeCamera.cullingMask;
                _mirrorCamera.clearFlags = _compositeCamera.clearFlags;
                _mirrorCamera.backgroundColor = _compositeCamera.backgroundColor;
                _mirrorCamera.projectionMatrix = _compositeCamera.projectionMatrix;
                _mirrorCamera.fieldOfView = _compositeCamera.fieldOfView;
                _mirrorCamera.nearClipPlane = _compositeCamera.nearClipPlane;
                _mirrorCamera.farClipPlane = _compositeCamera.farClipPlane;
                _mirrorCamera.useOcclusionCulling = _compositeCamera.useOcclusionCulling;
                _mirrorCamera.allowHDR = _compositeCamera.allowHDR;
                _mirrorCamera.depth = 100;

                _drawMirrorMode.name = "Mirror Mode Quad";
                _mirrorCamera.AddCommandBuffer(CameraEvent.AfterEverything, _drawMirrorMode);
                _drawMirrorMode.Blit(_compositeCamera.targetTexture, BuiltinRenderTextureType.CameraTarget);
            }

            _mirrorCamera.targetDisplay = (int)mirrorMode;
        }

        private void WebcamToggle(bool enabled)
        {
            _contentCamera.enabled = enabled;
            _compositeCamera.enabled = enabled;

            if (_mirrorCamera != null)
            {
                _mirrorCamera.enabled = enabled;
            }

            _webcamOn = enabled;
        }

        private bool IsWebcamBeingWatched()
        {
            return WebcamInterop.IsWebcamOn() || _targetDisplay != WebcamMirrorModes.None || _textureRequest;
        }

        private IEnumerator UpdatePlugin()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();

                GL.IssuePluginEvent(WebcamInterop.GetRenderCallback(), 0);
            }
        }

        private static Mesh CreateQuad()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-.5f, -.5f, 0);
            vertices[1] = new Vector3(.5f, -.5f, 0);
            vertices[2] = new Vector3(-.5f, .5f, 0);
            vertices[3] = new Vector3(.5f, .5f, 0);

            mesh.vertices = vertices;

            int[] tri = new int[6];
            tri[0] = 0;
            tri[1] = 2;
            tri[2] = 1;
            tri[3] = 2;
            tri[4] = 3;
            tri[5] = 1;

            mesh.triangles = tri;

            Vector3[] normals = new Vector3[4];
            normals[0] = -Vector3.forward;
            normals[1] = -Vector3.forward;
            normals[2] = -Vector3.forward;
            normals[3] = -Vector3.forward;

            mesh.normals = normals;

            Vector2[] uv = new Vector2[4];
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);

            mesh.uv = uv;

            return mesh;
        }
    }
}
