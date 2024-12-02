using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class GameInput : MonoBehaviour
{
    public static GameInput Instance;

    public static float pressingTime;

    public static float timeSinceLastLeftClick = 100f;

    public float maxTimeBetweenDoubleClicks = 0.5f;

    public static bool leftDoubleClick = false;

    public static bool pressingAfterLeftDoubleClick = false;

    public static float deltaTime;
    public static float simDeltaTime;
    public static float simSpeed = 1;

    void Awake()
    {
        Instance = this;
        simSpeed = 1f;
    }

    void Update()
    {
        deltaTime = Time.deltaTime;
        simDeltaTime = deltaTime * simSpeed;

        leftDoubleClick = false;

        if (JustClicked)
        {
            if (timeSinceLastLeftClick < maxTimeBetweenDoubleClicks)
            {
                leftDoubleClick = true;
                pressingAfterLeftDoubleClick = true;

                // after double click, we don't want our next click to also be double click
                timeSinceLastLeftClick = maxTimeBetweenDoubleClicks;
            }
            else
            {
                timeSinceLastLeftClick = 0f;
            }
        }
        else
        {
            if (JustStoppedClicking)
            {
                pressingAfterLeftDoubleClick = false;
            }

            if (Pressing)
            {
                pressingTime += deltaTime;
            }
            else
            {
                pressingTime = 0f;
            }

            timeSinceLastLeftClick += deltaTime;
        }
    }

    public static bool JustClicked
    {
        get
        {
            return Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began);
        }
    }

    public static bool JustClickedOutsideUI
    {
        get
        {
            return (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)) && !IsPointerOverUI;
        }
    }

    // shouldn't be used
    public static bool JustRightClicked
    {
        get
        {
            return Input.GetMouseButtonDown(1) || (Input.touchCount == 2 && Input.GetTouch(1).phase == TouchPhase.Began) && false;
        }
    }

    public static bool JustStoppedClicking
    {
        get
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Ended)
                {
                    return true;
                }
            }
            return Input.GetMouseButtonUp(0);
        }
    }

    public static bool JustStoppedClickingOutsideUI
    {
        get
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Ended && !IsPointerOverUI)
                {
                    return true;
                }
            }
            return Input.GetMouseButtonUp(0) && !IsPointerOverUI;
        }
    }

    public static bool Pressing
    {
        get
        {
            return Input.GetMouseButton(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase != TouchPhase.Ended);
        }
    }

    public static GameObject ObjectUnderPointer
    {
        get
        {
            Vector2 pointerPos = ViewportPointerPosition;

            Ray ray = Camera.main.ViewportPointToRay(pointerPos);

            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

            if (hit)
            {
                return hit.transform.gameObject;
            }

            return null;
        }
    }

    // warning: returns positive infinity for no touches 
    public static Vector2 WorldPointerPosition
    {
        get
        {
            Vector2 pointerPos = Vector2.positiveInfinity;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount >= 1)
                {
                    pointerPos = Input.GetTouch(0).position;
                }
            }
            else
            {
                pointerPos = Input.mousePosition;
            }

            pointerPos = Camera.main.ScreenToWorldPoint(pointerPos);

            return pointerPos;
        }
    }

    // warning: returns positive infinity for no touches
    public static Vector2 ScreenPointerPosition
    {
        get
        {
            Vector2 pointerPos = Vector2.positiveInfinity;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount >= 1)
                {
                    pointerPos = Input.GetTouch(0).position;
                }
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
            Vector2 pointerPos = ScreenPointerPosition;

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

    public static float TouchesAndPointersCount
    {
        get
        {
            return Input.touchCount + (Pressing ? 1 : 0);
        }
    }

    // warning: returns positive infinity for more touches than one or none
    public static Vector2 ViewportPointerPosition
    {
        get
        {
            Vector2 pointerPos = Vector2.positiveInfinity;
            if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (Input.touchCount == 1)
                {
                    pointerPos = Input.GetTouch(0).position;
                }
            }
            else
            {
                pointerPos = Input.mousePosition;
            }

            pointerPos = Camera.main.ScreenToViewportPoint(pointerPos);

            return pointerPos;
        }
    }

    public static bool IsPointerOverUI
    {
        get
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return true;
            }

            if (Input.touchCount > 0)
            {
                int id = Input.touches[0].fingerId;
                if (EventSystem.current.IsPointerOverGameObject(id))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
