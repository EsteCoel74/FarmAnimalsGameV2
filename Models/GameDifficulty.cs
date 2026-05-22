using System;

namespace FarmAnimalsGameV2.Services
{
    /// <summary>
    /// Définit les niveaux de difficulté du jeu.
    /// </summary>
    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Fournit l'argument de sélection de difficulté.
    /// </summary>
    public sealed class GameDifficultySelectedEventArgs : EventArgs
    {
        /// <summary>
        /// Initialise l'événement avec la difficulté choisie.
        /// </summary>
        public GameDifficultySelectedEventArgs(GameDifficulty difficulty)
        {
            Difficulty = difficulty;
        }

        /// <summary>
        /// Difficulté sélectionnée.
        /// </summary>
        public GameDifficulty Difficulty { get; }
    }
}
