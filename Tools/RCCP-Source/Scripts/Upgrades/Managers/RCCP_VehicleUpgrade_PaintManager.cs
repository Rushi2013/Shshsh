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
/// Manager for painters.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Customization/RCCP Vehicle Upgrade Paint Manager")]
public class RCCP_VehicleUpgrade_PaintManager : RCCP_UpgradeComponent, IRCCP_UpgradeComponent {

    /// <summary>
    /// All painters.
    /// </summary>
    public RCCP_VehicleUpgrade_Paint[] paints;

    /// <summary>
    /// Current color.
    /// </summary>
    public Color color = Color.white;

    /// <summary>
    /// Default colors.
    /// </summary>
    public List<Color> defaultColors = new List<Color>();

    /// <summary>
    /// Initializes all painters.
    /// </summary>
    public void Initialize() {

        //  Return if no painters found.
        if (paints == null)
            return;

        //  Return in no painters found.
        if (paints.Length < 1)
            return;

        //  Loadout color.
        color = Loadout.paint;

        //  Getting last saved color for this vehicle.
        if (color != new Color(1f, 1f, 1f, 0f))
            Paint(color);

        defaultColors.Clear();

        //  Getting default colors for restoring.
        for (int i = 0; i < paints.Length; i++) {

            if (paints[i] != null && paints[i].paintMaterial)
                defaultColors.Add(paints[i].paintMaterial.GetColor(paints[i].id));

        }

    }

    public void GetAllPainters() {

        paints = GetComponentsInChildren<RCCP_VehicleUpgrade_Paint>(true);

    }

    /// <summary>
    /// Runs all painters with the target color.
    /// </summary>
    /// <param name="newColor"></param>
    public void Paint(Color newColor) {

        //  Return if no painters found.
        if (paints == null)
            return;

        //  Return if no painters found.
        if (paints.Length < 1)
            return;

        //  Setting color.
        color = newColor;

        //  Painting.
        for (int i = 0; i < paints.Length; i++) {

            if (paints[i] != null)
                paints[i].UpdatePaint(color);

        }

        //  Painting spoilers.
        if (CarController.Customizer.SpoilerManager != null && Loadout.paint != new Color(1f, 1f, 1f, 0f))
            CarController.Customizer.SpoilerManager.Paint(Loadout.paint);

        //  Refreshing the loadout.
        Refresh(this);

        //  Saving the loadout.
        if (CarController.Customizer.autoSave)
            Save();

    }

    /// <summary>
    /// Runs all painters with the target color.
    /// </summary>
    /// <param name="newColor"></param>
    public void PaintWithoutSave(Color newColor) {

        //  Return if no painters found.
        if (paints == null)
            return;

        //  Return if no painters found.
        if (paints.Length < 1)
            return;

        //  Setting color.
        color = newColor;

        //  Painting.
        for (int i = 0; i < paints.Length; i++) {

            if (paints[i] != null)
                paints[i].UpdatePaint(color);

        }

        //  Painting spoilers.
        if (CarController.Customizer.SpoilerManager != null && Loadout.paint != new Color(1f, 1f, 1f, 0f))
            CarController.Customizer.SpoilerManager.Paint(Loadout.paint);

    }

    private void Reset() {

        paints = GetComponentsInChildren<RCCP_VehicleUpgrade_Paint>(true);

        if (paints == null || (paints != null && paints.Length == 0)) {

            paints = new RCCP_VehicleUpgrade_Paint[1];
            GameObject newPaint = new GameObject("Paint_1");
            newPaint.transform.SetParent(transform);
            newPaint.transform.localPosition = Vector3.zero;
            newPaint.transform.localRotation = Quaternion.identity;
            paints[0] = newPaint.AddComponent<RCCP_VehicleUpgrade_Paint>();

        }

    }

    /// <summary>
    /// Restores the settings to default.
    /// </summary>
    public void Restore() {

        //  Loadout color.
        color = Loadout.paint;

        if (defaultColors != null) {

            if (defaultColors.Count >= 1) {

                for (int i = 0; i < defaultColors.Count; i++) {

                    if (paints[i] != null)
                        paints[i].UpdatePaint(defaultColors[i]);

                }

            }

        }

    }

}
