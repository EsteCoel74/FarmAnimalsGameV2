using System;

namespace FarmAnimalsGameV2.Services
{
    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    public sealed class GameDifficultySelectedEventArgs : EventArgs
    {
        public GameDifficultySelectedEventArgs(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
        }

        public GameDifficulty Difficulty { get; }
    }
}
