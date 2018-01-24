using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetCameraDepthMode : MonoBehaviour
{
	public DepthTextureMode depthMode = DepthTextureMode.Depth;
	public DepthTextureMode depthMode2 = DepthTextureMode.None;
	[PowRange(0, 8, 2, true)]
	public int MSAA = 0;
	private Camera _camera;

	void Awake ()
	{
		SetDepthMode();
	}

	[ContextMenu("Set Depth Mode")]
	public void SetDepthMode ()
	{
		if (!_camera)
			_camera = GetComponent<Camera>();
		if (!_camera)
			return;

		_camera.depthTextureMode = depthMode | depthMode2;
		QualitySettings.antiAliasing = MSAA;
		_camera.allowMSAA = MSAA > 0;
	}
}
