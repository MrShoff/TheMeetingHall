using ACE.Entity;
using ACE.Entity.Enum;
using ACE.Entity.Enum.Properties;
using ACE.Server.WorldObjects;
using log4net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ACE.Server.ShoffsMods
{
    public static class WispWriter
    {
        private static readonly uint LetterHeight = 7;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        private static uint[] wisps = new uint[] { 35059, 35090, 35089 }; // Red, Blue, Green Wisps        

        public enum Color
        {
            Red,
            Green,
            Blue
        }

        public static void WriteWispString(string s, Position bottomMidPosition, Color color, double displayTimeSeconds)
        {
            // validate
            char[] str = Regex.Replace(s.ToUpperInvariant(), "[^A-Z0-9]", "").ToCharArray();
            if (str.Length == 0) return;

            // get string width
            float letterSpacing = 1.0f;
            Dictionary<char, float> charWidths = new Dictionary<char, float>();
            foreach(char c in str)
            {
                charWidths.Add(c, GetLetterWidth(c));
            }
            float totalWidth = letterSpacing * (str.Length - 1);
            foreach (float w in charWidths.Values) totalWidth += w;
            float radius = totalWidth / 2.0f;

            // print them
            for(int i = 0; i < str.Length; i++)
            {
                float widthOfPreviousLetters = 0;
                var previousLetterIndex = 0;
                while(previousLetterIndex < i)
                {
                    charWidths.TryGetValue(str[previousLetterIndex], out var w);
                    widthOfPreviousLetters += w;
                    previousLetterIndex++;
                }
                Position blp = new Position(bottomMidPosition);
                var xDelta = widthOfPreviousLetters + (letterSpacing * i) - radius;
                log.Info($"xDelta:{xDelta}");
                blp.PositionX = blp.PositionX + xDelta;
                Write(str[i], blp, color, displayTimeSeconds);
            }
        }

        private static void Write(char c, Position bottomLeftPosition, Color color, double displayTimeSeconds)
        {
            List<bool[]> letterRows = GetLetterRows(c);

            if (letterRows.Count != LetterHeight) return;

            for(int y = 0; y < 7; y++)
            {
                for(int x = 0; x < letterRows[y].Length; x++)
                {
                    if (letterRows[y][x])
                    {
                        List<WorldObject> wos = GetWispObjectsForPoint(color);
                        foreach (var wo in wos)
                        {
                            wo.Location = bottomLeftPosition;
                            wo.TimeToRot = displayTimeSeconds;
                            wo.EnterWorld();
                            wo.Location.PositionZ += y * (wo.PhysicsObj.GetHeight() / 4);
                            wo.Location.PositionX += x * (wo.PhysicsObj.GetRadius() / 2);
                            wo.Biota.WeenieType = WeenieType.Gem;
                            wo.RadarColor = null;                            
                            var mob = wo as Creature;
                            mob.AuralAwarenessRange = 0;
                        }
                    }
                }
            }
        }        

        private static float GetLetterWidth(char c)
        {
            var wos = GetWispObjectsForPoint();
            var letterRows = GetLetterRows(c);
            if (wos.Count == 0 || letterRows.Count == 0) return 0;
            else
            {
                wos[0].Location = new Position(0x0E02001F, 75f, 148f, -0.094750f, 0.0f, 0.0f, 0.0f, 0.0f); // spawn it outside caul rim to get its PhysicsObj
                wos[0].TimeToRot = 2;
                wos[0].EnterWorld();
                return letterRows[0].Length * wos[0].PhysicsObj.GetRadius() * 2;
            }            
        }

        private static List<bool[]> GetLetterRows(char c)
        {
            string s = c.ToString();
            List<bool[]> letterRows = new List<bool[]>();
            switch(s)
            {
                case "0":
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, true, true });
                    letterRows.Add(new bool[] { true, false, true, false, true });
                    letterRows.Add(new bool[] { true, true, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    break;
                case "1":
                    letterRows.Add(new bool[] { false, true, false });
                    letterRows.Add(new bool[] { true, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] { true, true, true });
                    break;
                case "2":
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { false, false, false, false, true });
                    letterRows.Add(new bool[] { false, false, false, true, true });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, true, false, false, false });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "3":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { false, false, false, true, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, false, true, false });
                    letterRows.Add(new bool[] { false, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    break;
                case "4":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "5":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "6":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "7":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "8":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "9":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "A":
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    letterRows.Add(new bool[] { true,false, false, false, true });
                    letterRows.Add(new bool[] { true,false, false, false, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    break;
                case "F":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, true, true, true, false });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    break;
                case "I":
                    letterRows.Add(new bool[] {true, true, true });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {false, true, false });
                    letterRows.Add(new bool[] {true, true, true });
                    break;
                case "G":
                    letterRows.Add(new bool[] { false, true, true, true, false });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, false, false, false, false });
                    letterRows.Add(new bool[] { true, false, false, true, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
                case "H":
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    letterRows.Add(new bool[] { true, false, false, false, true });
                    break;
                case "T":
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    letterRows.Add(new bool[] { false, false, true, false, false });
                    break;
                default:
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    letterRows.Add(new bool[] { true, true, true, true, true });
                    break;
            }
            return letterRows;
        }

        private static List<WorldObject> GetWispObjectsForPoint(Color color = Color.Blue)
        {
            List<WorldObject> wos = new List<WorldObject>();
            switch(color)
            {
                case Color.Red:
                    wos.Add(Factories.WorldObjectFactory.CreateNewWorldObject(35059));
                    break;
                case Color.Green:
                    wos.Add(Factories.WorldObjectFactory.CreateNewWorldObject(35089));
                    break;
                case Color.Blue:
                    wos.Add(Factories.WorldObjectFactory.CreateNewWorldObject(35090));
                    break;
            }
            foreach(var wo in wos)
            {
                wo.Ethereal = true;
                wo.IgnoreCollisions = true;
                wo.Stuck = true;
                wo.SetProperty(PropertyInt.Mass, 0);
                wo.ObjScale = 0.9f;
                wo.Attackable = false;
                wo.Name = string.Empty;
                wo.SetProperty(PropertyDataId.SoundTable, 0);

                wo.GravityStatus = false;
            }
            return wos;
        }
    }
}
