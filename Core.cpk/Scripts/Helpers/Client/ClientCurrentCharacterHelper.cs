﻿namespace AtomicTorch.CBND.CoreMod.Helpers.Client
{
    using AtomicTorch.CBND.CoreMod.Characters;
    using AtomicTorch.CBND.CoreMod.Characters.Player;
    using AtomicTorch.CBND.GameApi.Data.Characters;

    public static class ClientCurrentCharacterHelper
    {
        public static ICharacter Character { get; private set; }

        public static PlayerCharacterPrivateState PrivateState { get; private set; }

        public static PlayerCharacterPublicState PublicState { get; private set; }

        public static void Init(ICharacter character)
        {
            Character = character;
            PrivateState = PlayerCharacter.GetPrivateState(character);
            PublicState = PlayerCharacter.GetPublicState(character);

            ClientCurrentCharacterFinalStatsHelper.Init(character);
        }
    }
}