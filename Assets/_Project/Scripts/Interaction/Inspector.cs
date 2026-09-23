using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Close inspection: held items are brought up and turned with the mouse; fixed objects
    /// (the painting) are shown full-screen with zoom and pan. Holding a hidden symbol in view,
    /// close up, reveals it (+1 perception).
    /// </summary>
    public class Inspector : MonoBehaviour
    {
        public InspectableObject Current { get; private set; }
        bool fixedView;
        float zoom = 1f;
        Vector2 pan;
        float distance;
        float symbolTimer;
        Vector3 savedLocalPos;
        Quaternion savedLocalRot;

        public bool IsInspecting(GameObject go) => Current != null && go != null && (Current.gameObject == go || Current.transform.IsChildOf(go.transform) || go.transform.IsChildOf(Current.transform));

        public void OpenHeld(InspectableObject item)
        {
            Current = item;
            fixedView = false;
            distance = item.inspectDistance;
            savedLocalPos = item.transform.localPosition;
            savedLocalRot = item.transform.localRotation;
            item.transform.localPosition = new Vector3(0, 0, distance);
            item.transform.localRotation = Quaternion.Euler(item.inspectEuler);
            Begin();
        }

        public void OpenFixed(InspectableObject item)
        {
            Current = item;
            fixedView = true;
            zoom = 1f;
            pan = Vector2.zero;
            Begin();
        }

        void Begin()
        {
            symbolTimer = 0f;
            Game.Mode = InputMode.Inspect;
            Game.Audio.PlayAt("pickup", Game.MainCamera.transform.position, 0.3f, 0.8f);
            if (Current.IsCatalogueItem) Game.State.Set("inspected_" + Current.itemId);
        }

        public void Close()
        {
            if (Current != null && !fixedView && Current.IsHeld)
            {
                Current.transform.localPosition = savedLocalPos;
                Current.transform.localRotation = savedLocalRot;
            }
            Current = null;
            Game.Mode = InputMode.Play;
        }

        void Update()
        {
            if (Current == null || Game.Mode != InputMode.Inspect) return;
            if (!Game.ModeJustChanged && (GameInput.Down(GameKey.Inspect) || GameInput.Down(GameKey.Cancel) || GameInput.Down(GameKey.Interact)))
            {
                Close();
                return;
            }

            if (fixedView) UpdateFixed();
            else UpdateHeld();
        }

        void UpdateHeld()
        {
            var t = Current.transform;
            var look = GameInput.LookDelta * 3f;
            var camT = Game.MainCamera.transform;
            t.Rotate(camT.up, -look.x, Space.World);
            t.Rotate(camT.right, look.y, Space.World);
            distance = Mathf.Clamp(distance - GameInput.Scroll * 0.04f, 0.2f, 0.7f);
            t.localPosition = Vector3.Lerp(t.localPosition, new Vector3(0, 0, distance), Time.deltaTime * 10f);

            if (Current.symbolLocalDir == Vector3.zero) return;
            Vector3 face = t.TransformDirection(Current.symbolLocalDir.normalized);
            bool facing = Vector3.Dot(face, -camT.forward) > 0.82f;
            bool close = distance <= 0.32f;
            TickSymbol(facing && close);
        }

        void UpdateFixed()
        {
            zoom = Mathf.Clamp(zoom + GameInput.Scroll * 0.35f, 1f, 4f);
            pan += GameInput.LookDelta * 0.004f / zoom;
            float lim = 0.5f - 0.5f / zoom;
            pan.x = Mathf.Clamp(pan.x, -lim, lim);
            pan.y = Mathf.Clamp(pan.y, -lim, lim);

            if (Current.symbolUV.x < 0f) return;
            Rect visible = VisibleUV;
            TickSymbol(zoom >= 2.2f && visible.Contains(Current.symbolUV));
        }

        void TickSymbol(bool inView)
        {
            if (!Current.HasSymbol || Game.State.Has(Current.SymbolFlag)) return;
            symbolTimer = inView ? symbolTimer + Time.deltaTime : 0f;
            if (symbolTimer >= 1.3f) Current.RevealSymbol();
        }

        /// <summary>Region of the texture currently on screen (fixed inspection).</summary>
        public Rect VisibleUV
        {
            get
            {
                float size = 1f / zoom;
                var c = new Vector2(0.5f + pan.x, 0.5f + pan.y);
                return new Rect(c.x - size / 2f, c.y - size / 2f, size, size);
            }
        }

        void OnGUI()
        {
            if (Current == null || Game.Mode != InputMode.Inspect) return;
            GUI.depth = 5;
            float w = Screen.width, h = Screen.height;

            if (fixedView)
            {
                GUI.color = new Color(0, 0, 0, 0.92f);
                GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
                GUI.color = Color.white;
                var tex = Current.DisplayTexture;
                if (tex != null)
                {
                    float aspect = (float)tex.width / tex.height;
                    float dh = h * 0.78f, dw = dh * aspect;
                    if (dw > w * 0.9f) { dw = w * 0.9f; dh = dw / aspect; }
                    var r = new Rect((w - dw) / 2f, (h - dh) / 2f - h * 0.03f, dw, dh);
                    GUI.DrawTextureWithTexCoords(r, tex, VisibleUV);
                }
            }

            string title = Current.IsCatalogueItem ? $"No. {Current.itemId} — {Current.displayName}" : Current.displayName;
            Hud.ShadowLabel(new Rect(0, h - 128, w, 30), title, Fonts.UI(20), TextAnchor.MiddleCenter, new Color(0.92f, 0.88f, 0.8f));
            if (!string.IsNullOrEmpty(Current.description))
                Hud.ShadowLabel(new Rect(w * 0.2f, h - 98, w * 0.6f, 44), Current.description, Fonts.UI(15), TextAnchor.UpperCenter, new Color(0.8f, 0.78f, 0.72f));
            string help = fixedView ? "[Scroll] closer   [Mouse] look around   [F] step back" : "[Mouse] turn   [Scroll] closer   [F] done";
            Hud.ShadowLabel(new Rect(0, h - 44, w, 24), help, Fonts.UI(13), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.45f));
        }
    }
}
