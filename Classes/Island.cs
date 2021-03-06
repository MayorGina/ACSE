﻿namespace ACSE
{
    /// <summary>
    /// Island class for Doubutsu no Mori e+
    /// </summary>
    class Island
    {
        #region Island Offsets

        int IslandName = 0x00;
        int TownNameOffset = 0x06;
        int IslandId = 0x0C;
        int TownIdOffset = 0x0E;
        int WorldData = 0x10;
        int CottageData = 0x418;
        int FlagData = 0xD00;
        int IslanderData = 0xF20;
        int BuriedData = 0x15A0;
        int IslandLeftAcreData = 0x15E0;
        int IslandRightAcreData = 0x15E1;

        #endregion
        public class Cottage
        {

            public Room MainRoom;

            public Cottage(int Offset, Save SaveData)
            {
                MainRoom = new Room
                {
                    Offset = Offset,
                    Name = "Cabana",
                    Layers = new Layer[4],

                    Carpet = new Item((ushort)(0x2600 | SaveData.ReadByte(Offset + 0x8A0))),
                    Wallpaper = new Item((ushort)(0x2700 | SaveData.ReadByte(Offset + 0x8A1)))
                };

                for (int x = 0; x < 4; x++)
                {
                    int LayerOffset = Offset + 0x228 * x;
                    var Layer = new Layer
                    {
                        Offset = LayerOffset,
                        Index = x,
                        Items = new Furniture[256],
                        Parent = MainRoom
                    };

                    // Load furniture for the layer
                    for (int f = 0; f < 256; f++)
                    {
                        int FurnitureOffset = LayerOffset + f * 2;
                        Layer.Items[f] = new Furniture(SaveData.ReadUInt16(FurnitureOffset, SaveData.Is_Big_Endian));
                    }

                    MainRoom.Layers[x] = Layer;
                }
            }

            public void Write()
            {
                MainRoom.Write();
            }
        }

        private Save SaveFile;
        private int Offset;
        public string Name;
        public ushort Id;
        public string TownName;
        public ushort TownId;
        public NewPlayer Owner;
        public WorldItem[][] Items;
        public Cottage Cabana;
        public NewVillager Islander;
        public Pattern FlagPattern;
        public byte[] BuriedDataArray;
        public byte IslandLeftAcreIndex, IslandRightAcreIndex;

        public Island(int Offset, NewPlayer[] Players, Save SaveFile)
        {
            this.SaveFile = SaveFile;
            this.Offset = Offset;

            Name = new ACSE.Classes.Utilities.ACString(SaveFile.ReadByteArray(Offset + IslandName, 6), SaveFile.Save_Type).Trim();
            Id = SaveFile.ReadUInt16(Offset + IslandId, true);

            TownName = new ACSE.Classes.Utilities.ACString(SaveFile.ReadByteArray(Offset + TownNameOffset, 6), SaveFile.Save_Type).Trim();
            TownId = SaveFile.ReadUInt16(Offset + TownIdOffset, true);

            ushort Identifier = SaveFile.ReadUInt16(Offset - 0x2214, true);
            foreach (NewPlayer Player in Players)
            {
                if (Player != null && Player.Data.Identifier == Identifier)
                {
                    Owner = Player;
                }
            }

            BuriedDataArray = SaveFile.ReadByteArray(Offset + BuriedData, 0x40, false);

            Items = new WorldItem[2][];
            for (int Acre = 0; Acre < 2; Acre++)
            {
                Items[Acre] = new WorldItem[0x100];
                int i = 0;
                foreach (ushort ItemId in SaveFile.ReadUInt16Array(Offset + WorldData + Acre * 0x200, 0x100, true))
                {
                    Items[Acre][i] = new WorldItem(ItemId, i % 256);
                    SetBuried(Items[Acre][i], Acre, BuriedDataArray, SaveFile.Save_Type);
                    i++;
                }
            }

            Cabana = new Cottage(Offset + CottageData, SaveFile);
            FlagPattern = new Pattern(Offset + FlagData, 0, SaveFile);
            Islander = new NewVillager(Offset + IslanderData, 0, SaveFile);

            IslandLeftAcreIndex = SaveFile.ReadByte(Offset + IslandLeftAcreData);
            IslandRightAcreIndex = SaveFile.ReadByte(Offset + IslandRightAcreData);
        }

        private ushort IslandAcreIndexToIslandAcreId(byte Side, byte Index)
        {
            // Left side
            if (Side == 0)
            {
                switch (Index)
                {
                    case 0:
                        return 0x04A4;
                    case 1:
                        return 0x0598;
                    case 2:
                        return 0x05A0;
                    case 3:
                        return 0x05A8;
                    default:
                        return 0;
                }
            }
            else
            {
                switch (Index)
                {
                    case 0:
                        return 0x04A0;
                    case 1:
                        return 0x0594;
                    case 2:
                        return 0x059C;
                    case 3:
                        return 0x05A4;
                    default:
                        return 0;
                }
            }
        }

        private int GetBuriedDataLocation(WorldItem item, int acre, SaveType saveType)
        {
            if (item != null)
            {
                int worldPosition = 0;
                if (saveType == SaveType.Animal_Crossing || saveType == SaveType.Doubutsu_no_Mori_e_Plus || saveType == SaveType.City_Folk)
                    worldPosition = (acre * 256) + (15 - item.Location.X) + item.Location.Y * 16; //15 - item.Location.X because it's stored as a ushort in memory w/ reversed endianess
                else if (saveType == SaveType.Wild_World)
                    worldPosition = (acre * 256) + item.Index;
                return worldPosition / 8;
            }
            return -1;
        }

        private void SetBuried(WorldItem item, int acre, byte[] burriedItemData, SaveType saveType)
        {
            int burriedDataOffset = GetBuriedDataLocation(item, acre, saveType);
            if (burriedDataOffset > -1 && burriedDataOffset < burriedItemData.Length)
                item.Burried = DataConverter.ToBit(burriedItemData[burriedDataOffset], item.Location.X % 8) == 1;
        }

        public void SetBuriedInMemory(WorldItem item, int acre, byte[] burriedItemData, bool buried, SaveType saveType)
        {
            if (saveType != SaveType.New_Leaf && saveType != SaveType.Welcome_Amiibo)
            {
                int buriedLocation = GetBuriedDataLocation(item, acre, saveType);
                if (buriedLocation > -1)
                {
                    DataConverter.SetBit(ref burriedItemData[buriedLocation], item.Location.X % 8, buried);
                    item.Burried = DataConverter.ToBit(burriedItemData[buriedLocation], item.Location.X % 8) == 1;
                }
                else
                    item.Burried = false;
            }
        }

        public ushort[] GetAcreIds()
        {
            return new ushort[2] { IslandAcreIndexToIslandAcreId(0, IslandLeftAcreIndex), IslandAcreIndexToIslandAcreId(1, IslandRightAcreIndex) };
        }

        public void Write()
        {
            if (Owner != null)
            {
                SaveFile.Write(Offset + 0x00, ACSE.Classes.Utilities.ACString.GetBytes(Name, 6));
                SaveFile.Write(Offset + 0x06, ACSE.Classes.Utilities.ACString.GetBytes(TownName, 6));
                SaveFile.Write(Offset + 0x0C, Id, true);
                SaveFile.Write(Offset + 0x0E, TownId, true);
            }

            // Save World Items
            for (int Acre = 0; Acre < 2; Acre++)
            {
                for (int Item = 0; Item < 0x100; Item++)
                {
                    SaveFile.Write(Offset + WorldData + Acre * 0x200 + Item * 2, Items[Acre][Item].ItemID, true);
                }
            }

            // Save Cottage
            Cabana.Write();

            // TODO: Save Islander

            // Save Buried Data
            SaveFile.Write(Offset + BuriedData, BuriedDataArray);
        }
    }
}
