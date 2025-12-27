using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class FragmentCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private FragmentHuntGameManager manager;
    private Image image;
    private Button button;
    private GameObject borderContainer;
    private bool isSelected = false;
    private Color normalBorderColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

    // 仅通过选中状态显示边框，不响应悬停高亮

    public Vector2Int GridPosition { get; private set; }

    public void Init(FragmentHuntGameManager mgr, Vector2Int gridPos, Image img, Button btn)
    {
        manager = mgr;
        GridPosition = gridPos;
        image = img;
        button = btn;
        button.onClick.AddListener(OnClick);

        // 创建边框
        CreateBorder();
    }

    private void CreateBorder()
    {
        // 创建边框容器
        borderContainer = new GameObject("BorderContainer", typeof(RectTransform));
        borderContainer.transform.SetParent(transform, false);

        var containerRect = borderContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // 创建四个边框（上、下、左、右）- 4像素厚度
        CreateBorderLine(borderContainer, "TopBorder", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -4), new Vector2(0, 0));
        CreateBorderLine(borderContainer, "BottomBorder", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 4));
        CreateBorderLine(borderContainer, "LeftBorder", new Vector2(0, 0), new Vector2(0, 1), new Vector2(-4, 0), new Vector2(0, 0));
        CreateBorderLine(borderContainer, "RightBorder", new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(4, 0));

        // 边框容器默认隐藏
        borderContainer.SetActive(false);
    }

    private void CreateBorderLine(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var borderLine = new GameObject(name, typeof(RectTransform));
        borderLine.transform.SetParent(parent.transform, false);

        var rect = borderLine.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var image = borderLine.AddComponent<Image>();
        image.color = normalBorderColor;
        image.type = UnityEngine.UI.Image.Type.Simple;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 悬停时不改变边框，仅由选中状态控制
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 悬停离开时不改变边框，仅由选中状态控制
    }

    private void ShowBorder(bool show)
    {
        if (borderContainer != null)
        {
            borderContainer.SetActive(show);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (manager != null)
        {
            // 点击交由管理器统一处理选中和点击逻辑
            manager.OnCellClicked(this);
        }
    }

    public void SetSprite(Sprite sprite)
    {
        if (image != null)
        {
            image.sprite = sprite;
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public void ResetVisual(Sprite defaultSprite, bool interactable)
    {
        SetSprite(defaultSprite);
        SetInteractable(interactable);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (borderContainer != null)
        {
            if (selected)
            {
                borderContainer.SetActive(true);
                SetBorderColor(normalBorderColor);
            }
            else
            {
                borderContainer.SetActive(false);
            }
        }
    }

    private void SetBorderColor(Color color)
    {
        if (borderContainer != null)
        {
            foreach (Transform child in borderContainer.transform)
            {
                var image = child.GetComponent<Image>();
                if (image != null)
                {
                    image.color = color;
                }
            }
        }
    }
}