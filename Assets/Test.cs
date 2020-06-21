using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Test : MonoBehaviour
{
    public float testX;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        var cameraPos = Camera.main.transform.position;

        var translateMatrix = new Matrix4x4();
        translateMatrix.SetRow(0, new Vector4(1, 0, 0, -cameraPos.x));
        translateMatrix.SetRow(1, new Vector4(0, 1, 0, -cameraPos.y));
        translateMatrix.SetRow(2, new Vector4(0, 0, 1, -cameraPos.z));
        translateMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        Shader.SetGlobalMatrix("_TestMatrix", translateMatrix);

        var translateMatrix2 = new Matrix4x4();
        translateMatrix2.SetRow(0, new Vector4(1, 0, 0, cameraPos.x));
        translateMatrix2.SetRow(1, new Vector4(0, 1, 0, cameraPos.y));
        translateMatrix2.SetRow(2, new Vector4(0, 0, 1, cameraPos.z));
        translateMatrix2.SetRow(3, new Vector4(0, 0, 0, 1));

        // testMatrix2[0, 3] = testMatrix2[0, 3] + testX;
        // testMatrix2[0, 3] = testMatrix2[0, 3] + cameraPos.x;
        // testMatrix2[1, 3] = testMatrix2[1, 3] + cameraPos.y;
        // testMatrix2[2, 3] = testMatrix2[2, 3] + cameraPos.z;

        Shader.SetGlobalMatrix("_TestMatrix2", translateMatrix2);
    }
}