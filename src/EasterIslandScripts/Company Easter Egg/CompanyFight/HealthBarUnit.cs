using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    using UnityEngine;
    using UnityEngine.UI;
    
    // credit to BanMedo for the Health Bar code
    public class HealthBarUnit : MonoBehaviour
    {
        public GameObject _bgObject;
        public GameObject _fillObject;
        public Animator _animator;
        public bool _isOn = true;

        static public HealthBarUnit GetNewHealthBarUnit(
            RectTransform parentRect,
            Color fillColor,
            Color backgroundColor,
            Vector2 size,
            Vector2 anchoredPosition,
            RuntimeAnimatorController runtimeANC
        )
        {
            var hbuBase = new GameObject();
            hbuBase.transform.parent = parentRect.transform;
            hbuBase.AddComponent<Image>();
            var healthBarUnit = hbuBase.AddComponent<HealthBarUnit>();

            var rectTransform = hbuBase.GetComponent<RectTransform>();
            rectTransform.sizeDelta = size;
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.anchoredPosition = anchoredPosition;

            var mask = hbuBase.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var animator = hbuBase.AddComponent<Animator>();
            animator.runtimeAnimatorController = runtimeANC;

            var bgObject = getImage(hbuBase.GetComponent<RectTransform>(), backgroundColor, "Background", size.x * 0.85f, size.y * 2f);
            var fillObject = getImage(hbuBase.GetComponent<RectTransform>(), fillColor, "Fill", size.x * 0.75f, size.y * 2f);

            healthBarUnit._animator = animator;
            healthBarUnit._bgObject = bgObject;
            healthBarUnit._fillObject = fillObject;

            return healthBarUnit;
        }

        static private GameObject getImage(RectTransform parentRect, Color color, string name, float x_mod, float y_mod)
        {
            var obj = new GameObject();
            obj.transform.parent = parentRect.transform;
            obj.name = name;

            var image = obj.AddComponent<Image>();
            image.color = color;

            var rectTransform = obj.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = new Vector3(-x_mod / 2f, 0f, 0f);

            rectTransform.sizeDelta = new Vector2(x_mod, y_mod);
            rectTransform.eulerAngles = new Vector3(0, 0, -10);

            return obj;
        }

        public void Redraw(Vector2 size, Vector2 anchoredPosition, Color fillColor, Color backgroundColor)
        {
            GetComponent<RectTransform>().sizeDelta = size;
            GetComponent<RectTransform>().anchoredPosition = anchoredPosition;
            GetComponent<RectTransform>().localScale = new Vector2(1f, 1f);
            _fillObject.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x * 0.75f, size.y * 2f);
            _fillObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(-size.x * 0.75f / 2f, 0, 0);
            _fillObject.GetComponent<Image>().color = fillColor;
            _bgObject.GetComponent<RectTransform>().sizeDelta = new Vector2(size.x * 0.85f, size.y * 2f);
            _bgObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(-size.x * 0.85f / 2f, 0, 0);
            _bgObject.GetComponent<Image>().color = backgroundColor;
        }

        public void SetFilled(bool isOn)
        {
            if (_isOn == isOn) return;
            if (!Application.isPlaying) _fillObject.SetActive(isOn);
            if (isOn) _animator.Play("ACL_HealthBarUnit_Enter", 0);
            else _animator.Play("ACL_HealthBarUnit_Hit", 0);
            _isOn = isOn;
        }
    }
}
