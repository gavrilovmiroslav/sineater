namespace SINEATER.Game.CoreUtils.Input;

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
    Debug,
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

    StartFight,
    CancelFight,
    SwapLeft,
    SwapRight,
    Equip,
    ChangePage,

    Save,
}