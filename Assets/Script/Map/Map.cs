using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class Waypoints
{
    public string waypointName;
    public Transform pippaTeleportPos;
    public Transform piaTeleportPos;
}

public class Map : MonoBehaviour
{
    public Waypoints[] waypoints;
    public PlayerComponent pippa;
    public PlayerComponent pia;
    
    public void SetPlayers(PlayerComponent pippaInstance, PlayerComponent piaInstance)
    {
        pippa = pippaInstance;
        pia = piaInstance;
    }

    public void Teleport(string wpName)
    {
        Waypoints waypoint = Array.Find(
            waypoints,
            item => item.waypointName == wpName
        );

        if (waypoint == null)
        {
            Debug.LogWarning($"Waypoint '{wpName}' was not found.");
            return;
        }

        Debug.LogWarning(
            $"Map registration: Pippa = {pippa != null}, Pia = {pia != null}"
        );

        if (pippa == null || pia == null || waypoint.pippaTeleportPos == null || waypoint.piaTeleportPos == null)
        {
            Debug.LogWarning("Map: Players and both waypoint teleport positions must be assigned.");
            return;
        }

        StartCoroutine(PvtOnTeleport(waypoint));
    }

    private IEnumerator PvtOnTeleport(Waypoints waypoint)
    {
        CharacterController piaCC = pia.GetComponent<CharacterController>();
        CharacterController pippaCC = pippa.GetComponent<CharacterController>();

        if (piaCC == null || pippaCC == null)
        {
            Debug.LogWarning("Map: Both players must have a CharacterController.");
            yield break;
        }

        pia.IsTeleporting = true;
        pippa.IsTeleporting = true;
        piaCC.enabled = false;
        pippaCC.enabled = false;
        pippa.transform.SetPositionAndRotation(
            waypoint.pippaTeleportPos.position,
            waypoint.pippaTeleportPos.rotation
        );

        pia.transform.SetPositionAndRotation(
            waypoint.piaTeleportPos.position,
            waypoint.piaTeleportPos.rotation
        );
        yield return new WaitForSeconds(0.1f);
        piaCC.enabled = true;
        pippaCC.enabled = true;
        pia.IsTeleporting = false;
        pippa.IsTeleporting = false;

        // Manager_UI.Instance.OnCloseCurrentPanel();
        yield return new WaitForSeconds(0.3f);
        Manager_UI.Instance.OnShowPanel(waypoint.waypointName);
    }
}