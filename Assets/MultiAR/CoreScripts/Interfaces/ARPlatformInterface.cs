using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// delegate invoked after an anchor gets saved
public delegate void AnchorSavedDelegate(string anchorId, string errorMessage);

// delegate invoked after an anchor gets restored
public delegate void AnchorRestoredDelegate(GameObject anchorObj, string errorMessage);


public interface ARPlatformInterface 
{
	/// <summary>
	/// Gets the AR platform supported by the interface.
	/// </summary>
	/// <returns>The AR platform.</returns>
	MultiARInterop.ARPlatform GetARPlatform();

	/// <summary>
	/// Determines whether the platform is available or not.
	/// </summary>
	/// <returns><c>true</c> if the platform is available; otherwise, <c>false</c>.</returns>
	bool IsPlatformAvailable();

	/// <summary>
	/// Sets the enabled or disabled state of the interface.
	/// </summary>
	/// <param name="isEnabled">If set to <c>true</c> interface is enabled.</param>
	void SetEnabled(bool isEnabled, MultiARManager arManager);

	/// <summary>
	/// Determines whether the interface is enabled.
	/// </summary>
	/// <returns><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</returns>
	bool IsEnabled();

	/// <summary>
	/// Determines whether the interface is initialized.
	/// </summary>
	/// <returns><c>true</c> if this instance is initialized; otherwise, <c>false</c>.</returns>
	bool IsInitialized();

	/// <summary>
	/// Determines whether the interface is in tracking state or not
	/// </summary>
	/// <returns><c>true</c> if this instance is tracking; otherwise, <c>false</c>.</returns>
	bool IsTracking();

	/// <summary>
	/// Gets the AR main camera.
	/// </summary>
	/// <returns>The AR main camera.</returns>
	Camera GetMainCamera();

	/// <summary>
	/// Gets AR-detected light intensity.
	/// </summary>
	/// <returns>The light intensity.</returns>
	float GetLightIntensity();

	/// <summary>
	/// Gets the last frame timestamp.
	/// </summary>
	/// <returns>The last frame timestamp.</returns>
	double GetLastFrameTimestamp();

	/// <summary>
	/// Gets the state of the camera tracking.
	/// </summary>
	/// <returns>The camera tracking state.</returns>
	MultiARInterop.CameraTrackingState GetCameraTrackingState();

	/// <summary>
	/// Gets the tracking error message, if any.
	/// </summary>
	/// <returns>The tracking error message.</returns>
	string GetTrackingErrorMessage();

	/// <summary>
	/// Gets the tracked surfaces timestamp.
	/// </summary>
	/// <returns>The tracked surfaces timestamp.</returns>
	double GetTrackedSurfacesTimestamp();

	/// <summary>
	/// Gets the count of currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces count.</returns>
	int GetTrackedSurfacesCount();

	/// <summary>
	/// Gets the currently tracked surfaces.
	/// </summary>
	/// <returns>The tracked surfaces.</returns>
	MultiARInterop.TrackedSurface[] GetTrackedSurfaces(bool bGetPoints);

	/// <summary>
	/// Determines whether input action is available.for processing
	/// </summary>
	/// <returns><c>true</c> input action is available; otherwise, <c>false</c>.</returns>
	bool IsInputAvailable(bool inclRelease);

	/// <summary>
	/// Gets the input action.
	/// </summary>
	/// <returns>The input action.</returns>
	MultiARInterop.InputAction GetInputAction();

	/// <summary>
	/// Gets the input normalized navigation coordinates.
	/// </summary>
	/// <returns>The input nav coordinates.</returns>
	Vector3 GetInputNavCoordinates();

	/// <summary>
	/// Gets the current or default input position.
	/// </summary>
	/// <returns>The input position.</returns>
	/// <param name="defaultPos">If set to <c>true</c> returns the by-default position.</param>
	Vector2 GetInputScreenPos(bool defaultPos);

	/// <summary>
	/// Gets the input-action timestamp.
	/// </summary>
	/// <returns>The input-action timestamp.</returns>
	double GetInputTimestamp();

	/// <summary>
	/// Clears the input action.
	/// </summary>
	void ClearInputAction();

	/// <summary>
	/// Raycasts from screen point or camera to the scene colliders.
	/// </summary>
	/// <returns><c>true</c>, if an object was hit, <c>false</c> otherwise.</returns>
	/// <param name="fromInputPos">Whether to use the last input position for the raycast, or not.</param>
	/// <param name="hit">Hit data.</param>
	bool RaycastToScene(bool fromInputPos, out MultiARInterop.TrackableHit hit);

	/// <summary>
	/// Raycasts from screen point or camera to the scene colliders, and returns all hits.
	/// </summary>
	/// <returns><c>true</c>, if an object was hit, <c>false</c> otherwise.</returns>
	/// <param name="fromInputPos">Whether to use the last input position for the raycast, or not.</param>
	/// <param name="hits">Array of hit data.</param>
	bool RaycastAllToScene(bool fromInputPos, out MultiARInterop.TrackableHit[] hits);

	/// <summary>
	/// Raycasts from screen point or camera to the world.
	/// </summary>
	/// <returns><c>true</c>, if a plane was hit, <c>false</c> otherwise.</returns>
	/// <param name="screenPos">Screen position.</param>
	/// <param name="hit">Hit data.</param>
	bool RaycastToWorld(bool fromInputPos, out MultiARInterop.TrackableHit hit);

	/// <summary>
	/// Anchors the game object to world.
	/// </summary>
	/// <returns>The game object to world.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="hit">Trackable hit.</param>
	string AnchorGameObjectToWorld(GameObject gameObj, MultiARInterop.TrackableHit hit);

	/// <summary>
	/// Anchors the game object to world.
	/// </summary>
	/// <returns>The anchor Id, or empty string.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="worldPosition">World position.</param>
	/// <param name="worldRotation">World rotation.</param>
	string AnchorGameObjectToWorld(GameObject gameObj, Vector3 worldPosition, Quaternion worldRotation);

	/// <summary>
	/// Anchors the game object to anchor object.
	/// </summary>
	/// <returns><c>true</c>, if game object was anchored, <c>false</c> otherwise.</returns>
	/// <param name="gameObj">Game object.</param>
	/// <param name="anchorObj">Anchor object.</param>
	bool AnchorGameObject(GameObject gameObj, GameObject anchorObj);

	/// <summary>
	/// Unparents the game object and removes the anchor from the system (if possible).
	/// </summary>
	/// <returns><c>true</c>, if game object anchor was removed, <c>false</c> otherwise.</returns>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="keepObjActive">If set to <c>true</c> keeps the object active afterwards.</param>
	bool RemoveGameObjectAnchor(string anchorId, bool keepObjActive);

	/// <summary>
	/// Pauses the AR session.
	/// </summary>
	/// <returns><c>true</c>, if session was paused, <c>false</c> if pausing AR session is not supported.</returns>
	bool PauseSession();

	/// <summary>
	/// Resumes the AR session, if paused.
	/// </summary>
	void ResumeSession();

	/// <summary>
	/// Saves the world anchor.
	/// </summary>
	/// <param name="gameObj">Anchored game object.</param>
	/// <param name="anchorSaved">Delegate invoked after the anchor gets saved.</param>
	void SaveWorldAnchor(GameObject gameObj, AnchorSavedDelegate anchorSaved);

    /// <summary>
    /// Restores the world anchor.
    /// </summary>
    /// <param name="anchorId">Anchor identifier.</param>
    /// <param name="anchorRestored">Delegate invoked after the anchor gets restored.</param>
    void RestoreWorldAnchor(string anchorId, AnchorRestoredDelegate anchorRestored);

    /// <summary>
    /// Gets the saved anchor data as byte array, or null.
    /// </summary>
    /// <returns>The saved anchor data</returns>
    byte[] GetSavedAnchorData();

    /// <summary>
    /// Sets the saved anchor data that needs to be restored.
    /// </summary>
    /// <param name="btData"></param>
    void SetSavedAnchorData(byte[] btData);

	/// <summary>
	/// Inits the image anchors tracking.
	/// </summary>
	/// <param name="imageManager">Anchor image manager.</param>
	void InitImageAnchorsTracking(AnchorImageManager imageManager);

	/// <summary>
	/// Enables (starts tracking) image anchors.
	/// </summary>
	void EnableImageAnchorsTracking();

	/// <summary>
	/// Disables (stops tracking) image anchors.
	/// </summary>
	void DisableImageAnchorsTracking();

	/// <summary>
	/// Gets the currently found image anchor names.
	/// </summary>
	/// <returns>The image anchor names.</returns>
	List<string> GetTrackedImageAnchorNames();

	/// <summary>
	/// Gets the name of first found image anchor.
	/// </summary>
	/// <returns>The name of first image anchor.</returns>
	string GetFirstTrackedImageAnchorName();

	/// <summary>
	/// Gets the tracked image anchor by name.
	/// </summary>
	/// <returns>The tracked image anchor.</returns>
	/// <param name="imageAnchorName">Image anchor name.</param>
	GameObject GetTrackedImageAnchorByName(string imageAnchorName);

    /// <summary>
    /// Gets the background (reality) texture
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <returns>The background texture, or null</returns>
    Texture GetBackgroundTex(MultiARInterop.MultiARData arData);

    /// <summary>
    /// Sets or clears fixed background texture size
    /// </summary>
    /// <param name="arData">AR data</param>
    /// <param name="isFixedSize">Whether the background texture has fixed size</param>
    /// <param name="fixedSizeW">Fixed size width</param>
    /// <param name="fixedSizeH">Fixed size height</param>
    void SetFixedBackTexSize(MultiARInterop.MultiARData arData, bool isFixedSize, int fixedSizeW, int fixedSizeH);

}
