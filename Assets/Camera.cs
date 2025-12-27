using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public Transform player;    //角色位置信息
    public float smoothing = 5f;
    Vector3 offset;
    void Start()
    {
        offset = transform.position - player.position;
    }
    void FixedUpdate()
    {
        Vector3 playerCamPos = player.position + offset;
        transform.position = Vector3.Lerp(transform.position, playerCamPos, smoothing * Time.deltaTime);
    }
}
