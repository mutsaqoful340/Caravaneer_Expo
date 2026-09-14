/// <summary>
/// This script calculates the progress of a wagon between two points and updates a UI slider to reflect that progress.
/// </summary>
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class WagonProgress : MonoBehaviour
{
    [Header("Wagon Progress References")]
    public Transform pointStart;
    public Transform pointEnd;
    public Transform Wagon;
    public Slider progressSlider; // Reference to the UI Slider component

    [Header("Debug")]
    public string progressPercentageText = "Wagon Progress: ";

    private void FixedUpdate()
    {
        if (Wagon.IsDestroyed())
        {
            return;
        }
        
        if (pointStart == null || pointEnd == null || Wagon == null || progressSlider == null)
        {
            Debug.LogWarning("WagonProgress: One or more references are not assigned.");
            return;
        }

        // Calculate the total distance between the start and end points
        float totalDistance = Vector3.Distance(pointStart.position, pointEnd.position);

        // Calculate the distance from the start point to the wagon's current position
        float currentDistance = Vector3.Distance(pointStart.position, Wagon.position);

        // Calculate the progress as a value between 0 and 1
        float progress = Mathf.Clamp01(currentDistance / totalDistance);

        // Update the slider value
        progressSlider.value = progress;

        // Optionally, update a text element to show the percentage
        if (progressSlider.transform.Find("ProgressText") is Transform textTransform && textTransform.GetComponent<Text>() is Text progressText)
        {
            progressText.text = $"{progressPercentageText}{(progress * 100f):F1}%";
        }
    }
}
