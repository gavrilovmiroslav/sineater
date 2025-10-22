using Microsoft.Xna.Framework.Content;
using SINEATER.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.DataContracts;

namespace SINEATER;

public class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string name) : base(String.Format("Item with name {0} not found in library", name)) { }
}

[DataContract]
public class Library
{
    [DataMember]
    public List<Weapon> Weapons { get; set; } = new();
    [DataMember]
    public List<Item> Items { get; set; } = new();
    [DataMember]
    public List<Armor> Armors { get; set; } = new();
    [DataMember]
    public List<Shield> Shields { get; set; } = new();
}

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    public static Library Library { get; set; } = new();
    public static void LoadItems(ContentManager content)
    {
        Library =  DataSerializer.Load<Library>(content.Load<string>("items/items"));
    }

    public static Weapon? GetWeapon(string name)
    {
        var result = Library.Weapons.Find(x => x.Name == name);
        if (result == null)
        {
            throw new ItemNotFoundException(name);
        }
        return result;
    }

    public static Armor? GetArmor(string name)
    {
        var result = Library.Armors.Find(x => x.Name == name);
        if (result == null)
        {
            throw new ItemNotFoundException(name);
        }
        return result;
    }

    public static Shield? GetShield(string name)
    {
        var result = Library.Shields.Find(x => x.Name == name);
        if (result == null)
        {
            throw new ItemNotFoundException(name);
        }
        return result;
    }

    public static Item? GetItem(string name)
    {
        var result = Library.Items.Find(x => x.Name == name);
        if (result == null)
        {
            throw new ItemNotFoundException(name);
        }
        return result;
    }
}