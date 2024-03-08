using DeathRoll.Windows;

namespace DeathRoll.Bahamood;

public static class Settings
{
    public const int Width = 1600;
    public const int Height = 900;
    public const int HalfWidth = Width / 2;
    public const int HalfHeight = Height / 2;

    public const float PlayerSpeed = 0.004f;
    public const float PlayerRotSpeed = 0.002f;
    public const int PlayerSize = 60;
    public const int PlayerHealth = 100;

    public const float MouseSensitivity = 0.0003f;
    public const int MouseMaxRel = 40;
    public const int MouseBorderLeft = 100;
    public const int MouseBorderRight = Width - MouseBorderLeft;

    public static readonly uint FloorColor = Helper.Vec4ToUintColor(new Vector4(new Vector3(30 / 255.0f), 1));

    public const float FieldOfView = (float) Math.PI / 3.0f;
    public const float HalfFoV = FieldOfView / 2;
    public const int NumRays = Width / 2;
    public const int HalfNumRays = NumRays / 2;
    public const float DeltaAngle = FieldOfView / NumRays;
    public const int MaxDepth = 20;

    public static readonly float ScreenDist = HalfWidth / (float) Math.Tan(HalfFoV);
    public const int Scale = Width / NumRays;

    public const string CreditsTextTemplate =
        """
        Bahamood
        A Doom-Like Game for Dalamud
        Version {0}



        Created by:

        Infi



        Special thanks:
        
        Pohky



        Texturs by:

        FFXIV
        
        Revolver Sprite:
        
        Dr_Cosmobyte (zdoom.org)
        
        Shotgun Sprite:
        
        Copyright (c) ZeniMax Media Inc



        Music:

        Main Menu / Credits
        Title: Torn Flesh
        Music by Karl Casey @ White Bat Audio
        Credit: https://karlcasey.bandcamp.com/

        Limsa Stage
        First Time On Earth - Bulletproof @ Grigory Motorin
        Credit: https://soundcloud.com/grigory-motorin/bulletproof
        Licensed under the Creative Commons Attribution 3.0 Unported



        Font:

        AmazDooM @ Amazingmax
        Credit: https://www.dafont.com/amazdoom.font
        Licensed under the Creative Commons Attribution-Noncommercial 3.0 Unported
        """;
}