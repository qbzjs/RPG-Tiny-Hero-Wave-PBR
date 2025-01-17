using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    Transform target;
    public float speed = 3.0f;
    public Vector3 offset;

    private void Start()
    {
        target = FindObjectOfType<Player>()?.transform;
        
    }

    // 모든 업데이트 함수들이 실행된 이후
    private void FixedUpdate()
    {
        if (target != null)
        {
            transform.position = Vector3.Lerp(transform.position, target.position + offset, speed * Time.fixedDeltaTime);
        }

    }
}
