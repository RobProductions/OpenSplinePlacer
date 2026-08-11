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
			//[Header("")]
		}

		public SplinePlacerStats stats = new SplinePlacerStats();

		void Awake()
		{

		}

		//USER FUNCTIONS

		[ContextMenu("Generate Spline Objects")]
		public void UserGenerateSplineObjects()
		{
			GenerateSplineContainerObjects();
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

			//Iterate through all sub splines
			foreach(Spline thisSpline in splineContainer.Splines)
			{
				GenerateSplineTargetObjects(thisSpline);
			}
		}

		void GenerateSplineTargetObjects(Spline splineTarget)
		{
			if(splineContainer == null)
			{
				Debug.Log("OSP: Spline Container was null in GenerateSplineObjects()!", gameObject);
				return;
			}
			if(splineTarget == null)
			{
				Debug.Log("OSP: Spline Target was null in GenerateSplineObjects()!", gameObject);
				return;
			}
			if(splineObjectSet == null)
			{
				Debug.Log("OSP: SplineObjectSet was null in GenerateSplineObjects()! Assign it to the SplinePlacer!", gameObject);
				return;
			}

			//Traverse the spline and spawn objects along it
			float splineUnitLength = splineTarget.GetLength();

			Debug.Log(splineUnitLength);
		}
	}
}