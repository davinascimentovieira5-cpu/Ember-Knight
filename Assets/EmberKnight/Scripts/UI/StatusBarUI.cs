using UnityEngine;
using UnityEngine.UI;

namespace EmberKnight.UI
{
    /// <summary>
    /// Atualiza uma barra (HP ou Fúria Especial) a partir de um valor 0..1.
    /// Ligue PlayerController.onHealthChanged / onSpecialChanged a este
    /// componente pelo Inspector (UnityEvent<float>), sem precisar de código extra.
    /// </summary>
    public class StatusBarUI : MonoBehaviour
    {
        [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal")]
        public Image fillImage;

        public void SetValue(float normalized)
        {
            if (fillImage != null)
                fillImage.fillAmount = Mathf.Clamp01(normalized);
        }
    }
}
