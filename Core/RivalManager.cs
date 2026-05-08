using BurnoutCity.Entities;
using BurnoutCity.Data;

namespace BurnoutCity.Core
{
    public static class RivalManager
    {
        /// <summary>
        /// Devolve o rival atual � o primeiro da lista ainda n�o derrotado.
        /// Retorna null se todos foram derrotados.
        /// </summary>
        public static Rival? GetCurrentRival(PlayerData playerData)
        {
            foreach (var rival in RivalData.All)
            {
                if (!playerData.DefeatedRivals.Contains(rival.Id))
                    return rival;
            }
            return null; // Todos derrotados
        }

        /// <summary>
        /// O jogador tem n�vel suficiente para o rival atual?
        /// </summary>
        public static bool CanChallenge(PlayerData playerData)
        {
            var rival = GetCurrentRival(playerData);
            if (rival == null) return false;
            return playerData.Level >= rival.MinLevel;
        }

        /// <summary>
        /// Todos os rivais foram derrotados?
        /// </summary>
        public static bool AllDefeated(PlayerData playerData)
            => playerData.DefeatedRivals.Count >= RivalData.All.Count;
    }
}