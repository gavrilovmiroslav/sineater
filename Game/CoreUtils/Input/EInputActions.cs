namespace SINEATER.Game.CoreUtils.Input;

public enum EInputAction
{
    None,
    DebugStartCombat, 
    
    Exit,
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,

    Confirm,

    SubmenuUp,
    SubmenuDown,
    SubmenuConfirm,

    #region Sound
    VolumeDown,
    VolumeUp,
    Mute,
    #endregion

    #region Debug
    Debug,
    LoadItems,
    RestartExploration,
    ExplorationDebug,
    ShowImGui,
    #endregion

    OpenInventory,
    OpenInventoryOutfit,

    MoveMapUp,
    MoveMapDown,
    MoveMapLeft,
    MoveMapRight,
    Regenerate,
    ShowMap,

    ExitInspect,
    Ability,
    ActionsMenu,
    EndTurn,
    SelectNextCharacter,
    SelectPreviousCharacter,

    DetailedView,

    StartFight,
    CancelFight,
    SwapLeft,
    SwapRight,
    Equip,
    ChangePage,
    ShowHelp,
    Save,
    
    Combat1,
    Combat2,
    Combat3,
    Combat4,
}