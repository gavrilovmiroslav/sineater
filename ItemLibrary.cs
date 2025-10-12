namespace SINEATER;

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    public static readonly Weapon Gladius = new Weapon("Gladius", 3, EWeightClass.Medium, 3, (0, 0));
    public static readonly Weapon Mace = new Weapon("Mace", 4, EWeightClass.Heavy, 3, (1, 0));
    public static readonly Weapon Dagger = new Weapon("Dagger", 2, EWeightClass.Light, 5, (2, 0));
    public static readonly Weapon Scepter = new Weapon("Scepter", 4, EWeightClass.Heavy, 3, (4, 0));
    public static readonly Weapon WizardStaff = new Weapon("Wizard Staff", 2, EWeightClass.Heavy, 1, (5, 0));
    public static readonly Weapon Misericorde = new Weapon("Needle Sword", 4, EWeightClass.Large, 5, (3, 2));
    public static readonly Weapon SkolemStaff = new Weapon("Skolem Staff", 4, EWeightClass.Large, 3, (4, 2));
    public static readonly Weapon HeavyFlail = new Weapon("Heavy Flail", 3, EWeightClass.Heavy, 3, (5, 2));
    public static readonly Weapon Claymore = new Weapon("Claymore", 4, EWeightClass.Large, 4, (1, 3));
    public static readonly Weapon Cutlass = new Weapon("Cutlass", 3, EWeightClass.Medium, 1, (2, 3));
    public static readonly Weapon ScrollTome = new Weapon("Scroll Tome", 1, EWeightClass.Heavy, 7, (2, 4));
    public static readonly Weapon ThornWhip = new Weapon("Thorn Whip", 4, EWeightClass.Heavy, 5, (4, 3));
    
    public static readonly Armor Cloak = new Armor("Cloak", 1, EWeightClass.Light, 1, (1, 4));
    public static readonly Armor Chainmail = new Armor("Chainmail", 3, EWeightClass.Medium, 1, (1, 1));
    public static readonly Armor Tunic = new Armor("Tunic", 2, EWeightClass.Medium, 5, (0, 1));
    public static readonly Armor PlateArmor = new Armor("Plate Armor", 4, EWeightClass.Heavy, 5, (4, 1));
    public static readonly Armor LeatherArmor = new Armor("Leather Armor", 3, EWeightClass.Medium, 4, (2, 1));
    public static readonly Armor BreastPlate = new Armor("Breast Plate", 3, EWeightClass.Medium, 3, (3, 1));
    public static readonly Armor Robe = new Armor("Robe", 2, EWeightClass.Light, 4, (5, 1));
    
    public static readonly Shield RoundShield = new Shield("Round Shield", 2, EWeightClass.Medium, 4, (5, 3));
    
    public static readonly Item BrokenSword = new Item("Sword of Old", (3, 0));
    public static readonly Item FamilyRing = new Item("Family Ring", (4, 4));
    public static readonly Item AncientScroll = new Item("Ancient Scroll", (2, 2));
}