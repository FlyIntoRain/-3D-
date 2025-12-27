using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCam : MonoBehaviour
{
    private Vector3 targetPos = new Vector3(95.43829f, 3.3f, -66.8822f);
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        transform.LookAt(targetPos);
        transform.Rotate(0,180,0);
    }
}
