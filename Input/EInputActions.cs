namespace SINEATER.Input;

public enum EInputAction
{
    None,
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
    LoadItems,
    ExplorationMapScreen,
    ExplorationDebug,
    #endregion

    ChacterSheetEnter,
    ChacterSheetCycle,
    ChacterSheetExit,

    OpenInventory,
    OpenInventoryOutfit,

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
}