namespace SINEATER.Input;

public enum EInputAction
{
    None,
    Debug,
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
    RestartExploration,
    ExplorationDebug,
    ShowImGui,
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