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
			public GameObject prefabObject;

			/// <summary>
			/// Returns true if the prefabObject is null on this reference.
			/// </summary>
			/// <returns></returns>
			public bool IsPrefabNull()
			{
				return (prefabObject == null);
			}
		}

		[System.Serializable]
		public class SplineObjectSpawningParams
		{
			[Header("Base")]
			public SplineObjectSpawnReference baseSpawnReference;

			[Header("Stacks")]
			public SplineObjectSpawnReference[] stackReferenceVariations;
			public SplineObjectSpawnReference[] topReferenceVariations;

			public Vector2Int stackCountRange = new Vector2Int(0, 2);
			public float useTopBaseProbability = 1.0f;
		}

		public SplineObjectSpawningParams spawningParams;

		public enum SplineObjectRotationType
		{
			DiscreteValues = 0,
			RangeValue = 1,
		}

		public enum SplineObjectRotationSpace
		{
			LocalToSplineDirection = 0,
			LocalToHolderObject = 1,
			Global = 2,
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

			public Vector3[] possibleRotations;
			public Vector2 rotationXRange = new Vector2(0.0f, 360.0f);
			public Vector2 rotationYRange = new Vector2(0.0f, 360.0f);
			public Vector2 rotationZRange = new Vector2(0.0f, 360.0f);
		}

		public SplineObjectPlacementParams placementParams;
	}
}