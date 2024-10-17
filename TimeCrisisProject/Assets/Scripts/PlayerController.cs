using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public int clickCounter;
    public bool isGamePlayable;

    public void OnMouseClick(InputAction.CallbackContext context)
    {
        if (isGamePlayable)
        {
            clickCounter++;
            if (clickCounter == 2)
            {
                Debug.Log("Clicked!");
            }
            else if (clickCounter >= 3)
            {
                clickCounter = 0;
            }
        }
    }
}
