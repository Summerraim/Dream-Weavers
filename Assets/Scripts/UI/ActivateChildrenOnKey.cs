using UnityEngine;

/// <summary>
/// 挂在始终激活的父物体上，按下指定按键时切换 CanvasGroup 的显隐（通过 alpha），不再关闭 GameObject。
/// 避免 OnDisable 取消订阅导致逻辑失效。
/// </summary>
public class ActivateChildrenOnKey : MonoBehaviour
{
	[SerializeField]
	private KeyCode key = KeyCode.I;

	[SerializeField]
	private CanvasGroup targetCanvasGroup;

	[SerializeField]
	private float visibleAlpha = 1f;

	[SerializeField]
	private float hiddenAlpha = 0f;

	[SerializeField]
	private bool toggleInteractable = true;

	[SerializeField]
	private bool visibleInteractable = true;

	[SerializeField]
	private bool hiddenInteractable = false;

	[SerializeField]
	private bool toggleBlocksRaycasts = true;

	[SerializeField]
	private bool startVisible = true;

	private bool isVisible = true;

	private void OnEnable()
	{
		isVisible = startVisible;
		ApplyVisibility();
	}

	private void Update()
	{
		if (Input.GetKeyDown(key))
		{
			toggleCanvas();
		}
     
	}

	private void toggleCanvas()
	{
		var cg = ResolveCanvasGroup();
		if (cg == null)
			return;

		isVisible = !isVisible;
		ApplyVisibility();
	}

	private void ApplyVisibility()
	{
		var cg = ResolveCanvasGroup();
		if (cg == null)
			return;

		cg.alpha = isVisible ? visibleAlpha : hiddenAlpha;

		if (toggleInteractable)
		{
			cg.interactable = isVisible;
		}

		if (toggleBlocksRaycasts)
		{
			cg.blocksRaycasts = isVisible;
		}
	}

	private CanvasGroup ResolveCanvasGroup()
	{
		if (targetCanvasGroup != null)
			return targetCanvasGroup;

		targetCanvasGroup = GetComponent<CanvasGroup>();
		if (targetCanvasGroup == null)
		{
			targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();
		}

		return targetCanvasGroup;
	}
}
