﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using TShockAPI;
using Terraria;

namespace HousingDistricts
{
    class HTools
    {
        internal static string HConfigPath { get { return Path.Combine(TShock.SavePath, "hconfig.json"); } }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(HConfigPath))
                {
                    HousingDistricts.HConfig = HConfigFile.Read(HConfigPath);
                    // Add all the missing config properties in the json file
                }
                HousingDistricts.HConfig.Write(HConfigPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("Config Exception");
                Log.Error(ex.ToString());
            }
        }

        public static void BroadcastToHouse(House house, string text, string playername)
        {
            foreach (HPlayer player in HousingDistricts.HPlayers)
            {
                if (house.HouseArea.Intersects(new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1)))
                {
                    player.TSPlayer.SendMessage("<House> <" + playername + ">: " + text, Color.LightSkyBlue);
                }
            }
        }

        public static string InAreaHouseName(int x, int y)
        {
            foreach (House house in HousingDistricts.Houses)
            {
                if (house.WorldID == Main.worldID.ToString() &&
                    x >= house.HouseArea.Left && x < house.HouseArea.Right &&
                    y >= house.HouseArea.Top && y < house.HouseArea.Bottom)
                {
                    return house.Name;
                }
            }
            return null;
        }

        public static void BroadcastToHouseOwners(string housename, string text)
        {
            BroadcastToHouseOwners(HouseTools.GetHouseByName(housename), text);
        }

        public static void BroadcastToHouseOwners(House house, string text)
        {
            foreach (string ID in house.Owners)
            {
                foreach (TSPlayer player in TShock.Players)
                {
                    if (player != null)
                    {
                        if (player.UserID.ToString() == ID)
                        {
                            player.SendMessage(text, Color.LightSeaGreen);
                        }
                    }
                }
            }
        }



        public static bool OwnsHouse(string UserID, string housename)
        {
            return OwnsHouse(UserID, HouseTools.GetHouseByName(housename));
        }

        public static bool OwnsHouse(string UserID, House house)
        {
            try
            {
                foreach (string owner in house.Owners)
                {
                    if (owner == UserID)
                        return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        public static bool CanVisitHouse(string UserID, House house)
        {
            foreach (string visitor in house.Visitors)
            {
                if (visitor == UserID)
                    return true;
            }
            return false;
        }

        public static HPlayer GetPlayerByID(int id)
        {
            var retplayer = new HPlayer();

            foreach (HPlayer player in HousingDistricts.HPlayers)
            {
                if (player.Index == id)
                    return player;
            }

            return retplayer;
        }
    }
}
