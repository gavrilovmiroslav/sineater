using Microsoft.Xna.Framework.Content;
using SINEATER.Serialization;
using System;
using System.Collections.Generic;

namespace SINEATER;
public class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string name) : base(String.Format("Item with name {0} not found in library", name)) { }
}

public class Library
{
    public List<Weapon> Weapons { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<Armor> Armors { get; set; } = new();
    public List<Shield> Shields { get; set; } = new();

    public void Init()
    {
        //Weapons.Add(ItemLibrary.Dagger);
        //Weapons.Add(ItemLibrary.WizardStaff);
        //Weapons.Add(ItemLibrary.Fang);
        //Weapons.Add(ItemLibrary.Misericorde);
        //Weapons.Add(ItemLibrary.SkolemStaff);
        //Weapons.Add(ItemLibrary.Claymore);
        //Weapons.Add(ItemLibrary.ScrollTome);
        //Weapons.Add(ItemLibrary.ThornWhip);


        //Armors.Add(ItemLibrary.Cloak);
        //Armors.Add(ItemLibrary.Chainmail);
        //Armors.Add(ItemLibrary.Tunic);
        //Armors.Add(ItemLibrary.PlateArmor);
        //Armors.Add(ItemLibrary.LeatherArmor);
        //Armors.Add(ItemLibrary.BreastPlate);
        //Armors.Add(ItemLibrary.Robe);

        //Items.Add(ItemLibrary.BrokenSword);
        //Items.Add(ItemLibrary.FamilyRing);
        //Items.Add(ItemLibrary.AncientScroll);

        //Shields.Add(ItemLibrary.RoundShield);
    }
}

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    public static Library Library { get; set; } = new();
    public static void LoadItems(ContentManager content)
    {
        Library = DataSerializer.Load<Library>(content.Load<string>("items/items"));
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



    //public static Weapon Dagger = new Weapon("Dagger", [
    //    new WeaponAttack("Stab", 2, 4, 2, [ new TraitSneaky() ], [ new SkirmishStep_AttackFront(1) ])], 
    //    EWeightClass.Light, 5, (2, 5), 
    //    vigScaling: EScalingFactor.D, wilScaling: EScalingFactor.C);
    //public static readonly Weapon WizardStaff = new Weapon("Wizard Staff",
    //    [
    //    new WeaponAttack("Hit", 4, 6, 1, [ new TraitProficient() ], [ 
    //        new SkirmishStep_Forwards(1),
    //        new SkirmishStep_SidestepLeft(1),
    //        new SkirmishStep_Forwards(1),
    //        new SkirmishStep_SidestepRight(1),
    //        new SkirmishStep_Forwards(1),
    //        new SkirmishStep_AttackFront(1), 
    //    ])], EWeightClass.Heavy, 1, (5, 5), claScaling: EScalingFactor.A);

//public static readonly Weapon Fang = new Weapon("Fang", [
//    new WeaponAttack("Bite", 2, 6, 1, [], [ new SkirmishStep_AttackFront(1) ])
//], EWeightClass.Tiny, 1, (6, 5));

//public static readonly Weapon Misericorde = new Weapon("Misericorde", [
//    new WeaponAttack("Stab", 4, 6, 1, [], [ 
//        new SkirmishStep_AttackFront(1),
//        new SkirmishStep_Backwards(1),
//    ])
//], EWeightClass.Light, 5, (3, 7));

//public static readonly Weapon SkolemStaff = new Weapon("Skolem Staff", [
//    new WeaponAttack("Swab", 4, 6, 3, [ new TraitBalanced() ], [
//        new SkirmishStep_AttackLeft(),
//        new SkirmishStep_AttackFront(1),
//        new SkirmishStep_AttackRight(),
//    ])
//], EWeightClass.Large, 3, (4, 7), 
//    poiScaling: EScalingFactor.A, vigScaling: EScalingFactor.C, claScaling: EScalingFactor.D);

//public static readonly Weapon Claymore = new Weapon("Claymore", [
//    new WeaponAttack("Gridlock", 4, 6, 2, [], [
//        new SkirmishStep_Forwards(2),
//        new SkirmishStep_AttackFront(1),
//    ])
//], EWeightClass.Heavy, 4, (1, 8), vigScaling: EScalingFactor.C, poiScaling: EScalingFactor.C);

//public static readonly Weapon ScrollTome = new Weapon("Scroll Tome", [
//    new WeaponAttack("Bam", 1, 6, 1, [], [ new SkirmishStep_AttackFront(1) ])
//], EWeightClass.Heavy, 7, (2, 9), claScaling: EScalingFactor.S, poiScaling: EScalingFactor.B);

//public static readonly Weapon ThornWhip = new Weapon("Thorn Whip", [
//    new WeaponAttack("Whapoosh", 1, 6, 1, [ new TraitForceful() ], [ 
//        new SkirmishStep_AttackFront(1),
//        new SkirmishStep_AttackFront(2),
//        new SkirmishStep_AttackFront(3),
//        new SkirmishStep_AttackFront(4),
//    ])
//], EWeightClass.Heavy, 5, (4, 8), vigScaling: EScalingFactor.D, poiScaling: EScalingFactor.C);

//public static readonly Armor Cloak = new Armor("Cloak", 1, EWeightClass.Light, 1, (1, 9));
//public static readonly Armor Chainmail = new Armor("Chainmail", 3, EWeightClass.Medium, 1, (0, 6));
//public static readonly Armor Tunic = new Armor("Tunic", 2, EWeightClass.Medium, 5, (1, 6));
//public static readonly Armor PlateArmor = new Armor("Plate Armor", 4, EWeightClass.Heavy, 5, (4, 6));
//public static readonly Armor LeatherArmor = new Armor("Leather Armor", 3, EWeightClass.Medium, 4, (2, 6));
//public static readonly Armor BreastPlate = new Armor("Breast Plate", 3, EWeightClass.Medium, 3, (3, 6));
//public static readonly Armor Robe = new Armor("Robe", 2, EWeightClass.Light, 4, (5, 6));

//public static readonly Shield RoundShield = new Shield("Round Shield", [
//    new WeaponAttack("Bash", 1, 4, 2, [new TraitKnockback()], 
//    [
//        new SkirmishStep_AttackFront(1)
//    ])
//], 2, EWeightClass.Medium, 4, (5, 8), poiScaling: EScalingFactor.B);

//public static readonly Item BrokenSword = new Item("Sword of Old", (3, 5));
//public static readonly Item FamilyRing = new Item("Family Ring", (4, 9));
//public static readonly Item AncientScroll = new Item("Ancient Scroll", (2, 7));