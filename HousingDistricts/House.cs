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

        public House(Rectangle housearea, List<string> owners, int id, string name, string worldid, int locked, int chatenabled)
        {
            HouseArea = housearea;
            Owners = owners;
            ID = id;
            Name = name;
            WorldID = worldid;
            Locked = locked;
            ChatEnabled = chatenabled;
        }
    }

    public class HouseTools
    {
        public static bool AddHouse(int tx, int ty, int width, int height, string housename, int locked, int chatenabled)
        {
            List<SqlValue> values = new List<SqlValue>();
            values.Add(new SqlValue("Name", "'" + housename + "'"));
            values.Add(new SqlValue("TopX", tx));
            values.Add(new SqlValue("TopY", ty));
            values.Add(new SqlValue("BottomX", width));
            values.Add(new SqlValue("BottomY", height));
            values.Add(new SqlValue("Owners", "0"));
            values.Add(new SqlValue("WorldID", "'" + Main.worldID.ToString() + "'"));
            values.Add(new SqlValue("Locked", locked));
            values.Add(new SqlValue("ChatEnabled", chatenabled));
            HousingDistricts.SQLEditor.InsertValues("HousingDistrict", values);
            HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), new List<string>(), (HousingDistricts.Houses.Count + 1), housename, Main.worldID.ToString(), locked, chatenabled));
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
                HousingDistricts.Houses.Remove(house);
                HousingDistricts.Houses.Add(new House(new Rectangle(tx, ty, width, height), houseOwners, houseID, houseName, Main.worldID.ToString(), houseLocked, houseChatEnabled));
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
