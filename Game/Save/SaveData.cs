using SINEATER.Game.CoreUtils;
using SINEATER.Game.Gameplay;
using System;
using System.Collections.Generic;
using System.IO;

namespace SINEATER.Game.Save
{
    public class SaveData
    {
        public class CharacterData
        {
            public ECharacterClass Class;
            public List<string?> Inventory = new();

            public CharacterData(PartyMember character)
            {
                if (character == null)
                    return;

                Class = character.Job;

                foreach (var item in character.Items)
                {
                    Inventory.Add(item != null ? item.Name : null);
                }
            }
        }

        public List<string> Inventory = new();
        public List<CharacterData> characterDatas = new();
        public int PlayerPositionX;
        public int PlayerPositionY;

        public void Save()
        {
            var inventory = SineaterGame.Instance.Party.Inventory;
            foreach (var item in inventory.Items)
            {
                Inventory.Add(item.Name);
            }

            foreach (var character in SineaterGame.Instance.Party.Characters)
            {
                characterDatas.Add(new CharacterData(character));
            }

            PlayerPositionX = SineaterGame.Instance.Party.CurrentPlayerPosition.X;
            PlayerPositionY = SineaterGame.Instance.Party.CurrentPlayerPosition.Y;

        }
    }
}