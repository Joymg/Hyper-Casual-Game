using UnityEngine;

public class MoveAlongSpline : MonoBehaviour
{

	public BezierSpline spline;

	public float duration;

	private float progress;

	public bool lookForward;

	public MoveAlongSplineMode mode;
	private bool goingForward = true;

	private void Update()
	{
		if (goingForward)
		{
			progress += Time.deltaTime / duration;
			if (progress > 1f)
			{
				if (mode == MoveAlongSplineMode.Once)
				{
					progress = 1f;
				}
				else if (mode == MoveAlongSplineMode.Loop)
				{
					progress -= 1f;
				}
				else
				{
					progress = 2f - progress;
					goingForward = false;
				}
			}
		}
		else
		{
			progress -= Time.deltaTime / duration;
			if (progress < 0f)
			{
				progress = -progress;
				goingForward = true;
			}
		}

		Vector3 position = spline.GetPoint(progress);
		transform.localPosition = position;
		if (lookForward)
		{
			transform.LookAt(position + spline.GetDirection(progress));
		}
	}
}