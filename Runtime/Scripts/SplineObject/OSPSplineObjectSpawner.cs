using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

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
			bool evaluateValid = splineCurve.Evaluate(splineTimePoint, out float3 localSplinePos, out float3 splineTangent, out float3 splineUpVector);
			Quaternion curveForwardRotation = Quaternion.LookRotation(splineTangent, splineUpVector);

			if(!evaluateValid)
			{
				Debug.Log("OPSSplineObjectSpawner: Spline Curve evaluation invalid in SpawnSplineObjectOnPoint()!");
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
			Vector3 finalPosition = holderObject.TransformPoint(localSplinePos);

			//Spawn the spline object
			var spawnedObjects = SpawnSplineObject(splineObject, finalPosition, finalRotation, randomClass);
			if (spawnedObjects.Count <= 0)
			{
				Debug.Log("OPSSplineObjectSpawner: No objects spawned on spline point: " + splineTimePoint);
				return new List<GameObject>();
			}

			//Assign the parent to the spawned objects
			foreach(GameObject spawnedObject in spawnedObjects)
			{
				spawnedObject.transform.SetParent(holderObject, true);
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
		public static List<GameObject> SpawnSplineObject(OSPSplineObject splineObject, Vector3 basePosition, Quaternion baseRotation, System.Random randomClass)
		{
			var ret = new List<GameObject>();

			//If we don't have a base object, this is all useless
			if (splineObject.spawningParams.baseSpawnReference.IsPrefabNull())
			{
				return ret;
			}

			//Define params
			float stackHeightFromBase = 0.0f;

			//Spawn the base object
			ret.Add(SpawnReferenceObject(splineObject.spawningParams.baseSpawnReference, basePosition, baseRotation));
			stackHeightFromBase += splineObject.spawningParams.baseSpawnReference.stackHeightFromOrigin;

			//Spawn the stack objects
			if(splineObject.spawningParams.stackType != OSPSplineObject.SplineObjectStackType.None)
			{
				//Use a random number of stack objects
				int stackCount = randomClass.Next(splineObject.spawningParams.stackCountRange.x, splineObject.spawningParams.stackCountRange.y + 1);
				for(int i = 0; i < stackCount; i++)
				{
					var stackReferenceSelection = splineObject.GetRandomSpawnReference(splineObject.spawningParams.stackReferenceVariations, randomClass);

					Vector3 newStackObjectPos = basePosition + (baseRotation * new Vector3(0f, stackHeightFromBase, 0f));
					ret.Add(SpawnReferenceObject(stackReferenceSelection, newStackObjectPos, baseRotation));

					stackHeightFromBase += stackReferenceSelection.stackHeightFromOrigin;
				}
			}

			//Spawn the support objects
			if(splineObject.spawningParams.supportReferences != null)
			{
				foreach (OSPSplineObject.SplineObjectSupportReference supportRef in splineObject.spawningParams.supportReferences)
				{
					var supportObjects = SpawnSplineObjectSupport(supportRef, basePosition, baseRotation);
					ret.AddRange(supportObjects);
				}
			}

			return ret;
		}

		/// <summary>
		/// Creates the support objects for this spline by spawning in the beam
		/// and using raycasts to find any intersection points.
		/// A support base object can spawn at the ray hit location
		/// or at the end of the raycast based on length settings.
		/// </summary>
		/// <param name="supportReference"></param>
		/// <param name="basePosition"></param>
		/// <param name="baseRotation"></param>
		/// <returns></returns>
		private static List<GameObject> SpawnSplineObjectSupport(OSPSplineObject.SplineObjectSupportReference supportReference,
			Vector3 basePosition, Quaternion baseRotation)
		{
			var ret = new List<GameObject>();

			//First check for raycast hit
			Vector3 supportRaycastDirection = supportReference.supportBaseRaycastDirection;
			if(!supportReference.supportBaseRaycastWorldDirection)
			{
				supportRaycastDirection = baseRotation * supportReference.supportBaseRaycastDirection;
			}

			RaycastHit rayHitInfo;
			bool didHitGround = Physics.SphereCast(basePosition, supportReference.supportBaseRaycastRadius, supportRaycastDirection,
				out rayHitInfo, supportReference.supportBaseRaycastLength, supportReference.supportBaseRaycastMask);

			//Spawn the beam object
			var offsetPosition = basePosition + (baseRotation * supportReference.beamOffsetPositionFromBase);
			var offsetRotation = baseRotation * Quaternion.Euler(supportReference.beamOffsetRotationFromBase);

			if (!supportReference.supportBeamReference.IsPrefabNull() && (didHitGround || !supportReference.ignoreBeamIfNoRaycastHit))
			{
				ret.Add(SpawnReferenceObject(supportReference.supportBeamReference, offsetPosition, offsetRotation));
			}

			//Spawn the support base object
			Vector3 supportBasePosition = basePosition + (supportRaycastDirection * supportReference.supportBaseRaycastLength);
			Quaternion supportBaseRotation = baseRotation;
			if(didHitGround)
			{
				//If we hit the ground, we should align the support base to the hit point
				Vector3 forwardOnPlane = Vector3.ProjectOnPlane(baseRotation.eulerAngles, rayHitInfo.normal);
				Quaternion groundRotation = Quaternion.LookRotation(forwardOnPlane, rayHitInfo.normal);
				supportBaseRotation = Quaternion.Slerp(baseRotation, groundRotation, supportReference.supportBaseMatchGroundRotationAmount);

				supportBasePosition = rayHitInfo.point;
			}
			supportBasePosition += baseRotation * supportReference.supportBaseRelativeOffsetPosition;

			if(!supportReference.supportBaseReference.IsPrefabNull() && (didHitGround || !supportReference.ignoreSupportBaseIfNoRaycastHit))
			{
				ret.Add(SpawnReferenceObject(supportReference.supportBaseReference, supportBasePosition, supportBaseRotation));
			}

			return ret;
		}

		/// <summary>
		/// Create a reference object (direct instantiate or with prefab link)
		/// based on the prefab listed in the spawnReference provided.
		/// </summary>
		/// <param name="spawnReference"></param>
		/// <returns></returns>
		private static GameObject SpawnReferenceObject(OSPSplineObject.SplineObjectSpawnReference spawnReference,
			Vector3 objectPosition, Quaternion objectRotation)
		{
			GameObject ret;

			//Spawn the object differently depending on Editor mode
			//so we keep the Prefab link for future changes
#if UNITY_EDITOR
			ret = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(spawnReference.prefabObject);
			UnityEditor.Undo.RegisterCreatedObjectUndo(ret, "Created Spline Reference Object");
#else
			ret = GameObject.Instantiate(spawnReference.prefabObject);
#endif

			//Set transform values
			ret.transform.position = objectPosition;
			ret.transform.rotation = objectRotation;

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