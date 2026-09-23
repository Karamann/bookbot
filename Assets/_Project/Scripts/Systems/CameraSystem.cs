using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// The Second Lamp: a 2002 consumer digital camera. The LCD shows a low-resolution lens camera that
    /// also renders the Memory layer. Photos are frozen copies; they keep whatever was true when taken.
    /// </summary>
    public class CameraSystem : MonoBehaviour
    {
        public const string HasCameraFlag = "has_camera";
        const int Width = 320, Height = 240, MaxPhotos = 30;

        public class Photo
        {
            public RenderTexture image;
            public string stamp;
            public List<string> subjects = new List<string>();
        }

        public Camera LensCamera { get; private set; }
        public bool IsRaised { get; private set; }
        public bool IsReviewing { get; private set; }
        public IReadOnlyList<Photo> Album => album;

        readonly List<Photo> album = new List<Photo>();
        RenderTexture live;
        Light flash;
        float raise, savedTimer, flashTimer;
        int reviewIndex;
        bool capturing, tipShown;
        string savedText;

        public void Setup(Camera main)
        {
            live = new RenderTexture(Width, Height, 16) { filterMode = FilterMode.Point, name = "LensRT" };
            var go = new GameObject("LensCamera");
            go.transform.SetParent(main.transform, false);
            LensCamera = go.AddComponent<Camera>();
            LensCamera.CopyFrom(main);
            LensCamera.fieldOfView = 48f;
            LensCamera.nearClipPlane = 0.05f;
            LensCamera.targetTexture = live;
            LensCamera.depth = main.depth - 1;
            LensCamera.cullingMask = ~((1 << Game.LayerPlayerBody) | (1 << Game.LayerViewmodel) | (1 << Game.LayerIgnoreRaycast));
            LensCamera.enabled = false;

            var fl = new GameObject("Flash");
            fl.transform.SetParent(main.transform, false);
            fl.transform.localPosition = new Vector3(0.05f, 0.05f, 0.1f);
            flash = fl.AddComponent<Light>();
            flash.type = LightType.Spot;
            flash.spotAngle = 80f;
            flash.range = 12f;
            flash.intensity = 5f;
            flash.color = new Color(0.9f, 0.95f, 1f);
            flash.enabled = false;
        }

        void Update()
        {
            raise = Mathf.MoveTowards(raise, IsRaised ? 1f : 0f, Time.deltaTime * 5f);
            savedTimer -= Time.deltaTime;
            if (flashTimer > 0f && (flashTimer -= Time.deltaTime) <= 0f) flash.enabled = false;

            if (IsRaised && Game.Mode != InputMode.Play && Game.Mode != InputMode.Paused) SetRaised(false);
            if (!Game.PlayStable || !Game.State.Has(HasCameraFlag)) return;

            if (GameInput.Down(GameKey.CameraToggle) || (IsRaised && GameInput.SecondaryDown)) SetRaised(!IsRaised);
            else if (!IsRaised && GameInput.SecondaryDown) SetRaised(true);

            if (!IsRaised) return;
            if (GameInput.Down(GameKey.Review)) IsReviewing = !IsReviewing && album.Count > 0;
            if (IsReviewing)
            {
                if (GameInput.Down(GameKey.NavLeft)) reviewIndex = Mathf.Max(0, reviewIndex - 1);
                if (GameInput.Down(GameKey.NavRight)) reviewIndex = Mathf.Min(album.Count - 1, reviewIndex + 1);
            }
            else if (GameInput.PrimaryDown && !capturing && album.Count < MaxPhotos)
            {
                StartCoroutine(Capture());
            }
        }

        public void SetRaised(bool raised)
        {
            IsRaised = raised;
            IsReviewing = false;
            LensCamera.enabled = raised;
            Game.Audio.PlayAt("beep", transform.position, 0.25f, raised ? 1.2f : 0.9f);
            if (raised && !tipShown)
            {
                tipShown = true;
                Game.Hud.Tip("[Left mouse] take photo    [R] review photos    [C] lower", 6f);
            }
        }

        IEnumerator Capture()
        {
            capturing = true;
            bool dark = Game.Lighting.IsDarkAt(Game.Player.transform.position) && !Game.Lighting.MindActive;
            if (dark)
            {
                flash.enabled = true;
                flashTimer = 0.09f;
            }
            Game.Audio.PlayAt("shutter", transform.position, 0.6f);
            yield return new WaitForEndOfFrame();

            var copy = new RenderTexture(Width, Height, 0) { filterMode = FilterMode.Point, name = "Photo" };
            Graphics.Blit(live, copy);
            var photo = new Photo { image = copy, stamp = $"{GameState.DateText}  {Game.State.ClockText}" };
            DetectSubjects(photo);
            album.Add(photo);
            reviewIndex = album.Count - 1;
            Game.State.Add("photos_taken");
            savedText = photo.subjects.Count > 0 ? "SAVED  #" + string.Join(" #", photo.subjects) : "SAVED";
            savedTimer = 1.4f;
            yield return new WaitForSeconds(0.35f);
            capturing = false;
        }

        void DetectSubjects(Photo photo)
        {
            foreach (var item in FindObjectsByType<InspectableObject>(FindObjectsSortMode.None))
            {
                if (!item.IsCatalogueItem || item.IsHeld) continue;
                if (!Perceive.IsFocused(LensCamera, item.gameObject, 3.5f, 0.1f)) continue;
                photo.subjects.Add(item.itemId);
                Game.State.Set("photographed_" + item.itemId);
                if (item.GetComponentInChildren<MaterialStates>() is MaterialStates ms && ms.Current != null)
                    Game.State.Set($"photo_{item.itemId}_{ms.Current}");
            }
            foreach (var a in FindObjectsByType<Apparition>(FindObjectsSortMode.None))
            {
                if (Perceive.CanSee(LensCamera, a.gameObject, a.range)) Game.State.Set("photo_has_" + a.id);
            }
        }

        void OnGUI()
        {
            if (raise <= 0.001f) return;
            GUI.depth = 6;
            float w = Screen.width, h = Screen.height;
            float lw = Mathf.Min(w * 0.46f, h * 0.62f * 4f / 3f), lh = lw * 0.75f;
            float y = Mathf.Lerp(h + 20f, h - lh - h * 0.12f, Mathf.SmoothStep(0, 1, raise));
            var body = new Rect((w - lw) / 2f - 26f, y - 22f, lw + 52f, lh + 70f);
            var lcd = new Rect((w - lw) / 2f, y, lw, lh);

            Hud.Fill(body, new Color(0.14f, 0.14f, 0.15f));
            Hud.Fill(new Rect(body.x + 4, body.y + 4, body.width - 8, 3), new Color(0.24f, 0.24f, 0.26f));
            Hud.Fill(new Rect(lcd.x - 4, lcd.y - 4, lcd.width + 8, lcd.height + 8), Color.black);

            Texture shown = live;
            string stamp = $"{GameState.DateText}  {Game.State.ClockText}";
            if (IsReviewing && album.Count > 0)
            {
                shown = album[reviewIndex].image;
                stamp = album[reviewIndex].stamp;
            }
            if (shown != null) GUI.DrawTexture(lcd, shown, ScaleMode.StretchToFill, false);
            Game.Fx.DrawGrain(lcd, 0.14f);
            Game.Fx.DrawScanlines(lcd, 0.12f);

            var lcdText = new Color(0.95f, 0.95f, 0.95f, 0.9f);
            var font = Fonts.Mono(Mathf.RoundToInt(lh * 0.055f));
            Hud.Label(new Rect(lcd.x + 8, lcd.y + 6, 200, 20), IsReviewing ? "▶ PLAY" : "■ P  640x480", font, TextAnchor.UpperLeft, lcdText);
            string counter = IsReviewing ? $"{reviewIndex + 1}/{album.Count}" : $"[{MaxPhotos - album.Count}]";
            Hud.Label(new Rect(lcd.xMax - 108, lcd.y + 6, 100, 20), counter, font, TextAnchor.UpperRight, lcdText);
            Hud.Label(new Rect(lcd.xMax - 220, lcd.yMax - 26, 212, 20), stamp, font, TextAnchor.LowerRight, new Color(1f, 0.55f, 0.1f, 0.95f));
            if (!IsReviewing)
            {
                // focus brackets
                var bc = new Color(1, 1, 1, 0.5f);
                float cx = lcd.center.x, cy = lcd.center.y, s = lh * 0.12f;
                Hud.Fill(new Rect(cx - s, cy - s, 10, 2), bc); Hud.Fill(new Rect(cx - s, cy - s, 2, 10), bc);
                Hud.Fill(new Rect(cx + s - 10, cy - s, 10, 2), bc); Hud.Fill(new Rect(cx + s - 2, cy - s, 2, 10), bc);
                Hud.Fill(new Rect(cx - s, cy + s - 2, 10, 2), bc); Hud.Fill(new Rect(cx - s, cy + s - 10, 2, 10), bc);
                Hud.Fill(new Rect(cx + s - 10, cy + s - 2, 10, 2), bc); Hud.Fill(new Rect(cx + s - 2, cy + s - 10, 2, 10), bc);
            }
            if (savedTimer > 0f)
                Hud.Label(new Rect(lcd.x, lcd.center.y - 12, lcd.width, 24), savedText, font, TextAnchor.MiddleCenter, lcdText);
            Hud.Label(new Rect(body.x, body.yMax - 30, body.width, 20), "DIGICAM 2.1MP", Fonts.UI(11), TextAnchor.MiddleCenter, new Color(0.55f, 0.55f, 0.58f));
        }
    }
}
