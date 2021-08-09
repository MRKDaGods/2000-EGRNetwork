using System;
using System.Collections.Generic;
using System.IO;

namespace MRK.WTE {
    public class SerializationStream : Dictionary<string, string> {
    }

    public interface ISerializedObject {
        public string ViewIdentifier { get; }
        public uint InspectorEditor { get; }
        public Type[] Enumeration { get; }

        public void BinaryWrite(BinaryWriter writer);
        public void BinaryRead(BinaryReader reader);
        public void InternalWrite(SerializationStream stream);
        public void InternalRead(SerializationStream stream);
    }

    public class Context : ISerializedObject {
        public uint Version { get; set; }
        public List<Place> Places { get; set; }
        public string Name { get; set; }

        public string ViewIdentifier => $"DATABASE_{Name}";
        public uint InspectorEditor => 0x00000000; // last byte=0x00
        public Type[] Enumeration => null;

        public static Context Current { get; private set; }

        public Context() {
            Places = new List<Place>();
        }

        public void BinaryRead(BinaryReader reader) {
            Name = reader.ReadString();

            uint vSanity = reader.ReadUInt32();
            if (vSanity == 0x8721FFBA) {
                Version = reader.ReadUInt32();
            }
            else {
                reader.BaseStream.Position -= 4;

                //test for old ver
                if (reader.ReadByte() == 0xFF) {
                    Version = reader.ReadUInt32();
                }
                else
                    reader.BaseStream.Position--;
            }

            int pCount = reader.ReadInt32();
            Places = new List<Place>(pCount);
            for (int i = 0; i < pCount; i++) {
                Place p = new Place();

                Current = this;
                p.BinaryRead(reader);
                Current = null;

                Places.Add(p);
            }
        }

        public void BinaryWrite(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write((uint)0x8721FFBA);
            writer.Write(Version);

            writer.Write(Places.Count);
            foreach (Place p in Places) {
                p.BinaryWrite(writer);
            }
        }

        public void InternalRead(SerializationStream stream) {
            Name = stream["Name"];
        }

        public void InternalWrite(SerializationStream stream) {
            stream["Name"] = Name;
        }
    }

    public class Place : ISerializedObject {
        public string Name { get; set; }
        public ulong CID { get; set; }
        public List<Category> Categories { get; set; }

        public string ViewIdentifier => $"PLACE_{Name}";
        public uint InspectorEditor => 0x00; // 0000 0000
        public Type[] Enumeration => null;

        public Place() {
            Categories = new List<Category>();
        }

        public void BinaryRead(BinaryReader reader) {
            Name = reader.ReadString();

            if (Context.Current.Version >= 9) {
                CID = reader.ReadUInt64();
            }

            int cCount = reader.ReadInt32();
            Categories = new List<Category>(cCount);
            for (int i = 0; i < cCount; i++) {
                Category c = new Category();
                c.BinaryRead(reader);
                Categories.Add(c);
            }
        }

        public void BinaryWrite(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write(CID);

            writer.Write(Categories.Count);
            foreach (Category c in Categories) {
                c.BinaryWrite(writer);
            }
        }

        public void InternalRead(SerializationStream stream) {
            Name = stream["Name"];

            ulong _cid;
            ulong.TryParse(stream["CID"], out _cid);
            CID = _cid;
        }

        public void InternalWrite(SerializationStream stream) {
            stream["Name"] = Name;
            stream["CID"] = CID.ToString();

            if (Categories.Count > 0) {
                string cats = "";
                Categories.ForEach(x => cats += $"{x.Type},");
                cats = cats.Remove(cats.Length - 1);
                stream["Categories"] = cats;
            }
        }
    }

    public enum CategoryType {
        None,
        PlaceTags,
        PlacePricing
    }

    public interface ICategoryChild : ISerializedObject {
        public CategoryType CategoryType { get; }
    }

    public class Category : ISerializedObject {
        public CategoryType Type { get; set; }
        public List<ICategoryChild> Children { get; set; }

        public string ViewIdentifier => $"CATEGORY_{Type}";
        public uint InspectorEditor => 0x01; // 0000 0001
        public Type[] Enumeration => new Type[] { typeof(CategoryType) };

        public Category() {
            Children = new List<ICategoryChild>();
        }

        public void BinaryRead(BinaryReader reader) {
            Type = (CategoryType)reader.ReadUInt16();

            int cCount = reader.ReadInt32();
            Children = new List<ICategoryChild>(cCount);
            for (int i = 0; i < cCount; i++) {
                CategoryType catType = (CategoryType)reader.ReadUInt16();

                ICategoryChild catChild = null;
                switch (catType) {

                    case CategoryType.PlaceTags:
                        catChild = new PlaceTag();
                        break;

                    case CategoryType.PlacePricing:
                        catChild = new PlacePricing();
                        break;

                }

                if (catChild != null) {
                    catChild.BinaryRead(reader);
                    Children.Add(catChild);
                }
            }
        }

        public void BinaryWrite(BinaryWriter writer) {
            writer.Write((ushort)Type);

            writer.Write(Children.Count);
            foreach (ICategoryChild child in Children) {
                writer.Write((ushort)child.CategoryType);
                child.BinaryWrite(writer);
            }
        }

        public void InternalRead(SerializationStream stream) {
            Type = Enum.Parse<CategoryType>(stream["Type"]);
        }

        public void InternalWrite(SerializationStream stream) {
            stream["Type"] = Type.ToString();
        }
    }

    public enum PlaceTagType {
        None,
        FastFood,
        Custom
    }

    public class PlaceTag : ICategoryChild {
        public PlaceTagType Type { get; set; }
        public string CustomType { get; set; }

        public string ViewIdentifier => $"TAG_{Type}" + (Type == PlaceTagType.Custom ? $"_({CustomType})" : "");
        public uint InspectorEditor => 0x01; // 0000 0001
        public Type[] Enumeration => new Type[] { typeof(PlaceTagType) };
        public CategoryType CategoryType => CategoryType.PlaceTags;

        public PlaceTag() {
            CustomType = "";
        }

        public void BinaryRead(BinaryReader reader) {
            Type = (PlaceTagType)reader.ReadUInt16();
            CustomType = reader.ReadString();
        }

        public void BinaryWrite(BinaryWriter writer) {
            writer.Write((ushort)Type);
            writer.Write(CustomType);
        }

        public void InternalRead(SerializationStream stream) {
            Type = Enum.Parse<PlaceTagType>(stream["Type"]);
            CustomType = stream["Custom Type"];
        }

        public void InternalWrite(SerializationStream stream) {
            stream["Type"] = Type.ToString();
            stream["Custom Type"] = CustomType;
        }
    }

    public enum PlacePricingType {
        None,
        GeneralMinimum,
        GeneralMaximum,
        Custom
    }

    public enum PlacePeopleCount {
        _1, _2, _3, _4, _5, _6, _7, _8, _9, _10
    }

    public class PlacePricing : ICategoryChild {
        public PlacePricingType Type { get; set; }
        public string CustomType { get; set; }
        public float Value { get; set; }
        public PlacePeopleCount PeopleCount { get; set; }

        public string ViewIdentifier => $"PRICING_{Type}" + (Type == PlacePricingType.Custom ? $"_({CustomType})" : "")
            + $"_({Value})_({(int)PeopleCount + 1})";
        public uint InspectorEditor => 0x09; // 0000 1001
        public Type[] Enumeration => new Type[] { typeof(PlacePricingType), typeof(PlacePeopleCount) };
        public CategoryType CategoryType => CategoryType.PlacePricing;

        public PlacePricing() {
            CustomType = "";
        }

        public void BinaryRead(BinaryReader reader) {
            Type = (PlacePricingType)reader.ReadUInt16();
            CustomType = reader.ReadString();
            Value = reader.ReadSingle();
            PeopleCount = (PlacePeopleCount)reader.ReadByte();
        }

        public void BinaryWrite(BinaryWriter writer) {
            writer.Write((ushort)Type);
            writer.Write(CustomType);
            writer.Write(Value);
            writer.Write((byte)PeopleCount);
        }

        public void InternalRead(SerializationStream stream) {
            Type = Enum.Parse<PlacePricingType>(stream["Type"]);
            CustomType = stream["Custom Type"];

            float _val;
            float.TryParse(stream["Value"], out _val);
            Value = _val;

            PeopleCount = Enum.Parse<PlacePeopleCount>(stream["People Count"]);
        }

        public void InternalWrite(SerializationStream stream) {
            stream["Type"] = Type.ToString();
            stream["Custom Type"] = CustomType;
            stream["Value"] = Value.ToString();
            stream["People Count"] = PeopleCount.ToString();
        }
    }
}
