using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    [field: SerializeField] public Transform[] SpawnPoints { get; private set; }
    [field: SerializeField] public Color[] PlayerColors { get; private set; }
    public int PlayerCount { get; private set; }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int maxPlayerCount = Mathf.Min(SpawnPoints.Length, PlayerColors.Length);
        if (maxPlayerCount < 1)
        {
            string msg =
                $"You forgot to assign {name}'s {nameof(PlayerSpawn)}.{nameof(SpawnPoints)}" +
                $"and {nameof(PlayerSpawn)}.{nameof(PlayerColors)}!";
            Debug.Log(msg);
        }

        // Prevent adding in more than max number of players
        if (PlayerCount >= maxPlayerCount)
        {
            // Delete new object
            string msg =
                $"Max player count {maxPlayerCount} reached. " +
                $"Destroying newly spawned object {playerInput.gameObject.name}.";
            Debug.Log(msg);
            Destroy(playerInput.gameObject);
            return;
        }

        // Assign spawn transform values
        playerInput.transform.position = SpawnPoints[PlayerCount].position;
        playerInput.transform.rotation = SpawnPoints[PlayerCount].rotation;
        Color color = PlayerColors[PlayerCount];

        // Increment player count
        PlayerCount++;

        // Set up player controller
        PlayerController playerController = playerInput.gameObject.GetComponent<PlayerController>();
        playerController.AssignPlayerInputDevice(playerInput);
        playerController.AssignPlayerNumber(PlayerCount);
        playerController.AssignColor(color);
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        // Not handling anything right now.
        Debug.Log("Player left...");
    }
}
