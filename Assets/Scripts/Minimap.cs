using UnityEngine;

public class Minimap : MonoBehaviour
{

    public Transform playerTransform;

    void LateUpdate()
    {
        
        Vector3 newPosition = playerTransform.position;
        newPosition.y = transform.position.y;
        transform.position = newPosition;
        
    }

}
