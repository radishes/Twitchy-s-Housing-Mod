﻿using System;
using System.Collections.Generic;
using Terraria;
using Hooks;
using TShockAPI;
using TShockAPI.DB;
using MySql.Data.MySqlClient;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Data;
using System.Reflection;

namespace HousingDistricts
{
    [APIVersion(1, 12)]
    public class HousingDistricts : TerrariaPlugin
    {
        public static HConfigFile HConfig { get; set; }
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public static List<House> Houses = new List<House>();
        public static List<HPlayer> HPlayers = new List<HPlayer>();

        public override string Name
        {
            get { return "HousingDistricts"; }
        }
        public override string Author
        {
            get { return "By Twitchy, Dingo, radishes, CoderCow and B4"; }
        }
        public override string Description
        {
            get { return "Housing Districts 1.6.5"; }
        }
        public override Version Version
        {
            get { return new Version(1, 6, 5, 5); }
        }

        public override void Initialize()
        {
            HTools.SetupConfig();

            GameHooks.Initialize += OnInitialize;
            GameHooks.Update += OnUpdate;
            ServerHooks.Chat += OnChat;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
            NetHooks.GetData += GetData;

            GetDataHandlers.InitGetDataHandler();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                GameHooks.Update -= OnUpdate;
                ServerHooks.Chat -= OnChat;
                NetHooks.GreetPlayer -= OnGreetPlayer;
                ServerHooks.Leave -= OnLeave;
                NetHooks.GetData -= GetData;
            }
            base.Dispose(disposing);
        }
        public HousingDistricts(Main game)
            : base(game)
        {
            HConfig = new HConfigFile();
            Order = -5; // Should be reconsidered, perhaps this should be set by config or something
        }
        public void OnInitialize()
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            #region Setup
            bool sethouse = false;
            bool edithouse = false;
            bool enterlocked = false;
            bool adminhouse = false;

            foreach (Group group in TShock.Groups.groups)
            {
                if (group.Name != "superadmin")
                {
                    if (group.HasPermission("house.use"))
                        sethouse = true;
                    if (group.HasPermission("house.edit"))
                        edithouse = true;
                    if (group.HasPermission("house.enterlocked"))
                        enterlocked = true;
                    if (group.HasPermission("house.admin"))
                        adminhouse = true;
                }
            }
            List<string> perm = new List<string>();
            if (!sethouse)
                perm.Add("house.use");
            if (!edithouse)
                perm.Add("house.edit");
            if (!enterlocked)
                perm.Add("house.enterlocked");
            if (!adminhouse)
                perm.Add("house.admin");
            TShock.Groups.AddPermissions("trustedadmin", perm);

            var table = new SqlTable("HousingDistrict",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.VarChar, 255) { Unique = true },
                new SqlColumn("TopX", MySqlDbType.Int32),
                new SqlColumn("TopY", MySqlDbType.Int32),
                new SqlColumn("BottomX", MySqlDbType.Int32),
                new SqlColumn("BottomY", MySqlDbType.Int32),
                new SqlColumn("Owners", MySqlDbType.Text),
                new SqlColumn("WorldID", MySqlDbType.Text),
                new SqlColumn("Locked", MySqlDbType.Int32),
                new SqlColumn("ChatEnabled", MySqlDbType.Int32),
                new SqlColumn("Visitors", MySqlDbType.Text)
            );
            SQLWriter.EnsureExists(table);
        	var reader = TShock.DB.QueryReader("Select * from HousingDistrict");
			while( reader.Read() )
			{
				int id = reader.Get<int>("ID");
				string[] list = reader.Get<string>("Owners").Split(',');
				List<string> owners = new List<string>();
				foreach( string i in list)
					owners.Add( i );
				int locked = reader.Get<int>("Locked");
                int chatenabled;
                if (reader.Get<int>("ChatEnabled") == 1) { chatenabled = 1; }
                else { chatenabled = 0; }
                List<string> visitors = new List<string>();
                foreach (string i in list)
                    visitors.Add(i);
				Houses.Add( new House( new Rectangle( reader.Get<int>("TopX"),reader.Get<int>("TopY"),reader.Get<int>("BottomX"),reader.Get<int>("BottomY") ), 
					owners, id, reader.Get<string>("Name"), reader.Get<string>("WorldID"), locked, chatenabled, visitors));
			}
            #endregion

            #region Commands
            Commands.ChatCommands.Add(new Command("house.use", HCommands.House, "house"));
            Commands.ChatCommands.Add(new Command("house.root", HCommands.Convert, "converthouse"));
            Commands.ChatCommands.Add(new Command("house.lock", HCommands.ChangeLock, "changelock"));
            Commands.ChatCommands.Add(new Command(HCommands.TellAll, "all"));
            Commands.ChatCommands.Add(new Command("house.root", HCommands.HouseReload, "housereload"));
            #endregion
        }

        private DateTime PrevUpdateTime;
        public void OnUpdate()
        {
            if (DateTime.Now < PrevUpdateTime + TimeSpan.FromMilliseconds(500))
                return;
            else
              PrevUpdateTime = DateTime.Now;

            if (HConfig.NotifyOnEntry || HConfig.NotifyOnExit)
            {
                lock (HPlayers)
                {
                    foreach (HPlayer player in HPlayers)
                    {
                        List<string> NewCurHouses = new List<string>(player.CurHouses);
                        int HousesNotIn = 0;
                        try
                        {
                            foreach (House house in HousingDistricts.Houses)
                            {
                                try
                                {
                                    if (HConfig.NotifyOnEntry)
                                    {
                                        if (house.HouseArea.Intersects(new Rectangle(player.TSPlayer.TileX, player.TSPlayer.TileY, 1, 1)) && house.WorldID == Main.worldID.ToString())
                                        {
                                            if (house.Locked == 1 && !player.TSPlayer.Group.HasPermission("house.enterlocked"))
                                            {
                                                if (!HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), house) || !HTools.CanVisitHouse(player.TSPlayer.UserID.ToString(), house))
                                                {
                                                    player.TSPlayer.Teleport((int)player.LastTilePos.X, (int)player.LastTilePos.Y + 3);
                                                    player.TSPlayer.SendMessage("House: '" + house.Name + "' Is locked", Color.LightSeaGreen);
                                                }
                                            }
                                            else
                                            {
                                                if (!player.CurHouses.Contains(house.Name))
                                                {
                                                    NewCurHouses.Add(house.Name);
                                                    if (HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), house.Name))
                                                        player.TSPlayer.SendMessage(HConfig.NotifyOnOwnHouseEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
                                                    else
                                                    {
                                                        player.TSPlayer.SendMessage(HConfig.NotifyOnEntryString.Replace("$HOUSE_NAME", house.Name), Color.LightSeaGreen);
                                                        HTools.BroadcastToHouseOwners(house.Name, HConfig.NotifyOnOtherEntryString.Replace("$PLAYER_NAME", player.TSPlayer.Name).Replace("$HOUSE_NAME", house.Name));
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            NewCurHouses.Remove(house.Name);
                                            HousesNotIn++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.ToString());
                                    continue;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                            continue;
                        }

                        if (HConfig.NotifyOnExit)
                        {
                          //  if (HousesNotIn == HousingDistricts.Houses.Count)
                            {
                                foreach (string cHouse in player.CurHouses)
                                {
                                    if (!NewCurHouses.Contains(cHouse))
                                    {
                                        if (HTools.OwnsHouse(player.TSPlayer.UserID.ToString(), cHouse))
                                            player.TSPlayer.SendMessage(HConfig.NotifyOnOwnHouseExitString.Replace("$HOUSE_NAME", cHouse), Color.LightSeaGreen);
                                        else
                                        {
                                            player.TSPlayer.SendMessage(HConfig.NotifyOnExitString.Replace("$HOUSE_NAME", cHouse), Color.LightSeaGreen);
                                            HTools.BroadcastToHouseOwners(cHouse, HConfig.NotifyOnOtherExitString.Replace("$PLAYER_NAME", player.TSPlayer.Name).Replace("$HOUSE_NAME", cHouse));
                                        }
                                        //NewCurHouses.Remove(cHouse);
                                    }
                                }
                            }
                            
                        }
                        player.CurHouses = NewCurHouses;
                        player.LastTilePos = new Vector2(player.TSPlayer.TileX, player.TSPlayer.TileY);
                    }
                }
            }
        }
        public void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
        {
            if (!e.Handled)
            {
                if (HConfig.HouseChatEnabled)
                {
                    if (text[0] == '/')
                        return;

                    var tsplr = TShock.Players[msg.whoAmI];
                    foreach (House house in HousingDistricts.Houses)
                    {
                        if (house.WorldID == Main.worldID.ToString() && house.ChatEnabled == 1 && house.HouseArea.Intersects(new Rectangle(tsplr.TileX, tsplr.TileY, 1, 1)))
                        {
                            HTools.BroadcastToHouse(house, text, tsplr.Name);
                            e.Handled = true;
                        }
                    }
                }
            }
        }
        public void OnGreetPlayer(int who, HandledEventArgs e)
        {
            lock (HPlayers)
                HPlayers.Add(new HPlayer(who, new Vector2(TShock.Players[who].TileX, TShock.Players[who].TileY)));
        }
        public void OnLeave(int ply)
        {
            lock (HPlayers)
            {
                for (int i = 0; i < HPlayers.Count; i++)
                {
                    if (HPlayers[i].Index == ply)
                    {
                        HPlayers.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        private void GetData(GetDataEventArgs e)
        {
            PacketTypes type = e.MsgID;
            var player = TShock.Players[e.Msg.whoAmI];
            if (player == null)
            {
                e.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                e.Handled = true;
                return;
            }

            using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        e.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
    }
}