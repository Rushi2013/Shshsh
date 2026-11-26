//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright © 2014 - 2024 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI paint button. 
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/UI/Modification/RCCP UI Color Button")]
public class RCCP_UI_Color : RCCP_UIComponent {

    /// <summary>
    /// Picked color.
    /// </summary>
    public PickedColor _pickedColor = PickedColor.Orange;
    public enum PickedColor { Orange, Red, Green, Blue, Black, White, Cyan, Magenta, Pink }

    public void OnClick() {

        //  Finding the player vehicle.
        RCCP_CarController playerVehicle = RCCP_SceneManager.Instance.activePlayerVehicle;

        //  If no player vehicle found, return.
        if (!playerVehicle)
            return;

        //  If player vehicle doesn't have the customizer component, return.
        if (!playerVehicle.Customizer)
            return;

        if (!playerVehicle.Customizer.PaintManager)
            return;

        //  Color.
        Color selectedColor = new Color();

        switch (_pickedColor) {

            case PickedColor.Orange:
                selectedColor = Color.red + (Color.green / 2f);
                break;

            case PickedColor.Red:
                selectedColor = Color.red;
                break;

            case PickedColor.Green:
                selectedColor = Color.green;
                break;

            case PickedColor.Blue:
                selectedColor = Color.blue;
                break;

            case PickedColor.Black:
                selectedColor = Color.black;
                break;

            case PickedColor.White:
                selectedColor = Color.white;
                break;

            case PickedColor.Cyan:
                selectedColor = Color.cyan;
                break;

            case PickedColor.Magenta:
                selectedColor = Color.magenta;
                break;

            case PickedColor.Pink:
                selectedColor = new Color(1, 0f, .5f);
                break;

        }

        playerVehicle.Customizer.PaintManager.Paint(selectedColor);

    }

}
