using System;
using System.Collections.Generic;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    using System.Collections.Generic;
    using UnityEngine;

    // credit to BanMedo for the Health Bar code
    [ExecuteAlways]
    public class HealthBar : MonoBehaviour
    {
        [Header("Base Settings")]
        public int maxHealth = 5;
        public int currentHealth = 1;
        public Color healthbarColor;
        public Color backgroundColor;
        public RuntimeAnimatorController childAnimator;
        [Header("Spread")]
        public float maxHorizontalUnits = 10;
        public float horizontalSpacing = -10;
        public float verticalSpacing = 10;
        public float unitWidth = 60;
        public float unitHeight = 40;

        public List<HealthBarUnit> _healthBarUnits = new();

#if UNITY_EDITOR
void Update() => updateHealthBar();
private void OnValidate() => currentHealth = math.min(maxHealth, currentHealth);
#endif

        public bool SetMaxhealth(int health)
        {
            if (health < 1) return false;
            maxHealth = health;
            return SetHealth(Math.Min(maxHealth, currentHealth));
        }

        public bool SetHealth(int health)
        {
            if (health > maxHealth || health < 0) return false;
            currentHealth = health;
            updateHealthBar();
            return true;
        }

        public void updateHealthBar()
        {
            makeBars();
            cleanBars();
            redrawBars();
            updateBars();
        }

        private Vector2 getUnitPosition(int i)
        {
            var column = i % maxHorizontalUnits;
            var row = (int)(i / maxHorizontalUnits);
            var x = column * (unitWidth + horizontalSpacing);
            var y = -row * (unitHeight + verticalSpacing) - unitHeight / 2;
            return new Vector2(x, y);
        }

        private void makeBars()
        {
            for (int i = 0; i < maxHealth; i++) makeBar(i);
        }
        private void makeBar(int i)
        {
            if (_healthBarUnits.Count >= (i + 1)) return;
            var rect = GetComponent<RectTransform>();
            var bar = HealthBarUnit.GetNewHealthBarUnit(
                rect,
                healthbarColor,
                backgroundColor,
                new Vector2(unitWidth, unitHeight),
                getUnitPosition(i),
                childAnimator
            );
            _healthBarUnits.Add(bar);
            bar.name = $"bar_{i}";
        }

        private void cleanBars()
        {
            if (maxHealth >= _healthBarUnits.Count) return;
            var objectsToDestroy = new List<HealthBarUnit>();
            for (int i = maxHealth; i < _healthBarUnits.Count; i++) objectsToDestroy.Add(_healthBarUnits[i]);
            foreach (var objectToDestroy in objectsToDestroy)
            {
                _healthBarUnits.Remove(objectToDestroy);
                if (Application.isPlaying) Destroy(objectToDestroy.gameObject);
                else DestroyImmediate(objectToDestroy.gameObject);
            }
        }

        private void redrawBars()
        {
            for (int i = 0; i < _healthBarUnits.Count; i++)
            {
                var pos = getUnitPosition(i);
                _healthBarUnits[i].Redraw(
                    new Vector2(unitWidth, unitHeight),
                    pos,
                    healthbarColor,
                    backgroundColor
                );
            }
            var rect = GetComponent<RectTransform>();
            var totalColumns = maxHealth < maxHorizontalUnits ? maxHealth : maxHorizontalUnits;
            var totalRows = Mathf.Ceil(maxHealth / maxHorizontalUnits);
            rect.sizeDelta = new Vector2(
                unitWidth * totalColumns + horizontalSpacing * (totalColumns - 1),
                unitHeight * totalRows + verticalSpacing * (totalRows - 1)
            );
        }

        private void updateBars()
        {
            for (int i = 0; i < _healthBarUnits.Count; i++)
            {
                _healthBarUnits[i].SetFilled(i < currentHealth);
            }
        }
    }
}
