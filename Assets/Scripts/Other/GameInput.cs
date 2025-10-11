using UnityEngine;
using UnityEngine.EventSystems;

namespace Other
{
    public sealed class GameInput : MonoBehaviour
    {
        public static GameInput Instance;

        public static float PressingTime;

        public static float TimeSinceLastLeftClick = 100f;

        public static bool LeftDoubleClick;

        public static bool PressingAfterLeftDoubleClick;

        public static float DeltaTime;
        public static float SimDeltaTime;
        public static float SimSpeed = 1;

        public static Camera MainCamera;

        public float maxTimeBetweenDoubleClicks = 0.5f;

        public static bool JustClicked => Input.GetMouseButtonDown(0) ||
                                          (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);

        public static bool JustClickedOutsideUI =>
            (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)) &&
            !IsPointerOverUI;

        // shouldn't be used
        public static bool JustRightClicked => Input.GetMouseButtonDown(1);

        public static bool JustStoppedClicking
        {
            get
            {
                foreach (var touch in Input.touches)
                    if (touch.phase == TouchPhase.Ended)
                        return true;

                return Input.GetMouseButtonUp(0);
            }
        }

        public static bool JustStoppedClickingOutsideUI
        {
            get
            {
                foreach (var touch in Input.touches)
                    if (touch.phase == TouchPhase.Ended && !IsPointerOverUI)
                        return true;

                return Input.GetMouseButtonUp(0) && !IsPointerOverUI;
            }
        }

        public static bool Pressing => Input.GetMouseButton(0) ||
                                       (Input.touchCount == 1 && Input.GetTouch(0).phase != TouchPhase.Ended);

        public static GameObject ObjectUnderPointer
        {
            get
            {
                var pointerPos = ViewportPointerPosition;

                var ray = MainCamera.ViewportPointToRay(pointerPos);

                var hit = Physics2D.Raycast(ray.origin, ray.direction);

                if (hit) return hit.transform.gameObject;

                return null;
            }
        }

        // warning: returns positive infinity for no touches 
        public static Vector2 WorldPointerPosition
        {
            get
            {
                var pointerPos = Vector2.positiveInfinity;
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touchCount >= 1) pointerPos = Input.GetTouch(0).position;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                pointerPos = MainCamera.ScreenToWorldPoint(pointerPos);

                return pointerPos;
            }
        }

        // warning: returns positive infinity for no touches
        public static Vector2 ScreenPointerPosition
        {
            get
            {
                var pointerPos = Vector2.positiveInfinity;
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touchCount >= 1) pointerPos = Input.GetTouch(0).position;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                return pointerPos;
            }
        }

        // warning: returns positive infinity for no touches
        public static Vector2 CenteredScreenPointerPosition
        {
            get
            {
                var pointerPos = ScreenPointerPosition;

                return pointerPos - new Vector2(Screen.width, Screen.height);
            }
        }

        //public static bool PointerInsideMap
        //{
        //    get
        //    {
        //        Vector2 pointerPos = WorldPointerPosition;

        //        Vector2Int tilePos = Cord.ToVecInt(pointerPos);

        //        return Tiles.PosInsideMap(tilePos);
        //    }
        //}

        // if tiles are in 1D array
        //public static int TileIndexFromPointer
        //{
        //    get
        //    {
        //        Vector3 pointerPos = WorldPointerPosition;

        //        Debug.Log(pointerPos);

        //        int indexOfTileUnderPointer = Cord.TileIndexFromPos(pointerPos);

        //        return indexOfTileUnderPointer;
        //    }
        //}

        public static float TouchesAndPointersCount => Input.touchCount + (Pressing ? 1 : 0);

        // warning: returns positive infinity for more touches than one or none
        public static Vector2 ViewportPointerPosition
        {
            get
            {
                var pointerPos = Vector2.positiveInfinity;
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    if (Input.touchCount == 1) pointerPos = Input.GetTouch(0).position;
                }
                else
                {
                    pointerPos = Input.mousePosition;
                }

                pointerPos = MainCamera.ScreenToViewportPoint(pointerPos);

                return pointerPos;
            }
        }

        public static bool IsPointerOverUI
        {
            get
            {
                if (EventSystem.current.IsPointerOverGameObject()) return true;

                if (Input.touchCount > 0)
                {
                    var id = Input.touches[0].fingerId;
                    if (EventSystem.current.IsPointerOverGameObject(id)) return true;
                }

                return false;
            }
        }

        private void Awake()
        {
            Instance = this;
            SimSpeed = 1f;
        }

        private void Start()
        {
            MainCamera = Camera.main;
        }

        private void Update()
        {
            DeltaTime = Time.deltaTime;
            SimDeltaTime = DeltaTime * SimSpeed;

            LeftDoubleClick = false;

            if (JustClicked)
            {
                if (TimeSinceLastLeftClick < maxTimeBetweenDoubleClicks)
                {
                    LeftDoubleClick = true;
                    PressingAfterLeftDoubleClick = true;

                    // after double click, we don't want our next click to also be double click
                    TimeSinceLastLeftClick = maxTimeBetweenDoubleClicks;
                }
                else
                {
                    TimeSinceLastLeftClick = 0f;
                }
            }
            else
            {
                if (JustStoppedClicking) PressingAfterLeftDoubleClick = false;

                if (Pressing)
                    PressingTime += DeltaTime;
                else
                    PressingTime = 0f;

                TimeSinceLastLeftClick += DeltaTime;
            }
        }
    }
}