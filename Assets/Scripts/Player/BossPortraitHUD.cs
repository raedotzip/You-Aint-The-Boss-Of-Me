using UnityEngine;
using UnityEngine.UI;

public class BossPortraitHUD : MonoBehaviour
{
    public RawImage portraitImage;

    public Texture boss1Icon;
    public Texture boss2Icon;

    public void SetActiveBoss(int bossIndex)
    {
        if (portraitImage == null) return;

        if (bossIndex == 0)
        {
            portraitImage.enabled = false;
            return;
        }

        portraitImage.enabled = true;
        portraitImage.texture = bossIndex == 1 ? boss1Icon : boss2Icon;
        portraitImage.uvRect = new Rect(0, 0, 1, 1);
    }
}
