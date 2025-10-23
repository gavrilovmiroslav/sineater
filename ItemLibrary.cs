namespace SINEATER;

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    public static readonly Weapon Gladius = new Weapon("Gladius", 3, EWeightClass.Medium, 3, (0, 5), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon Mace = new Weapon("Mace", 4, EWeightClass.Heavy, 3, (1, 5), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon Dagger = new Weapon("Dagger", 2, EWeightClass.Light, 5, (2, 5), 
        [ new TraitSneaky() ],
        [ new SkirmishStep_AttackFront(1) ], critOn: 4, 
        vigScaling: EScalingFactor.D, wilScaling: EScalingFactor.C);
    public static readonly Weapon Scepter = new Weapon("Scepter", 4, EWeightClass.Heavy, 3, (3, 5), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon WizardStaff = new Weapon("Wizard Staff", 2, EWeightClass.Heavy, 1, (5, 5), [ new TraitProficient() ], 
        [ 
            new SkirmishStep_Forwards(1),
            new SkirmishStep_SidestepLeft(1),
            new SkirmishStep_Forwards(1),
            new SkirmishStep_SidestepRight(1),
            new SkirmishStep_Forwards(1),
            new SkirmishStep_AttackFront(1), 
        ], claScaling: EScalingFactor.A);
    public static readonly Weapon Fang = new Weapon("Fang", 1, EWeightClass.Tiny, 1, (6, 5), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon Misericorde = new Weapon("Misericorde", 4, EWeightClass.Light, 5, (3, 7), null, [ 
        new SkirmishStep_AttackFront(1),
        new SkirmishStep_Backwards(1),
    ]);
    public static readonly Weapon SkolemStaff = new Weapon("Skolem Staff", 2, EWeightClass.Large, 3, (4, 7), [ new TraitBalanced() ], [
        new SkirmishStep_AttackLeft(),
        new SkirmishStep_AttackFront(1),
        new SkirmishStep_AttackRight(),
    ], openingsPerCrit: 3, poiScaling: EScalingFactor.A, vigScaling: EScalingFactor.C, claScaling: EScalingFactor.D);
    public static readonly Weapon HeavyFlail = new Weapon("Heavy Flail", 3, EWeightClass.Heavy, 3, (5, 7), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon Claymore = new Weapon("Claymore", 4, EWeightClass.Heavy, 4, (1, 8), null, [
        new SkirmishStep_Forwards(2),
        new SkirmishStep_AttackFront(1),
    ], openingsPerCrit: 2, vigScaling: EScalingFactor.C, poiScaling: EScalingFactor.C);
    public static readonly Weapon Cutlass = new Weapon("Cutlass", 3, EWeightClass.Medium, 1, (2, 8), null, [ new SkirmishStep_AttackFront(1) ]);
    public static readonly Weapon ScrollTome = new Weapon("Scroll Tome", 1, EWeightClass.Heavy, 7, (2, 9), null, [ new SkirmishStep_AttackFront(1) ], claScaling: EScalingFactor.S, poiScaling: EScalingFactor.B);
    public static readonly Weapon ThornWhip = new Weapon("Thorn Whip", 1, EWeightClass.Heavy, 5, (4, 8), [ new TraitForceful() ], [ 
        new SkirmishStep_AttackFront(1),
        new SkirmishStep_AttackFront(2),
        new SkirmishStep_AttackFront(3),
        new SkirmishStep_AttackFront(4),
    ], vigScaling: EScalingFactor.D, poiScaling: EScalingFactor.C);
    
    public static readonly Armor Cloak = new Armor("Cloak", 1, EWeightClass.Light, 1, (1, 9));
    public static readonly Armor Chainmail = new Armor("Chainmail", 3, EWeightClass.Medium, 1, (0, 6));
    public static readonly Armor Tunic = new Armor("Tunic", 2, EWeightClass.Medium, 5, (1, 6));
    public static readonly Armor PlateArmor = new Armor("Plate Armor", 4, EWeightClass.Heavy, 5, (4, 6));
    public static readonly Armor LeatherArmor = new Armor("Leather Armor", 3, EWeightClass.Medium, 4, (2, 6));
    public static readonly Armor BreastPlate = new Armor("Breast Plate", 3, EWeightClass.Medium, 3, (3, 6));
    public static readonly Armor Robe = new Armor("Robe", 2, EWeightClass.Light, 4, (5, 6));
    
    public static readonly Shield RoundShield = new Shield("Round Shield", 1, 2, EWeightClass.Medium, 4, (5, 8), 
        [new TraitKnockback()], 
        [
            new SkirmishStep_AttackFront(1)
        ], critOn: 4, openingsPerCrit: 1, poiScaling: EScalingFactor.B);
    
    public static readonly Item BrokenSword = new Item("Sword of Old", (3, 5));
    public static readonly Item FamilyRing = new Item("Family Ring", (4, 9));
    public static readonly Item AncientScroll = new Item("Ancient Scroll", (2, 7));
}