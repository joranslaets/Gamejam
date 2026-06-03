using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
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
        const int screenWidth = 900;
        const int screenHeight = 700;

        Raylib.InitWindow(screenWidth, screenHeight, "Cookie Clicker Deluxe");
        Raylib.SetTargetFPS(60);

        int cookies = 0;
        int cps = 0;
        int upgradeCost = 20;

        float cookieRadius = 130f;
        Vector2 cookiePos = new Vector2(screenWidth / 2f, screenHeight / 2f - 40);

        float cookieScale = 1f;
        bool cookieClicked = false;

        double lastCpsTick = Raylib.GetTime();

        List<Particle> particles = new List<Particle>();

        while (!Raylib.WindowShouldClose())
        {
            double now = Raylib.GetTime();
            if (now - lastCpsTick >= 1.0)
            {
                cookies += cps;
                lastCpsTick = now;
            }

            // Klik op cookie
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mouse = Raylib.GetMousePosition();
                float dist = Vector2.Distance(mouse, cookiePos);

                if (dist <= cookieRadius * cookieScale)
                {
                    cookies++;
                    cookieClicked = true;
                    cookieScale = 0.85f;

                    // Particle burst
                    for (int i = 0; i < 20; i++)
                    {
                        particles.Add(new Particle
                        {
                            Pos = cookiePos,
                            Vel = new Vector2(
                                Raylib.GetRandomValue(-200, 200) / 100f,
                                Raylib.GetRandomValue(-200, 200) / 100f
                            ),
                            Life = 1f,
                            Size = Raylib.GetRandomValue(4, 8),
                            Color = new Color(255, 230, 150, 255)
                        });
                    }
                }
            }

            // Upgrade knop
            Rectangle upgradeButton = new Rectangle(300, 550, 300, 70);

            if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
                Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), upgradeButton))
            {
                if (cookies >= upgradeCost)
                {
                    cookies -= upgradeCost;
                    cps++;
                    upgradeCost = (int)MathF.Ceiling(upgradeCost * 1.4f);
                }
            }

            // Cookie animatie
            if (cookieClicked)
            {
                cookieScale += 0.04f;
                if (cookieScale >= 1f)
                {
                    cookieScale = 1f;
                    cookieClicked = false;
                }
            }

            // Particle update
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                particles[i].Pos += particles[i].Vel;
                particles[i].Life -= 0.02f;
                particles[i].Vel *= 0.98f;

                if (particles[i].Life <= 0)
                    particles.RemoveAt(i);
            }

            // TEKENEN
            Raylib.BeginDrawing();
            Raylib.ClearBackground(new Color(255, 245, 225, 255));

            // Titel
            Raylib.DrawText("COOKIE CLICKER DELUXE", 180, 20, 45, new Color(120, 70, 20, 255));

            // Stats
            Raylib.DrawText($"Cookies: {cookies}", 20, 100, 35, new Color(90, 50, 20, 255));
            Raylib.DrawText($"CPS: {cps}", 20, 150, 35, new Color(90, 50, 20, 255));

            // Glow achter cookie
            Raylib.DrawCircleV(cookiePos, cookieRadius * 1.4f, new Color(255, 230, 150, 80));

            // Cookie body
            float r = cookieRadius * cookieScale;
            Raylib.DrawCircleV(cookiePos, r, new Color(210, 150, 80, 255));

            // Cookie shading
            Raylib.DrawCircleV(cookiePos + new Vector2(-20, -20), r * 0.9f, new Color(230, 170, 100, 120));

            // Chocolate chips
            DrawChip(cookiePos + new Vector2(-50, -40), 14);
            DrawChip(cookiePos + new Vector2(60, -30), 16);
            DrawChip(cookiePos + new Vector2(-30, 50), 15);
            DrawChip(cookiePos + new Vector2(40, 60), 12);
            DrawChip(cookiePos + new Vector2(0, -10), 18);

            // Particles
            foreach (var p in particles)
            {
                // Maak nieuwe kleur met alpha op basis van Life
                Color baseColor = p.Color;
                byte alpha = (byte)(p.Life * 255);
                Color c = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
                Raylib.DrawCircleV(p.Pos, p.Size, c);
            }

            // Upgrade knop
            Color btnColor = cookies >= upgradeCost
                ? new Color(255, 180, 80, 255)
                : new Color(180, 180, 180, 255);

            // Rounded rect + outline (let op: extra parameter lineThick)
            Raylib.DrawRectangleRounded(upgradeButton, 0.4f, 10, btnColor);
            Raylib.DrawRectangleRoundedLines(upgradeButton, 0.4f, 10, new Color(120, 70, 20, 255));


            string btnText = $"Koop upgrade (+1 CPS) – Kost: {upgradeCost}";
            int tw = Raylib.MeasureText(btnText, 22);
            Raylib.DrawText(btnText, (int)(upgradeButton.X + (upgradeButton.Width - tw) / 2), (int)upgradeButton.Y + 22, 22, new Color(90, 50, 20, 255));

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    static void DrawChip(Vector2 pos, float radius)
    {
        Raylib.DrawCircleV(pos, radius, new Color(90, 50, 20, 255));
    }
}

