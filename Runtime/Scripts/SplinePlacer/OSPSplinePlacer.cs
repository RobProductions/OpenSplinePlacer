using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

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
			public float additionalObjectSpacing = 0.0f;
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

#if UNITY_EDITOR
		[UnityEditor.ShortcutManagement.Shortcut("Open Spline Placer/Generate Spline Objects", KeyCode.O, UnityEditor.ShortcutManagement.ShortcutModifiers.Shift)]
		public static void EditorGenerateSplineObjectsOnChildren()
		{
			foreach (GameObject thisObject in UnityEditor.Selection.gameObjects)
			{
				var instancingGroups = thisObject.GetComponentsInChildren<OSPSplinePlacer>();
				foreach (OSPSplinePlacer thisGroup in instancingGroups)
				{
					if (!thisGroup.gameObject.activeInHierarchy)
					{
						continue;
					}

					thisGroup.UserGenerateSplineObjects();
				}
			}
		}
#endif

		/// <summary>
		/// Start the destruction of children objects
		/// to clear space for new objects to be generated.
		/// </summary>
		[ContextMenu("Destroy Children Objects")]
		public void UserDestroyChildrenObjects()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RegisterCompleteObjectUndo(gameObject, "Spline Placer: Destroy Children Objects");
#endif

			DestroyChildrenObjects();
		}

		/// <summary>
		/// Destroy children objects (if settings permit)
		/// and start the spline object generation function
		/// based on the object group and stats set up on this placer component.
		/// </summary>
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
			int placeObjectsBeforeConnectorCount = 0;
			int containerSpawnCount = 0;
			while(objectsLengthTraverse < splineUnitLength)
			{
				//Determine which type of object to spawn
				OSPSplineObjectSet.SplineObjectContainer[] containerList = splineObjectSet.spawningParams.splineObjectContainers;
				if(splineObjectSet.spawningParams.objectsBeforeConnectorInterval > 0)
				{
					//We should consider connectors
					if(placeObjectsBeforeConnectorCount >= splineObjectSet.spawningParams.objectsBeforeConnectorInterval)
					{
						//This is a connector object
						placeObjectsBeforeConnectorCount = 0;
						containerList = splineObjectSet.spawningParams.splineConnectorContainers;
					}
					else
					{
						//This is a regular object, so increment the count
						placeObjectsBeforeConnectorCount++;
					}
				}

				//Time to spawn a new spline object!
				//Here we decide which spline object to spawn and its rotation
				OSPSplineObjectSet.SplineObjectContainer selectedContainer = splineObjectSet.GetRandomSplineObjectContainer(containerList, randomClass);
				OSPSplineObjectSpawner.DetermineSplineObjectRotation(selectedContainer.splineObject, randomClass, 
					out Quaternion objectModificationRotation, out float finalObjectLength);

				//Define lengths
				float halfObjectLength = finalObjectLength * 0.5f;
				float halfObjectPadding = splineObjectSet.spawningParams.objectPadding * 0.5f;

				//Increase traversal by half the object length to get a good starting point
				objectsLengthTraverse += halfObjectLength;
				objectsLengthTraverse += halfObjectPadding;

				//Calculate the spline time point via ratio (L + ratio)
				float splineTimePoint = Mathf.InverseLerp(0.0f, splineUnitLength, objectsLengthTraverse);

				//Then actually spawn the object at the traversal point on the spline
				List<GameObject> spawnedObjects = OSPSplineObjectSpawner.SpawnSplineObjectOnSplinePoint(selectedContainer.splineObject, splineTarget, splineTimePoint,
					transform, objectModificationRotation, randomClass, out Vector3 finalBaseObjectPosition);

				//Increase raversal length for next container
				objectsLengthTraverse += halfObjectLength;
				objectsLengthTraverse += halfObjectPadding;
				objectsLengthTraverse += stats.additionalObjectSpacing;

				//Increase spawn count
				containerSpawnCount++;

				//Fallback break for too many objects
				if (containerSpawnCount >= stats.maxContainerSpawnCount)
				{
					Debug.Log("OSP: Container Spawn limit exceeded! " + stats.maxContainerSpawnCount, gameObject);
					break;
				}
			}

			return containerSpawnCount;
		}
	}
}