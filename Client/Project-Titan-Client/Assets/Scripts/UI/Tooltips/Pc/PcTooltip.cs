using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pc
{
    public abstract class PcTooltip<T> : Tooltip
    {
        public Image background;

        public Image outline;

        public RectTransform content;

        protected Vector2 contentSize;
        protected Vector2 ContentSize
        {
            get => contentSize;
            set
            {
                background.rectTransform.sizeDelta = new Vector2(value.x + 10, value.y + 10);
                contentSize = value;
            }
        }

        // Cached from the prefab on first layout pass so reused tooltips (skill tree)
        // can grow for a 3-line name and shrink back for a 1-line name.
        private float nameHeaderMinHeight = -1f;
        private float nameToContentGap = -1f;

        public abstract bool AutoSize { get; }

        public virtual bool FollowMouse => true;

        protected virtual void Awake()
        {
            ContentSize = background.rectTransform.sizeDelta - new Vector2(10, 10);
        }

        public override void LoadObject(Player player, bool owned, object obj)
        {
            Load(player, owned, (T)obj);
            if (AutoSize)
                ResizeContent();
        }

        protected abstract void Load(Player player, bool owned, T obj);

        public override void Hide()
        {
            Destroy(gameObject);
        }


        protected void ResizeContent()
        {
            var children = content.GetComponentsInChildren<RectTransform>();
            var max = new Vector2(0, 0);
            foreach (var child in children)
            {
                if (child == content) continue;
                var pos = child.position;
                var size = child.sizeDelta;
                var offset = size * child.pivot;
                pos -= new Vector3(offset.x, offset.y, 0);
                pos -= content.position;

                max.x = Mathf.Max(max.x, pos.x + size.x);
                max.y = Mathf.Max(max.y, -pos.y);
            }
            ContentSize = max;
        }

        // Name is stretch-filled inside the Name Shadow box. Growing that parent
        // (not the inner label) keeps the drop-shadow layer aligned.
        protected void FitNameHeader(TextMeshProUGUI nameLabel, TextMeshProUGUI contentLabel)
        {
            if (nameLabel == null) return;

            var header = nameLabel.rectTransform.parent as RectTransform;
            if (header == null)
                header = nameLabel.rectTransform;

            if (nameHeaderMinHeight < 0f)
                nameHeaderMinHeight = header.sizeDelta.y;

            if (nameToContentGap < 0f && contentLabel != null)
                nameToContentGap = -contentLabel.rectTransform.anchoredPosition.y - nameHeaderMinHeight;

            float width = header.rect.width;
            if (width < 8f)
                width = header.sizeDelta.x;
            if (width < 8f)
                width = 145f;

            nameLabel.enableWordWrapping = true;
            nameLabel.ForceMeshUpdate();
            var pref = nameLabel.GetPreferredValues(width, 0f);
            // Keep the default 40px row for 1–2 line names (matches the item icon).
            // Extra 4px stops a 3rd line's descenders from sitting on the body text.
            float height = Mathf.Max(nameHeaderMinHeight, pref.y + 4f);
            header.sizeDelta = new Vector2(header.sizeDelta.x, height);

            if (contentLabel != null)
            {
                var pos = contentLabel.rectTransform.anchoredPosition;
                pos.y = -(height + nameToContentGap);
                contentLabel.rectTransform.anchoredPosition = pos;
            }
        }

        protected void FitTextLabel(TextMeshProUGUI label, float defaultWidth = 220f)
        {
            if (label == null) return;

            float width = label.rectTransform.sizeDelta.x;
            if (width < 8f)
                width = label.rectTransform.rect.width;
            if (width < 8f)
                width = defaultWidth;

            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.rectTransform.sizeDelta = new Vector2(width, label.rectTransform.sizeDelta.y);
            label.ForceMeshUpdate();

            // Prefer the last glyph line's descender over GetPreferredValues, which
            // often reserves a blank line under "Usable by".
            float height = label.GetPreferredValues(width, 0f).y;
            var info = label.textInfo;
            if (info != null && info.lineCount > 0)
            {
                float bottom = info.lineInfo[info.lineCount - 1].descender;
                if (bottom < 0f)
                    height = -bottom;
            }
            label.rectTransform.sizeDelta = new Vector2(width, height);
        }

        protected void SetBackgroundColor(Color color)
        {
            background.color = color;
        }

        protected void SetOutlineColor(Color color)
        {
            outline.color = color;
        }

        protected virtual void LateUpdate()
        {
            if (!FollowMouse) return;
            PositionAtMouse();
        }

        public void PositionAtMouse()
        {
            var mousePos = Input.mousePosition;
            mousePos = new Vector3((int)mousePos.x, (int)mousePos.y, mousePos.z);

            var size = background.rectTransform.rect.size + new Vector2(16, 16);
            var offset = Vector3.zero;

            if (mousePos.x - size.x < 0)
                offset.x += size.x + 16;
            if (mousePos.y + size.y > Screen.height)
                offset.y += -size.y - 16;

            background.rectTransform.anchoredPosition = mousePos + new Vector3(-16, 16) + offset;
        }
    }
}
