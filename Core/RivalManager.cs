using BurnoutCity.Entities;
using BurnoutCity.Data;

namespace BurnoutCity.Core
{
    // Utilitários estáticos para consultar o estado de progressão dos rivais.
    // Não guarda estado — toda a informação vive em PlayerData.DefeatedRivals.
    public static class RivalManager
    {
        /// <summary>
        /// Devolve o rival atual — o primeiro da lista ainda não derrotado.
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
        /// O jogador tem nível suficiente para o rival atual?
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