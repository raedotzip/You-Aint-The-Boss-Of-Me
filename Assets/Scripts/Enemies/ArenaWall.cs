using UnityEngine;

// Attach to a wall GameObject in the scene.
// Progression walls (between arenas) start active and open when a boss is beaten.
// Entrance walls (behind the player) start inactive and close when a fight begins.
public class ArenaWall : MonoBehaviour
{
    public void Open()  => gameObject.SetActive(false);
    public void Close() => gameObject.SetActive(true);
}
