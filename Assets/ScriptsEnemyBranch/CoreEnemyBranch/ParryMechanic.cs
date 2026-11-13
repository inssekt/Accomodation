using UnityEngine;
using UnityEngine.InputSystem;

public class ParryMechanic : MonoBehaviour
{
    public float parryWindow = 1f;   // how long the parry stays active
    public float cooldown = 3f;      // time before you can parry again

    public bool IsParrying { get; private set; }

    float parryEndTime;
    float nextParryTime;

    void Update()
    {
        // Press F to parry
        if (Keyboard.current.fKey.wasPressedThisFrame && Time.time >= nextParryTime)
        {
            IsParrying = true;
            parryEndTime = Time.time + parryWindow;
            nextParryTime = Time.time + cooldown;
            Debug.Log("Parry Pressed");
        }

        // Parry window ran out
        if (IsParrying && Time.time >= parryEndTime)
            IsParrying = false;
    }
    void OnTriggerEnter(Collider other)
    {
        var parry = other.GetComponentInParent<ParryMechanic>();
        Debug.Log("Hit " + other.name + " | parry found: " + (parry != null) + " | parrying: " + (parry != null && parry.IsParrying));

        if (parry != null && parry.IsParrying)
        {
            Debug.Log("Parried!");
            return;
        }

        // deal damage as normal
    }
}
