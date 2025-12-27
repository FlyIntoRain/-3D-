using UnityEngine;

public interface INPCInteractable
{
    /// <summary>
    /// 执行交互行为
    /// </summary>
    void Interact();
    
    /// <summary>
    /// 获取交互提示文本
    /// </summary>
    /// <returns>显示给玩家的交互文本</returns>
    string GetInteractionText();
}