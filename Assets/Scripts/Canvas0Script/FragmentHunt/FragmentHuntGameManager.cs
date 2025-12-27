using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 添加这个命名空间以使用协程

public class FragmentHuntGameManager : MonoBehaviour
{
    [SerializeField]
    private RectTransform existingGridRoot; // 场景中已摆好子物体

    [SerializeField]
    private bool bindExistingGridOnStart = false; // 启动时自动绑定已有Grid

    [SerializeField]
    private Vector2 gridCellSize = new Vector2(160, 160);

    [SerializeField]
    private Vector2 gridSpacing = new Vector2(12, 12);

    [SerializeField]
    private Sprite spriteNear; // 相邻时的精灵贴图

    [SerializeField]
    private Sprite spriteFar; // 不相邻时的精灵贴图

    [SerializeField]
    private Sprite spriteCorrect; // 正确位置的精灵贴图

    [SerializeField]
    private Sprite spriteDefault; // 默认精灵贴图

    [SerializeField]
    private RectTransform gameLayer;

    [SerializeField]
    private float gameEndDelay = 2f; // 游戏结束后的延迟时间（秒）

    private GridLayoutGroup gridLayout;
    private readonly List<FragmentCell> cells = new List<FragmentCell>();
    private Vector2Int fragmentPos;
    private bool gameOver;

    // 键盘导航相关（与鼠标共存）
    private Vector2Int currentSelection = new Vector2Int(0, 0); // 当前选中的格子位置

    [ContextMenu("ShowGame")]
    public void ShowGame()
    {
        RestartGame();
        if (gameLayer != null)
        {
            gameLayer.gameObject.SetActive(true);
            gameLayer.parent.gameObject.SetActive(true);
        }
        // 初始默认选中第一个
        currentSelection = new Vector2Int(0, 0);
        UpdateSelectionDisplay();
    }

    public void HideGame()
    {
        if (gameLayer != null)
        {
            gameLayer.gameObject.SetActive(false);
            gameLayer.parent.gameObject.SetActive(false);
        }
        HideAllBorders();
    }

    public void RestartGame()
    {
        gameOver = false;
        fragmentPos = new Vector2Int(Random.Range(0, 3), Random.Range(0, 3));
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].ResetVisual(spriteDefault, true);
        }
        // 重置选中状态
        currentSelection = new Vector2Int(0, 0);
        UpdateSelectionDisplay();
    }

    private void Start()
    {
        if (bindExistingGridOnStart && existingGridRoot != null)
        {
            SetupFromExistingGrid(existingGridRoot, 3);
        }
    }

    [ContextMenu("SetupFromExistingGrid (use existingGridRoot)")]
    public void SetupFromExistingGrid()
    {
        if (existingGridRoot == null)
        {
            Debug.LogError("existingGridRoot 未指定，无法绑定已有Grid。");
            return;
        }
        SetupFromExistingGrid(existingGridRoot, 3);
    }

    public void SetupFromExistingGrid(RectTransform gridRoot, int columns)
    {
        if (gridRoot == null)
        {
            Debug.LogError("gridRoot 为空，无法绑定。");
            return;
        }

        // 将游戏图层指向已有Grid的根（用于Show/Hide）
        gameLayer = gridRoot;
        gridLayout = gridRoot.GetComponent<GridLayoutGroup>();

        cells.Clear();
        int childCount = gridRoot.childCount;
        if (columns <= 0) columns = 3;
        int rows = Mathf.CeilToInt(childCount / (float)columns);

        for (int i = 0; i < childCount; i++)
        {
            var child = gridRoot.GetChild(i) as RectTransform;
            if (child == null) continue;

            int x = i % columns;
            int y = i / columns;
            var image = child.GetComponent<Image>();
            if (image == null) image = child.gameObject.AddComponent<Image>();
            image.sprite = spriteDefault;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            var button = child.GetComponent<Button>();
            if (button == null) button = child.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.9f);
            colors.pressedColor = new Color(1f, 1f, 1f, 0.8f);
            button.colors = colors;

            var cell = child.GetComponent<FragmentCell>();
            if (cell == null) cell = child.gameObject.AddComponent<FragmentCell>();
            cell.Init(this, new Vector2Int(x, y), image, button);
            cells.Add(cell);
        }

        // 初始化一局
        RestartGame();
    }

    internal void OnCellClicked(FragmentCell cell)
    {
        if (gameOver) return;

        var pos = cell.GridPosition;
        // 同步键盘选中到被点击的格子，确保只有一个选中边框
        currentSelection = pos;
        UpdateSelectionDisplay();
        if (pos == fragmentPos)
        {
            cell.SetSprite(spriteCorrect);
            gameOver = true;
            DisableAllCellsExcept(null);
            Debug.Log("找到碎片，游戏结束！");

            // 使用协程延迟关闭游戏界面
            StartCoroutine(DelayedHideGame());
            return;
        }

        bool isAdjacent = IsOrthogonallyAdjacent(pos, fragmentPos);
        cell.SetSprite(isAdjacent ? spriteNear : spriteFar);
    }

    // 延迟关闭游戏的协程
    private IEnumerator DelayedHideGame()
    {
        // 等待指定的延迟时间
        yield return new WaitForSeconds(gameEndDelay);

        // 延迟结束后隐藏游戏
        HideGame();
    }

    private static bool IsOrthogonallyAdjacent(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return (dx + dy) == 1; // 仅上下左右
    }

    private void DisableAllCellsExcept(FragmentCell except)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] == null) continue;
            if (except != null && cells[i] == except) continue;
            cells[i].SetInteractable(false);
        }
    }

    // 键盘输入处理（与鼠标共存，始终可用）
    private void Update()
    {
        if (gameOver) return;

        HandleKeyboardInput();
    }

    private void HandleKeyboardInput()
    {
        Vector2Int newSelection = currentSelection;

        // WASD移动
        if (Input.GetKeyDown(KeyCode.W))
        {
            newSelection.y = Mathf.Max(0, newSelection.y - 1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            newSelection.y = Mathf.Min(2, newSelection.y + 1);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            newSelection.x = Mathf.Max(0, newSelection.x - 1);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            newSelection.x = Mathf.Min(2, newSelection.x + 1);
        }

        // Enter确认点击
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ClickCurrentSelection();
            return;
        }

        // 更新选中位置
        if (newSelection != currentSelection)
        {
            currentSelection = newSelection;
            UpdateSelectionDisplay();
        }
    }

    private void ClickCurrentSelection()
    {
        FragmentCell targetCell = GetCellAtPosition(currentSelection);
        if (targetCell != null)
        {
            OnCellClicked(targetCell);
        }
    }

    private FragmentCell GetCellAtPosition(Vector2Int pos)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null && cells[i].GridPosition == pos)
            {
                return cells[i];
            }
        }
        return null;
    }

    private void UpdateSelectionDisplay()
    {
        // 隐藏所有边框
        HideAllBorders();

        // 显示当前选中格子的边框
        FragmentCell selectedCell = GetCellAtPosition(currentSelection);
        if (selectedCell != null)
        {
            selectedCell.SetSelected(true);
        }
    }

    private void HideAllBorders()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null)
            {
                cells[i].SetSelected(false);
            }
        }
    }
}