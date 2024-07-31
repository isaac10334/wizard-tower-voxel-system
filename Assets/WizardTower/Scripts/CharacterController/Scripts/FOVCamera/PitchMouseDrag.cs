using UnityEngine;

namespace FOVCamera
{
    [RequireComponent(typeof(PitchCamera))]
    public class PitchMouseDrag : MonoBehaviour
    {
        public bool lockImmediately = true;
        public bool invertY = false;
        private const string c_MouseX = "Mouse X";
        private const string c_MouseY = "Mouse Y";

        [SerializeField] private float _sensetive = 1f;

        private PitchCamera _pitchCameraCache;

        private PitchCamera _pitchCamera
        {
            get
            {
                if (_pitchCameraCache == null)
                {
                    _pitchCameraCache = GetComponent<PitchCamera>();
                }

                return _pitchCameraCache;
            }
        }

        public bool IsLocked { private set; get; }

        private void Start()
        {
            if (lockImmediately)
            {
                Lock();
            }
        }


        private Vector2 DeltaMouse()
        {
            return new Vector2(Input.GetAxis(c_MouseX), invertY ? Input.GetAxis(c_MouseY) : -Input.GetAxis(c_MouseY));
        }

        private void Update()
        {
            if (IsLocked)
            {
                Vector2 preDelta = DeltaMouse() * _sensetive;
                var delta = new Vector2(preDelta.x, -preDelta.y);

                _pitchCamera.Angles -= delta;

                _pitchCamera.ApplyTransform();
            }
        }

        public void Unlock()
        {
            IsLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Lock()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            IsLocked = true;
        }
    }
}