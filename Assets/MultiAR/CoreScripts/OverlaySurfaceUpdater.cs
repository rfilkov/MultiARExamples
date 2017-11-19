using System.Collections.Generic;
using UnityEngine;


public class OverlaySurfaceUpdater : MonoBehaviour
{
	private Mesh surfaceMesh = null;

	private MeshRenderer meshRenderer = null;

	private MeshCollider meshCollider = null;

	private bool isEnabled = true;


	private void Awake()
	{
		// get or create mesh filter
		MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
		if(meshFilter == null)
		{
			meshFilter = gameObject.AddComponent<MeshFilter>();
		}

		// get the mesh
		surfaceMesh = meshFilter.mesh;

		// get or create mesh renderer
		meshRenderer = GetComponent<MeshRenderer>();
		if(meshRenderer == null)
		{
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
		}

		// set the surface layer
		int surfaceLayer = LayerMask.NameToLayer("SpatialSurface");
		if(surfaceLayer >= 0)
			gameObject.layer = surfaceLayer;
	}

	/// <summary>
	/// Sets the surface material.
	/// </summary>
	/// <param name="matSurface">Surface material.</param>
	public void SetSurfaceMaterial(Material matSurface)
	{
		if(meshRenderer && matSurface)
		{
			meshRenderer.material = matSurface;
		}
	}

	/// <summary>
	/// Sets the surface collider.
	/// </summary>
	/// <param name="isCollider">If set to <c>true</c> adds mesh collider to the object.</param>
	public void SetSurfaceCollider(bool isCollider)
	{
		// get or create mesh collider
		meshCollider = GetComponent<MeshCollider>();

		if(isCollider)
		{
			if(meshCollider == null)
			{
				meshCollider = gameObject.AddComponent<MeshCollider>();
			}
		}
		else if(meshCollider)
		{
			Destroy(meshCollider);
		}
	}

	public void SetEnabled(bool isEnabled)
	{
		if(this.isEnabled == isEnabled)
			return;

		this.isEnabled = isEnabled;

		if(meshRenderer)
		{
			meshRenderer.enabled = isEnabled;
		}

		if(meshCollider)
		{
			meshCollider.enabled = isEnabled;
		}
	}

	/// <summary>
	/// Updates the surface mesh.
	/// </summary>
	/// <returns><c>true</c>, if surface was updated, <c>false</c> otherwise.</returns>
	public bool UpdateSurfaceMesh(List<Vector3> meshVertices, List<int> meshIndices)
	{
		surfaceMesh.Clear();
		if(meshVertices != null)
		{
			surfaceMesh.SetVertices(meshVertices);
		}

		if(meshIndices != null)
		{
			surfaceMesh.SetIndices(meshIndices.ToArray(), MeshTopology.Triangles, 0);
		}
		else if(meshVertices != null)
		{
			// workaround if indices are missing
			int[] indices = new int[meshVertices.Count];

			for (int i = 0; i < meshVertices.Count; i++)
			{
				indices[i] = i;
			}

			surfaceMesh.SetIndices(indices, MeshTopology.Points, 0, false);
		}

		return true;
	}

}