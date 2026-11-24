using UnityEngine;

public class PickupInfo : MonoBehaviour
{
    public string itemName = "The Lewis Chessmen";

    [TextArea(5, 12)]
    public string description;

    void Awake()
    {
        itemName = "The Lewis Chessmen";

        description =
            "These chess pieces form a remarkable group of iconic objects " +
            "within the world collection of the British Museum. " +
            "They were probably made in Trondheim, Norway, about AD 1150-1200.\n\n" +
            "At this period, the Western Isles, where the chess pieces were found, " +
            "were part of the Kingdom of Norway, not Scotland.\n\n" +
            "It seems likely they were buried for safe keeping on route " +
            "to be traded in Ireland.";
    }
}