using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace RobProductions.OpenSplinePlacer.Runtime
{
	public static class OSPSplineObjectSpawner
	{
		/// <summary>
		/// As a preprocess check, we will determine the rotation
		/// and therefore the final "length" of an object before
		/// it is spawned in so that we know where to place it on the spline.
		/// </summary>
		/// <param name="splineObject"></param>
		/// <param name="randomClass"></param>
		/// <param name="objectModificationRotation"></param>
		/// <param name="finalObjectLength"></param>
		public static void DetermineSplineObjectRotation(OSPSplineObject splineObject, System.Random randomClass,
			out Quaternion objectModificationRotation, out float finalObjectLength)
		{
			//Set up default values
			objectModificationRotation = Quaternion.identity;
			finalObjectLength = splineObject.placementParams.objectLengthZ;

			//Determine the object's modification rotation
			if(splineObject.placementParams.rotationType == OSPSplineObject.SplineObjectRotationType.DiscreteValues)
			{
				if(splineObject.placementParams.possibleRotations.Length > 0)
				{
					objectModificationRotation = Quaternion.Euler(splineObject.GetRandomDiscreteRotation(randomClass));
				}
				else
				{
					Debug.Log("OPSSplineObjectSpawner: Possible Rotations length was 0 in Spline Object placement params in DetermineSplineObjectRotation()!");
				}
			}
			else if (splineObject.placementParams.rotationType == OSPSplineObject.SplineObjectRotationType.RangeValue)
			{
				var xRotAmount = (float)randomClass.NextDouble();
				var xRot = Mathf.Lerp(splineObject.placementParams.rotationXRange.x, splineObject.placementParams.rotationXRange.y, xRotAmount);
				var yRotAmount = (float)randomClass.NextDouble();
				var yRot = Mathf.Lerp(splineObject.placementParams.rotationYRange.x, splineObject.placementParams.rotationYRange.y, yRotAmount);
				var zRotAmount = (float)randomClass.NextDouble();
				var zRot = Mathf.Lerp(splineObject.placementParams.rotationZRange.x, splineObject.placementParams.rotationZRange.y, zRotAmount);

				objectModificationRotation = Quaternion.Euler(xRot, yRot, zRot);
			}

			//Determine length from rotation
			var yModification = FloatMod(objectModificationRotation.eulerAngles.y, 360f);
			var yModification90Reverse = Mathf.PingPong(yModification, 90f);
			var yModificationTo90Amount = Mathf.InverseLerp(0f, 90f, yModification90Reverse);

			finalObjectLength = Mathf.Lerp(splineObject.placementParams.objectLengthZ, splineObject.placementParams.objectLengthX, yModificationTo90Amount);
		}

		/// <summary>
		/// Spawns an object at a spline location with the given
		/// holder object and rotation.
		/// </summary>
		/// <param name="splineObject"></param>
		/// <param name="splineCurve"></param>
		/// <param name="splineTimePoint"></param>
		/// <param name="randomClass"></param>
		/// <param name="finalBaseObjectPosition"></param>
		/// <param name="finalObjectLength"></param>
		/// <returns></returns>
		public static List<GameObject> SpawnSplineObjectOnSplinePoint(OSPSplineObject splineObject, Spline splineCurve, float splineTimePoint,
			Transform holderObject, Quaternion objectModificationRotation, System.Random randomClass, 
			out Vector3 finalBaseObjectPosition)
		{
			//Set up default values
			finalBaseObjectPosition = Vector3.zero;

			//Get the curve position and rotation
			bool evaluateValid = splineCurve.Evaluate(splineTimePoint, out float3 splinePos, out float3 splineTangent, out float3 splineUpVector);
			Quaternion curveForwardRotation = Quaternion.LookRotation(splineTangent, splineUpVector);

			if(!evaluateValid)
			{
				Debug.Log("OPSSplineObjectSpawner: Spline Curve evaluation invalid in SpawnSplineObjectOnPoint()!");
				return new List<GameObject>();
			}

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

			//Get the final transformation
			Quaternion finalRotation = startRotation * objectModificationRotation;
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
				thisObject.transform.SetParent(holderObject, false);
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
			UnityEditor.Undo.RegisterCreatedObjectUndo(ret, "Created Spline Reference Object");
#else
			ret = GameObject.Instantiate(spawnReference.prefabObject);
#endif

			return ret;
		}

		//UTILITY

		/// <summary>
		/// Get the remainder (positive and negative) of a % b.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static float FloatMod(float a, float b)
		{
			return a - (b * Mathf.Floor(a / b));
		}
	}

}