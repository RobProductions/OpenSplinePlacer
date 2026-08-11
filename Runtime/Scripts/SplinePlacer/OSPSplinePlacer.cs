using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace RobProductions.OpenSplinePlacer.Runtime
{
	public class OSPSplinePlacer : MonoBehaviour
	{
		public SplineContainer splineContainer;
		public OSPSplineObjectSet splineObjectSet;

		[System.Serializable]
		public class SplinePlacerStats
		{
			[Header("Spawning")]
			public int randomSeedValue = 500;
			public float additionalContainerPadding = 0.0f;
			public bool ignoreDestroyChildrenOnGenerate = false;

			[Header("Limits")]
			public int maxContainerSpawnCount = 1000;

			[Header("Logging")]
			public bool logObjectGeneration = true;
		}

		public SplinePlacerStats stats = new SplinePlacerStats();

		void Awake()
		{

		}

		//USER FUNCTIONS

		[ContextMenu("Destroy Children Objects")]
		public void UserDestroyChildrenObjects()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCompleteObjectUndo(gameObject, "Spline Placer: Destroy Children Objects");
#endif

			DestroyChildrenObjects();
		}

		[ContextMenu("Generate Spline Objects")]
		public void UserGenerateSplineObjects()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCompleteObjectUndo(gameObject, "Spline Placer: Generate Spline Objects");
#endif

			if(!stats.ignoreDestroyChildrenOnGenerate)
			{
				DestroyChildrenObjects();
			}
			GenerateSplineContainerObjects();
		}

		//OBJECT MANAGEMENT

		/// <summary>
		/// Iterate through all children of this placer object
		/// and destroy them so we can make room for a new generation.
		/// </summary>
		void DestroyChildrenObjects()
		{
			for(int i = transform.childCount - 1; i >= 0; i--)
			{
				PlacerDestroyObject(transform.GetChild(i).gameObject);
			}
		}

		void PlacerDestroyObject(GameObject objectToDestroy)
		{
#if UNITY_EDITOR
			UnityEditor.Undo.DestroyObjectImmediate(objectToDestroy);
#else
			Destroy(objectToDestroy);
#endif
		}

		//SPLINE PLACEMENT

		/// <summary>
		/// Generate objects for each spline contained
		/// in the spline container - a spline container
		/// can hold multiple child splines.
		/// </summary>
		void GenerateSplineContainerObjects()
		{
			if(splineContainer == null)
			{
				Debug.Log("OSP: Spline Container was null in GenerateSplineObjects()!", gameObject);
				return;
			}

			//Create random class
			System.Random randomClass = new System.Random(stats.randomSeedValue);

			//Iterate through all sub splines
			int totalObjectsCount = 0;
			foreach(Spline thisSpline in splineContainer.Splines)
			{
				totalObjectsCount += GenerateSplineTargetObjects(thisSpline, randomClass);
			}

			//Finalize
			if(stats.logObjectGeneration)
			{
				Debug.Log("OSP: Successfully genarated spline objects! Count: " + totalObjectsCount, gameObject);
			}
		}

		int GenerateSplineTargetObjects(Spline splineTarget, System.Random randomClass)
		{
			if(splineContainer == null)
			{
				Debug.Log("OSP: Spline Container was null in GenerateSplineObjects()!", gameObject);
				return 0;
			}
			if(splineTarget == null)
			{
				Debug.Log("OSP: Spline Target was null in GenerateSplineObjects()!", gameObject);
				return 0;
			}
			if(splineObjectSet == null)
			{
				Debug.Log("OSP: SplineObjectSet was null in GenerateSplineObjects()! Assign it to the SplinePlacer!", gameObject);
				return 0;
			}

			//Traverse the spline and spawn objects along it
			float splineUnitLength = splineTarget.GetLength();

			float objectsLengthTraverse = 0.0f;
			int containerSpawnCount = 0;
			while(objectsLengthTraverse < splineUnitLength)
			{
				//Calculate the spline time point via ratio (L + ratio)
				float splineTimePoint = Mathf.InverseLerp(0.0f, splineUnitLength, objectsLengthTraverse);

				//Time to spawn a new spline object!
				OSPSplineObjectSet.SplineObjectContainer selectedContainer = splineObjectSet.GetRandomSplineObjectContainer(randomClass);
				List<GameObject> spawnedObjects = OSPSplineObjectSpawner.SpawnSplineObjectOnSplinePoint(selectedContainer.splineObject, splineTarget, splineTimePoint,
					transform, randomClass, out Vector3 finalPlacePosition, out float finalObjectLength);

				//Increase raversal length for next container
				objectsLengthTraverse += finalObjectLength;
				objectsLengthTraverse += splineObjectSet.spawningParams.objectPadding;
				objectsLengthTraverse += stats.additionalContainerPadding;

				//Increase spawn count
				containerSpawnCount++;

				//Fallback break for too many objects
				if(containerSpawnCount >= stats.maxContainerSpawnCount)
				{
					Debug.Log("OSP: Container Spawn limit exceeded! " + stats.maxContainerSpawnCount, gameObject);
					break;
				}
			}

			return containerSpawnCount;
		}
	}
}