using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace RobProductions.OpenSplinePlacer.Runtime
{
	[CreateAssetMenu(fileName = "OSP Spline Object", menuName = "Open Spline Placer/OSP Spline Object")]
	public class OSPSplineObject : ScriptableObject
	{
		[System.Serializable]
		public class SplineObjectSpawnReference
		{
			[Header("Prefab")]
			public GameObject prefabObject;

			[Header("Settings")]
			public float objectProbability = 100f;
			public float stackHeightFromOrigin = 10f;

			/// <summary>
			/// Returns true if the prefabObject is null on this reference.
			/// </summary>
			/// <returns></returns>
			public bool IsPrefabNull()
			{
				return (prefabObject == null);
			}
		}

		public enum SplineObjectStackType
		{
			None = 0,
			StackCount = 1,
		}

		public enum SplineObjectSupportsType
		{
			None = 0,
			AllSupports = 1,
		}
		//TODO: Could eventually support picking a random support

		[System.Serializable]
		public class SplineObjectSupportReference
		{
			[Header("References")]
			public SplineObjectSpawnReference supportBeamReference;
			public SplineObjectSpawnReference supportBaseReference;

			[Header("Support Settings")]
			public bool ignoreBeamIfNoRaycastHit = false;
			public bool ignoreSupportBaseIfNoRaycastHit = false;

			[Header("Beam Settings")]
			public Vector3 beamOffsetPositionFromBase = Vector3.zero;
			public Vector3 beamOffsetRotationFromBase = Vector3.zero;

			[Header("Support Base Settings")]
			public bool supportBaseRaycastWorldDirection = false;
			public Vector3 supportBaseRaycastDirection = Vector3.down;
			public float supportBaseRaycastLength = 10f;
			public float supportBaseRaycastRadius = 1.0f;
			public LayerMask supportBaseRaycastMask;

			public Vector3 supportBaseRelativeOffsetPosition = Vector3.zero;
			[Range(0f, 1f)]
			public float supportBaseMatchGroundRotationAmount = 0.5f;
		}

		[System.Serializable]
		public class SplineObjectSpawningParams
		{
			[Header("Base")]
			public SplineObjectSpawnReference baseSpawnReference;

			[Header("Stacks")]
			public SplineObjectStackType stackType = SplineObjectStackType.None;
			public SplineObjectSpawnReference[] stackReferenceVariations;
			public SplineObjectSpawnReference[] topReferenceVariations;

			public Vector2Int stackCountRange = new Vector2Int(0, 2);
			[Range(0f, 1f)]
			public float useTopReferenceProbability = 1.0f;

			[Header("Supports")]
			public SplineObjectSupportsType supportsType = SplineObjectSupportsType.None;
			public SplineObjectSupportReference[] supportReferences;
		}

		public SplineObjectSpawningParams spawningParams = new SplineObjectSpawningParams();

		public enum SplineObjectRotationType
		{
			None = 0,
			DiscreteValues = 1,
			RangeValue = 2,
		}

		public enum SplineObjectRotationSpace
		{
			LocalToSplineDirection = 0,
			LocalToHolderObject = 1,
			Global = 2,
		}

		[System.Serializable]
		public class SplineObjectWeightedDiscreteRotation
		{
			[Header("Rotation")]
			public Vector3 rotationEuler;

			[Header("Settings")]
			public float rotationProbability = 100f;
		}

		[System.Serializable]
		public class SplineObjectPlacementParams
		{
			[Header("Spacing")]
			public float objectLengthZ = 10.0f;
			public float objectLengthX = 10.0f;

			[Header("Rotation")]
			public SplineObjectRotationType rotationType = SplineObjectRotationType.DiscreteValues;
			public SplineObjectRotationSpace rotationSpace = SplineObjectRotationSpace.LocalToSplineDirection;

			public SplineObjectWeightedDiscreteRotation[] possibleRotations;
			public Vector2 rotationXRange = new Vector2(0.0f, 360.0f);
			public Vector2 rotationYRange = new Vector2(0.0f, 360.0f);
			public Vector2 rotationZRange = new Vector2(0.0f, 360.0f);
		}

		public SplineObjectPlacementParams placementParams = new SplineObjectPlacementParams();

		/// <summary>
		/// Return a random discrete rotation possibility
		/// based on the weight using the provided random class.
		/// </summary>
		/// <param name="randomClass"></param>
		/// <returns></returns>
		public Vector3 GetRandomDiscreteRotation(System.Random randomClass)
		{
			if (placementParams.possibleRotations.Length <= 0)
			{
				Debug.Log("OSPSplineObject: Discrete Rotation possibilities list was length 0 in GetRandomDiscreteRotation()!");
				return Vector3.zero;
			}

			//Sum the probabilities
			float totalWeight = placementParams.possibleRotations.Sum(container => container.rotationProbability);

			//Get the location of the rotation to pick
			float randomFloat = (float)randomClass.NextDouble();
			float weightSelection = Mathf.Lerp(0.0f, totalWeight, randomFloat);

			//Iterate through rotations
			for (int i = 0; i < placementParams.possibleRotations.Length; i++)
			{
				var thisContainer = placementParams.possibleRotations[i];
				if (weightSelection < thisContainer.rotationProbability)
				{
					return thisContainer.rotationEuler;
				}

				weightSelection -= thisContainer.rotationProbability;
			}

			//Fallback in case we hit the end
			return placementParams.possibleRotations[placementParams.possibleRotations.Length - 1].rotationEuler;
		}

		/// <summary>
		/// Returns a random spawn reference within the given list
		/// based on weighted probability.
		/// </summary>
		/// <param name="spawnReferenceList"></param>
		/// <param name="randomClass"></param>
		/// <returns></returns>
		public SplineObjectSpawnReference GetRandomSpawnReference(SplineObjectSpawnReference[] spawnReferenceList, System.Random randomClass)
		{
			if(spawnReferenceList.Length <= 0)
			{
				Debug.Log("OSPSplineObject: Spawn Reference List length was 0 in GetRandomSpawnReference()!");
				return null;
			}

			//Sum the probabilities
			float totalWeight = spawnReferenceList.Sum(spawnReference => spawnReference.objectProbability);

			//Get the location of the reference object to pick
			float randomFloat = (float)randomClass.NextDouble();
			float weightSelection = Mathf.Lerp(0.0f, totalWeight, randomFloat);

			//Iterate through reference objects
			for(int i = 0; i < spawnReferenceList.Length; i++)
			{
				var thisRef = spawnReferenceList[i];
				if(weightSelection < thisRef.objectProbability)
				{
					return thisRef;
				}

				weightSelection -= thisRef.objectProbability;
			}

			//Fallback in case we hit the end
			return spawnReferenceList[spawnReferenceList.Length - 1];
		}
	}
}