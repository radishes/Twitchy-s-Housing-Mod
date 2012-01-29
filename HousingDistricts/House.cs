using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI.DB;
using TShockAPI;
using Terraria;

namespace HousingDistricts
{
    public class House
    {
        public Rectangle HouseArea { get; set; }
        public List<string> Owners { get; set; }
        public int ID { get; set; }
        public string Name { get; set; }
        public string WorldID { get; set; }
        public int Locked { get; set; }
        public int ChatEnabled { get; set; }
        public List<string> Visitors { get; set; }

        public House(Rectangle housearea, List<string> owners, int id, string name, string worldid, int locked, int chatenabled, List<string> visitors)
        {
            HouseArea = housearea;
            Owners = owners;
            ID = id;
            Name = name;
            WorldID = worldid;
            Locked = locked;
            ChatEnabled = chatenabled;
            Visitors = visitors;
        }
    }

    public class HouseTools
    {
        public static bool AddHouse(int tx, int ty, int width, int height, string housename, string owner, int locked, int chatenabled)
        {
            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Name", "'" + housename + "'"));
            values.Add(new SqlValue("TopX", tx));
            values.Add(new SqlValue("TopY", ty));
            values.Add(new SqlValue("BottomX", width));
            values.Add(new SqlValue("BottomY", height));
            values.Add(new SqlValue("Owners", owner));
            values.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
            values.Add(new SqlValue("Locked", locked));
            values.Add(new SqlValue("ChatEnabled", chatenabled));
            values.Add(new SqlValue("Visitors", "0"));
            HousingDistricts.SQLEditor.InsertValues("HousingDistrict", values);
            HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), new List<string>(), (HousingDistricts.Houses.Count + 1), housename, Main.worldID.ToString(), locked, chatenabled, new List<string>()));
            return true;
        }

        public static bool AddNewUser(string houseName, string id)
        {
            var house = GetHouseByName(houseName);
            StringBuilder sb = new StringBuilder();
            int count = 0;
            house.Owners.Add(id);
            foreach(string owner in house.Owners)
            {
                count++;
                sb.Append(owner);
                if(count != house.Owners.Count)
                    sb.Append(",");
            }
            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Owners", "'" + sb.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + houseName + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
        }
        public static bool AddNewVisitor(House house, string id)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            house.Visitors.Add(id);
            foreach (string visitor in house.Visitors)
            {
                count++;
                sb.Append(visitor);
                if (count != house.Visitors.Count)
                    sb.Append(",");
            }
            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Visitors", "'" + sb.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + house.Name + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
        }
        public static bool ToggleChat(House house, int onOrOff)
        {
            house.ChatEnabled = onOrOff;

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("ChatEnabled", "'" + house.ChatEnabled.ToString() + "'"));

            List<SqlValue> wheres = new List<SqlValue>();
            wheres.Add(new SqlValue("Name", "'" + house.Name + "'"));

            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
            return true;
        }
        public static bool ChangeLock(string houseName)
        {
            var house = GetHouseByName(houseName);
            bool locked = false;

            if (house.Locked == 0)
                locked = true;
            else
                locked = false;

            house.Locked = locked ? 1 : 0;

            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Locked", locked ? 1 : 0));
            HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, new List<SqlValue>());
            return locked;
        }
        public static bool RedefineHouse(int tx, int ty, int width, int height, string housename)
        {
            try
            {
                var house = GetHouseByName(housename);
                var houseName = house.Name;
                var houseOwners = house.Owners;
                var houseWorldID = house.WorldID;
                var houseID = house.ID;
                var houseLocked = house.Locked;
                var houseChatEnabled = house.ChatEnabled;
                var houseVisitors = house.Visitors;
                HousingDistricts.Houses.Remove(house);
                HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), houseOwners, houseID, houseName, Main.worldID.ToString(), houseLocked, houseChatEnabled, houseVisitors));
                List<SqlValue> wheres = new List<SqlValue>();
                wheres.Add(new SqlValue("Name", "'" + houseName + "'"));
                wheres.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
                //So, UpdateValues only allows 1 value at a time. I don't know how else to do what follows.
                List<SqlValue> values = new List<SqlValue>();
                values.Add(new SqlValue("TopX", tx));
                HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
                values.Clear();
                values.Add(new SqlValue("TopY", ty));
                HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
                values.Clear();
                values.Add(new SqlValue("BottomX", width));
                HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
                values.Clear();
                values.Add(new SqlValue("BottomY", height));
                HousingDistricts.SQLEditor.UpdateValues("HousingDistrict", values, wheres);
                return true;
            }
            catch
            {
                return false;
            }
        }


        public static House GetHouseByName(string name)
        {
            foreach (House house in HousingDistricts.Houses)
            {
                if (house.Name == name)
                    return house;
            }
            return null;
        }
    }
}
