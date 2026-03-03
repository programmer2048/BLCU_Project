using UnityEngine;
using UnityEngine.UI;

public class OrnamentItem : MonoBehaviour
{
    public Image iconImage;
    public float slotSize = 100f; // ²à±ßÀ¸¸ñ×ÓµÄ³ß´ç

    public void Init(MaidData data, MultiScratchManager manager)
    {
        iconImage.sprite = data.iconSprite;
        iconImage.preserveAspect = true;

        // ÉèÖÃ²à±ßÀ¸Í¼±êµÄ³ß´çËõ·Å
        float w = data.iconSprite.rect.width;
        float h = data.iconSprite.rect.height;
        if (w > h)
        {
            iconImage.rectTransform.sizeDelta = new Vector2(slotSize, h * (slotSize / w));
        }
        else
        {
            iconImage.rectTransform.sizeDelta = new Vector2(w * (slotSize / h), slotSize);
        }

        GetComponent<Button>().onClick.AddListener(() => {
            manager.SelectOrnament(data);
        });
    }
}