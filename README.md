# CameraRelativeRendering
处理Unity大世界浮点精度导致的渲染变形问题，在顶点着色器中使用摄像机相对坐标渲染，避免了大数值世界坐标直接参与顶点运算。
借鉴了HDRP的Camera-relative rendering思路：
https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@7.1/manual/Camera-Relative-Rendering.html

![pic](https://github.com/CharlesFeng207/CameraRelativeRendering/blob/master/Pic/1.gif)
