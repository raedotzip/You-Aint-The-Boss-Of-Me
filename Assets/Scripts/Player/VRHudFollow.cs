using UnityEngine;

public class VRHudFollow : MonoBehaviour
{
    [SerializeField] private Transform headTransform;
    [SerializeField] private float distance = 1.5f;
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private Vector3 offset = Vector3.zero;

    public void Init(Transform head)
    {
        headTransform = head;
    }

    private void Update()
    {
        if (headTransform == null) return;

        Vector3 targetPos = headTransform.position
            + headTransform.forward * distance
            + offset;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, headTransform.rotation, Time.deltaTime * followSpeed);
    }
}
