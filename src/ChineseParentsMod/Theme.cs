using System.Collections.Generic;
using UnityEngine;

namespace ChineseParentsMod
{
    // Paper-and-seal look that matches the game: cream paper, brown ink, seal-red actions.
    // Sizes are authored for 1080p and multiplied by the screen scale here, instead of through GUI.matrix,
    // so glyphs and corners are rasterised at their final size and stay sharp. Textures are generated
    // at runtime, so the plugin ships as a single DLL.
    internal class Theme
    {
        public static readonly Color Paper = Hex(0xFBF5E8);
        public static readonly Color PaperDeep = Hex(0xF2E7CF);
        public static readonly Color Line = Hex(0xDCC9A4);
        public static readonly Color Ink = Hex(0x3E2D21);
        public static readonly Color InkSoft = Hex(0x8C7560);
        public static readonly Color Seal = Hex(0xC0392B);
        public static readonly Color SealDark = Hex(0x9E2E23);
        public static readonly Color SealDeep = Hex(0x7F241B);
        public static readonly Color Good = Hex(0x4E8240);
        public static readonly Color Warn = Hex(0xB7791F);

        public readonly float Scale;
        public GUIStyle Window, Shadow, Title, Hint, Section, Body, Note, NoteRow, Value, Empty, RowName;
        public GUIStyle Segment, Tab, Card, Row, RowAlt, Primary, Ghost, Field, Close, Message;
        public GUIStyle ChipGray, ChipAmber, ChipGreen, LockOff, LockOn;

        private readonly Font m_font;
        private readonly Texture2D m_clear;
        private readonly List<Texture2D> m_textures = new List<Texture2D>();

        public Theme(Font font, float scale)
        {
            m_font = font;
            Scale = scale;
            m_clear = Solid(new Color(0, 0, 0, 0));

            Window = Box(Rounded(Paper, Line, 14), 14, Pad(22, 22, 18, 20));
            Shadow = Box(ShadowTex(28), 28, new RectOffset());

            Title = Text(22, Ink, FontStyle.Bold);
            Hint = Text(13, InkSoft);
            Section = Text(17, Ink, FontStyle.Bold);
            Body = Text(14, InkSoft, wrap: true);
            Note = Text(13, InkSoft, wrap: true);
            Empty = Text(15, InkSoft, wrap: true);
            Empty.alignment = TextAnchor.MiddleCenter;
            RowName = Text(15, Ink);
            RowName.alignment = TextAnchor.MiddleLeft;
            Value = Text(17, Ink, FontStyle.Bold);
            Value.alignment = TextAnchor.MiddleRight;
            RowName.fixedHeight = Value.fixedHeight = S(30);
            RowName.padding = Value.padding = Pad(0, 0, 1, 5);
            NoteRow = Text(13, InkSoft);
            NoteRow.alignment = TextAnchor.MiddleLeft;
            NoteRow.fixedHeight = S(30);
            NoteRow.padding = Pad(0, 0, 1, 5);

            Segment = Box(Rounded(PaperDeep, Line, 10), 10, Pad(4, 4, 4, 4));
            Tab = Button(16, m_clear, m_clear, m_clear, InkSoft, Ink, 8);
            Tab.onNormal.background = Tab.onHover.background = Tab.onActive.background = Rounded(Seal, Seal, 8);
            Tab.onNormal.textColor = Tab.onHover.textColor = Tab.onActive.textColor = Color.white;
            Tab.fontStyle = FontStyle.Bold;
            Tab.margin = Pad(0, 0, 0, 0);
            Tab.fixedHeight = S(36);

            Card = Box(Rounded(Color.white, Line, 10), 10, Pad(16, 14, 12, 12));
            Card.margin = Pad(0, 0, 0, 10);
            Row = Box(Rounded(Color.white, Color.white, 8), 8, Pad(12, 8, 4, 4));
            RowAlt = Box(Rounded(PaperDeep, PaperDeep, 8), 8, Pad(12, 8, 4, 4));
            Row.margin = RowAlt.margin = Pad(0, 0, 0, 2);

            Primary = Button(15, Rounded(Seal, Seal, 8), Rounded(SealDark, SealDark, 8), Rounded(SealDeep, SealDeep, 8), Color.white, Color.white, 8);
            Primary.fontStyle = FontStyle.Bold;
            Primary.fixedHeight = S(38);
            Ghost = Button(14, Rounded(Color.white, Line, 7), Rounded(PaperDeep, Seal, 7), Rounded(Line, SealDark, 7), Ink, Seal, 7);
            Ghost.fixedHeight = S(30);
            Close = Button(24, m_clear, Rounded(PaperDeep, PaperDeep, 8), Rounded(Line, Line, 8), InkSoft, Seal, 8);
            Close.padding = Pad(0, 0, 0, 3);
            Close.fixedWidth = Close.fixedHeight = S(34);

            Field = new GUIStyle(GUI.skin.textField) { font = font, fontSize = S(15), alignment = TextAnchor.MiddleRight, fixedHeight = S(30) };
            Field.normal.background = Field.hover.background = Rounded(Color.white, Line, 7);
            Field.focused.background = Field.onFocused.background = Rounded(Color.white, Seal, 7, 2);
            Field.normal.textColor = Field.hover.textColor = Field.focused.textColor = Field.onFocused.textColor = Ink;
            Field.border = Pad(7, 7, 7, 7);
            Field.padding = Pad(8, 10, 2, 5);
            Field.margin = Pad(4, 4, 2, 2);

            ChipGray = Chip(Hex(0xE7DFD2), InkSoft);
            ChipAmber = Chip(Hex(0xF6E3BD), Warn);
            ChipGreen = Chip(Hex(0xDDEBD3), Good);

            LockOff = Button(14, Rounded(Color.white, Line, 15), Rounded(PaperDeep, Seal, 15), Rounded(Line, Line, 15), Ink, Seal, 15);
            LockOn = Button(14, Rounded(Seal, Seal, 15), Rounded(SealDark, SealDark, 15), Rounded(SealDeep, SealDeep, 15), Color.white, Color.white, 15);
            LockOff.fixedHeight = LockOn.fixedHeight = S(30);
            LockOn.fontStyle = FontStyle.Bold;

            Message = Box(Rounded(PaperDeep, PaperDeep, 8), 8, Pad(12, 12, 8, 9));
            Message.font = font;
            Message.fontSize = S(14);
            Message.wordWrap = true;
        }

        // Textures are not garbage collected; free them when the theme is rebuilt for a new resolution.
        public void Dispose()
        {
            foreach (Texture2D tex in m_textures)
            {
                Object.Destroy(tex);
            }
            m_textures.Clear();
        }

        public int S(float designPixels) => Mathf.RoundToInt(designPixels * Scale);

        private RectOffset Pad(int left, int right, int top, int bottom) => new RectOffset(S(left), S(right), S(top), S(bottom));

        private GUIStyle Text(int size, Color color, FontStyle style = FontStyle.Normal, bool wrap = false)
        {
            // Microsoft YaHei draws taller than the height IMGUI reserves, so leave room and never clip.
            var s = new GUIStyle(GUI.skin.label) { font = m_font, fontSize = S(size), fontStyle = style, richText = false, wordWrap = wrap, clipping = TextClipping.Overflow, padding = Pad(0, 0, 3, 4), margin = Pad(0, 0, 0, 0) };
            s.normal.textColor = color;
            return s;
        }

        private GUIStyle Box(Texture2D background, int radius, RectOffset padding)
        {
            var s = new GUIStyle { border = Pad(radius, radius, radius, radius), padding = padding };
            s.normal.background = background;
            return s;
        }

        private GUIStyle Button(int size, Texture2D normal, Texture2D hover, Texture2D active, Color text, Color hoverText, int radius)
        {
            var s = new GUIStyle(GUI.skin.button) { font = m_font, fontSize = S(size), border = Pad(radius, radius, radius, radius), padding = Pad(14, 14, 2, 5), margin = Pad(4, 4, 2, 2), clipping = TextClipping.Overflow };
            s.normal.background = s.focused.background = normal;
            s.hover.background = hover;
            s.active.background = active;
            s.normal.textColor = s.focused.textColor = text;
            s.hover.textColor = s.active.textColor = hoverText;
            return s;
        }

        private GUIStyle Chip(Color fill, Color text)
        {
            var s = Box(Rounded(fill, fill, 12), 12, Pad(12, 12, 3, 4));
            s.font = m_font;
            s.fontSize = S(13);
            s.fontStyle = FontStyle.Bold;
            s.normal.textColor = text;
            s.alignment = TextAnchor.MiddleCenter;
            s.fixedHeight = S(26);
            s.margin = Pad(10, 0, 5, 0);
            return s;
        }

        // Anti-aliased rounded rectangle, sliced 9 ways through GUIStyle.border.
        private Texture2D Rounded(Color fill, Color border, int designRadius, int borderWidth = 1)
        {
            int radius = S(designRadius);
            int size = radius * 2 + 4;
            float half = size / 2f, inner = half - radius;
            var tex = NewTexture(size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    Color c = Color.Lerp(border, fill, Mathf.Clamp01(radius - borderWidth * Scale - d + 0.5f));
                    c.a *= Mathf.Clamp01(radius - d + 0.5f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D ShadowTex(int designRadius)
        {
            int radius = S(designRadius);
            int size = radius * 2 + 4;
            float half = size / 2f, inner = half - radius;
            var tex = NewTexture(size);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    float t = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / radius);
                    tex.SetPixel(x, y, new Color(0.18f, 0.1f, 0.05f, t * t * 0.32f));
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D Solid(Color c)
        {
            var tex = NewTexture(1);
            tex.SetPixel(0, 0, c);
            tex.Apply();
            return tex;
        }

        private Texture2D NewTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            m_textures.Add(tex);
            return tex;
        }

        private static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
