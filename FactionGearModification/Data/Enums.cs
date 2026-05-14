using System;

namespace FactionGearCustomizer
{
    public enum GearCategory
    {
        Weapons,
        MeleeWeapons,
        Armors,
        Apparel,
        Others
    }

    public enum EditorMode 
    { 
        Simple, 
        Advanced 
    }

    public enum AdvancedTab
    {
        General,
        Apparel,
        Weapons,
        Hediffs,
        Items,
        Groups,
        Skills,
        Traits,
        Genes,
        Appearance
    }

    public enum PawnGroupType
    {
        Raid,
        Defense,
        Trading,
        Visitor,
        Aid,
        Attack
    }
}