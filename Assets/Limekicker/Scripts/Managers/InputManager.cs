using UnityEngine;

public class InputManager : IInputManager
{
    public Vector2 CurrentTouchPosition { get; private set; }

    public Vector2 StartingTouchPosition { get; private set; }

    public bool InputGiven { get; private set; }

    public void Initialize() { }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            StartingTouchPosition = new Vector2(0, 0);
            CurrentTouchPosition = new Vector2(-500, 0);
            InputGiven = true;
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            StartingTouchPosition = new Vector2(0, 0);
            CurrentTouchPosition = new Vector2(500, 0);
            InputGiven = true;
        }
        else if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D))
        {
            if (!Input.anyKey)
                InputGiven = false;
        }

        if (GameManager.Instance.GetMouseMovementInput)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartingTouchPosition = Input.mousePosition;
                InputGiven = true;
            }
            if (Input.GetMouseButton(0))
            {
                CurrentTouchPosition = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                InputGiven = false;
            }
            return;
        }

        if (Input.touchCount == 0)
        {
            StartingTouchPosition = Vector2.zero;
            CurrentTouchPosition = Vector2.zero;
            InputGiven = false;
            return;
        }

        if (Input.touches[0].phase == TouchPhase.Began)
        {
            StartingTouchPosition = Input.touches[0].position;
            CurrentTouchPosition = Input.GetTouch(0).position;
        }
        else if (Input.touches[0].phase == TouchPhase.Moved)
        {
            CurrentTouchPosition = Input.GetTouch(0).position;
        }

        InputGiven = true;
    }
}
