using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

// ══════════════════════════════════════════════════════════════════
//  KLEURENPALET  (centrale plek, makkelijk te wijzigen)
// ══════════════════════════════════════════════════════════════════
static class Palette
{
    public static readonly Color BG1        = new Color( 18,  10,   5, 255); // bijna zwart
    public static readonly Color BG2        = new Color( 38,  22,  10, 255); // donker bruin
    public static readonly Color Panel      = new Color( 30,  18,   8, 220);
    public static readonly Color PanelBord  = new Color(180, 120,  40, 180);

    public static readonly Color Gold1      = new Color(255, 200,  60, 255);
    public static readonly Color Gold2      = new Color(220, 150,  30, 255);
    public static readonly Color Gold3      = new Color(255, 230, 120, 255);

    public static readonly Color CookieBase = new Color(200, 130,  55, 255);
    public static readonly Color CookieMid  = new Color(230, 165,  80, 255);
    public static readonly Color CookieHigh = new Color(255, 200, 110, 200);
    public static readonly Color Chip       = new Color( 70,  35,  10, 255);
    public static readonly Color ChipHigh   = new Color(110,  60,  20, 180);

    public static readonly Color TextMain   = new Color(255, 220, 130, 255);
    public static readonly Color TextSub    = new Color(200, 160,  80, 200);
    public static readonly Color TextDim    = new Color(140, 100,  40, 180);

    public static readonly Color BtnOff     = new Color( 55,  35,  15, 255);
    public static readonly Color BtnOffBord = new Color( 90,  60,  20, 255);

    public static readonly Color BoostGlow  = new Color(255, 120,  20, 255);
    public static readonly Color BoostText  = new Color(255, 160,  40, 255);

    public static Color WithAlpha(Color c, byte a) => new Color(c.R, c.G, c.B, a);
}

// ══════════════════════════════════════════════════════════════════
//  NIEUW ─ ShopItem  (ongewijzigd t.o.v. vorige versie)
// ══════════════════════════════════════════════════════════════════
class ShopItem
{
    public string Name;
    public string Desc;
    public string Icon;   // emoji-achtig label (ASCII)
    public int    Cost;
    public int    Count;
    public float  CostMult;
    public string Type;
    public int    Value;
    public float  BoostDuration;
    public float  BoostMult;

    public ShopItem(string name, string desc, string icon, int cost, float costMult,
                    string type, int value = 0,
                    float boostDuration = 0f, float boostMult = 1f)
    {
        Name = name; Desc = desc; Icon = icon;
        Cost = cost; Count = 0; CostMult = costMult;
        Type = type; Value = value;
        BoostDuration = boostDuration; BoostMult = boostMult;
    }

    public bool TryBuy(ref int cookies)
    {
        if (cookies < Cost) return false;
        cookies -= Cost;
        Count++;
        Cost = (int)MathF.Ceiling(Cost * CostMult);
        return true;
    }
}

// ══════════════════════════════════════════════════════════════════
//  NIEUW ─ BoostManager  (ongewijzigd)
// ══════════════════════════════════════════════════════════════════
class BoostManager
{
    public float Multiplier { get; private set; } = 1f;
    public float TimeLeft   { get; private set; } = 0f;
    public bool  IsActive   => TimeLeft > 0f;

    public void Activate(float mult, float duration)
    {
        Multiplier = MathF.Max(Multiplier, mult);
        TimeLeft   = MathF.Max(TimeLeft,   duration);
    }

    public void Update(float dt)
    {
        if (TimeLeft <= 0f) return;
        TimeLeft -= dt;
        if (TimeLeft <= 0f) { TimeLeft = 0f; Multiplier = 1f; }
    }
}

// ══════════════════════════════════════════════════════════════════
//  NIEUW ─ UpgradeShop  (visueel volledig herschreven)
// ══════════════════════════════════════════════════════════════════
class UpgradeShop
{
    public readonly List<ShopItem> Items = new();

    private const int PX   = 598;
    private const int PY   = 58;
    private const int PW   = 285;
    private const int IH   = 66;
    private const int IPAD = 4;

    // hover state
    private int _hoverIndex = -1;

    public UpgradeShop()
    {
        Items.Add(new ShopItem("Oma",          "+1 koek/sec",    "OMA",   15,  1.35f, "cps",    1));
        Items.Add(new ShopItem("Bakkerij",      "+5 koek/sec",    "BAK",   80,  1.40f, "cps",    5));
        Items.Add(new ShopItem("Fabriek",       "+20 koek/sec",   "FAB",  400,  1.45f, "cps",   20));
        Items.Add(new ShopItem("Magie",         "+50 koek/sec",   "MAG", 2000,  1.50f, "cps",   50));
        Items.Add(new ShopItem("Betere vinger", "+1 per klik",    "VNG",   25,  1.50f, "click",  1));
        Items.Add(new ShopItem("Gouden nagel",  "+5 per klik",    "NAG",  150,  1.55f, "click",  5));
        Items.Add(new ShopItem("Robotarm",      "+15 per klik",   "ROB",  800,  1.60f, "click", 15));
        Items.Add(new ShopItem("Suikerkoorts",  "2x klik (10s)",  "SUK",   60,  1.60f, "boost",  0, 10f, 2f));
        Items.Add(new ShopItem("Cookie Frenzy", "5x klik (7s)",   "FRZ",  350,  1.70f, "boost",  0,  7f, 5f));
        Items.Add(new ShopItem("Gouden Koek",   "10x klik (5s)",  "GOD", 1500,  1.80f, "boost",  0,  5f, 10f));
    }

    public int BonusCps()
    {
        int t = 0;
        foreach (var i in Items) if (i.Type == "cps") t += i.Count * i.Value;
        return t;
    }

    public int BonusClicks()
    {
        int t = 0;
        foreach (var i in Items) if (i.Type == "click") t += i.Count * i.Value;
        return t;
    }

    public void HandleClick(Vector2 mouse, ref int cookies, BoostManager boosts)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (!Raylib.CheckCollisionPointRec(mouse, GetRect(i))) continue;
            var item = Items[i];
            if (item.TryBuy(ref cookies) && item.Type == "boost")
                boosts.Activate(item.BoostMult, item.BoostDuration);
        }
    }

    public void UpdateHover(Vector2 mouse)
    {
        _hoverIndex = -1;
        for (int i = 0; i < Items.Count; i++)
            if (Raylib.CheckCollisionPointRec(mouse, GetRect(i))) { _hoverIndex = i; break; }
    }

    public void Draw(int cookies, BoostManager boosts, float time)
    {
        int totalH = Items.Count * (IH + IPAD) + 38;
        var panelRect = new Rectangle(PX - 10, PY - 10, PW + 20, totalH + 14);

        // Paneel achtergrond met rand
        Raylib.DrawRectangleRounded(panelRect, 0.04f, 8, Palette.Panel);
        Raylib.DrawRectangleRoundedLines(panelRect, 0.04f, 8, Palette.PanelBord);

        // Decoratieve bovenlijn
        Raylib.DrawRectangle(PX - 10, PY - 10, PW + 20, 3, Palette.Gold1);

        // Titel
        DrawCenteredText("W I N K E L", PX, PY + 2, PW, 20, Palette.Gold1);

        // Scheidingslijn onder titel
        Raylib.DrawLineEx(
            new Vector2(PX, PY + 24),
            new Vector2(PX + PW, PY + 24),
            1f, Palette.PanelBord);

        for (int i = 0; i < Items.Count; i++)
        {
            var item  = Items[i];
            var rect  = GetRect(i);
            bool can  = cookies >= item.Cost;
            bool hover = i == _hoverIndex && can;

            // Achtergrond van item
            Color bgBase = can
                ? (hover ? new Color(70, 45, 12, 255) : new Color(50, 30, 8, 255))
                : Palette.BtnOff;

            Raylib.DrawRectangleRounded(rect, 0.18f, 6, bgBase);

            // Gouden rand — gloeit bij hover
            Color border = can
                ? (hover ? Palette.Gold3 : Palette.Gold2)
                : Palette.BtnOffBord;
            Raylib.DrawRectangleRoundedLines(rect, 0.18f, 6, border);

            // Type-badge links
            Color badgeColor = item.Type switch
            {
                "cps"   => new Color(60, 160, 80, 220),
                "click" => new Color(60, 100, 200, 220),
                _       => new Color(200, 80, 20, 220),
            };
            var badgeRect = new Rectangle(rect.X + 5, rect.Y + (IH - 44) / 2f, 38, 44);
            Raylib.DrawRectangleRounded(badgeRect, 0.3f, 4, badgeColor);
            DrawCenteredText(item.Icon, (int)badgeRect.X, (int)badgeRect.Y + 4, (int)badgeRect.Width, 14,
                new Color(255, 255, 255, 220));

            // Naam
            Color nameColor = can ? Palette.TextMain : Palette.TextDim;
            Raylib.DrawText(item.Name, (int)rect.X + 50, (int)rect.Y + 7, 17, nameColor);

            // Beschrijving
            Raylib.DrawText(item.Desc, (int)rect.X + 50, (int)rect.Y + 27, 13, Palette.TextSub);

            // Prijs rechtsonder
            string priceStr = can ? $"{item.Cost} koekjes" : $"Kost: {item.Cost}";
            Color  priceCol = can ? Palette.Gold1 : Palette.TextDim;
            int    ptw      = Raylib.MeasureText(priceStr, 13);
            Raylib.DrawText(priceStr,
                (int)(rect.X + rect.Width - ptw - 8),
                (int)(rect.Y + IH - 20), 13, priceCol);

            // Count badge rechtsonder
            if (item.Count > 0)
            {
                string cnt = $"x{item.Count}";
                int ctw = Raylib.MeasureText(cnt, 12);
                Raylib.DrawText(cnt, (int)rect.X + 50, (int)(rect.Y + IH - 20), 12, Palette.TextSub);
            }

            // Boost actief indicator
            if (item.Type == "boost" && boosts.IsActive)
            {
                string t = $"ACTIEF {boosts.TimeLeft:F1}s";
                Raylib.DrawText(t, (int)rect.X + 50, (int)(rect.Y + IH - 20), 12, Palette.BoostText);
            }

            // Shimmer lijn bij hover
            if (hover)
            {
                float shimX = rect.X + ((time * 180f) % (rect.Width + 40)) - 20;
                Raylib.DrawLineEx(
                    new Vector2(shimX, rect.Y + 4),
                    new Vector2(shimX + 18, rect.Y + IH - 4),
                    3f, Palette.WithAlpha(Palette.Gold3, 80));
            }
        }
    }

    private Rectangle GetRect(int i) =>
        new Rectangle(PX, PY + 30 + i * (IH + IPAD), PW, IH);

    private static void DrawCenteredText(string text, int x, int y, int width, int fontSize, Color color)
    {
        int tw = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, x + (width - tw) / 2, y, fontSize, color);
    }
}

// ══════════════════════════════════════════════════════════════════
//  VISUELE HELPERS  (nieuwe statische klasse, raakt Main niet aan)
// ══════════════════════════════════════════════════════════════════
static class Gfx
{
    // Gevulde cirkel met zachte rand (gesimuleerde glow via lagen)
    public static void DrawGlow(Vector2 pos, float radius, Color color, int layers = 5)
    {
        for (int i = layers; i >= 1; i--)
        {
            float   r = radius * (1f + i * 0.18f);
            byte    a = (byte)(color.A / (i * 2.2f));
            Raylib.DrawCircleV(pos, r, Palette.WithAlpha(color, a));
        }
        Raylib.DrawCircleV(pos, radius, color);
    }

    // Decoratieve achtergrond (radiaal verloop via lagen)
    public static void DrawBackground(int w, int h, float time)
    {
        Raylib.ClearBackground(Palette.BG1);

        // Subtiele centrale glow
        for (int i = 8; i >= 1; i--)
        {
            float r = w * 0.45f * i / 8f;
            byte  a = (byte)(18 - i);
            Raylib.DrawCircleV(new Vector2(w / 2f - 120, h / 2f - 40), r,
                Palette.WithAlpha(Palette.BG2, a));
        }

        // Subtiele stippen / sterrenachtergrond
        var rng = new System.Random(42);
        for (int i = 0; i < 60; i++)
        {
            float x = (float)rng.NextDouble() * w;
            float y = (float)rng.NextDouble() * h;
            float pulse = 0.4f + 0.6f * MathF.Abs(MathF.Sin(time * 0.7f + i));
            byte  a = (byte)(20 * pulse);
            Raylib.DrawCircleV(new Vector2(x, y), 1.2f, Palette.WithAlpha(Palette.Gold2, a));
        }
    }

    // Mooie cookie tekenen
    public static void DrawCookie(Vector2 pos, float radius, float scale, float time)
    {
        float r = radius * scale;

        // Buitenste glow
        for (int i = 6; i >= 1; i--)
        {
            float gr = r * (1f + i * 0.12f);
            byte   a = (byte)(12 - i);
            Raylib.DrawCircleV(pos, gr, Palette.WithAlpha(Palette.Gold1, a));
        }

        // Cookie basis (meerdere lagen voor diepte)
        Raylib.DrawCircleV(pos, r,        Palette.CookieBase);
        Raylib.DrawCircleV(pos, r * 0.96f, Palette.CookieMid);

        // Highlight (top-links)
        Raylib.DrawCircleV(pos + new Vector2(-r * 0.22f, -r * 0.22f),
            r * 0.72f, Palette.WithAlpha(Palette.CookieHigh, 80));

        // Rand/schaduw (donkere rand)
        Raylib.DrawCircleLines((int)pos.X, (int)pos.Y, r,
            Palette.WithAlpha(Palette.Chip, 140));

        // Chips
        DrawChip(pos + new Vector2(-r * 0.38f, -r * 0.30f), r * 0.11f);
        DrawChip(pos + new Vector2( r * 0.46f, -r * 0.23f), r * 0.12f);
        DrawChip(pos + new Vector2(-r * 0.23f,  r * 0.38f), r * 0.115f);
        DrawChip(pos + new Vector2( r * 0.31f,  r * 0.46f), r * 0.09f);
        DrawChip(pos + new Vector2( r * 0.05f, -r * 0.08f), r * 0.135f);
        DrawChip(pos + new Vector2(-r * 0.52f,  r * 0.10f), r * 0.095f);
        DrawChip(pos + new Vector2( r * 0.18f,  r * 0.15f), r * 0.085f);
    }

    private static void DrawChip(Vector2 pos, float radius)
    {
        Raylib.DrawCircleV(pos, radius,           Palette.Chip);
        Raylib.DrawCircleV(pos + new Vector2(-radius * 0.3f, -radius * 0.3f),
            radius * 0.4f, Palette.ChipHigh);
    }

    // Fancy button (links in beeld)
    public static void DrawFancyButton(Rectangle rect, string text, bool canAfford, bool hover, float time)
    {
        Color bg = canAfford
            ? (hover ? new Color(90, 55, 10, 255) : new Color(65, 38, 8, 255))
            : new Color(40, 28, 12, 255);

        Color border = canAfford
            ? (hover ? Palette.Gold3 : Palette.Gold2)
            : Palette.BtnOffBord;

        Raylib.DrawRectangleRounded(rect, 0.35f, 8, bg);
        Raylib.DrawRectangleRoundedLines(rect, 0.35f, 8, border);

        // Glans lijn bovenaan
        if (canAfford)
        {
            Raylib.DrawLineEx(
                new Vector2(rect.X + 12, rect.Y + 4),
                new Vector2(rect.X + rect.Width - 12, rect.Y + 4),
                1.5f, Palette.WithAlpha(Palette.Gold3, 60));
        }

        // Shimmer
        if (hover && canAfford)
        {
            float shimX = rect.X + ((time * 160f) % (rect.Width + 30)) - 15;
            Raylib.DrawLineEx(
                new Vector2(shimX, rect.Y + 5),
                new Vector2(shimX + 14, rect.Y + rect.Height - 5),
                3f, Palette.WithAlpha(Palette.Gold3, 90));
        }

        Color txtColor = canAfford ? Palette.TextMain : Palette.TextDim;
        int   tw       = Raylib.MeasureText(text, 18);
        Raylib.DrawText(text,
            (int)(rect.X + (rect.Width - tw) / 2),
            (int)(rect.Y + (rect.Height - 18) / 2), 18, txtColor);
    }

    // Stat-paneel links
    public static void DrawStatPanel(int cookies, int cps, int perClick, BoostManager boosts, float time)
    {
        var panRect = new Rectangle(8, 58, 280, 145);
        Raylib.DrawRectangleRounded(panRect, 0.06f, 6, Palette.Panel);
        Raylib.DrawRectangleRoundedLines(panRect, 0.06f, 6, Palette.PanelBord);
        Raylib.DrawRectangle(8, 58, 280, 3, Palette.Gold1);

        Raylib.DrawText($"{cookies}",    18,  68, 22, Palette.Gold1);
        Raylib.DrawText("koekjes",       18,  92, 14, Palette.TextSub);
        Raylib.DrawText($"CPS: {cps}",   18, 112, 16, Palette.TextMain);
        Raylib.DrawText($"Klik: +{perClick}", 18, 132, 16, Palette.TextMain);

        // Boost balk
        if (boosts.IsActive)
        {
            float pct  = boosts.TimeLeft / 10f; // max 10s referentie
            float pulse = 0.7f + 0.3f * MathF.Sin(time * 8f);
            byte  ba   = (byte)(200 * pulse);

            var boostRect = new Rectangle(8, 205, 280, 28);
            Raylib.DrawRectangleRounded(boostRect, 0.4f, 6,
                Palette.WithAlpha(new Color(180, 60, 10, 255), 180));
            Raylib.DrawRectangleRounded(new Rectangle(8, 205, 280 * MathF.Min(pct, 1f), 28),
                0.4f, 6, Palette.WithAlpha(Palette.BoostGlow, ba));

            string boostTxt = $"BOOST x{boosts.Multiplier:F0}  {boosts.TimeLeft:F1}s";
            int btw = Raylib.MeasureText(boostTxt, 15);
            Raylib.DrawText(boostTxt, 8 + (280 - btw) / 2, 210, 15, Color.White);
        }
    }

    // Titel met glow
    public static void DrawTitle(float time)
    {
        string title = "COOKIE CLICKER DELUXE";
        float  pulse = 0.85f + 0.15f * MathF.Sin(time * 1.4f);
        byte   ga    = (byte)(160 * pulse);

        // Glow laag
        int gtw = Raylib.MeasureText(title, 36);
        Raylib.DrawText(title, (900 - gtw) / 2 + 1, 13, 36, Palette.WithAlpha(Palette.Gold2, ga));
        // Echte tekst
        Raylib.DrawText(title, (900 - gtw) / 2,     12, 36, Palette.Gold1);
    }
}

// ══════════════════════════════════════════════════════════════════
//  BESTAANDE LOGICA ─ zo min mogelijk gewijzigd.
//  Elke aanpassing staat met  // <- NIEUW  gemarkeerd.
// ══════════════════════════════════════════════════════════════════
class Program
{
    class Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Life;
        public float Size;
        public Color Color;
    }

    static void Main()
    {
        const int screenWidth  = 900;
        const int screenHeight = 700;

        Raylib.InitWindow(screenWidth, screenHeight, "Cookie Clicker Deluxe");
        Raylib.SetTargetFPS(60);

        int cookies     = 0;
        int cps         = 0;
        int upgradeCost = 20;

        float cookieRadius = 130f;
        Vector2 cookiePos  = new Vector2(screenWidth / 2f - 120, screenHeight / 2f - 20); // <- NIEUW: iets naar links

        float cookieScale   = 1f;
        bool  cookieClicked = false;

        double lastCpsTick = Raylib.GetTime();

        List<Particle> particles = new List<Particle>();

        float cursorAngle = 0f;

        var shop           = new UpgradeShop();
        var boosts         = new BoostManager();
        int clicksPerClick = 1;

        // <- NIEUW: hover state voor originele knop
        bool btnHover = false;

        while (!Raylib.WindowShouldClose())
        {
            double now      = Raylib.GetTime();
            float  dt       = Raylib.GetFrameTime();
            float  time     = (float)now;              // <- NIEUW: voor animaties

            boosts.Update(dt);

            cps            = shop.BonusCps();
            clicksPerClick = 1 + shop.BonusClicks();

            if (now - lastCpsTick >= 1.0)
            {
                cookies    += cps;
                lastCpsTick = now;
            }

            Vector2 mouse = Raylib.GetMousePosition();  // <- NIEUW: eenmalig ophalen

            // <- NIEUW: hover bijhouden
            Rectangle upgradeButton = new Rectangle(15, 590, 270, 58);
            btnHover = Raylib.CheckCollisionPointRec(mouse, upgradeButton);
            shop.UpdateHover(mouse);

            // Klik op cookie
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                float dist = Vector2.Distance(mouse, cookiePos);
                if (dist <= cookieRadius * cookieScale)
                {
                    cookies      += (int)(clicksPerClick * boosts.Multiplier);
                    cookieClicked = true;
                    cookieScale   = 0.85f;

                    for (int i = 0; i < 22; i++)
                    {
                        particles.Add(new Particle
                        {
                            Pos   = cookiePos,
                            Vel   = new Vector2(
                                Raylib.GetRandomValue(-250, 250) / 100f,
                                Raylib.GetRandomValue(-250, 250) / 100f),
                            Life  = 1f,
                            Size  = Raylib.GetRandomValue(3, 9),
                            Color = (i % 3 == 0) ? Palette.Gold3 : Palette.CookieHigh
                        });
                    }
                }

                shop.HandleClick(mouse, ref cookies, boosts);

                // Originele upgrade knop
                if (Raylib.CheckCollisionPointRec(mouse, upgradeButton) && cookies >= upgradeCost)
                {
                    cookies     -= upgradeCost;
                    cps++;
                    upgradeCost  = (int)MathF.Ceiling(upgradeCost * 1.4f);
                }
            }

            // Cookie animatie
            if (cookieClicked)
            {
                cookieScale += 0.05f;
                if (cookieScale >= 1f) { cookieScale = 1f; cookieClicked = false; }
            }

            // Particle update
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Pos  += particles[i].Vel;
                particles[i].Life -= 0.018f;
                particles[i].Vel  *= 0.97f;
                if (particles[i].Life <= 0) particles.RemoveAt(i);
            }

            cursorAngle += 0.35f * dt;

            // ── TEKENEN ───────────────────────────────────────────
            Raylib.BeginDrawing();

            Gfx.DrawBackground(screenWidth, screenHeight, time);    // <- NIEUW
            Gfx.DrawTitle(time);                                     // <- NIEUW
            Gfx.DrawStatPanel(cookies, cps, clicksPerClick, boosts, time); // <- NIEUW

            // Cursors
            DrawCursors(cookiePos, cookieRadius, cps, cursorAngle);

            // Cookie (volledig herschreven via Gfx)
            Gfx.DrawCookie(cookiePos, cookieRadius, cookieScale, time); // <- NIEUW

            // Particles
            foreach (var p in particles)
            {
                byte  alpha = (byte)(p.Life * 255);
                Color c     = Palette.WithAlpha(p.Color, alpha);
                Raylib.DrawCircleV(p.Pos, p.Size * p.Life + 1f, c);
            }

            // Originele upgrade knop (via nieuwe Gfx helper)
            string btnText = $"+1 CPS  |  Kost: {upgradeCost}";
            Gfx.DrawFancyButton(upgradeButton, btnText, cookies >= upgradeCost, btnHover, time); // <- NIEUW

            // Winkel
            shop.Draw(cookies, boosts, time);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    // DrawChip blijft staan maar wordt nu via Gfx.DrawCookie gebruikt
    static void DrawChip(Vector2 pos, float radius)
    {
        Raylib.DrawCircleV(pos, radius, new Color(90, 50, 20, 255));
    }

    static void DrawCursors(Vector2 center, float cookieRadius, int cps, float angleOffset)
    {
        if (cps <= 0) return;
        int   count       = Math.Min(cps, 64);
        float orbitRadius = cookieRadius * 1.62f;
        float angleStep   = (MathF.PI * 2f) / count;

        for (int i = 0; i < count; i++)
        {
            float   angle      = i * angleStep + angleOffset;
            Vector2 pos        = center + new Vector2(MathF.Cos(angle) * orbitRadius, MathF.Sin(angle) * orbitRadius);
            float   pointAngle = angle + MathF.PI;
            float   size       = 13f;

            Vector2 tip   = pos + new Vector2(MathF.Cos(pointAngle)        * size,        MathF.Sin(pointAngle)        * size);
            Vector2 left  = pos + new Vector2(MathF.Cos(pointAngle + 2.5f) * size * 0.6f, MathF.Sin(pointAngle + 2.5f) * size * 0.6f);
            Vector2 right = pos + new Vector2(MathF.Cos(pointAngle - 2.5f) * size * 0.6f, MathF.Sin(pointAngle - 2.5f) * size * 0.6f);

            // Gouden cursor
            Raylib.DrawTriangle(tip, left, right, Palette.Gold2);
            Raylib.DrawTriangleLines(tip, left, right, Palette.Gold1);
        }
    }
}