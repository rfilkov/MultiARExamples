using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeoUtils : MonoBehaviour 
{

	// converts geo-coordinates to meters
	public static Vector3 LatLong2Meters(float lat, float lon, float alt)
	{
		// based on: 
		// Latitude: 1 deg = 110.574 km
		// Longitude: 1 deg = 111.320*cos(latitude) km

		float latM = lat * 110574f;
		float lonM = lon * 111320 * Mathf.Cos(lat * Mathf.Deg2Rad);

		return new Vector3(latM, lonM, alt);
	}

	// converts meter-coordinates to geo-coordinates (lat,long,alt)
	public Vector3 Meters2LatLong(Vector3 latLonM)
	{
		float lat = latLonM.x / 110574f;
		float lon = latLonM.y / (111320f * Mathf.Cos(lat * Mathf.Deg2Rad));

		return new Vector3(lat, lon, latLonM.z);
	}

	// returns the distance between 2 geo-locations in meters
	public float GetLatLongDist(float lat1, float lon1, float lat2, float lon2) 
	{
		float R = 6371f; // Radius of the earth in km
		float dLat = (lat2-lat1) * Mathf.Deg2Rad;
		float dLon = (lon2-lon1) * Mathf.Deg2Rad; 

		var a = 
			Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
			Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) * 
			Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
		
		float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a)); 
		var d = R * c; // Distance in km

		return d * 1000f;
	}


}
