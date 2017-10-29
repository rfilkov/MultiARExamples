using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	/// Gets the tracked planes timestamp.
	/// </summary>
	/// <returns>The tracked planes timestamp.</returns>
	double GetTrackedPlanesTimestamp();

	/// <summary>
	/// Gets the count of currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes count.</returns>
	int GetTrackedPlanesCount();

	/// <summary>
	/// Gets the currently tracked planes.
	/// </summary>
	/// <returns>The tracked planes.</returns>
	MultiARInterop.TrackedPlane[] GetTrackedPlanes(bool bGetPoints);

	/// <summary>
	/// Gets the input action.
	/// </summary>
	/// <returns>The input action.</returns>
	MultiARInterop.InputAction GetInputAction();

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
	/// Unparents the game object and removes the anchor from the system (if possible).
	/// </summary>
	/// <returns><c>true</c>, if game object anchor was removed, <c>false</c> otherwise.</returns>
	/// <param name="anchorId">Anchor identifier.</param>
	/// <param name="keepObjActive">If set to <c>true</c> keeps the object active afterwards.</param>
	bool RemoveGameObjectAnchor(string anchorId, bool keepObjActive);

}
