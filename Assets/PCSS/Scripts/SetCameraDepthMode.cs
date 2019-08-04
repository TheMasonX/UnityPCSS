using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SetCameraDepthMode : MonoBehaviour
{
	public bool enabled;
	public DepthTextureMode depthMode = DepthTextureMode.Depth;
	public DepthTextureMode depthMode2 = DepthTextureMode.None;
	public enum antiAliasing
	{
		None = 0,
		Two = 2,
		Four = 4,
		Eight = 8,
	}
	public antiAliasing MSAA = antiAliasing.None;
	private Camera _camera;

	void Awake ()
	{
		SetDepthMode();
	}

	[ContextMenu("Set Depth Mode")]
	public void SetDepthMode ()
	{
		if (!enabled)
			return;
		
		if (!_camera)
			_camera = GetComponent<Camera>();
		if (!_camera)
			return;

		_camera.depthTextureMode = depthMode | depthMode2;
		QualitySettings.antiAliasing = (int)MSAA;
		_camera.allowMSAA = (MSAA != antiAliasing.None);
	}
}
