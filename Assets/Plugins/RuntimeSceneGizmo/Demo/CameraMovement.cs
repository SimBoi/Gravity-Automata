using UnityEngine;

namespace RuntimeSceneGizmo
{
	public class CameraMovement : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private float sensitivity = 0.5f;
		[SerializeField]
		private float zoomSensitivity = 1.2f;
#pragma warning restore 0649

		public int MouseButton = 1;

		private Vector3 prevMousePos;
		private Transform mainCamParent;

		private void Awake()
		{
			mainCamParent = Camera.main.transform.parent;
		}

		private void Update()
		{
			if (Input.GetMouseButtonDown(MouseButton))
				prevMousePos = Input.mousePosition;
			else if (Input.GetMouseButton(MouseButton))
			{
				Vector3 mousePos = Input.mousePosition;
				Vector2 deltaPos = (mousePos - prevMousePos) * sensitivity;

				Vector3 rot = mainCamParent.localEulerAngles;
				while (rot.x > 180f)
					rot.x -= 360f;
				while (rot.x < -180f)
					rot.x += 360f;

				rot.x = Mathf.Clamp(rot.x - deltaPos.y, -89.8f, 89.8f);
				rot.y += deltaPos.x;
				rot.z = 0f;

				mainCamParent.localEulerAngles = rot;
				prevMousePos = mousePos;
			}

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            {
                transform.localScale /= zoomSensitivity;
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            {
                transform.localScale *= zoomSensitivity;
            }
        }
	}
}