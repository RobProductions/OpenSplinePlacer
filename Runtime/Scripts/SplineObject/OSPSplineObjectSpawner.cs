using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace RobProductions.OpenSplinePlacer.Runtime
{
	public static class OSPSplineObjectSpawner
	{
		/// <summary>
		/// Spawns an object along a spline
		/// </summary>
		/// <param name="splineObject"></param>
		/// <param name="splineCurve"></param>
		/// <param name="splineTimePoint"></param>
		/// <param name="randomClass"></param>
		/// <param name="finalPlacePosition"></param>
		/// <param name="finalObjectLength"></param>
		/// <returns></returns>
		public static List<GameObject> SpawnSplineObjectOnSplinePoint(OSPSplineObject splineObject, Spline splineCurve, float splineTimePoint,
			Transform holderObject,
			System.Random randomClass, out Vector3 finalPlacePosition, out float finalObjectLength)
		{
			//Set up default values
			finalPlacePosition = Vector3.zero;
			finalObjectLength = splineObject.placementParams.objectLengthZ;

			//Get the curve position and rotation
			bool evaluateValid = splineCurve.Evaluate(splineTimePoint, out float3 splinePos, out float3 splineTangent, out float3 splineUpVector);
			Quaternion curveForwardRotation = Quaternion.LookRotation(splineTangent, splineUpVector);

			//Spawn the spline object
			var spawnedObjects = SpawnSplineObject(splineObject, randomClass);
			if (spawnedObjects.Count <= 0)
			{
				Debug.Log("OPSSplineObjectSpawner: No objects spawned on spline point: " + splineTimePoint);
				return new List<GameObject>();
			}

			//Set the object start rotation
			Quaternion startRotation = Quaternion.identity;
			if(splineObject.placementParams.rotationSpace == OSPSplineObject.SplineObjectRotationSpace.LocalToHolderObject)
			{
				startRotation = holderObject.rotation;
			}
			else if (splineObject.placementParams.rotationSpace == OSPSplineObject.SplineObjectRotationSpace.LocalToSplineDirection)
			{
				startRotation = curveForwardRotation;
			}

			//Set the modification rotation
			Quaternion modificationRotation = Quaternion.identity;
			if(splineObject.placementParams.rotationType == OSPSplineObject.SplineObjectRotationType.DiscreteValues)
			{
				//TODO: This and also alter the object length based on modification rotation
				//perhaps based on angle difference between the object (Y axis) and curve
			}

			//Set the object position
			//TODO: This

			//Get the final transformation
			Quaternion finalRotation = startRotation * modificationRotation;
			Vector3 finalPosition = splinePos;

			//Set object rotation, position, and parent
			Vector3 stackPositionDifference = Vector3.zero;
			for (int i = 0; i < spawnedObjects.Count; i++)
			{
				GameObject thisObject = spawnedObjects[i];

				if(i == 0)
				{
					//This is the base object
					stackPositionDifference = finalPosition - thisObject.transform.position;
					thisObject.transform.position = finalPosition;
				}
				else
				{
					//This is a stack object that should move relative with base
					thisObject.transform.position += stackPositionDifference;
				}

				thisObject.transform.rotation = finalRotation;
				thisObject.transform.SetParent(holderObject);
			}

			return spawnedObjects;
		}


		/// <summary>
		/// Spawn a SplineObject and stack references/top if needed.
		/// Does not place or rotate the objects.
		/// </summary>
		/// <param name="splineObject"></param>
		/// <param name="randomClass"></param>
		/// <param name="placedObjectLengthOnSpline"></param>
		/// <returns></returns>
		public static List<GameObject> SpawnSplineObject(OSPSplineObject splineObject, System.Random randomClass)
		{
			var ret = new List<GameObject>();

			//If we don't have a base object, this is all useless
			if (splineObject.spawningParams.baseSpawnReference.IsPrefabNull())
			{
				return ret;
			}

			//Spawn the base object
			ret.Add(SpawnReferenceObject(splineObject.spawningParams.baseSpawnReference));

			//Spawn the stack objects
			//TODO: This

			return ret;
		}

		/// <summary>
		/// Create a reference object (direct instantiate or with prefab link)
		/// based on the prefab listed in the spawnReference provided.
		/// </summary>
		/// <param name="spawnReference"></param>
		/// <returns></returns>
		private static GameObject SpawnReferenceObject(OSPSplineObject.SplineObjectSpawnReference spawnReference)
		{
			GameObject ret;

#if UNITY_EDITOR
			ret = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(spawnReference.prefabObject);
#else
		ret = GameObject.Instantiate(spawnReference.prefabObject);
#endif

			return ret;
		}
	}

}