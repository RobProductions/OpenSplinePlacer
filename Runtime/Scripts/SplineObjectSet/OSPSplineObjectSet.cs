using System.Linq;
using UnityEngine;

namespace RobProductions.OpenSplinePlacer.Runtime
{
	[CreateAssetMenu(fileName = "OSP Spline Object Set", menuName = "Open Spline Placer/OSP Spline Object Set")]
	public class OSPSplineObjectSet : ScriptableObject
	{
		[System.Serializable]
		public class SplineObjectContainer
		{
			[Header("References")]
			public OSPSplineObject splineObject;

			[Header("Settings")]
			public float objectProbability = 100f;
		}

		[System.Serializable]
		public class SplineObjectSetSpawning
		{
			[Header("Definitions")]
			public SplineObjectContainer[] splineObjectContainers;
			public SplineObjectContainer[] splineConnectorContainers;

			[Header("Settings")]
			public float objectPadding = 0.5f;
			public int objectsBeforeConnectorInterval = 0;
		}

		public SplineObjectSetSpawning spawningParams = new SplineObjectSetSpawning();

		/// <summary>
		/// Returns a random container from our spawning list
		/// based on the probability of each container.
		/// </summary>
		/// <param name="randomClass"></param>
		/// <returns></returns>
		public SplineObjectContainer GetRandomSplineObjectContainer(SplineObjectContainer[] containerList, System.Random randomClass)
		{
			if(containerList.Length <= 0)
			{
				return null;
			}

			//Sum the probabilities
			float totalWeight = containerList.Sum(container => container.objectProbability);

			//Get the location of the container to pick
			float randomFloat = (float)randomClass.NextDouble();
			float weightSelection = Mathf.Lerp(0.0f, totalWeight, randomFloat);

			//Iterate through containers
			for(int i = 0; i < containerList.Length; i++)
			{
				var thisContainer = containerList[i];
				if (weightSelection < thisContainer.objectProbability)
				{
					return thisContainer;
				}

				weightSelection -= thisContainer.objectProbability;
			}

			//Fallback in case we hit the end
			return containerList[containerList.Length - 1];
		}
	}
}