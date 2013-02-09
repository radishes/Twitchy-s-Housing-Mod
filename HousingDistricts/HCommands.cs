using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace HousingDistricts
{
    public class HCommands
    {
        public static void House(CommandArgs args)
        {
            string cmd = "help";
            if (args.Parameters.Count > 0)
            {
                cmd = args.Parameters[0].ToLower();
            }
            var player = HTools.GetPlayerByID(args.Player.Index);
            switch (cmd)
            {
                case "name":
                    {
                        {
                            args.Player.SendMessage("Hit a block to get the name of the house", Color.Yellow);
                            player.AwaitingHouseName = true;
                        }
                        break;
                    }
                case "set":
                    {
                        int choice = 0;
                        if (args.Parameters.Count == 2 &&
                            int.TryParse(args.Parameters[1], out choice) &&
                            choice >= 1 && choice <= 2)
                        {
                            if (choice == 1)
                                args.Player.SendMessage("Now hit the TOP-LEFT block of the area to be protected.", Color.Yellow);
                            if (choice == 2)
                                args.Player.SendMessage("Now hit the BOTTOM-RIGHT block of the area to be protected.", Color.Yellow);
                            args.Player.AwaitingTempPoint = choice;
                        }
                        else
                        {
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house set [1/2]", Color.Red);
                        }
                        break;
                    }
                case "add":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            List<int> userOwnedHouses = new List<int>();
                            foreach (House house in HousingDistricts.Houses)
                            {
                                foreach (string owner in house.Owners)
                                {
                                    if (args.Player.UserID.ToString() == owner)
                                    {
                                        userOwnedHouses.Add(house.ID);
                                        break;
                                    }

                                }
                            }
                            if (userOwnedHouses.Count < HousingDistricts.HConfig.MaxHousesByUsername || args.Player.Group.HasPermission("adminhouse") || args.Player.Group.Name == "superadmin")
                            {
                                if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                                {
                                    string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));

                                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
                                    if ((width * height) <= HousingDistricts.HConfig.MaxHouseSize && width >= HousingDistricts.HConfig.MinHouseWidth && height >= HousingDistricts.HConfig.MinHouseHeight)
                                    {
                                        Rectangle newHouseR = new Rectangle(x, y, width, height);
                                        foreach (House house in HousingDistricts.Houses)
                                        {
                                            if ((newHouseR.Intersects(house.HouseArea) && !userOwnedHouses.Contains(house.ID)) && !HousingDistricts.HConfig.OverlapHouses)
                                            { // user is allowed to intersect their own house
                                                args.Player.SendMessage("Your selected area overlaps another players' house, which is not allowed.", Color.Red);
                                                return;
                                            }
                                        }
                                        if (HouseTools.AddHouse(x, y, width, height, houseName, args.Player.UserID.ToString(), 0, 0))
                                        {
                                            args.Player.TempPoints[0] = Point.Zero;
                                            args.Player.TempPoints[1] = Point.Zero;
                                            args.Player.SendMessage("You have created new house " + houseName, Color.Yellow);
                                            HouseTools.AddNewUser(houseName, args.Player.UserID.ToString());
                                        }
                                        else
                                        {
                                            args.Player.SendMessage("House " + houseName + " already exists", Color.Red);
                                        }
                                    }
                                    else
                                    {
                                        if ((width * height) >= HousingDistricts.HConfig.MaxHouseSize)
                                        {
                                            args.Player.SendMessage("Your house exceeds the maximum size of " + HousingDistricts.HConfig.MaxHouseSize.ToString() + "blocks.", Color.Yellow);
                                            args.Player.SendMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.", Color.Yellow);
                                            args.Player.TempPoints[0] = Point.Zero;
                                            args.Player.TempPoints[1] = Point.Zero;
                                        }
                                        else if (width < HousingDistricts.HConfig.MinHouseWidth)
                                        {
                                            args.Player.SendMessage("Your house width is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseWidth.ToString() + "blocks.", Color.Yellow);
                                            args.Player.SendMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.", Color.Yellow);
                                            args.Player.TempPoints[0] = Point.Zero;
                                            args.Player.TempPoints[1] = Point.Zero;
                                        }
                                        else
                                        {
                                            args.Player.SendMessage("Your house height is smaller than server minimum of " + HousingDistricts.HConfig.MinHouseHeight.ToString() + "blocks.", Color.Yellow);
                                            args.Player.SendMessage("Width: " + width.ToString() + ", Height: " + height.ToString() + ". Points have been cleared.", Color.Yellow);
                                            args.Player.TempPoints[0] = Point.Zero;
                                            args.Player.TempPoints[1] = Point.Zero;
                                        }
                                    }
                                }
                                else
                                {
                                    args.Player.SendMessage("Points not set up yet", Color.Red);
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("House add failed: You have too many houses!", Color.Red);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house add [name]", Color.Red);
                        break;
                    }
                case "allow":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(args.Parameters[2]);
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name) || args.Player.Group.HasPermission("adminhouse") || args.Player.Group.Name == "superadmin")
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.AddNewUser(houseName, playerID.ID.ToString()))
                                    {
                                        args.Player.SendMessage("Added user " + playerName + " to " + houseName, Color.Yellow);
                                    }
                                    else
                                        args.Player.SendMessage("House " + houseName + " not found", Color.Red);
                                }
                                else
                                {
                                    args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("You do not own house: " + houseName);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house allow [name] [house]", Color.Red);
                        break;
                    }
                case "delete":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            string houseName = args.Parameters[1];
                            var house = HouseTools.GetHouseByName(houseName);
                            if (HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name) || args.Player.Group.HasPermission("adminhouse") || args.Player.Group.Name == "superadmin")
                            {
                                List<SqlValue> where = new List<SqlValue>();
                                where.Add(new SqlValue("Name", "'" + houseName.Replace("'", "''") + "'"));
                                HousingDistricts.SQLWriter.DeleteRow("HousingDistrict", where);
                                HousingDistricts.Houses.Remove(house);
                                args.Player.SendMessage("House: " + houseName + " deleted", Color.Yellow);
                                break;
                            }
                            else
                            {
                                args.Player.SendMessage("You do not own house: " + houseName, Color.Yellow);
                                break;
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house delete [house]", Color.Red);
                        break;
                    }
                case "clear":
                    {
                        args.Player.TempPoints[0] = Point.Zero;
                        args.Player.TempPoints[1] = Point.Zero;
                        args.Player.AwaitingTempPoint = 0;
                        args.Player.SendMessage("Cleared temp area", Color.Yellow);
                        break;
                    }
                case "list":
                    {
                        //How many regions per page
                        const int pagelimit = 15;
                        //How many regions per line
                        const int perline = 5;
                        //Pages start at 0 but are displayed and parsed at 1
                        int page = 0;


                        if (args.Parameters.Count > 1)
                        {
                            if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                            {
                                args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
                                return;
                            }
                            page--; //Substract 1 as pages are parsed starting at 1 and not 0
                        }

                        List<House> houses = new List<House>();

                        foreach (House house in HousingDistricts.Houses)
                        {
                            if (house.WorldID == Main.worldID.ToString())
                            {
                                houses.Add(house);
                            }
                        }

                        // Are there even any houses to display?
                        if (houses.Count == 0)
                        {
                            args.Player.SendMessage("There are currently no houses defined.", Color.Red);
                            return;
                        }

                        //Check if they are trying to access a page that doesn't exist.
                        int pagecount = houses.Count / pagelimit;
                        if (page > pagecount)
                        {
                            args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
                            return;
                        }

                        //Display the current page and the number of pages.
                        args.Player.SendMessage(string.Format("Current Houses ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

                        //Add up to pagelimit names to a list
                        var nameslist = new List<string>();
                        for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < houses.Count; i++)
                        {
                            nameslist.Add(houses[i].Name);
                        }

                        //convert the list to an array for joining
                        var names = nameslist.ToArray();
                        for (int i = 0; i < names.Length; i += perline)
                        {
                            args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                        }

                        if (page < pagecount)
                        {
                            args.Player.SendMessage(string.Format("Type /house list {0} for more houses.", (page + 2)), Color.Yellow);
                        }
                        break;
                    }
                case "redefine":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            if (!args.Player.TempPoints.Any(p => p == Point.Zero))
                            {
                                string houseName = String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1));
                                if (HTools.OwnsHouse(args.Player.UserID.ToString(), houseName) || args.Player.Group.HasPermission("adminhouse") || args.Player.Group.Name.ToLower() == "superadmin")
                                {
                                    var x = Math.Min(args.Player.TempPoints[0].X, args.Player.TempPoints[1].X);
                                    var y = Math.Min(args.Player.TempPoints[0].Y, args.Player.TempPoints[1].Y);
                                    var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
                                    var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);

                                    if (HouseTools.RedefineHouse(x, y, width, height, houseName))
                                    {
                                        args.Player.TempPoints[0] = Point.Zero;
                                        args.Player.TempPoints[1] = Point.Zero;
                                        args.Player.SendMessage("Redefined house " + houseName, Color.Yellow);
                                    }
                                    else
                                    {
                                        args.Player.SendMessage("Error redefining house " + houseName, Color.Red);
                                    }
                                }
                                else
                                {
                                    args.Player.SendMessage("You do not own house: " + houseName, Color.Yellow);
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("Points not set up yet", Color.Red);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house redefine [name]", Color.Red);
                        break;
                    }
                case "debug":
                    { //this is just here to pass me whatever info i need for a test
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            args.Player.SendMessage("Chat enabled: " + house.ChatEnabled.ToString());
                            args.Player.SendMessage("Owners: " + house.Owners.ToString());
                        }
                        break;
                    }
                case "chat":
                    {
                        if (args.Parameters.Count > 1)
                        {
                            var house = HouseTools.GetHouseByName(args.Parameters[1]);
                            if (HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name))
                            {
                                if (args.Parameters.Count > 2)
                                {
                                    if (args.Parameters[2].ToLower() == "on")
                                    {
                                        HouseTools.ToggleChat(house, 1);
                                        args.Player.SendMessage(house.Name + " chat is now enabled.");
                                    }
                                    else if (args.Parameters[2].ToLower() == "off")
                                    {
                                        HouseTools.ToggleChat(house, 0);
                                        args.Player.SendMessage(house.Name + " chat is now disabled.");
                                    }
                                    else
                                    {
                                        args.Player.SendMessage("Invalid syntax! Use /house chat <housename> (on|off)");
                                    }
                                }
                                else
                                {
                                    HouseTools.ToggleChat(house, (house.ChatEnabled == 0 ? 1 : 0));
                                    args.Player.SendMessage(house.Name + " chat is now " + (house.ChatEnabled == 0 ? "disabled." : "enabled."));
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("You do not own " + house.Name + ".");
                            }
                        }
                        else
                        {
                            args.Player.SendMessage("Invalid syntax! Use /house chat <housename> (on|off)");
                        }
                        break;
                    }
                case "addvisitor":
                    {
                        if (args.Parameters.Count > 2)
                        {
                            string playerName = args.Parameters[1];
                            User playerID;
                            var house = HouseTools.GetHouseByName(args.Parameters[2]);
                            string houseName = house.Name;
                            if (HTools.OwnsHouse(args.Player.UserID.ToString(), house.Name) || args.Player.Group.HasPermission("adminhouse") || args.Player.Group.Name == "superadmin")
                            {
                                if ((playerID = TShock.Users.GetUserByName(playerName)) != null)
                                {
                                    if (HouseTools.AddNewVisitor(house, playerID.ID.ToString()))
                                    {
                                        args.Player.SendMessage("Added user " + playerName + " to " + houseName + "as a visitor.", Color.Yellow);
                                    }
                                    else
                                        args.Player.SendMessage("House " + houseName + " not found", Color.Red);
                                }
                                else
                                {
                                    args.Player.SendMessage("Player " + playerName + " not found", Color.Red);
                                }
                            }
                            else
                            {
                                args.Player.SendMessage("You do not own house: " + houseName);
                            }
                        }
                        else
                            args.Player.SendMessage("Invalid syntax! Proper syntax: /house addvisitor [name] [house]", Color.Red);
                        break;
                    }
                default:
                    {
                        args.Player.SendMessage("To create a house, use these commands:", Color.Red);
                        args.Player.SendMessage("/house set 1", Color.Red);
                        args.Player.SendMessage("/house set 2", Color.Red);
                        args.Player.SendMessage("/house add HouseName", Color.Red);
                        args.Player.SendMessage("Other /house commands: list, allow, name, delete, clear", Color.Red);
                        break;
                    }
            }
        }

        public static void TellAll(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string text = "";
                foreach (string word in args.Parameters)
                {
                    text = text + " " + word;
                }

            	Group super = new SuperAdminGroup();
                if (args.Player.Group.HasPermission("adminchat") && !text.StartsWith("/"))
                {
					TShock.Utils.Broadcast(super.Prefix + "<" + args.Player.Name + ">" + text,
                                    args.Player.Group.R, args.Player.Group.G,
                                    args.Player.Group.B);
                    return;
                }

                TShock.Utils.Broadcast("{2}<{0}>{1}".SFormat(args.Player.Name, text, true ? "[{0}] ".SFormat(args.Player.Group.Name) : ""),
                                args.Player.Group.R, args.Player.Group.G,
                                args.Player.Group.B);
            }
        }

        public static void Convert(CommandArgs args)
        {
            List<SqlValue> value = new List<SqlValue>();
            value.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", value, new List<SqlValue>());

            foreach (House house in HousingDistricts.Houses)
            {
                house.WorldID = Main.worldID.ToString();
            }
        }

        public static void ChangeLock(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                string houseName = "";

                for (int i = 0; i < args.Parameters.Count; i++)
                {
                    if (houseName == "")
                    {
                        houseName = args.Parameters[i];
                    }
                    else
                    {
                        houseName = houseName + " " + args.Parameters[i];
                    }
                }

                if (HTools.OwnsHouse(args.Player.UserID.ToString(), houseName))
                {
                    bool locked = HouseTools.ChangeLock(houseName);
                    args.Player.SendMessage("House: " + houseName + (locked ? " locked" : " unlocked"), Color.Yellow);
                }
                else
                    args.Player.SendMessage("You do not own House: " + houseName, Color.Yellow);
            }
            else
                args.Player.SendMessage("Invalid syntax! Proper syntax: /changelock [house]", Color.Red);
        }
        public static void HouseReload(CommandArgs args)
        {
            HTools.SetupConfig();
            Log.Info("House Reload Initiated");
            args.Player.SendMessage("House Reload Initiated");
        }
    }
}
